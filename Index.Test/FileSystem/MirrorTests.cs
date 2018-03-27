using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IndexExercise.Index.Collections;
using IndexExercise.Index.FileSystem;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture]
	public class MirrorTests
	{
		[SetUp]
		public void Setup()
		{
			_util = new MirrorUtility();
		}

		[TearDown]
		public void Teardown()
		{
			_util.Dispose();
		}

		[TestCase( /* delayBeforeWatch */ true)]
		[TestCase( /* delayBeforeWatch */ false)]
		public async Task Preexisting_file_is_synchronized(bool delayBeforeWatch)
		{
			string directoryName = _util.CreateDirectory("directory");

			_util.CreateFile("file", parent: directoryName);

			// delayBeforeWatch == true guarantees that preexisting files / directory creations
			// actually happen before the watcher starts.
			// delayBeforeWatch == false makes test non-deterministic
			// sometimes the creations complete after the watcher started
			if (delayBeforeWatch)
				await _util.SmallDelay();

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 0+1+0",
				"	file #0");
		}

		[TestCase( /* delayBeforeWatch */ true)]
		[TestCase( /* delayBeforeWatch */ false)]
		public async Task Preexisting_directory_is_synchronized(bool delayBeforeWatch)
		{
			string directoryName = _util.CreateDirectory("directory");

			_util.CreateDirectory("subdirectory", parent: directoryName);

			// delayBeforeWatch == true guarantees that preexisting files / directory creations
			// actually happen before the watcher starts.
			// delayBeforeWatch == false makes test non-deterministic
			// sometimes the creations complete after the watcher started
			if (delayBeforeWatch)
				await _util.SmallDelay();

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 1+0+0",
				"	subdirectory/ 0+0+0");
		}

		[Test]
		public async Task Created_file_is_detected()
		{
			string directoryName = _util.CreateDirectory("directory");

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();
			_util.AssertDirectoryStructure(directoryName,
				"directory/ 0+0+0");

			_util.CreateFile("file", parent: directoryName);
			await _util.SmallDelay();

			// file contentId is
			// #0 after file is created 
			// #1 after content is written
			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 0+1+0",
				"	file #1");
		}

		[Test]
		public async Task Created_directory_is_detected()
		{
			string directoryName = _util.CreateDirectory("directory");

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();
			_util.AssertDirectoryStructure(directoryName,
				"directory/ 0+0+0");

			_util.CreateDirectory("subdirectory", parent: directoryName);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 1+0+0",
				"	subdirectory/ 0+0+0");
		}

		[TestCase( /* filesCount */ 1)]
		[TestCase( /* filesCount */ 5)]
		[TestCase( /* filesCount */ 25)]
		public async Task Files_created_in_currently_synchronized_directory_are_detected(int filesCount)
		{
			var preexisting = _util.CreateFiles(filesCount);
			_util.Watch(EntryType.Directory);

			var added = _util.CreateFiles(filesCount);
			await _util.SmallDelay();

			var expectedStructure =
				Unit.Sequence($"{Path.GetFileName(_util.WorkingDirectory)}/ 0+{added.Count + preexisting.Count}+0")
					.Concat(
						added.Concat(preexisting).Select(_ => $"\t{Path.GetFileName(_)}")
							.OrderBy(_ => _, PathString.Comparer))
					.ToArray();

			_util.AssertDirectoryStructure(
				_util.WorkingDirectory,
				compareData: false,
				expectedStructureLines: expectedStructure);
		}

		[TestCase( /* directoriesCount */ 1)]
		[TestCase( /* directoriesCount */ 5)]
		[TestCase( /* directoriesCount */ 25)]
		public async Task Directories_created_in_currently_synchronized_directory_are_detected(int directoriesCount)
		{
			var preexisting = _util.CreateDirectories(directoriesCount);
			_util.Watch(EntryType.Directory);

			var added = _util.CreateDirectories(directoriesCount);
			await _util.SmallDelay();

			var expectedStructure =
				Unit.Sequence((Path.GetFileName(_util.WorkingDirectory) + $"/ {added.Count + preexisting.Count}+0+0"))
					.Concat(added
						.Concat(preexisting)
						.OrderBy(_ => _, PathString.Comparer)
						.Select(Path.GetFileName)
						.Select(_ => $"\t{_}/ 0+0+0"))
					.ToArray();

			_util.AssertDirectoryStructure(
				_util.WorkingDirectory,
				compareData: false,
				expectedStructureLines: expectedStructure);
		}



		[Test]
		public async Task Deleted_file_is_detected()
		{
			string directoryName = _util.CreateDirectory("directory");
			string fileName = _util.CreateFile("file", parent: directoryName);

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();

			_util.DeleteFile(fileName);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 0+0+0");
		}

		[Test]
		public async Task Deleted_directory_is_detected()
		{
			string directoryName = _util.CreateDirectory("directory");
			_util.CreateDirectory("subdirectory", parent: directoryName);

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();

			_util.DeleteDirectory(directoryName);
			await _util.SmallDelay();

			Assert.That(_util.Mirror.GetEntry(directoryName), Is.Null);
		}

		[Test]
		public async Task Deleted_subdirectory_is_detected()
		{
			string directoryName = _util.CreateDirectory("directory");
			string subdirectoryName = _util.CreateDirectory("subdirectory", parent: directoryName);

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();

			_util.DeleteDirectory(subdirectoryName);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 0+0+0");
		}



		[Test]
		public async Task When_file_is_renamed_Then_its_content_id_is_preserved()
		{
			string directoryName = _util.CreateDirectory("directory");
			string originalFileName = _util.CreateFile("original_file", parent: directoryName);
			string renamedFileName = Path.Combine(directoryName, "renamed_file");

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();
			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 0+1+0",
				"	original_file #0");


			_util.MoveFile(originalFileName, renamedFileName);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 0+1+0",
				"	renamed_file #0");
		}

		[TestCase( /*updateCyclesCount*/ 10, /*filesCount*/ 20, /*wordsInFile*/ 300)]
		public async Task When_files_are_quickly_renamed_Then_eventually_directory_structure_becomes_up_to_date(
			int updateCyclesCount,
			int filesCount,
			int wordsInFile)
		{
			var originalFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"file_original_{j}"))
				.ToArray();

			var renamedFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"file_renamed_{j}"))
				.ToArray();

			var filesContent = Enumerable.Range(0, filesCount)
				.Select(j => string.Join(" ", Enumerable.Range(0, wordsInFile).Select(k => $"content_{j}_word_{k}")))
				.ToList();

			_util.Watch(EntryType.Directory);

			var filesOrder = Enumerable.Range(0, filesCount)
				.ToList();

			foreach (int j in filesOrder)
				_util.CreateFile(originalFileNames[j], content: filesContent[j]);

			for (int i = 0; i < updateCyclesCount; i++)
			{
				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.MoveFile(originalFileNames[j], renamedFileNames[j]);

				// do not rename files back on last iteration
				if (i == updateCyclesCount - 1)
					break;

				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.MoveFile(renamedFileNames[j], originalFileNames[j]);
			}

			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				_util.WorkingDirectory,
				compareData: false,
				expectedStructureLines:
				Unit.Sequence($"working/ 0+{filesCount}+0")
					.Concat(Enumerable.Range(0, filesCount)
						.Select(j => $"	file_renamed_{j}")
						.OrderBy(_ => _, PathString.Comparer)));
		}

		[Test]
		public async Task When_file_is_moved_its_content_id_is_NOT_preserved()
		{
			// because FileSystemWatcher generates Deleted and Created events

			string directoryName = _util.CreateDirectory("directory");
			string sourceSubdirectoryName = _util.CreateDirectory("source_subdirectory", parent: directoryName);
			string targetSubdirectoryName = _util.CreateDirectory("target_subdirectory", parent: directoryName);

			string name = "file";
			string fileName = _util.CreateFile(name, parent: sourceSubdirectoryName);

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 2+0+0",
				"	source_subdirectory/ 0+1+0",
				"		file #0",
				"	target_subdirectory/ 0+0+0");

			_util.MoveFile(fileName, Path.Combine(targetSubdirectoryName, name));
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 2+0+0",
				"	source_subdirectory/ 0+0+0",
				"	target_subdirectory/ 0+1+0",
				"		file #1");
		}

		[Test]
		public async Task When_directory_is_renamed_Then_files_content_id_are_preserved()
		{
			string directoryName = _util.CreateDirectory("directory");
			string originalSubdirectoryName = _util.CreateDirectory("original_subdir", parent: directoryName);
			_util.CreateFile("file", parent: originalSubdirectoryName);
			string renamedSubdirectoryName = Path.Combine(directoryName, "renamed_subdir");

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 1+0+0",
				"	original_subdir/ 0+1+0",
				"		file #0");

			_util.MoveDirectory(originalSubdirectoryName, renamedSubdirectoryName);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 1+0+0",
				"	renamed_subdir/ 0+1+0",
				"		file #0");
		}

		[Test]
		public async Task When_directory_is_moved_Then_files_content_id_are_NOT_preserved()
		{
			// because FileSystemWatcher generates Deleted and Created events

			string directoryName = _util.CreateDirectory("directory");
			string sourceSubdirectoryName = _util.CreateDirectory("source_subdirectory", parent: directoryName);
			string targetSubdirectoryName = _util.CreateDirectory("target_subdirectory", parent: directoryName);

			string name = "moving_subdirectory";
			string movedSubdirectoryName = _util.CreateDirectory(name, parent: sourceSubdirectoryName);
			_util.CreateFile("file", movedSubdirectoryName, empty: true);

			_util.Watch(EntryType.Directory);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 2+0+0",
				"	source_subdirectory/ 1+0+0",
				"		moving_subdirectory/ 0+1+0",
				"			file #0",
				"	target_subdirectory/ 0+0+0");

			_util.MoveDirectory(movedSubdirectoryName, Path.Combine(targetSubdirectoryName, name));
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(
				directoryName,
				"directory/ 2+0+0",
				"	source_subdirectory/ 0+0+0",
				"	target_subdirectory/ 1+0+0",
				"		moving_subdirectory/ 0+1+0",
				"			file #1");
		}


		[Test]
		public async Task Deleted_root_is_detected()
		{
			string root = _util.CreateDirectory("root");
			string directory = _util.CreateDirectory("dir", parent: root);
			_util.CreateDirectory("subdir", parent: directory);

			_util.Watch(EntryType.Directory, root);
			await _util.SmallDelay();

			Assert.That(_util.Mirror.GetEntry(root), Is.Not.Null);

			_util.DeleteDirectory(root);
			await _util.SmallDelay();

			Assert.That(_util.Mirror.GetEntry(root), Is.Null);
		}

		[Test]
		public async Task Renamed_root_is_noticed()
		{
			string directoryName = _util.CreateDirectory("testdir");
			_util.CreateDirectory("subdir", parent: directoryName);

			_util.Watch(EntryType.Directory, directoryName);
			await _util.SmallDelay();

			Assert.That(_util.Mirror.GetEntry(directoryName), Is.Not.Null);

			string renamedDirectoryName = _util.GetFileName();
			_util.MoveDirectory(directoryName, renamedDirectoryName);
			await _util.SmallDelay();

			Assert.That(_util.Mirror.GetEntry(directoryName), Is.Null);
		}



		private MirrorUtility _util;
	}
}