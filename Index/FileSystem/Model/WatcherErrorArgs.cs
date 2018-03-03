using System;

namespace IndexExercise.Index.FileSystem
{
	public class WatcherErrorArgs
	{
		public WatcherErrorArgs(WatchTarget target, Exception exception)
		{
			Target = target;
			Exception = exception;
		}

		public WatchTarget Target { get; }
		public Exception Exception { get; }
	}
}