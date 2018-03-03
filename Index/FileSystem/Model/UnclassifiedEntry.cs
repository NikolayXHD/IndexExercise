namespace IndexExercise.Index.FileSystem
{
	public class UnclassifiedEntry<TData> : Entry<TData>
	{
		public UnclassifiedEntry(RootEntry<TData> root) : base(root)
		{
		}

		public override EntryType Type => EntryType.Uncertain;
	}
}