using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IndexExercise.Index
{
	public abstract class BackgroundLoopOwner : IDisposable
	{
		protected BackgroundLoopOwner()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			CancellationToken = _cancellationTokenSource.Token;
		}

		public virtual async Task RunAsync()
		{
			lock (SyncRoot)
			{
				if (_started)
					throw new InvalidOperationException("Already started");

				if (_disposed)
					throw new ObjectDisposedException(GetType().FullName);

				_started = true;
			}

			_task = Task.Run(backgroundWorkLoop, CancellationToken);

			try
			{
				await _task;
			}
			catch (Exception ex) when (shouldIgnoreException(ex))
			{
			}
			catch (Exception ex)
			{
				BackgorundLoopFailed?.Invoke(this, ex);
				throw;
			}
		}

		public virtual void Dispose()
		{
			lock (SyncRoot)
			{
				_disposed = true;

				if (!_started)
					return;
			}

			_cancellationTokenSource.Cancel();

			try
			{
				_task.Wait();
			}
			catch (Exception ex) when (shouldIgnoreException(ex))
			{
			}
		}



		protected abstract Task BackgroundLoopIteration();

		protected async Task IdleDelayTask()
		{
			Idle?.Invoke(this, IdleDelay);
			await Task.Delay(IdleDelay, CancellationToken);
		}



		private async Task backgroundWorkLoop()
		{
			while (true)
			{
				if (CancellationToken.IsCancellationRequested)
					return;

				await BackgroundLoopIteration();
			}
		}

		private static bool shouldIgnoreException(Exception ex)
		{
			return ex is OperationCanceledException ||
				ex is AggregateException aggregateEx &&
				aggregateEx.Flatten().InnerExceptions.All(inner => inner is OperationCanceledException);
		}



		public event EventHandler<Exception> BackgorundLoopFailed;
		public event EventHandler<TimeSpan> Idle;

		public TimeSpan IdleDelay
		{
			get => _idleDelay;
			set
			{
				if (_idleDelay <= TimeSpan.Zero)
					throw new ArgumentException($"{nameof(IdleDelay)} must be positive");

				_idleDelay = value;
			}
		}

		private TimeSpan _idleDelay = TimeSpan.FromMilliseconds(value: 100);

		protected CancellationToken CancellationToken { get; }
		private readonly CancellationTokenSource _cancellationTokenSource;

		private bool _started;
		protected readonly object SyncRoot = new object();
		private Task _task;
		private bool _disposed;
	}
}