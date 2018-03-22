using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
			await _util.SmallDelay();

			Assert.That(_util.IndexFacade.Search("textual").FileNames, Is.EquivalentTo(Enumerable.Repeat(fileName, 1)));
		}

		[Test]
		public async Task When_file_is_updated_Then_index_changes()
		{
			var watchedDirectory = _util.CreateDirectory("watched_directory");
			var fileName = _util.CreateFile(name: "file", parent: watchedDirectory, content: "original content");
			await _util.SmallDelay();

			_util.StartIndexFacade();
			await _util.SmallDelay();

			Assert.That(_util.IndexFacade.Search("original").FileNames, Is.EquivalentTo(Enumerable.Repeat(fileName, 1)));
			Assert.That(_util.IndexFacade.Search("updated").FileNames, Is.EquivalentTo(Enumerable.Empty<string>()));

			File.WriteAllText(fileName, "updated content");
			await _util.SmallDelay();

			Assert.That(_util.IndexFacade.Search("original").FileNames, Is.EquivalentTo(Enumerable.Empty<string>()));
			Assert.That(_util.IndexFacade.Search("updated").FileNames, Is.EquivalentTo(Enumerable.Repeat(fileName, 1)));
		}

		[Test]
		public async Task When_file_is_renamed_Then_search_result_changes()
		{
			var watchedDirectory = _util.CreateDirectory("watched_directory");
			var fileName = _util.CreateFile(name: "file", parent: watchedDirectory, content: "textual content");
			await _util.SmallDelay();

			_util.StartIndexFacade();
			await _util.SmallDelay();

			Assert.That(_util.IndexFacade.Search("textual").FileNames, Is.EquivalentTo(Enumerable.Repeat(fileName, 1)));

			var renamedFileName = _util.GetFileName("renamed", parent: watchedDirectory);
			_util.MoveFile(fileName, renamedFileName);
			await _util.SmallDelay();

			Assert.That(_util.IndexFacade.Search("textual").FileNames, Is.EquivalentTo(Enumerable.Repeat(renamedFileName, 1)));
		}

		[Test]
		public async Task When_file_is_deleted_Then_search_result_becomes_empty()
		{
			var watchedDirectory = _util.CreateDirectory("watched_directory");
			var fileName = _util.CreateFile(name: "file", parent: watchedDirectory, content: "textual content");
			await _util.SmallDelay();

			_util.StartIndexFacade();
			await _util.SmallDelay();

			Assert.That(_util.IndexFacade.Search("textual").FileNames, Is.EquivalentTo(Enumerable.Repeat(fileName, 1)));

			_util.DeleteFile(fileName);
			await _util.SmallDelay();

			Assert.That(_util.IndexFacade.Search("textual").FileNames, Is.EquivalentTo(Enumerable.Empty<string>()));
		}



		[Test]
		public async Task When_files_are_quickly_added_and_removed_Then_eventually_search_result_becomes_up_to_date()
		{

		}

		[Test]
		public async Task When_files_are_quickly_changed_Then_eventually_search_result_becomes_up_to_date()
		{

		}

		[Test]
		public async Task When_files_are_quickly_renamed_Then_eventually_search_result_becomes_up_to_date()
		{

		}

		[Test]
		public async Task When_files_are_quickly_moved_Then_eventually_search_result_becomes_up_to_date()
		{

		}

		[Test]
		public async Task When_directories_are_quickly_renamed_Then_eventually_search_result_becomes_up_to_date()
		{

		}

		[Test]
		public async Task When_directories_are_quickly_moved_Then_eventually_search_result_becomes_up_to_date()
		{

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