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



		public void ResetScanningTime()
		{
			_scanStartedTime = DateTime.MinValue;
			_scanFinishedTime = DateTime.MinValue;
		}

		public bool ScanStarted => _scanStartedTime != DateTime.MinValue;
		public bool ScanFinished => _scanFinishedTime != DateTime.MinValue;

		public void BeginScan() => _scanStartedTime = DateTime.UtcNow;
		public void EndScan() => _scanFinishedTime = DateTime.UtcNow;

		public void CopyScanStatusFrom(Metadata other)
		{
			if (other.ContentId != ContentId)
			{
				throw new InvalidOperationException($"{nameof(CopyScanStatusFrom)} requres that other {nameof(Metadata)} refers to the same {nameof(ContentId)}");
			}

			if (!ScanStarted)
				_scanStartedTime = other._scanStartedTime;
			
			if (!ScanFinished)
				_scanFinishedTime = other._scanFinishedTime;
		}

		public TimeSpan ElapsedSinceScanFinished =>
			ScanFinished
				? DateTime.UtcNow - _scanFinishedTime
				: TimeSpan.MinValue;

		private TimeSpan ElapsedSinceScanStarted =>
			ScanStarted
				? DateTime.UtcNow - _scanStartedTime
				: TimeSpan.MinValue;

		public string GetScanStatus()
		{
			if (ScanFinished)
				return $"scan finished {(int) ElapsedSinceScanFinished.TotalMilliseconds} ms ago";

			if (ScanStarted)
				return $"scan started {(int) ElapsedSinceScanStarted.TotalMilliseconds} ms ago";

			return "not scanned";
		}

		public override string ToString()
		{
			return "#" + ContentId;
		}

		private DateTime _scanStartedTime;
		private DateTime _scanFinishedTime;
	}
}