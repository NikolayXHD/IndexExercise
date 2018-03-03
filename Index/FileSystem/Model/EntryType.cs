namespace IndexExercise.Index.FileSystem
{
	public enum EntryType
	{
		File,
		Directory,
		Root,

		/// <summary>
		/// May be either <see cref="File"/> or <see cref="Directory"/>
		/// </summary>
		Uncertain
	}
}