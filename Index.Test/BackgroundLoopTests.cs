﻿using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture]
	public class BackgroundLoopTests
	{
		[Test]
		public async Task Background_loop_exception_Is_thrown_in_dispose_method()
		{
			var loopOwner = new BackgroundLoopThrowing<InvalidOperationException>(
				exceptionDelay: TimeSpan.FromMilliseconds(value: 10));

			loopOwner.Start();

			await Task.Delay(millisecondsDelay: 100);
			Assert.Throws<InvalidOperationException>(loopOwner.Dispose);
		}
	}
}