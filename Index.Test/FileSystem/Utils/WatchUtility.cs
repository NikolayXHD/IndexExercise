using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IndexExercise.Index.FileSystem;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	internal class WatchUtility : WatchUtilityBase
	{
		public WatchUtility()
		{
			Watcher.ChangeDetected += (sender, change) => _detectedChanges.Add(change);
			Watcher.Error += (sender, args) => _failedWatchTargets.Add(args.Target);
			Watcher.Enabled = true;
		}

		public void Watch(EntryType entryType, string path = null)
		{
			var target = new WatchTarget(entryType, path ?? WorkingDirectory);

			Watcher.Watch(target);
			Log.Debug($"Watch {target}");
		}

		public Change AssertDetected(WatcherChangeTypes changeType, string path = null, string oldFullPath = null, bool allowIncorrectPath = false)
		{
			return AssertDetected(EntryType.Uncertain, changeType, path, oldFullPath, allowIncorrectPath);
		}

		public Change AssertDetected(
			EntryType entryType,
			WatcherChangeTypes changeType,
			string path = null,
			string oldFullPath = null,
			bool allowIncorrectPath = false)
		{
			var change = findChange(entryType, changeType, path, oldFullPath);

			if (!allowIncorrectPath)
				Watcher.ThrowIfIncorrectPath(change);

			return change;
		}

		private Change findChange(EntryType entryType, WatcherChangeTypes changeType, string path, string oldFullPath)
		{
			if (_detectedChanges.Count == 0)
			{
				Assert.Fail(new StringBuilder()
					.AppendLine($"No more events. Expected: {entryType} {changeType} {oldFullPath} -> {path}")
					.ToString());
			}

			var detectedEventIndex = _detectedChanges.FindIndex(c =>
				changeMatchesFilter(c, entryType, changeType, path, oldFullPath));

			if (detectedEventIndex < 0)
			{
				Assert.Fail(new StringBuilder()
					.AppendLine($"No such event: {entryType} {changeType} {oldFullPath} -> {path}. Actual events:")
					.AppendLine(string.Join(Environment.NewLine, _detectedChanges))
					.ToString());
			}

			var change = _detectedChanges[detectedEventIndex];
			_detectedChanges.RemoveAt(detectedEventIndex);
			return change;
		}

		private static bool changeMatchesFilter(Change change, EntryType entryType, WatcherChangeTypes changeType, string fullPath, string oldFullPath)
		{
			return change.EntryType == entryType &&
				change.ChangeType == changeType &&
				(fullPath == null || PathString.Comparer.Equals(change.Path, fullPath)) &&
				(oldFullPath == null || PathString.Comparer.Equals(oldFullPath, change.OldPath));
		}

		public void AssertNoMoreEvents(WatcherChangeTypes types = WatcherChangeTypes.All)
		{
			var matchingEvents = _detectedChanges.Where(_ => (_.ChangeType & types) != 0)
				.ToList();

			if (matchingEvents.Count > 0)
			{
				Assert.Fail(new StringBuilder()
					.AppendLine("Expected no more events. Actual events:")
					.AppendLine(string.Join(Environment.NewLine, _detectedChanges))
					.ToString());
			}
		}

		public void AssertWatchError(EntryType type, string path = null)
		{
			Assert.That(_failedWatchTargets, Does.Contain(new WatchTarget(type, path ?? WorkingDirectory)));
		}

		private readonly HashSet<WatchTarget> _failedWatchTargets = new HashSet<WatchTarget>();
		private readonly List<Change> _detectedChanges = new List<Change>();
	}
}