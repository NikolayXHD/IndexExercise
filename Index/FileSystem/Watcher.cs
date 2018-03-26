using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IndexExercise.Index.FileSystem
{
	/// <summary>
	/// Detects file system changes within observed scope and raises events
	/// </summary>
	public class Watcher : IDisposable
	{
		public void Watch(WatchTarget target)
		{
			lock (_sync)
			{
				if (_watchers.ContainsKey(target))
					return;

				var watcher = createWatcher(target);

				watcher.EnableRaisingEvents = _enabled;

				_watchers.Add(target, watcher);

				if (_enabled)
					saveInitialWatchLocation(watcher);
			}
		}

		public void Unwatch(WatchTarget target)
		{
			lock (_sync)
			{
				if (!_watchers.TryGetValue(target, out var watcher))
					return;

				watcher.EnableRaisingEvents = false;
				watcher.Dispose();

				_watchers.Remove(target);
				_initialWatchPaths.Remove(watcher);
			}
		}

		private FileSystemWatcher createWatcher(WatchTarget target)
		{
			switch (target.Type)
			{
				case EntryType.File:
					return createFileWatcher(target);

				case EntryType.Directory:
					return createDirectoryWatcher(target);

				default:
					throw new NotSupportedException($"Watching {target.Type} is not supported");
			}
		}

		private FileSystemWatcher createFileWatcher(WatchTarget target)
		{
			var watcher = new FileSystemWatcher
			{
				Path = GetWatcherDirectory(target),
				Filter = Path.GetFileName(target.Path),
				IncludeSubdirectories = false
			};

			watcher.NotifyFilter &= ~NotifyFilters.DirectoryName;

			watcher.Created += fileCreated;
			watcher.Deleted += fileDeleted;
			watcher.Changed += fileChanged;
			watcher.Renamed += fileRenamed;

			watcher.Error += fileWatcherError;

			return watcher;
		}

		private FileSystemWatcher createDirectoryWatcher(WatchTarget target)
		{
			var watcher = new FileSystemWatcher
			{
				Path = GetWatcherDirectory(target),
				Filter = null,
				IncludeSubdirectories = true
			};

			watcher.Created += directoryChildCreated;
			watcher.Deleted += directoryChildDeleted;
			watcher.Changed += directoryChildChanged;
			watcher.Renamed += directoryChildRenamed;

			watcher.Error += directoryWatcherError;

			return watcher;
		}

		private void saveInitialWatchLocation(FileSystemWatcher watcher)
		{
			string initialPath = WatcherPathInspector.GetActualPath(watcher);

			if (String.IsNullOrEmpty(initialPath))
				throw new ApplicationException("Failed to retrieve actual watcher path");

			_initialWatchPaths[watcher] = initialPath;
		}

		public string GetWatcherDirectory(WatchTarget target)
		{
			switch (target.Type)
			{
				case EntryType.File:
					return Path.GetDirectoryName(target.Path);

				case EntryType.Directory:
					return target.Path;

				default:
					throw new NotSupportedException($"Watching {target.Type} is not supported");
			}
		}

		private WatchTarget getTarget(EntryType entryType, FileSystemWatcher fileSystemWatcher)
		{
			switch (entryType)
			{
				case EntryType.File:
					return new WatchTarget(entryType, Path.Combine(fileSystemWatcher.Path, fileSystemWatcher.Filter));

				case EntryType.Directory:
					return new WatchTarget(entryType, fileSystemWatcher.Path);

				default:
					throw new ArgumentException($"Watching {entryType} is not supported", nameof(entryType));
			}
		}



		/// <summary>
		/// Returns false if <see cref="Change.Path"/> and / or <see cref="Change.OldPath"/>
		/// properties have incorrect values.
		/// 
		/// The performance of this check is same as <see cref="Directory.Exists"/>
		/// </summary>
		public bool IsPathCorrect(WatchTarget target)
		{
			lock (_sync)
			{
				if (!_watchers.TryGetValue(target, out var watcher))
					return true;

				string expectedPath = _initialWatchPaths[watcher];
				string actualPath = WatcherPathInspector.GetActualPath(watcher);

				return PathString.Comparer.Equals(expectedPath, actualPath);
			}
		}

		/// <summary>
		/// Throws <see cref="ApplicationException"/> if <see cref="Change.Path"/> and / or <see cref="Change.OldPath"/>
		/// properties have incorrect values.
		/// 
		/// The performance of this check is same as <see cref="Directory.Exists"/>
		/// </summary>
		/// <exception cref="ApplicationException"></exception>
		public void ThrowIfIncorrectPath(Change change)
		{
			if (change.FileSystemWatcher == null)
				return;

			lock (_sync)
			{
				if (!_initialWatchPaths.TryGetValue(change.FileSystemWatcher, out var expectedPath))
					return;
			
				string actualPath = WatcherPathInspector.GetActualPath(change.FileSystemWatcher);

				if (PathString.Comparer.Equals(expectedPath, actualPath))
					return;

				throw new ApplicationException(new StringBuilder()
					.AppendLine("Observed directory was moved. Initial path:")
					.AppendLine(expectedPath)
					.AppendLine()
					.AppendLine("Current path:")
					.AppendLine(actualPath)
					.ToString());
			}
		}



		private void directoryChildCreated(object sender, FileSystemEventArgs e)
		{
			var watcher = (FileSystemWatcher) sender;

			ChangeDetected?.Invoke(this,
				new Change(EntryType.Uncertain,
					WatcherChangeTypes.Created,
					e.FullPath,
					fileSystemWatcher: watcher));
		}

		private void directoryChildDeleted(object sender, FileSystemEventArgs e)
		{
			var watcher = (FileSystemWatcher) sender;

			ChangeDetected?.Invoke(this,
				new Change(EntryType.Uncertain,
					WatcherChangeTypes.Deleted,
					e.FullPath,
					fileSystemWatcher: watcher));
		}

		private void directoryChildRenamed(object sender, RenamedEventArgs e)
		{
			var watcher = (FileSystemWatcher) sender;

			ChangeDetected?.Invoke(this,
				new Change(EntryType.Uncertain,
					WatcherChangeTypes.Renamed,
					e.FullPath,
					e.OldFullPath,
					watcher));
		}

		private void directoryChildChanged(object sender, FileSystemEventArgs e)
		{
			var watcher = (FileSystemWatcher) sender;

			ChangeDetected?.Invoke(this,
				new Change(EntryType.Uncertain,
					WatcherChangeTypes.Changed,
					e.FullPath,
					fileSystemWatcher: watcher));
		}



		private void fileCreated(object sender, FileSystemEventArgs e)
		{
			var watcher = (FileSystemWatcher) sender;

			ChangeDetected?.Invoke(this,
				new Change(EntryType.File,
					WatcherChangeTypes.Created,
					e.FullPath,
					fileSystemWatcher: watcher));
		}

		private void fileDeleted(object sender, FileSystemEventArgs e)
		{
			var watcher = (FileSystemWatcher) sender;

			ChangeDetected?.Invoke(this,
				new Change(EntryType.File,
					WatcherChangeTypes.Deleted,
					e.FullPath,
					fileSystemWatcher: watcher));
		}

		private void fileRenamed(object sender, RenamedEventArgs e)
		{
			var watcher = (FileSystemWatcher) sender;

			ChangeDetected?.Invoke(this,
				new Change(EntryType.File,
					WatcherChangeTypes.Renamed,
					e.FullPath,
					e.OldFullPath,
					watcher));
		}

		private void fileChanged(object sender, FileSystemEventArgs e)
		{
			var watcher = (FileSystemWatcher) sender;

			ChangeDetected?.Invoke(this,
				new Change(EntryType.Uncertain,
					WatcherChangeTypes.Changed,
					e.FullPath,
					fileSystemWatcher: watcher));
		}



		private void directoryWatcherError(object sender, ErrorEventArgs e)
		{
			watcherError(EntryType.Directory, sender, e);
		}

		private void fileWatcherError(object sender, ErrorEventArgs e)
		{
			watcherError(EntryType.File, sender, e);
		}

		private void watcherError(EntryType entryType, object sender, ErrorEventArgs e)
		{
			var target = getTarget(entryType, (FileSystemWatcher) sender);
			Error?.Invoke(this, new WatcherErrorArgs(target, e.GetException()));
		}

		public void Dispose()
		{
			var allTargets = _watchers.Keys.ToArray();

			foreach (var target in allTargets)
				Unwatch(target);
		}



		public event EventHandler<Change> ChangeDetected;

		public event EventHandler<WatcherErrorArgs> Error;

		public bool Enabled
		{
			get => _enabled;
			set
			{
				lock (_sync)
				{
					_enabled = value;

					foreach (var watcher in _watchers.Values)
					{
						watcher.EnableRaisingEvents = value;

						if (value)
							saveInitialWatchLocation(watcher);
					}
				}
			}
		}

		private bool _enabled;

		private readonly Dictionary<WatchTarget, FileSystemWatcher> _watchers =
			new Dictionary<WatchTarget, FileSystemWatcher>();

		private readonly Dictionary<FileSystemWatcher, string> _initialWatchPaths =
			new Dictionary<FileSystemWatcher, string>();

		private readonly object _sync = new object();
	}
}