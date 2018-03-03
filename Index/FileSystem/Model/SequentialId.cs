using System.Threading;

namespace IndexExercise.Index.FileSystem
{
	public class SequentialId
	{
		public long New()
		{
			var result = Interlocked.Increment(ref _contentIdCounter);
			return result;
		}

		public long None => -1L;
		private long _contentIdCounter = -1L;
	}
}