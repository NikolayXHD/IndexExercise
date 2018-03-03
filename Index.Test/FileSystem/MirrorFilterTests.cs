using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IndexExercise.Index.FileSystem;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	public class MirrorFilterTests
	{
		[SetUp]
		public void Setup()
		{
			var ignoredFilesRegex = new Regex(@".ignored($|\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			_util = new MirrorUtility(filesFilter: fullFileName => !ignoredFilesRegex.IsMatch(fullFileName));
		}

		[TearDown]
		public void Teardown()
		{
			_util.Dispose();
		}


		[TestCase( /* delayBeforeWatch */ true)]
		[TestCase( /* delayBeforeWatch */ false)]
		public async Task When_synchronizing_preexisting_files_and_directories_Then_filter_is_honored(bool delayBeforeWatch)
		{
			string root = getDirectoryStructureRoot();
			createDirectoryStructure();

			// delayBeforeWatch == true guarantees that preexisting files / directory creations
			// actually happen before the watcher starts.
			// delayBeforeWatch == false makes test non-deterministic
			// sometimes the creations complete after the watcher started
			if (delayBeforeWatch)
				await _util.SmallDelay();

			_util.Watch(EntryType.Directory, root);
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(root,
				compareData: false,
				expectedStructureLines: expectedDirectoryStructure());
		}

		[Test]
		public async Task When_detecting_new_files_and_directories_Then_filter_is_honored()
		{
			string root = getDirectoryStructureRoot();

			_util.Watch(EntryType.Directory, root);

			createDirectoryStructure();
			await _util.SmallDelay();

			_util.AssertDirectoryStructure(root,
				compareData: false,
				expectedStructureLines: expectedDirectoryStructure());
		}

		private string getDirectoryStructureRoot() => _util.GetFileName("root");

		private void createDirectoryStructure()
		{
			var rootDirectoryName = _util.CreateDirectory("root");

			var directoryName = _util.CreateDirectory("directory_watched", parent: rootDirectoryName);
			var ignoredDirectoryName = _util.CreateDirectory("directory_ignored", parent: rootDirectoryName);

			var subdirectoryName = _util.CreateDirectory("subdirectory_watched", parent: directoryName);
			var subdirectoryInIgnoredParentName = _util.CreateDirectory("subdirectory_watched", parent: ignoredDirectoryName);

			var ignoredSubdirectoryName = _util.CreateDirectory("subdirectory_ignored", parent: directoryName);
			var ignoredSubdirectoryInIgnoredParentName = _util.CreateDirectory("subdirectory_ignored", parent: ignoredDirectoryName);

			_util.CreateFile("file_watched", rootDirectoryName);
			_util.CreateFile("file_watched", directoryName);
			_util.CreateFile("file_watched", ignoredDirectoryName);
			_util.CreateFile("file_watched", subdirectoryName);
			_util.CreateFile("file_watched", subdirectoryInIgnoredParentName);
			_util.CreateFile("file_watched", ignoredSubdirectoryName);
			_util.CreateFile("file_watched", ignoredSubdirectoryInIgnoredParentName);

			_util.CreateFile("file_ignored", rootDirectoryName);
			_util.CreateFile("file_ignored", directoryName);
			_util.CreateFile("file_ignored", ignoredDirectoryName);
			_util.CreateFile("file_ignored", subdirectoryName);
			_util.CreateFile("file_ignored", subdirectoryInIgnoredParentName);
			_util.CreateFile("file_ignored", ignoredSubdirectoryName);
			_util.CreateFile("file_ignored", ignoredSubdirectoryInIgnoredParentName);
		}

		private static string[] expectedDirectoryStructure()
		{
			return new[]
			{
				"root/ 2+1+0",
				"	file_watched",
				"	directory_ignored/ 2+0+0",
				"		subdirectory_ignored/ 0+0+0",
				"		subdirectory_watched/ 0+0+0",
				"	directory_watched/ 2+1+0",
				"		file_watched",
				"		subdirectory_ignored/ 0+0+0",
				"		subdirectory_watched/ 0+1+0",
				"			file_watched"
			};
		}



		private MirrorUtility _util;
	}
}