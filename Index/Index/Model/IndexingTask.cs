using System;
using System.Text;
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

			_creationTime = DateTime.UtcNow;
		}

		public void Reset()
		{
			Path = null;
			HardlinkPath = null;
			FileAccessException = null;
			HasToBeRepeated = false;
			Attempts++;
			FileEntry.Data.ResetScanningTime();
		}

		public void BeginScan() => FileEntry.Data.BeginScan();

		public void EndScan() => FileEntry.Data.EndScan();

		public void Cancel()
		{
			_taskCancellationSource.Cancel();
		}

		public IndexingAction Action { get; }

		public FileEntry<Metadata> FileEntry { get; }

		public long FileLength => FileEntry.Data.Length;
		public long ContentId => FileEntry.Data.ContentId;

		public string Path { get; set; }
		public string HardlinkPath { get; set; }
		public Exception FileAccessException { get; set; }

		public bool HasToBeRepeated { get; set; }
		public int Attempts { get; set; }

		public TimeSpan ElapsedSinceCreation => DateTime.UtcNow - _creationTime;

		public string GetState()
		{
			var state = new StringBuilder();

			if (FileAccessException != null)
				state.Append("file access error ");
			if (Path == null)
				state.Append("entry was removed ");
			if (CancellationToken.IsCancellationRequested)
				state.Append("canceled ");
			if (HasToBeRepeated)
				state.Append("has to be repeated ");

			if (state.Length == 0)
				state.Append("completed ");

			return state.ToString().TrimEnd();
		}

		public CancellationToken CancellationToken => _combinedCancellationSource.Token;
		private readonly CancellationTokenSource _combinedCancellationSource;
		private readonly CancellationTokenSource _taskCancellationSource;
		
		private readonly DateTime _creationTime;
	}
}