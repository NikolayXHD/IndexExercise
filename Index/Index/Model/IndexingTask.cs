using System;
using System.Threading;
using IndexExercise.Index.FileSystem;

namespace IndexExercise.Index
{
	public class IndexingTask
	{
		public IndexingTask(
			IndexingAction action,
			FileEntry<Metadata> fileEntry,
			CancellationToken queueCancellationToken)
		{
			Action = action;
			FileEntry = fileEntry;

			_taskCancellationSource = new CancellationTokenSource();

			_combinedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
				_taskCancellationSource.Token,
				queueCancellationToken);

			CreationTime = DateTime.UtcNow;
		}

		public void BeginProcessing()
		{
			Path = null;
			FileAccessException = null;
			HasToBeRepeated = false;
			Attempts++;
		}

		public void EndProcessing()
		{
			if (Path != null && FileAccessException == null && !CancellationToken.IsCancellationRequested)
				FileEntry.Data.IndexedTime = DateTime.UtcNow;
		}

		public void Cancel()
		{
			_taskCancellationSource.Cancel();
		}

		public IndexingAction Action { get; }

		public FileEntry<Metadata> FileEntry { get; }
		
		public string Path { get; set; }
		public string HardlinkPath { get; set; }
		public Exception FileAccessException { get; set; }

		public bool HasToBeRepeated { get; set; }
		public int Attempts { get; set; }
		public DateTime CreationTime { get; }

		public CancellationToken CancellationToken => _combinedCancellationSource.Token;
		private readonly CancellationTokenSource _combinedCancellationSource;
		private readonly CancellationTokenSource _taskCancellationSource;
	}
}