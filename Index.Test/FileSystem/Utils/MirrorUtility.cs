using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IndexExercise.Index.FileSystem;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	public class MirrorUtility : WatchUtilityBase
	{
		public MirrorUtility(Mirror.FileNameFilter fileNameFilter = null)
		{
			Mirror = new Mirror(Watcher, new SequentialId(), fileNameFilter)
			{
				IdleDelay = TimeSpan.FromMilliseconds(value: 10)
			};

			Mirror.EntryFound += entryFound;
			Mirror.ProcessingChange += processingChange;
			
			Mirror.EnqueuedCreatedEntry += enqueuedCreatedEntry;
			Mirror.EnqueuedDeletedEntry += enqueuedDeletedEntry;
			Mirror.EnqueuedMovedEntry += enqueuedMovedEntry;

			Mirror.ProcessingCreatedEntry += processingCreatedEntry;
			Mirror.ProcessingDeletedEntry += processingDeletedEntry;
			Mirror.ProcessingMovedEntry += processingMovedEntry;

			Mirror.Idle += mirrorIdle;
			Mirror.EntryAccessError += entryAccessError;
			Mirror.BackgorundLoopFailed += backgroundLoopFailed;
		}

		public override void Dispose()
		{
			Mirror.Dispose();
			base.Dispose();
		}

		private static void processingChange(object sender, Change change)
		{
			Log.Debug($"mirror processing {change}");
		}

		private static void enqueuedCreatedEntry(object sender, Entry<Metadata> e)
		{
			Log.Debug($"mirror enqueue created {e.Type} {e.Name} {e.Data}");
		}

		private static void enqueuedDeletedEntry(object sender, Entry<Metadata> e)
		{
			Log.Debug($"mirror enqueue deleted {e.Type} {e.Name} {e.Data}");
		}

		private static void enqueuedMovedEntry(object sender, Entry<Metadata> e)
		{
			Log.Debug($"mirror enqueue moved {e.Type} {e.Name} {e.Data}");
		}

		private static void processingCreatedEntry(object sender, Entry<Metadata> e)
		{
			Log.Debug($"mirror process created {e.Type} {e.Name} {e.Data}");
		}

		private static void processingDeletedEntry(object sender, Entry<Metadata> e)
		{
			Log.Debug($"mirror process deleted {e.Type} {e.Name} {e.Data}");
		}

		private static void processingMovedEntry(object sender, Entry<Metadata> e)
		{
			Log.Debug($"mirror process moved {e.Type} {e.Name} {e.Data}");
		}

		private static void mirrorIdle(object sender, TimeSpan delay)
		{
			Log.Debug($"mirror idle {(int) delay.TotalMilliseconds} ms");
		}

		private static void entryFound(object sender, Find found)
		{
			Log.Debug(found.ToString);
		}

		private static void entryAccessError(object sender, EntryAccessError accessError)
		{
			Log.Error(accessError.ToString);
		}

		private static void backgroundLoopFailed(object sender, Exception ex)
		{
			Log.Error(ex, "mirror background loop failed");
		}



		public void Watch(EntryType entryType, string path = null)
		{
			var target = new WatchTarget(entryType, path ?? WorkingDirectory);

			Mirror.Watch(target);
			Mirror.RunAsync();

			Log.Debug($"mirror watch {target}");
		}

		public void AssertDirectoryStructure(string directoryName, params string[] expectedStructureLines)
		{
			AssertDirectoryStructure(directoryName, compareData: true, expectedStructureLines: expectedStructureLines);
		}

		public void AssertDirectoryStructure(string directoryName, bool compareData, params string[] expectedStructureLines)
		{
			AssertDirectoryStructure(directoryName, compareData, (IEnumerable<string>) expectedStructureLines);
		}

		public void AssertDirectoryStructure(string directoryName, bool compareData, IEnumerable<string> expectedStructureLines)
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