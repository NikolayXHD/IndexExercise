namespace IndexExercise.Index.FileSystem
{
	public class Find
	{
		public Find(EntryType type, string path)
		{
			Type = type;
			Path = path;
		}

		public EntryType Type { get; }
		public string Path { get; }

		public override string ToString()
		{
			return $"Found {Type}: {Path}";
		}
	}
}