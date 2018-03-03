using System.IO;

namespace IndexExercise.Index.FileSystem
{
	public class Change
	{
		public Change(
			EntryType entryType,
			WatcherChangeTypes changeType,
			string path,
			string oldPath = null,
			FileSystemWatcher fileSystemWatcher = null)
		{
			FileSystemWatcher = fileSystemWatcher;
			ChangeType = changeType;
			Path = path;
			OldPath = oldPath;
			EntryType = entryType;
		}

		public string Path { get; }
		public string OldPath { get; }

		public WatcherChangeTypes ChangeType { get; }
		public EntryType EntryType { get; set; }

		public FileSystemWatcher FileSystemWatcher { get; }

		public override string ToString()
		{
			return $"{EntryType} {ChangeType}: {OldPath} -> {Path}";
		}
	}
}