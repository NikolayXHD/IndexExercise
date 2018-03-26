using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IndexExercise.Index.Collections;
using IndexExercise.Index.FileSystem;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture]
	public class IndexFacadeTests
	{
		[Test]
		public async Task Preexisting_file_is_indexed()
		{
			var watchedDirectory = _util.CreateDirectory("watched_directory");
			var fileName = _util.CreateFile("file", watchedDirectory, "textual file content");
			await _util.SmallDelay();

			_util.StartIndexFacade();

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			Assert.That(_util.Search("textual").FileNames, Is.EquivalentTo(Unit.Sequence(fileName)).Using((IComparer) PathString.Comparer));
		}

		[Test]
		public async Task When_file_is_updated_Then_index_changes()
		{
			var watchedDirectory = _util.CreateDirectory("watched_directory");
			var fileName = _util.CreateFile(name: "file", parent: watchedDirectory, content: "original content");
			await _util.SmallDelay();

			_util.StartIndexFacade();

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			Assert.That(_util.Search("original").FileNames, Is.EquivalentTo(Unit.Sequence(fileName)).Using((IComparer) PathString.Comparer));
			Assert.That(_util.Search("updated").FileNames, Is.EquivalentTo(Enumerable.Empty<string>()).Using((IComparer) PathString.Comparer));

			File.WriteAllText(fileName, "updated content");

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			Assert.That(_util.Search("original").FileNames, Is.EquivalentTo(Enumerable.Empty<string>()).Using((IComparer) PathString.Comparer));
			Assert.That(_util.Search("updated").FileNames, Is.EquivalentTo(Unit.Sequence(fileName)).Using((IComparer) PathString.Comparer));
		}

		[Test]
		public async Task When_file_is_renamed_Then_search_result_changes()
		{
			var watchedDirectory = _util.CreateDirectory("watched_directory");
			var fileName = _util.CreateFile(name: "file", parent: watchedDirectory, content: "textual content");
			await _util.SmallDelay();

			_util.StartIndexFacade();

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			Assert.That(_util.Search("textual").FileNames, Is.EquivalentTo(Unit.Sequence(fileName)).Using((IComparer) PathString.Comparer));

			var renamedFileName = _util.GetFileName("renamed", parent: watchedDirectory);
			_util.MoveFile(fileName, renamedFileName);

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			Assert.That(_util.Search("textual").FileNames, Is.EquivalentTo(Unit.Sequence(renamedFileName)).Using((IComparer) PathString.Comparer));
		}

		[Test]
		public async Task When_file_is_deleted_Then_search_result_becomes_empty()
		{
			var watchedDirectory = _util.CreateDirectory("watched_directory");
			var fileName = _util.CreateFile(name: "file", parent: watchedDirectory, content: "textual content");
			await _util.SmallDelay();

			_util.StartIndexFacade();

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			Assert.That(_util.Search("textual").FileNames, Is.EquivalentTo(Unit.Sequence(fileName)).Using((IComparer) PathString.Comparer));

			_util.DeleteFile(fileName);

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			Assert.That(_util.Search("textual").FileNames, Is.EquivalentTo(Enumerable.Empty<string>()).Using((IComparer) PathString.Comparer));
		}



		[TestCase( /*updateCyclesCount*/ 20, /*filesCount*/ 05, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 05, /*filesCount*/ 20, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 10, /*filesCount*/ 10, /*wordsInFile*/ 400)]
		public async Task When_files_are_quickly_added_and_removed_Then_eventually_search_result_becomes_up_to_date(
			int updateCyclesCount,
			int filesCount,
			int wordsInFile)
		{
			var fileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"file_{j}"))
				.ToArray();

			var filesContent = Enumerable.Range(0, filesCount)
				.Select(j => string.Join(" ", Enumerable.Range(0, wordsInFile).Select(k => $"content_{j:D2}_word_{k}")))
				.ToList();

			_util.StartIndexFacade();

			var filesOrder = Enumerable.Range(0, filesCount)
				.ToList();

			for (int i = 0; i < updateCyclesCount; i++)
			{
				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.CreateFile(fileNames[j], content: filesContent[j]);

				// do not delete files on last iteration
				if (i == updateCyclesCount - 1)
					break;

				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.DeleteFile(fileNames[j]);
			}

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			for (int j = 0; j < filesCount; j++)
			{
				// prefix query
				var searchResult = _util.Search($"content_{j:D2}*");
				Assert.That(searchResult.FileNames, Is.EquivalentTo(Unit.Sequence(fileNames[j])).Using((IComparer) PathString.Comparer));
			}
		}

		[TestCase( /*updateCyclesCount*/ 20, /*filesCount*/ 05, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 05, /*filesCount*/ 20, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 10, /*filesCount*/ 10, /*wordsInFile*/ 400)]
		public async Task When_files_are_quickly_changed_Then_eventually_search_result_becomes_up_to_date(
			int updateCyclesCount,
			int filesCount,
			int wordsInFile)
		{
			var fileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"file_{j}"))
				.ToArray();

			var originalFilesContent = Enumerable.Range(0, filesCount)
				.Select(j => string.Join(" ", Enumerable.Range(0, wordsInFile).Select(k => $"content_{j:D2}_original_{k}")))
				.ToList();

			var modifiedFilesContent = Enumerable.Range(0, filesCount)
				.Select(j => string.Join(" ", Enumerable.Range(0, wordsInFile).Select(k => $"content_{j:D2}_modified_{k}")))
				.ToList();

			_util.StartIndexFacade();

			var filesOrder = Enumerable.Range(0, filesCount)
				.ToList();

			for (int i = 0; i < updateCyclesCount; i++)
			{
				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.CreateFile(fileNames[j], content: originalFilesContent[j]);

				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.CreateFile(fileNames[j], content: modifiedFilesContent[j]);
			}

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			for (int j = 0; j < filesCount; j++)
			{
				// prefix query
				var originalContentSearchResult = _util.Search($"content_{j:D2}_original*");
				Assert.That(originalContentSearchResult.FileNames, Is.EquivalentTo(Enumerable.Empty<string>()).Using((IComparer) PathString.Comparer));

				// because each update cycle original content was rewritten by modified
				var modifiedContentSearchResult = _util.Search($"content_{j:D2}_modified*");
				Assert.That(modifiedContentSearchResult.FileNames, Is.EquivalentTo(Unit.Sequence(fileNames[j])).Using((IComparer) PathString.Comparer));
			}
		}

		[TestCase( /*updateCyclesCount*/ 20, /*filesCount*/ 05, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 05, /*filesCount*/ 20, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 10, /*filesCount*/ 10, /*wordsInFile*/ 400)]
		public async Task When_files_are_quickly_renamed_Then_eventually_search_result_becomes_up_to_date(
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
				.Select(j => string.Join(" ", Enumerable.Range(0, wordsInFile).Select(k => $"content_{j:D2}_word_{k}")))
				.ToList();

			_util.StartIndexFacade();

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

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			for (int j = 0; j < filesCount; j++)
			{
				// prefix query
				var searchResult = _util.Search($"content_{j:D2}*");
				Assert.That(searchResult.FileNames, Is.EquivalentTo(Unit.Sequence(renamedFileNames[j])).Using((IComparer) PathString.Comparer));
			}
		}

		[TestCase( /*updateCyclesCount*/ 20, /*filesCount*/ 05, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 05, /*filesCount*/ 20, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 10, /*filesCount*/ 10, /*wordsInFile*/ 400)]
		public async Task When_files_are_quickly_moved_Then_eventually_search_result_becomes_up_to_date(
			int updateCyclesCount,
			int filesCount,
			int wordsInFile)
		{
			var sourceDirectoryName = _util.CreateDirectory("source_directory");

			var sourceFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"file_{j}", parent: sourceDirectoryName))
				.ToArray();

			var targetDirectoryName = _util.CreateDirectory("target_directory");

			var targetFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"file_{j}", parent: targetDirectoryName))
				.ToArray();

			var filesContent = Enumerable.Range(0, filesCount)
				.Select(j => string.Join(" ", Enumerable.Range(0, wordsInFile).Select(k => $"content_{j:D2}_word_{k}")))
				.ToList();

			_util.StartIndexFacade();

			var filesOrder = Enumerable.Range(0, filesCount)
				.ToList();

			foreach (int j in filesOrder)
				_util.CreateFile(sourceFileNames[j], content: filesContent[j]);

			for (int i = 0; i < updateCyclesCount; i++)
			{
				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.MoveFile(sourceFileNames[j], targetFileNames[j]);

				// do not move files back on last iteration
				if (i == updateCyclesCount - 1)
					break;

				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.MoveFile(targetFileNames[j], sourceFileNames[j]);
			}

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			for (int j = 0; j < filesCount; j++)
			{
				// prefix query
				var searchResult = _util.Search($"content_{j:D2}*");
				Assert.That(searchResult.FileNames, Is.EquivalentTo(Unit.Sequence(targetFileNames[j])).Using((IComparer) PathString.Comparer));
			}
		}

		[TestCase( /*updateCyclesCount*/ 20, /*filesCount*/ 05, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 05, /*filesCount*/ 20, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 10, /*filesCount*/ 10, /*wordsInFile*/ 400)]
		public async Task When_directories_are_quickly_renamed_Then_eventually_search_result_becomes_up_to_date(
			int updateCyclesCount,
			int filesCount,
			int wordsInFile)
		{
			var originalDirectoryNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.CreateDirectory($"original_directory_{j:D2}"))
				.ToArray();

			var originalFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName("file", parent: originalDirectoryNames[j]))
				.ToArray();

			var renamedDirectoryNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"renamed_directory_{j:D2}"))
				.ToArray();

			var movedFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName("file", parent: renamedDirectoryNames[j]))
				.ToArray();

			var filesContent = Enumerable.Range(0, filesCount)
				.Select(j => string.Join(" ", Enumerable.Range(0, wordsInFile).Select(k => $"content_{j:D2}_word_{k}")))
				.ToList();

			_util.StartIndexFacade();

			var filesOrder = Enumerable.Range(0, filesCount)
				.ToList();

			foreach (int j in filesOrder)
			{
				_util.CreateDirectory(originalDirectoryNames[j]);
				_util.CreateFile(originalFileNames[j], content: filesContent[j]);
			}

			for (int i = 0; i < updateCyclesCount; i++)
			{
				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.MoveDirectory(originalDirectoryNames[j], renamedDirectoryNames[j]);

				// do not move directories back on last iteration
				if (i == updateCyclesCount - 1)
					break;

				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.MoveDirectory(renamedDirectoryNames[j], originalDirectoryNames[j]);
			}

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			for (int j = 0; j < filesCount; j++)
			{
				// prefix query
				var searchResult = _util.Search($"content_{j:D2}*");
				Assert.That(searchResult.FileNames, Is.EquivalentTo(Unit.Sequence(movedFileNames[j])).Using((IComparer) PathString.Comparer));
			}
		}

		[TestCase( /*updateCyclesCount*/ 20, /*filesCount*/ 05, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 05, /*filesCount*/ 20, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 10, /*filesCount*/ 10, /*wordsInFile*/ 400)]
		public async Task When_directory_is_quickly_renamed_Then_eventually_search_result_becomes_up_to_date(
			int updateCyclesCount,
			int filesCount,
			int wordsInFile)
		{
			var originalDirectoryName = _util.CreateDirectory("original_directory");

			var originalFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"file_{j}", parent: originalDirectoryName))
				.ToArray();

			var renamedDirectoryName = _util.GetFileName("renamed_directory");

			var movedFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"file_{j}", parent: renamedDirectoryName))
				.ToArray();

			var filesContent = Enumerable.Range(0, filesCount)
				.Select(j => string.Join(" ", Enumerable.Range(0, wordsInFile).Select(k => $"content_{j:D2}_word_{k}")))
				.ToList();

			_util.StartIndexFacade();

			for (int j = 0; j < filesCount; j++)
				_util.CreateFile(originalFileNames[j], content: filesContent[j]);

			for (int i = 0; i < updateCyclesCount; i++)
			{
				_util.MoveDirectory(originalDirectoryName, renamedDirectoryName);

				// do not move directory back on last iteration
				if (i == updateCyclesCount - 1)
					break;

				_util.MoveDirectory(renamedDirectoryName, originalDirectoryName);
			}

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			for (int j = 0; j < filesCount; j++)
			{
				// prefix query
				var searchResult = _util.Search($"content_{j:D2}*");
				Assert.That(searchResult.FileNames, Is.EquivalentTo(Unit.Sequence(movedFileNames[j])).Using((IComparer) PathString.Comparer));
			}
		}

		[TestCase( /*updateCyclesCount*/ 20, /*filesCount*/ 05, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 05, /*filesCount*/ 20, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 10, /*filesCount*/ 10, /*wordsInFile*/ 400)]
		public async Task When_directories_are_quickly_moved_Then_eventually_search_result_becomes_up_to_date(
			int updateCyclesCount,
			int filesCount,
			int wordsInFile)
		{
			var sourceDirectory = _util.CreateDirectory("source_directory");

			var originalDirectoryNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.CreateDirectory($"directory_{j:D2}", sourceDirectory))
				.ToArray();

			var originalFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName("file", parent: originalDirectoryNames[j]))
				.ToArray();

			var targetDirectory = _util.CreateDirectory("target_directory");

			var movedDirectoryNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"directory_{j:D2}", parent: targetDirectory))
				.ToArray();

			var movedFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName("file", parent: movedDirectoryNames[j]))
				.ToArray();

			var filesContent = Enumerable.Range(0, filesCount)
				.Select(j => string.Join(" ", Enumerable.Range(0, wordsInFile).Select(k => $"content_{j:D2}_word_{k}")))
				.ToList();

			_util.StartIndexFacade();

			var filesOrder = Enumerable.Range(0, filesCount)
				.ToList();

			foreach (int j in filesOrder)
			{
				_util.CreateDirectory(originalDirectoryNames[j]);
				_util.CreateFile(originalFileNames[j], content: filesContent[j]);
			}

			for (int i = 0; i < updateCyclesCount; i++)
			{
				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.MoveDirectory(originalDirectoryNames[j], movedDirectoryNames[j]);

				// do not move directories back on last iteration
				if (i == updateCyclesCount - 1)
					break;

				filesOrder.Shuffle();

				foreach (int j in filesOrder)
					_util.MoveDirectory(movedDirectoryNames[j], originalDirectoryNames[j]);
			}

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			for (int j = 0; j < filesCount; j++)
			{
				// prefix query
				var searchResult = _util.Search($"content_{j:D2}*");
				Assert.That(searchResult.FileNames, Is.EquivalentTo(Unit.Sequence(movedFileNames[j])).Using((IComparer) PathString.Comparer));
			}
		}

		[TestCase( /*updateCyclesCount*/ 20, /*filesCount*/ 05, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 05, /*filesCount*/ 20, /*wordsInFile*/ 100)]
		[TestCase( /*updateCyclesCount*/ 10, /*filesCount*/ 10, /*wordsInFile*/ 400)]
		public async Task When_directory_is_quickly_moved_Then_eventually_search_result_becomes_up_to_date(
			int updateCyclesCount,
			int filesCount,
			int wordsInFile)
		{
			var sourceDirectory = _util.CreateDirectory("source_directory");

			var originalDirectoryName = _util.CreateDirectory("directory", parent: sourceDirectory);

			var originalFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"file_{j}", parent: originalDirectoryName))
				.ToArray();

			var targetDirectory = _util.CreateDirectory("target_directory");

			var movedDirectoryName = _util.GetFileName("directory", parent: targetDirectory);

			var movedFileNames = Enumerable.Range(0, filesCount)
				.Select(j => _util.GetFileName($"file_{j}", parent: movedDirectoryName))
				.ToArray();

			var filesContent = Enumerable.Range(0, filesCount)
				.Select(j => string.Join(" ", Enumerable.Range(0, wordsInFile).Select(k => $"content_{j:D2}_word_{k}")))
				.ToList();

			_util.StartIndexFacade();

			for (int j = 0; j < filesCount; j++)
				_util.CreateFile(originalFileNames[j], content: filesContent[j]);

			for (int i = 0; i < updateCyclesCount; i++)
			{
				_util.MoveDirectory(originalDirectoryName, movedDirectoryName);

				// do not move directory back on last iteration
				if (i == updateCyclesCount - 1)
					break;

				_util.MoveDirectory(movedDirectoryName, originalDirectoryName);
			}

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			for (int j = 0; j < filesCount; j++)
			{
				// prefix query
				var searchResult = _util.Search($"content_{j:D2}*");
				Assert.That(searchResult.FileNames, Is.EquivalentTo(Unit.Sequence(movedFileNames[j])).Using((IComparer) PathString.Comparer));
			}
		}

		[SetUp]
		public void Setup()
		{
			_util = new IndexFacadeUtility();
		}

		[TearDown]
		public void Teardown()
		{
			_util.Dispose();
		}

		private IndexFacadeUtility _util;
	}
}