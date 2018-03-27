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
				return $"{toString(ElapsedSinceScanFinished)} ago";

			if (ScanStarted)
				return $"scanning {toString(ElapsedSinceScanStarted)}";

			return "not scanned";
		}

		private static string toString(TimeSpan timeSpan)
		{
			if (timeSpan.TotalDays > 2)
				return $"{(int) timeSpan.TotalDays}d";

			if (timeSpan.TotalHours > 2)
				return $"{(int) timeSpan.TotalHours}h";

			if (timeSpan.TotalMinutes > 2)
				return $"{(int) timeSpan.TotalMinutes}m";

			if (timeSpan.TotalSeconds > 2)
				return $"{(int) timeSpan.TotalSeconds}s";

			return $"{(int) timeSpan.TotalMilliseconds}ms";
		}

		public override string ToString()
		{
			return "#" + ContentId;
		}

		private DateTime _scanStartedTime;
		private DateTime _scanFinishedTime;
	}
}