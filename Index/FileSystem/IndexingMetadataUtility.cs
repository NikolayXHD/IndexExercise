using System;
using System.IO;

namespace IndexExercise.Index.FileSystem
{
	public static class IndexingMetadataUtility
	{
		public static void WriteMetadata(string filePath, long contentId, DateTime lastWriteTime)
		{
			var stream = AlternativeDataStream.TryOpen(
				filePath,
				MetadataStreamName,
				FileAccess.Write,
				FileShare.Read | FileShare.Write | FileShare.Delete);

			if (stream == null)
				return;

			var bytes = new byte[sizeof(long) * 2];
			long timeBinary = lastWriteTime.ToBinary();

			Array.Copy(BitConverter.GetBytes(contentId), 0, bytes, 0, sizeof(long));
			Array.Copy(BitConverter.GetBytes(timeBinary), 0, bytes, sizeof(long), sizeof(long));

			using (stream)
				stream.Write(bytes, 0, bytes.Length);
		}

		public static (long contentId, DateTime lastWriteTime) ReadMetadata(string filePath)
		{
			var stream = AlternativeDataStream.TryOpen(
				filePath,
				MetadataStreamName,
				FileAccess.Read,
				FileShare.Read | FileShare.Write | FileShare.Delete);

			if (stream == null)
				return (long.MinValue, DateTime.MinValue);

			var bytes = new byte[sizeof(long) * 2];


			using (stream)
			{
				if (stream.Read(bytes, 0, bytes.Length) <= 0)
					return (long.MinValue, DateTime.MinValue);

				long contentId = BitConverter.ToInt64(bytes, 0);
				long timeInBinary = BitConverter.ToInt64(bytes, sizeof(long));

				var lastWriteTime = DateTime.FromBinary(timeInBinary);

				return (contentId, lastWriteTime);
			}
		}

		private const string MetadataStreamName = "IndexExcercise";
	}
}