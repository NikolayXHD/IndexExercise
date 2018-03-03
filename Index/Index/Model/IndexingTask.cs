using System.Threading;

namespace IndexExercise.Index
{
	public class IndexingTask
	{
		public IndexingTask(
			IndexingAction action,
			long contentId,
			CancellationToken queueCancellationToken,
			long length = 0,
			string path = null)
		{
			Path = path;
			ContentId = contentId;
			Length = length;
			Action = action;
			
			_queueCancellationToken = queueCancellationToken;
			_taskCancellationSource = new CancellationTokenSource();
			
			_combinedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
				_taskCancellationSource.Token,
				_queueCancellationToken);
		}

		public IndexingTask CreateRepetitionTask()
		{
			return new IndexingTask(Action, ContentId, _queueCancellationToken, Length, Path)
			{
				Attempts = Attempts + 1
			};
		}

		public void Cancel()
		{
			_taskCancellationSource.Cancel();
		}

		public IndexingAction Action { get; }
		public long ContentId { get; }
		public long Length { get; }
		public string Path { get; }

		public bool HasToBeRepeated { get; set; }
		public int Attempts { get; private set; }

		private readonly CancellationToken _queueCancellationToken;
		
		public CancellationToken CancellationToken => _combinedCancellationSource.Token;
		private readonly CancellationTokenSource _combinedCancellationSource;
		private readonly CancellationTokenSource _taskCancellationSource;
	}
}