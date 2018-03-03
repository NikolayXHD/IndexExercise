using System;
using System.IO;
using System.Threading.Tasks;
using IndexExercise.Index.FileSystem;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture, Explicit("feature development in progress")]
	public class IndexingMetadataTests
	{
		[Test]
		public async Task When_file_is_copied_Then_last_write_time_is_not_changed()
		{
			var fileName = _util.CreateFile("original");
			var fileLastWriteTime = new FileInfo(fileName).LastWriteTime;
			await _util.SmallDelay();

			var otherFileName = _util.CreateFile("other");
			var copiedFileName = _util.GetFileName("copy");
			_util.CopyFile(fileName, copiedFileName);

			var otherFile = new FileInfo(otherFileName);
			var copiedFile = new FileInfo(copiedFileName);

			Assert.That(otherFile.LastWriteTime, Is.Not.EqualTo(fileLastWriteTime));
			Assert.That(copiedFile.LastWriteTime, Is.EqualTo(fileLastWriteTime));
		}

		[Test]
		public async Task When_file_is_moved_Then_last_write_time_is_not_changed()
		{
			var fileName = _util.CreateFile("original");
			var fileLastWriteTime = new FileInfo(fileName).LastWriteTime;
			await _util.SmallDelay();

			var otherFileName = _util.CreateFile("other");
			var movedFileName = _util.GetFileName("moved");
			_util.MoveFile(fileName, movedFileName);

			var otherFile = new FileInfo(otherFileName);
			var copiedFile = new FileInfo(movedFileName);

			
			Assert.That(otherFile.LastWriteTime, Is.Not.EqualTo(fileLastWriteTime));
			Assert.That(copiedFile.LastWriteTime, Is.EqualTo(fileLastWriteTime));
		}

		[Test]
		public void Written_metadata_Can_be_read()
		{
			var fileName = _util.CreateFile("file");

			long contentId = new Random().Next(1, 100);
			DateTime lastWriteTime = DateTime.Now.AddDays(-1);

			IndexingMetadataUtility.WriteMetadata(fileName, contentId, lastWriteTime);

			(long readContentId, DateTime readLastWriteTime) = IndexingMetadataUtility.ReadMetadata(fileName);

			Assert.That(readContentId, Is.EqualTo(contentId));
			Assert.That(readLastWriteTime, Is.EqualTo(lastWriteTime));
		}

		[Test]
		public void When_NO_metadata_was_written_Then_read_metadata_returns_default_values()
		{
			var fileName = _util.CreateFile("file");
			(long readContentId, DateTime readLastWriteTime) = IndexingMetadataUtility.ReadMetadata(fileName);

			Assert.That(readContentId, Is.EqualTo(long.MinValue));
			Assert.That(readLastWriteTime, Is.EqualTo(DateTime.MinValue));
		}

		[Test]
		public void When_file_is_copied_Then_metadata_is_copied_too()
		{
			var fileName = _util.CreateFile("file");

			long contentId = new Random().Next(1, 100);
			DateTime lastWriteTime = DateTime.Now.AddDays(-1);

			IndexingMetadataUtility.WriteMetadata(fileName, contentId, lastWriteTime);

			var copiedFileName = _util.GetFileName("file-copied");
			_util.CopyFile(fileName, copiedFileName);

			(long readContentId, DateTime readLastWriteTime) = IndexingMetadataUtility.ReadMetadata(copiedFileName);

			Assert.That(readContentId, Is.EqualTo(contentId));
			Assert.That(readLastWriteTime, Is.EqualTo(lastWriteTime));
		}

		[Test]
		public void When_file_is_renamed_Then_metadata_is_not_lost()
		{
			var fileName = _util.CreateFile("file");

			long contentId = new Random().Next(1, 100);
			DateTime lastWriteTime = DateTime.Now.AddDays(-1);

			IndexingMetadataUtility.WriteMetadata(fileName, contentId, lastWriteTime);

			var movedFileName = _util.GetFileName("file-moved");
			_util.MoveFile(fileName, movedFileName);

			(long readContentId, DateTime readLastWriteTime) = IndexingMetadataUtility.ReadMetadata(movedFileName);

			Assert.That(readContentId, Is.EqualTo(contentId));
			Assert.That(readLastWriteTime, Is.EqualTo(lastWriteTime));
		}



		[SetUp]
		public void Setup()
		{
			_util = new FileSystemUtility();
		}

		[TearDown]
		public void Teardown()
		{
			_util.Dispose();
		}

		private FileSystemUtility _util;
	}
}
