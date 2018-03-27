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

			Assert.That(_util.Search("textual").FileNames.ToArray(),
				Is.EquivalentTo(Unit.Sequence(fileName)).Using((IComparer) PathString.Comparer));
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

			Assert.That(_util.Search("original").FileNames.ToArray(),
				Is.EquivalentTo(Unit.Sequence(fileName)).Using((IComparer) PathString.Comparer));

			Assert.That(_util.Search("updated").FileNames.ToArray(),
				Is.EquivalentTo(Enumerable.Empty<string>()).Using((IComparer) PathString.Comparer));

			File.WriteAllText(fileName, "updated content");

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			Assert.That(_util.Search("original").FileNames.ToArray(),
				Is.EquivalentTo(Enumerable.Empty<string>()).Using((IComparer) PathString.Comparer));

			Assert.That(_util.Search("updated").FileNames.ToArray(),
				Is.EquivalentTo(Unit.Sequence(fileName)).Using((IComparer) PathString.Comparer));
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

			Assert.That(_util.Search("textual").FileNames.ToArray(),
				Is.EquivalentTo(Unit.Sequence(fileName)).Using((IComparer) PathString.Comparer));

			var renamedFileName = _util.GetFileName("renamed", parent: watchedDirectory);
			_util.MoveFile(fileName, renamedFileName);

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			Assert.That(_util.Search("textual").FileNames.ToArray(),
				Is.EquivalentTo(Unit.Sequence(renamedFileName)).Using((IComparer) PathString.Comparer));
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

			Assert.That(_util.Search("textual").FileNames.ToArray(),
				Is.EquivalentTo(Unit.Sequence(fileName)).Using((IComparer) PathString.Comparer));

			_util.DeleteFile(fileName);

			await _util.ThrottleDelay();
			await _util.SmallDelay();

			Assert.That(_util.Search("textual").FileNames.ToArray(),
				Is.EquivalentTo(Enumerable.Empty<string>()).Using((IComparer) PathString.Comparer));
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