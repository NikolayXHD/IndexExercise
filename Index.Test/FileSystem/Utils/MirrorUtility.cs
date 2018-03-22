using System;
using System.IO;
using System.Text;
using IndexExercise.Index.FileSystem;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	public class MirrorUtility : WatchUtilityBase
	{
		public MirrorUtility(Mirror.FilesFilter filesFilter = null)
		{
			Mirror = new Mirror(Watcher, new SequentialId(), filesFilter)
			{
				IdleDelay = TimeSpan.FromMilliseconds(value: 10)
			};

			Mirror.EntryFound += entryFound;
			Mirror.ProcessingChange += processingChange;
			Mirror.EnqueuedCreatedEntry += enqueuedCreatedEntry;
			Mirror.ProcessingCreatedEntry += processingCreatedEntry;
			Mirror.EnqueuedDeletedEntry += enqueuedDeletedEntry;
			Mirror.ProcessingDeletedEntry += processingDeletedEntry;
			Mirror.Idle += watcherIdle;
			Mirror.EntryAccessError += entryAccessError;
			Mirror.BackgorundLoopFailed += backgroundLoopFailed;
		}

		public override void Dispose()
		{
			Mirror.Dispose();
			base.Dispose();
		}

		private void processingChange(object sender, Change change)
		{
			Log.Debug($"processing {change}");
		}

		private void enqueuedCreatedEntry(object sender, Entry<Metadata> e)
		{
			Log.Debug($"enqueue created {e.Type} {e.Name} {e.Data}");
		}

		private void enqueuedDeletedEntry(object sender, Entry<Metadata> e)
		{
			Log.Debug($"enqueue deleted {e.Type} {e.Name} {e.Data}");
		}

		private void processingCreatedEntry(object sender, Entry<Metadata> e)
		{
			Log.Debug($"process created {e.Type} {e.Name} {e.Data}");
		}

		private void processingDeletedEntry(object sender, Entry<Metadata> e)
		{
			Log.Debug($"process deleted {e.Type} {e.Name} {e.Data}");
		}

		private void watcherIdle(object sender, TimeSpan delay)
		{
			Log.Debug($"watcher idle {(int) delay.TotalMilliseconds} ms");
		}

		private void entryFound(object sender, Find found)
		{
			Log.Debug(found.ToString);
		}

		private void entryAccessError(object sender, EntryAccessError accessError)
		{
			Log.Error(accessError.ToString);
		}

		private void backgroundLoopFailed(object sender, Exception ex)
		{
			Log.Error(ex, "background loop failed");
		}



		public void Watch(EntryType entryType, string path = null)
		{
			var target = new WatchTarget(entryType, path ?? WorkingDirectory);

			Mirror.Watch(target);
			Mirror.Start();

			Log.Debug($"watch {target}");
		}

		public void AssertDirectoryStructure(string directoryName, params string[] expectedStructureLines)
		{
			AssertDirectoryStructure(directoryName, compareData: true, expectedStructureLines: expectedStructureLines);
		}

		public void AssertDirectoryStructure(string directoryName, bool compareData, params string[] expectedStructureLines)
		{
			var expectedStructure = string.Join(Environment.NewLine, expectedStructureLines) + Environment.NewLine;
			var absolutePath = Path.Combine(WorkingDirectory, directoryName);
			var entry = Mirror.Find(absolutePath);

			Assert.That(entry, Is.Not.Null);
			Assert.That(entry, Is.InstanceOf<DirectoryEntry<Metadata>>());

			var directory = (DirectoryEntry<Metadata>) entry;
			var actualStructure = directory.ToString(printData: compareData);

			if (PathString.Comparer.Equals(actualStructure, expectedStructure))
			{
				Log.Debug(actualStructure);
			}
			else
			{
				Assert.Fail(new StringBuilder()
					.AppendLine("Directory structure is different from expected:")
					.Append(expectedStructure)
					.AppendLine("Actual:")
					.Append(actualStructure)
					.ToString());
			}
		}

		public Mirror Mirror { get; }
	}
}