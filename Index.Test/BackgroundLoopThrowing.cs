using System;
using System.Threading.Tasks;

namespace IndexExercise.Index.Test
{
	public class BackgroundLoopThrowing<TException> : BackgroundLoopOwner
		where TException: Exception, new()
	{
		public BackgroundLoopThrowing(TimeSpan exceptionDelay)
		{
			_exceptionDelay = exceptionDelay;
		}

		protected override async Task BackgroundLoopIteration()
		{
			await Task.Delay(_exceptionDelay);
			throw new TException();
		}

		private readonly TimeSpan _exceptionDelay;
	}
}