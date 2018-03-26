using System;

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

		public DateTime IndexedTime { get; set; }

		public override string ToString()
		{
			return "#" + ContentId;
		}
	}
}