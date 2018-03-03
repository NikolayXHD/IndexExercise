namespace IndexExercise.Index.FileSystem
{
	public class FileEntry<TData> : Entry<TData>
	{
		public FileEntry(RootEntry<TData> root) : base(root)
		{
		}

		public override EntryType Type => EntryType.File;
	}
}