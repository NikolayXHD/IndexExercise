using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IndexExercise.Index.Collections;
using IndexExercise.Index.FileSystem;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture]
	public class WatchDirectoryTests
	{
		[SetUp]
		public void Setup()
		{
			_util = new WatchUtility();
		}

		[TearDown]
		public void Teardown()
		{
			_util.Dispose();
		}

		[Test]
		public void Root_directory_is_observable()
		{
			_util.Watch(EntryType.Directory, "c:\\");
		}

		[Test]
		public void When_observing_root_as_file_Then_exception_is_thrown()
		{
			Assert.Throws<ArgumentException>(() => _util.Watch(EntryType.File, "c:\\"));
		}

		[Test]
		public async Task When_empty_file_created_Then_created_event_raised()
		{
			_util.Watch(EntryType.Directory);

			var fileName = _util.CreateFile(empty: true);
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Created, fileName);
			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_non_empty_file_created_Then_created_and_changed_events_raised()
		{
			_util.Watch(EntryType.Directory);

			var fileName = _util.CreateFile();
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Created, fileName);
			_util.AssertDetected(WatcherChangeTypes.Changed, fileName);
			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_file_deleted_Then_deleted_event_raised()
		{
			var fileName = _util.CreateFile();

			_util.Watch(EntryType.Directory);

			_util.DeleteFile(fileName);
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Deleted, fileName);
			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_file_deleted_and_created_Then_both_events_raised()
		{
			var fileName = _util.CreateFile();

			_util.Watch(EntryType.Directory);

			_util.DeleteFile(fileName);
			_util.CreateFile(name: fileName, empty: true);

			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Deleted, fileName);
			_util.AssertDetected(WatcherChangeTypes.Created, fileName);
			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_file_changed_Then_changed_event_raised()
		{
			var fileName = _util.CreateFile();

			_util.Watch(EntryType.Directory);

			_util.ChangeFile(fileName);
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Changed, fileName);
			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_file_renamed_Then_renamed_event_raised()
		{
			string fileName = _util.CreateFile();

			var renamedFileName = _util.GetFileName();
			Assert.That(renamedFileName, Is.Not.EqualTo(fileName).Using((IComparer<string>) PathString.Comparer));

			_util.Watch(EntryType.Directory);

			_util.MoveFile(fileName, renamedFileName);
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Renamed, renamedFileName, fileName);
			_util.AssertNoMoreEvents();
		}



		[Test]
		public async Task When_file_moved_into_observed_directory_Then_created_event_raised()
		{
			var name = "file";

			var originalDirectory = _util.CreateDirectory("original-non-observed-dir");
			var originalFile = Path.Combine(originalDirectory, name);

			var targetDirectory = _util.CreateDirectory("target-observed-dir");
			var targetFile = Path.Combine(targetDirectory, name);

			_util.CreateFile(originalFile);
			_util.Watch(EntryType.Directory, targetDirectory);

			_util.MoveFile(originalFile, targetFile);
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Created, targetFile);
		}

		[Test]
		public async Task When_file_moved_from_observed_directory_Then_deleted_event_raised()
		{
			var name = "subdir";

			var originalDirectory = _util.CreateDirectory("original-observed-dir");
			var originalSubdirectory = Path.Combine(originalDirectory, name);

			var targetDirectory = _util.CreateDirectory("target-non-observed-dir");
			var targetSubdirectory = Path.Combine(targetDirectory, name);

			_util.CreateDirectory(originalSubdirectory);
			_util.Watch(EntryType.Directory, originalDirectory);

			_util.MoveDirectory(originalSubdirectory, targetSubdirectory);
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Deleted, originalSubdirectory);
		}

		[Test]
		public async Task When_directory_moved_into_observed_directory_Then_created_event_raised()
		{
			var name = "subdir";

			var originalDirectory = _util.CreateDirectory("original-non-observed-dir");
			var originalSubdirectory = Path.Combine(originalDirectory, name);

			var targetDirectory = _util.CreateDirectory("target-observed-dir");
			var targetSubdirectory = Path.Combine(targetDirectory, name);

			_util.CreateDirectory(originalSubdirectory);
			_util.Watch(EntryType.Directory, targetDirectory);

			_util.MoveDirectory(originalSubdirectory, targetSubdirectory);
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Created, targetSubdirectory);
		}

		[Test]
		public async Task When_directory_moved_from_observed_directory_Then_deleted_event_raised()
		{
			var name = "file";

			var originalDirectory = _util.CreateDirectory("original-observed-dir");
			var originalFile = Path.Combine(originalDirectory, name);

			var targetDirectory = _util.CreateDirectory("target-non-observed-dir");
			var targetFile = Path.Combine(targetDirectory, name);

			_util.CreateFile(originalFile);
			_util.Watch(EntryType.Directory, originalDirectory);

			_util.MoveFile(originalFile, targetFile);
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Deleted, originalFile);
		}



		[Test]
		public async Task When_directory_created_Then_created_event_raised()
		{
			_util.Watch(EntryType.Directory);

			var directoryName = _util.CreateDirectory();
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Created, directoryName);
			_util.AssertNoMoreEvents();
		}

		[TestCase( /* filesCount */ 1)]
		[TestCase( /* filesCount */ 10)]
		[TestCase( /* filesCount */ 100)]
		public async Task When_directory_deleted_Then_event_raised_for_each_file(int filesCount)
		{
			string directoryName = _util.CreateDirectory();
			var fileNames = _util.CreateFiles(filesCount, parent: directoryName);

			_util.Watch(EntryType.Directory);

			_util.DeleteDirectory(directoryName);
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Deleted, directoryName);

			for (int i = 0; i < fileNames.Count; i++)
				_util.AssertDetected(WatcherChangeTypes.Deleted, fileNames[i]);

			// additional (WatcherChangeTypes.Changed, directoryName) was observed on different OS version
			_util.AssertNoMoreEvents(WatcherChangeTypes.All & ~WatcherChangeTypes.Changed);
		}

		[Test]
		public async Task When_directory_deleted_and_created_Then_both_events_raised()
		{
			var directoryName = _util.CreateDirectory();

			_util.Watch(EntryType.Directory);

			_util.DeleteDirectory(directoryName);
			_util.CreateDirectory(directoryName);

			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Deleted, directoryName);
			_util.AssertDetected(WatcherChangeTypes.Created, directoryName);
			_util.AssertNoMoreEvents();
		}

		[TestCase( /* filesCount */ 1)]
		[TestCase( /* filesCount */ 10)]
		[TestCase( /* filesCount */ 100)]
		public async Task When_directory_renamed_Then_directory_renamed_event_raised(int filesCount)
		{
			string fromDirectoryName = _util.CreateDirectory();
			_util.CreateFiles(filesCount, parent: fromDirectoryName);
			string directoryName = _util.GetFileName();

			_util.Watch(EntryType.Directory);

			_util.MoveDirectory(fromDirectoryName, directoryName);
			await _util.SmallDelay();

			_util.AssertDetected(WatcherChangeTypes.Renamed, directoryName, fromDirectoryName);

			// additional event was observed on different OS version
			_util.AssertNoMoreEvents(WatcherChangeTypes.All & ~WatcherChangeTypes.Changed);
		}



		[Test]
		public async Task When_creation_time_attribute_changed_Then_no_event_raised()
		{
			await modifyFileAttribute(
				File.GetCreationTimeUtc,
				File.SetCreationTimeUtc);

			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_last_access_time_attribute_changed_Then_no_event_raised()
		{
			await modifyFileAttribute(
				File.GetLastAccessTimeUtc,
				File.SetLastAccessTimeUtc);

			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_last_write_time_attribute_changed_Then_changed_event_raised()
		{
			await modifyFileAttribute(
				File.GetLastWriteTimeUtc,
				File.SetLastWriteTimeUtc);

			_util.AssertDetected(WatcherChangeTypes.Changed);
			_util.AssertNoMoreEvents();
		}



		[Test]
		public async Task When_observed_directory_deleted_Then_error_event_raised()
		{
			_util.Watch(EntryType.Directory);

			_util.DeleteDirectory(_util.WorkingDirectory);
			await _util.SmallDelay();

			_util.AssertWatchError(EntryType.Directory, _util.WorkingDirectory);
		}

		[Test]
		public async Task When_observed_directory_moved_Then_incorrect_path_is_noticed()
		{
			var originalDirectoryName = Path.GetRandomFileName();
			var renamedDirectoryName = Path.GetRandomFileName();
			var originalFileName = Path.GetRandomFileName();

			var originalDirectoryPath = Path.Combine(_util.WorkingDirectory, originalDirectoryName);
			var renamedDirectoryPath = Path.Combine(_util.WorkingDirectory, renamedDirectoryName);
			var originalFilePath = Path.Combine(originalDirectoryPath, originalFileName);
			var renamedFilePath = Path.Combine(renamedDirectoryPath, originalFileName);

			_util.CreateDirectory(originalDirectoryPath);
			_util.CreateFile(originalFilePath);
			_util.Watch(EntryType.Directory, originalDirectoryPath);

			_util.MoveDirectory(originalDirectoryPath, renamedDirectoryPath);
			_util.ChangeFile(renamedFilePath);
			await _util.SmallDelay();

			// notice that path == originalFilePath returned by FileSystemWatcher is wrong!
			// the actual changed file was located at path == renamedFilePath
			var change = _util.AssertDetected(EntryType.Uncertain, WatcherChangeTypes.Changed, originalFilePath, allowIncorrectPath: true);
			Assert.Throws<ApplicationException>(() => _util.Watcher.ThrowIfIncorrectPath(change));

			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_observed_directory_is_recreated_Then_no_events_raised()
		{
			_util.Watch(EntryType.Directory);

			_util.DeleteDirectory(_util.WorkingDirectory);
			await _util.SmallDelay();

			_util.CreateDirectory(_util.WorkingDirectory);
			_util.CreateFile();

			_util.AssertNoMoreEvents();
		}



		private async Task modifyFileAttribute(Func<string, DateTime> getter, Action<string, DateTime> setter)
		{
			var fileName = _util.CreateFile();

			var initialValue = getter(fileName);

			_util.Watch(EntryType.Directory);

			var modifiedValue = initialValue.AddSeconds(value: 1);
			setter(fileName, modifiedValue);

			var actualValue = getter(fileName);

			Assert.That(actualValue, Is.EqualTo(modifiedValue));
			await _util.SmallDelay();
		}

		private WatchUtility _util;
	}
}