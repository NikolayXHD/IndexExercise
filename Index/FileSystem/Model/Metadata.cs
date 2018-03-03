namespace IndexExercise.Index.FileSystem
{
	public class Metadata
	{
		public Metadata(long contentId)
		{
			ContentId = contentId;
		}

		public long ContentId { get; }

		public long Length { get; set; } = -1L;

		public override string ToString()
		{
			return "#" + ContentId;
		}
	}
}