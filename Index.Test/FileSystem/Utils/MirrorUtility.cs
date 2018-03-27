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
			Mirror.BackgroundLoopFailed += backgroundLoopFailed;

			Mirror.FileCreated += fileCreatedNotification;
			Mirror.FileDeleted += fileDeletedNotification;
			Mirror.FileMoved += fileMovedNotification;

			Mirror.DirectoryScanStarted += directoryScanStarted;
			Mirror.DirectoryScanFinished += directoryScanFinished;
			Mirror.DirectoryScanInterrupted += directoryScanInterrupted;
			Mirror.DirectoryScanNotFound += directoryScanNotFound;
		}

		public override void Dispose()
		{
			Mirror.Dispose();
			base.Dispose();
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
			var entry = Mirror.GetEntry(absolutePath);

			Assert.That(entry, Is.Not.Null);
			Assert.That(entry, Is.InstanceOf<DirectoryEntry<Metadata>>());

			var directory = (DirectoryEntry<Metadata>) entry;

			var actualStructure = directory.ToString((sb, en) =>
			{
				switch (en)
				{
					case FileEntry<Metadata> fileEntr:
						if (compareData)
							sb.Append($" #{fileEntr.Data.ContentId}");
						break;

					case DirectoryEntry<Metadata> dirEntr:
						sb.Append($" {dirEntr.Directories.Count}+{dirEntr.Files.Count}+{dirEntr.UnclassifiedEntries.Count}");
						break;
				}
			});

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

		public void LogDirectoryStructure()
		{
			var entry = Mirror.GetEntry(WorkingDirectory);
			var directory = (DirectoryEntry<Metadata>) entry;

			var actualStructure = directory.ToString(
				(sb, data) =>
				{
					switch (entry.Type)
					{
						case EntryType.File:
							sb.Append($" #{entry.Data.ContentId} {entry.Data.GetScanStatus()}");
							break;
						case EntryType.Directory:
							sb.Append($" {entry.Data.GetScanStatus()}");
							break;
					}
				});

			Log.Debug(actualStructure);
		}



		public Mirror Mirror { get; }



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

		private static void fileCreatedNotification(object sender, FileEntry<Metadata> e)
		{
			Log.Debug($"mirror file created {e.Name} #{e.Data.ContentId}");
		}

		private static void fileDeletedNotification(object sender, FileEntry<Metadata> e)
		{
			Log.Debug($"mirror file deleted {e.Name} #{e.Data.ContentId}");
		}

		private static void fileMovedNotification(object sender, FileEntry<Metadata> e)
		{
			Log.Debug($"mirror file moved {e.Name} #{e.Data.ContentId}");
		}

		private static void directoryScanStarted(object sender, DirectoryEntry<Metadata> e)
		{
			Log.Debug($"mirror directory scan started {e.GetPath()}");
		}

		private static void directoryScanFinished(object sender, DirectoryEntry<Metadata> e)
		{
			Log.Debug($"mirror directory scan finished {e.GetPath()}");
		}

		private static void directoryScanInterrupted(object sender, DirectoryEntry<Metadata> e)
		{
			Log.Debug($"mirror directory scan interrupted {e.GetPath()}");
		}

		private static void directoryScanNotFound(object sender, DirectoryEntry<Metadata> e)
		{
			Log.Debug($"mirror directory scan not found {e.GetPath()}");
		}

		private static void backgroundLoopFailed(object sender, Exception ex)
		{
			Log.Error(ex, "mirror background loop failed");
		}
	}
}