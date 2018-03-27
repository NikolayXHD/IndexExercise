using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using IndexExercise.Index.Collections;

namespace IndexExercise.Index.FileSystem
{
	/// <summary>
	/// Reflects directory structure of observed files and directories.
	/// 
	/// Synchronizes <see cref="FileSystem.Mirror"/> with watched files and directories in a 
	/// background loop to make handling <see cref="Watcher"/> events fast and independent from file 
	/// system.
	/// </summary>
	public class Mirror : BackgroundLoopOwner
	{
		/// <summary>
		/// Reflects directory structure of observed files and directories
		/// </summary>
		/// <param name="watcher">Raises events on file system changes</param>
		/// <param name="sequentialId">Used to generate identifiers to track file content</param>
		/// <param name="systemFileNameFilter">A deterministic function to limit the observed scope</param>
		public Mirror(
			Watcher watcher,
			SequentialId sequentialId,
			FileNameFilter systemFileNameFilter = null)
		{
			_watcher = watcher;
			_watcher.ChangeDetected += changeDetected;
			_watcher.Error += watcherError;

			_fileNameFilter = systemFileNameFilter ?? (fullFileName => true);
			_sequentialId = sequentialId;

			_root = new RootEntry<Metadata>(() => new Metadata(SequentialId.None));

			_createdEntriesQueue.Enqueued += (sender, entry) => EnqueuedCreatedEntry?.Invoke(this, entry);
			_deletedEntriesQueue.Enqueued += (sender, entry) => EnqueuedDeletedEntry?.Invoke(this, entry);
			_movedEntriesQueue.Enqueued += (sender, entry) => EnqueuedMovedEntry?.Invoke(this, entry);
		}

		protected override async Task BackgroundLoopIteration()
		{
			var change = _changeQueue.TryDequeue();
			if (change != null)
			{
				processChange(change);
				return;
			}

			var createdEntry = _createdEntriesQueue.TryDequeue();
			if (createdEntry != null)
			{
				processCreatedEntry(createdEntry);
				return;
			}

			var deletedEntry = _deletedEntriesQueue.TryDequeue();
			if (deletedEntry != null)
			{
				processDeletedEntry(deletedEntry);
				return;
			}

			var movedEntry = _movedEntriesQueue.TryDequeue();
			if (movedEntry != null)
			{
				processMovedEntry(movedEntry);
				return;
			}

			inspectWatchTargets();

			await IdleDelayTask();
		}

		public override Task RunAsync()
		{
			// before base.Start() to make sure the watcher is allways enabled
			// when BackgroundLoopIteration is executed
			_watcher.Enabled = true;
			return base.RunAsync();
		}

		public override void Dispose()
		{
			_watcher.Enabled = false;
			base.Dispose();
		}

		public void Watch(WatchTarget target)
		{
			lock (_syncWatcher)
			{
				_watchTargetStates[target] = WatchTargetState.Suspended;

				var watchedLocation = _watchedLocations.GetEntry(target.Path);

				if (watchedLocation == null)
				{
					_watchedLocations.Add(
						EntryType.Directory,
						target.Path,
						new HashSet<WatchTarget> { target });
				}
				else
				{
					watchedLocation.Data.Add(target);
				}
			}
		}

		public Entry<Metadata> Find(string path) => _root.GetEntry(path);



		private void changeDetected(object sender, Change change)
		{
			_changeQueue.TryEnqueue(change);
		}

		private void processChange(Change change)
		{
			if (change.FileSystemWatcher?.EnableRaisingEvents == false)
				return;

			if (change.EntryType == EntryType.Uncertain && change.ChangeType != WatcherChangeTypes.Deleted)
			{
				var entryType = getEntryType(change.Path);

				if (entryType.HasValue)
					change.EntryType = entryType.Value;
			}

			ProcessingChange?.Invoke(this, change);

			bool isWatched = this.isWatched(change.EntryType, change.Path);

			switch (change.ChangeType)
			{
				case WatcherChangeTypes.Created:
				case WatcherChangeTypes.Changed:
					if (isWatched)
						processChangedOrCreatedEntry(change.EntryType, change.Path);
					break;

				case WatcherChangeTypes.Deleted:
					if (isWatched)
						processDeletedEntry(change.Path);
					break;

				case WatcherChangeTypes.Renamed:

					bool wasWatched = this.isWatched(change.EntryType, change.OldPath);
					if (isWatched && wasWatched)
						processRenamedEntry(change.EntryType, change.Path, change.OldPath);
					else if (isWatched)
						processChangedOrCreatedEntry(change.EntryType, change.Path);
					else if (wasWatched)
						processDeletedEntry(change.Path);
					break;

				default:
					throw new NotSupportedException($"{nameof(change.ChangeType)} {change.ChangeType} is not supported");
			}
		}



		private void processCreatedEntry(Entry<Metadata> entry)
		{
			ProcessingCreatedEntry?.Invoke(this, entry);

			switch (entry)
			{
				case RootEntry<Metadata> _:
					throw new InvalidOperationException($"processing {entry.GetType()} entry is not supported");

				case FileEntry<Metadata> file:
					FileCreated?.Invoke(this, file);
					break;

				case DirectoryEntry<Metadata> directory:
					scanDirectory(directory);
					break;

				case UnclassifiedEntry<Metadata> unclassified:
					classifyEntry(unclassified);
					break;

				default:
					throw new InvalidOperationException($"processing {entry.GetType()} entry is not supported");
			}
		}

		private void processDeletedEntry(Entry<Metadata> entry)
		{
			ProcessingDeletedEntry?.Invoke(this, entry);

			switch (entry)
			{
				case RootEntry<Metadata> _:
					throw new InvalidOperationException($"processing {entry.GetType()} entry is not supported");

				case FileEntry<Metadata> file:
					FileDeleted?.Invoke(this, file);
					break;

				case DirectoryEntry<Metadata> directory:
					processDeletedDirectoryRecursively(directory);
					break;

				case UnclassifiedEntry<Metadata> _:
					break;

				default:
					throw new InvalidOperationException($"processing {entry.GetType()} entry is not supported");
			}
		}

		private void processMovedEntry(Entry<Metadata> entry)
		{
			ProcessingMovedEntry?.Invoke(this, entry);

			switch (entry)
			{
				case RootEntry<Metadata> _:
					throw new InvalidOperationException($"processing {entry.GetType()} entry is not supported");

				case FileEntry<Metadata> file:
					FileMoved?.Invoke(this, file);
					break;

				case DirectoryEntry<Metadata> directory:
					processMovedDirectory(directory);
					break;

				case UnclassifiedEntry<Metadata> _:
					break;

				default:
					throw new InvalidOperationException($"processing {entry.GetType()} entry is not supported");
			}
		}

		private void processMovedDirectory(DirectoryEntry<Metadata> directoryEntry)
		{
			processMovedDirectoryRecursively(directoryEntry);

			// If the moved directory was scanned moments ago we cannot assume we scanned the right 
			// one. Maybe we found another directory in the previous location of this one.
			bool needToRescan = directoryEntry.Data.ElapsedSinceScanFinished < AllowablePathSynchronizationLag;

			if (needToRescan)
				scanDirectory(directoryEntry);
		}


		private void inspectWatchTargets()
		{
			KeyValuePair<WatchTarget, WatchTargetState>[] statesByTarget;

			lock (_syncWatcher)
				statesByTarget = _watchTargetStates.ToArray();

			foreach (var pair in statesByTarget)
			{
				lock (_syncWatcher)
				{
					inspectWatchTarget(target: pair.Key, storedState: pair.Value);
				}
			}
		}

		private void inspectWatchTarget(WatchTarget target, WatchTargetState storedState)
		{
			var actualState = getActualTargetState(target);

			if (actualState == storedState)
				return;

			switch (actualState)
			{
				case WatchTargetState.Active:
					activateTarget(target);
					break;

				case WatchTargetState.Suspended:
					suspendRedundantTarget(target);
					break;

				case WatchTargetState.Failed:
					suspendFailedTarget(target);
					break;

				default:
					throw new NotSupportedException($"{nameof(WatchTargetState)} {actualState} is not supported");
			}
		}

		private WatchTargetState getActualTargetState(WatchTarget target)
		{
			if (existsActiveWatchedParent(target.Path))
				return WatchTargetState.Suspended;

			if (!_watcher.IsPathCorrect(target))
				return WatchTargetState.Failed;

			string directory = _watcher.GetWatcherDirectory(target);

			if (!Directory.Exists(directory))
				return WatchTargetState.Failed;

			return WatchTargetState.Active;
		}

		private void activateTarget(WatchTarget target)
		{
			_watchTargetStates[target] = WatchTargetState.Active;
			_watcher.Watch(target);

			string directory = _watcher.GetWatcherDirectory(target);

			// after _watcher.Watch to make sure the files or directories
			// created while executing scanDirectory are detected by Watcher
			entryFound(new Find(EntryType.Directory, directory));
		}

		private void suspendFailedTarget(WatchTarget target)
		{
			_watchTargetStates[target] = WatchTargetState.Failed;
			_watcher.Unwatch(target);

			var watchedDirectory = _watcher.GetWatcherDirectory(target);

			// We don't know what happens to the watched directory while the watcher is broken.
			// To reflect this we assume the watched directory is deleted.
			_changeQueue.TryEnqueue(new Change(EntryType.Directory, WatcherChangeTypes.Deleted, watchedDirectory));
		}

		private void suspendRedundantTarget(WatchTarget target)
		{
			_watchTargetStates[target] = WatchTargetState.Suspended;
			_watcher.Unwatch(target);
		}



		private bool isWatched(EntryType type, string path)
		{
			if (type == EntryType.File && !_fileNameFilter(path))
				return false;

			lock (_syncWatcher)
			{
				var watchedTarget = _watchedLocations.GetEntry(path);

				if (watchedTarget?.Data.Count > 0)
					return true;

				if (existsWatchedParent(path))
					return true;

				return false;
			}
		}

		private bool existsWatchedParent(string path)
		{
			return _watchedLocations.FindAncestors(path)
				.Any(entry => entry.Data.Any(_ => _.Type == EntryType.Directory));
		}

		private bool existsActiveWatchedParent(string path)
		{
			return _watchedLocations.FindAncestors(path)
				.Any(entry => entry.Data.Any(_ => _.Type == EntryType.Directory && _watchTargetStates[_] == WatchTargetState.Active));
		}



		private void classifyEntry(UnclassifiedEntry<Metadata> unclassified)
		{
			string path = unclassified.GetPath();

			var entryType = getEntryType(path);

			if (entryType.HasValue)
				entryFound(new Find(entryType.Value, path));
		}

		private void scanDirectory(DirectoryEntry<Metadata> directory)
		{
			_scannedDirectory = directory;
			_interruptScanningDirectory = false;

			var path = directory.GetPath();
			directory.Data.ResetScanningTime();

			try
			{
				scanDirectory(directory, path);
			}
			catch (DirectoryNotFoundException)
			{
				DirectoryScanNotFound?.Invoke(this, directory);
			}
			catch (SecurityException ex)
			{
				EntryAccessError?.Invoke(this, new EntryAccessError(EntryType.Directory, path, ex));
			}

			_scannedDirectory = null;
		}

		private void scanDirectory(DirectoryEntry<Metadata> directory, string path)
		{
			var directoryInfo = new DirectoryInfo(path);

			if (!directoryInfo.Exists)
			{
				DirectoryScanNotFound?.Invoke(this, directory);
				return;
			}

			directory.Data.BeginScan();
			DirectoryScanStarted?.Invoke(this, directory);

			// if other process removes the scanned directory, to this process the directory
			// remains existing and files and subdirectories enumeration continues normally

			foreach (var info in directoryInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
			{
				if (_interruptScanningDirectory)
					break;

				entryFound(new Find(EntryType.File, info.FullName));
			}

			if (_interruptScanningDirectory)
			{
				directory.Data.ResetScanningTime();
				DirectoryScanInterrupted?.Invoke(this, directory);
				return;
			}

			directoryInfo.Refresh();
			if (!directoryInfo.Exists)
			{
				directory.Data.ResetScanningTime();
				DirectoryScanNotFound?.Invoke(this, directory);
				return;
			}

			foreach (var info in directoryInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
			{
				if (_interruptScanningDirectory)
					break;

				entryFound(new Find(EntryType.Directory, info.FullName));
			}

			if (_interruptScanningDirectory)
			{
				directory.Data.ResetScanningTime();
				DirectoryScanInterrupted?.Invoke(this, directory);
				return;
			}

			directory.Data.EndScan();
			DirectoryScanFinished?.Invoke(this, directory);
		}

		private void processDeletedDirectoryRecursively(DirectoryEntry<Metadata> directory)
		{
			if (_scannedDirectory == directory)
				_interruptScanningDirectory = true;

			foreach (var file in directory.Files.Values)
				FileDeleted?.Invoke(this, file);

			foreach (var subdirectory in directory.Directories.Values)
				processDeletedDirectoryRecursively(subdirectory);
		}

		private void processMovedDirectoryRecursively(DirectoryEntry<Metadata> directory)
		{
			if (_scannedDirectory == directory)
				_interruptScanningDirectory = true;

			foreach (var file in directory.Files.Values)
				FileMoved?.Invoke(this, file);

			foreach (var subdirectory in directory.Directories.Values)
				processMovedDirectoryRecursively(subdirectory);
		}



		private void entryFound(Find find)
		{
			switch (find.Type)
			{
				case EntryType.File:
					if (!_fileNameFilter(find.Path))
						return;
					break;

				case EntryType.Directory:
					break;

				default:
					throw new NotSupportedException($"{find.Type} is not supported");
			}

			EntryFound?.Invoke(this, find);
			processFoundEntry(find.Type, find.Path);
		}

		private void processChangedOrCreatedEntry(EntryType type, string path)
		{
			var existingEntry = _root.GetEntry(path);

			if (existingEntry == null)
			{
				createEntry(type, path);
				return;
			}

			var actualType = type == EntryType.Uncertain
				? existingEntry.Type
				: type;

			if (actualType != existingEntry.Type || actualType == EntryType.File)
			{
				deleteEntry(existingEntry);
				createEntry(type, path);
			}
		}

		private void processFoundEntry(EntryType type, string path)
		{
			var existingEntry = _root.GetEntry(path);

			if (existingEntry == null)
			{
				createEntry(type, path);
				return;
			}

			if (type != existingEntry.Type)
			{
				deleteEntry(existingEntry);
				createEntry(type, path);
				return;
			}

			if (type == EntryType.Directory)
				// non_unique_created_directory_enqueue_attempts
				_createdEntriesQueue.TryEnqueue(existingEntry);
		}

		private void processDeletedEntry(string path)
		{
			var existingEntry = _root.GetEntry(path);

			if (existingEntry == null)
				return;

			deleteEntry(existingEntry);
		}

		private void processRenamedEntry(EntryType type, string path, string oldPath)
		{
			var existingEntry = _root.GetEntry(oldPath);
			if (existingEntry == null)
			{
				processChangedOrCreatedEntry(type, path);
				return;
			}

			// remove possibly existing entry at renaming target path
			processDeletedEntry(path);

			if (type != existingEntry.Type && type != EntryType.Uncertain)
			{
				processDeletedEntry(oldPath);
				processChangedOrCreatedEntry(type, path);
			}
			else
			{
				_root.Move(existingEntry, path);
				_movedEntriesQueue.TryEnqueue(existingEntry);
			}
		}

		private void createEntry(EntryType type, string path)
		{
			long contentId = type == EntryType.File
				? _sequentialId.New()
				: SequentialId.None;

			var entry = _root.Add(type, path, new Metadata(contentId));

			bool added = _createdEntriesQueue.TryEnqueue(entry);

			// non_unique_created_directory_enqueue_attempts
			if (!added && type == EntryType.File)
				throw new ArgumentException($"{nameof(Mirror)} logic assumes the {nameof(FileEntry<Metadata>)} are not processed as created twice");
		}

		private void deleteEntry(Entry<Metadata> entry)
		{
			_root.Remove(entry);

			_createdEntriesQueue.TryRemove(entry);

			// after removing from _createdEntriesQueue
			// so that the entry never coexists in both 
			// _createdEntriesQueue and _deletedEntriesQueue
			// such coexistence could lead to processing element creation after its deletion
			_deletedEntriesQueue.TryEnqueue(entry);
		}



		private void watcherError(object sender, WatcherErrorArgs e)
		{
			lock (_syncWatcher)
				suspendFailedTarget(e.Target);
		}

		private static EntryType? getEntryType(string path)
		{
			if (File.Exists(path))
				return EntryType.File;

			if (Directory.Exists(path))
				return EntryType.Directory;

			return null;
		}

		/// <summary>
		/// A maximum time interval we expect between an observed <see cref="FileEntry{TData}"/> is 
		/// moved and the return value of <see cref="Entry{TData}.GetPath"/> changes
		/// </summary>
		public TimeSpan AllowablePathSynchronizationLag
		{
			get => _allowablePathSynchronizationLag;

			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentException($"{nameof(AllowablePathSynchronizationLag)} must be a positive {nameof(TimeSpan)}");

				_allowablePathSynchronizationLag = value;
			}
		}

		private TimeSpan _allowablePathSynchronizationLag = TimeSpan.FromMilliseconds(400);


		public event EventHandler<Change> ProcessingChange;
		public event EventHandler<Entry<Metadata>> EnqueuedCreatedEntry;
		public event EventHandler<Entry<Metadata>> EnqueuedDeletedEntry;
		public event EventHandler<Entry<Metadata>> EnqueuedMovedEntry;
		public event EventHandler<Entry<Metadata>> ProcessingCreatedEntry;
		public event EventHandler<Entry<Metadata>> ProcessingDeletedEntry;
		public event EventHandler<Entry<Metadata>> ProcessingMovedEntry;
		public event EventHandler<Find> EntryFound;
		public event EventHandler<EntryAccessError> EntryAccessError;

		public event EventHandler<FileEntry<Metadata>> FileCreated;
		public event EventHandler<FileEntry<Metadata>> FileDeleted;
		public event EventHandler<FileEntry<Metadata>> FileMoved;

		public event EventHandler<DirectoryEntry<Metadata>> DirectoryScanStarted;
		public event EventHandler<DirectoryEntry<Metadata>> DirectoryScanInterrupted;
		public event EventHandler<DirectoryEntry<Metadata>> DirectoryScanFinished;
		public event EventHandler<DirectoryEntry<Metadata>> DirectoryScanNotFound;

		private DirectoryEntry<Metadata> _scannedDirectory;
		private bool _interruptScanningDirectory;

		private readonly RootEntry<Metadata> _root;
		private readonly FifoSet<Change> _changeQueue = new FifoSet<Change>();
		private readonly FifoSet<Entry<Metadata>> _createdEntriesQueue = new FifoSet<Entry<Metadata>>();
		private readonly FifoSet<Entry<Metadata>> _deletedEntriesQueue = new FifoSet<Entry<Metadata>>();
		private readonly FifoSet<Entry<Metadata>> _movedEntriesQueue = new FifoSet<Entry<Metadata>>();

		private readonly Watcher _watcher;
		private readonly RootEntry<HashSet<WatchTarget>> _watchedLocations = new RootEntry<HashSet<WatchTarget>>(() => new HashSet<WatchTarget>());
		private readonly Dictionary<WatchTarget, WatchTargetState> _watchTargetStates = new Dictionary<WatchTarget, WatchTargetState>();
		private readonly object _syncWatcher = new object();

		private readonly SequentialId _sequentialId;
		private readonly FileNameFilter _fileNameFilter;

		public delegate bool FileNameFilter(string fullFileName);
	}
}