using System;

namespace IndexExercise.Index.FileSystem
{
	public class EntryAccessError
	{
		public EntryAccessError(EntryType entryType, string path, Exception exception)
		{
			EntryType = entryType;
			Path = path;
			Exception = exception;
		}

		public EntryType EntryType { get; }
		public string Path { get; }
		public Exception Exception { get; }

		public override string ToString()
		{
			return $"Error accessing {EntryType} {Path}: {Exception}";
		}
	}
}