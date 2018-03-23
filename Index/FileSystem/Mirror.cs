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
		/// <param name="systemFilesFilter">A deterministic function to limit the observed scope</param>
		public Mirror(
			Watcher watcher,
			SequentialId sequentialId,
			FilesFilter systemFilesFilter = null)
		{
			_watcher = watcher;
			_watcher.ChangeDetected += changeDetected;
			_watcher.Error += watcherError;

			_filesFilter = systemFilesFilter ?? (fullFileName => true);
			_sequentialId = sequentialId;

			_root = new RootEntry<Metadata>(() => new Metadata(_sequentialId.None));

			_createdEntriesQueue.Enqueued += (sender, entry) => EnqueuedCreatedEntry?.Invoke(this, entry);
			_deletedEntriesQueue.Enqueued += (sender, entry) => EnqueuedDeletedEntry?.Invoke(this, entry);
		}

		protected override async Task BackgroundLoopIteration()
		{
			var change = _changeQueue.TryDequeue();
			if (change != null)
			{
				processChange(change);
				return;
			}

			var createdEntry = _createdEntriesQueue.TryPeek();
			if (createdEntry != null)
			{
				processCreatedEntry(createdEntry);
				// concurrent_removal_from_created_entries_queue
				_createdEntriesQueue.TryRemove(createdEntry);
				return;
			}

			var deletedEntry = _deletedEntriesQueue.TryPeek();
			if (deletedEntry != null)
			{
				processDeletedEntry(deletedEntry);
				_deletedEntriesQueue.Remove(deletedEntry);
				return;
			}

			inspectWatchTargets();

			await IdleDelayTask();
		}

		public override Task Start()
		{
			// before base.Start() to make sure the watcher is allways enabled
			// when BackgroundLoopIteration is executed
			_watcher.Enabled = true;
			return base.Start();
		}

		public override void Dispose()
		{
			_watcher.Enabled = false;
			_watcher.Dispose();

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
					unregisterDirectoryFilesContent(directory);
					break;

				case UnclassifiedEntry<Metadata> _:
					break;

				default:
					throw new InvalidOperationException($"processing {entry.GetType()} entry is not supported");
			}
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
			if (type == EntryType.File && !_filesFilter(path))
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
			var path = directory.GetPath();
			var directoryInfo = new DirectoryInfo(path);

			if (!directoryInfo.Exists)
				return;

			try
			{
				foreach (var info in directoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
				{
					if (!isAdded(directory))
						return;

					var entryType = getEntryType(info);

					if (entryType.HasValue)
						entryFound(new Find(entryType.Value, info.FullName));
				}
			}
			catch (DirectoryNotFoundException)
			{
			}
			catch (SecurityException ex)
			{
				EntryAccessError?.Invoke(this, new EntryAccessError(EntryType.Directory, path, ex));
			}
		}

		private bool isAdded(Entry<Metadata> entry)
		{
			lock (_createdEntriesQueue.Sync)
			lock (_deletedEntriesQueue.Sync)
			{
				var branch = entry.GetSelfAndAncestors();

				bool existAdded = false;

				foreach (var e in branch)
				{
					if (_deletedEntriesQueue.Contains(e))
						return false;

					existAdded |= _createdEntriesQueue.Contains(e);
				}

				return existAdded;
			}
		}

		private void unregisterDirectoryFilesContent(DirectoryEntry<Metadata> directory)
		{
			var current = directory;

			while (true)
			{
				while (current.Directories.Count > 0)
					current = current.Directories.Values.First();

				foreach (var file in current.Files.Values)
					FileDeleted?.Invoke(this, file);

				if (current.Parent == null)
					break;

				current.Parent.Directories.Remove(current.Name);
				current = current.Parent;
			}
		}



		private void entryFound(Find find)
		{
			switch (find.Type)
			{
				case EntryType.File:
					if (!_filesFilter(find.Path))
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

			_root.Move(existingEntry, path);
		}

		private void createEntry(EntryType type, string path)
		{
			long contentId = type == EntryType.File
				? _sequentialId.New()
				: _sequentialId.None;

			var entry = _root.Add(type, path, new Metadata(contentId));

			bool added = _createdEntriesQueue.TryEnqueue(entry);

			// non_unique_created_directory_enqueue_attempts
			if (!added && type == EntryType.File)
				throw new ArgumentException($"{nameof(Mirror)} logic assumes the {nameof(FileEntry<Metadata>)} are not processed as created twice");
		}

		private void deleteEntry(Entry<Metadata> entry)
		{
			_root.Remove(entry);

			// concurrent_removal_from_created_entries_queue
			// no need to process entry creation which was further deleted
			_createdEntriesQueue.TryRemove(entry);

			// after removing from _createdEntriesQueue
			// so that the entry never coexists in both _createdEntriesQueue and _deletedEntriesQueue
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

		private static EntryType? getEntryType(FileSystemInfo info)
		{
			if (info is DirectoryInfo)
				return EntryType.Directory;

			if (info is FileInfo)
				return EntryType.File;

			return null;
		}



		public event EventHandler<Change> ProcessingChange;
		public event EventHandler<Entry<Metadata>> EnqueuedCreatedEntry;
		public event EventHandler<Entry<Metadata>> EnqueuedDeletedEntry;
		public event EventHandler<Entry<Metadata>> ProcessingCreatedEntry;
		public event EventHandler<Entry<Metadata>> ProcessingDeletedEntry;
		public event EventHandler<Find> EntryFound;
		public event EventHandler<EntryAccessError> EntryAccessError;

		public event EventHandler<FileEntry<Metadata>> FileCreated;
		public event EventHandler<FileEntry<Metadata>> FileDeleted;

		private readonly RootEntry<Metadata> _root;
		private readonly FifoSet<Change> _changeQueue = new FifoSet<Change>();
		private readonly FifoSet<Entry<Metadata>> _createdEntriesQueue = new FifoSet<Entry<Metadata>>();
		private readonly FifoSet<Entry<Metadata>> _deletedEntriesQueue = new FifoSet<Entry<Metadata>>();

		private readonly Watcher _watcher;
		private readonly RootEntry<HashSet<WatchTarget>> _watchedLocations = new RootEntry<HashSet<WatchTarget>>(() => new HashSet<WatchTarget>());
		private readonly Dictionary<WatchTarget, WatchTargetState> _watchTargetStates = new Dictionary<WatchTarget, WatchTargetState>();
		private readonly object _syncWatcher = new object();

		private readonly SequentialId _sequentialId;
		private readonly FilesFilter _filesFilter;

		public delegate bool FilesFilter(string fullFileName);
	}
}