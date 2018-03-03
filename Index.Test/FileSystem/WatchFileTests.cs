using System;
using System.IO;
using System.Threading.Tasks;
using IndexExercise.Index.FileSystem;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture]
	public class WatchFileTests
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
		public async Task When_empty_file_created_Then_created_event_raised()
		{
			var fileName = _util.GetFileName();
			_util.Watch(EntryType.File, fileName);

			_util.CreateFile(fileName, empty: true);
			await _util.SmallDelay();

			_util.AssertDetected(EntryType.File, WatcherChangeTypes.Created, fileName);
			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_nonempty_file_created_Then_created_and_changed_events_raised()
		{
			var fileName = _util.GetFileName();
			_util.Watch(EntryType.File, fileName);

			_util.CreateFile(fileName);
			await _util.SmallDelay();

			_util.AssertDetected(EntryType.File, WatcherChangeTypes.Created, fileName);
			_util.AssertDetected(EntryType.Uncertain, WatcherChangeTypes.Changed, fileName);
			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_file_changed_Then_changed_event_raised()
		{
			var fileName = _util.CreateFile();
			_util.Watch(EntryType.File, fileName);

			_util.ChangeFile(fileName);
			await _util.SmallDelay();

			_util.AssertDetected(EntryType.Uncertain, WatcherChangeTypes.Changed, fileName);
			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_file_deleted_Then_event_raised()
		{
			var fileName = _util.CreateFile(empty: true);
			_util.Watch(EntryType.File, fileName);

			_util.DeleteFile(fileName);
			await _util.SmallDelay();

			_util.AssertDetected(EntryType.File, WatcherChangeTypes.Deleted, fileName);
			_util.AssertNoMoreEvents();
		}

		[Test]
		public async Task When_file_renamed_Then_renamed_event_raised()
		{
			var oldFileName = _util.CreateFile();
			_util.Watch(EntryType.File, oldFileName);

			var fileName = _util.GetFileName();

			_util.MoveFile(oldFileName, fileName);
			await _util.SmallDelay();

			_util.AssertDetected(EntryType.File, WatcherChangeTypes.Renamed, fileName, oldFileName);
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
			_util.Watch(EntryType.File, targetFile);

			_util.MoveFile(originalFile, targetFile);
			await _util.SmallDelay();

			_util.AssertDetected(EntryType.File, WatcherChangeTypes.Created, targetFile);
		}

		[Test]
		public async Task When_file_moved_from_observed_directory_Then_deleted_event_raised()
		{
			var name = "file";

			var originalDirectory = _util.CreateDirectory("original-observed-dir");
			var originalFile = Path.Combine(originalDirectory, name);

			var targetDirectory = _util.CreateDirectory("target-non-observed-dir");
			var targetFile = Path.Combine(targetDirectory, name);

			_util.CreateFile(originalFile);
			_util.Watch(EntryType.File, originalFile);

			_util.MoveFile(originalFile, targetFile);
			await _util.SmallDelay();

			_util.AssertDetected(EntryType.File, WatcherChangeTypes.Deleted, originalFile);
		}



		[Test]
		public async Task When_observed_file_directory_deleted_Then_error_event_raised()
		{
			var fileName = _util.GetFileName();
			_util.Watch(EntryType.File, fileName);

			_util.DeleteDirectory(_util.WorkingDirectory);
			await _util.SmallDelay();

			_util.AssertWatchError(EntryType.File, fileName);
		}

		[Test]
		public async Task When_observed_file_directory_renamed_Then_incorrect_path_is_noticed()
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
			_util.Watch(EntryType.File, originalFilePath);

			_util.MoveDirectory(originalDirectoryPath, renamedDirectoryPath);
			_util.ChangeFile(renamedFilePath);
			await _util.SmallDelay();

			// notice that path == originalFilePath returned by FileSystemWatcher is wrong!
			// the actual changed file was located at path == renamedFilePath
			var change = _util.AssertDetected(EntryType.Uncertain, WatcherChangeTypes.Changed, originalFilePath, allowIncorrectPath: true);
			Assert.Throws<ApplicationException>(() => _util.Watcher.ThrowIfIncorrectPath(change));

			_util.AssertNoMoreEvents();
		}

		private WatchUtility _util;
	}
}