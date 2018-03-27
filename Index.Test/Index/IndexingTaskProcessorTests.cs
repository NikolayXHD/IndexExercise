using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using IndexExercise.Index.Collections;
using IndexExercise.Index.FileSystem;
using IndexExercise.Index.Lucene;
using NLog;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture]
	public class IndexingTaskProcessorTests
	{
		[Test]
		public void Processing_addition_task_Changes_search_result()
		{
			const long contentId = 11L;

			var task = createAdditionTaskForNewFile(contentId, content: "textual file content");
			processTask(task);

			Assert.That(_indexEngine.Search("textual").ContentIds, Is.EquivalentTo(Unit.Sequence(contentId)));
		}

		[Test]
		public void Processing_subsequent_addition_task_for_the_same_contentId_Changes_search_result()
		{
			const long contentId = 1;

			var additionTask = createAdditionTaskForNewFile(contentId, content: "original phrase");
			processTask(additionTask);

			Assert.That(_indexEngine.Search("original").ContentIds, Is.EquivalentTo(Unit.Sequence(contentId)));
			Assert.That(_indexEngine.Search("changed").ContentIds, Is.EquivalentTo(Enumerable.Empty<long>()));
			Assert.That(_indexEngine.Search("phrase").ContentIds, Is.EquivalentTo(Unit.Sequence(contentId)));

			var updateTask = createAdditionTaskForNewFile(contentId, content: "changed phrase");
			processTask(updateTask);

			Assert.That(_indexEngine.Search("original").ContentIds, Is.EquivalentTo(Enumerable.Empty<long>()));
			Assert.That(_indexEngine.Search("changed").ContentIds, Is.EquivalentTo(Unit.Sequence(contentId)));
			Assert.That(_indexEngine.Search("phrase").ContentIds, Is.EquivalentTo(Unit.Sequence(contentId)));
		}

		[Test]
		public void Processing_removal_task_Changes_search_result()
		{
			const long contentId = 11;

			var additionTask = createAdditionTaskForNewFile(contentId, content: "phrase to be searched");
			processTask(additionTask);

			Assert.That(_indexEngine.Search("phrase").ContentIds, Is.EquivalentTo(Unit.Sequence(contentId)));

			var removalTask = createRemovalTask(additionTask.ContentId);
			processTask(removalTask);

			_indexEngine.Remove(contentId);
			Assert.That(_indexEngine.Search("phrase").ContentIds, Is.EquivalentTo(Enumerable.Empty<long>()));
		}



		[Test]
		public void Currently_indexed_file_Can_be_deleted()
		{
			const long contentId = 11L;
			var indexingTask = createAdditionTaskForNewFile(contentId, content: "textual file content");

			_taskProcessor.FileOpened += (sender, args) =>
			{
				File.Delete(args.Path);
				Assert.That(File.Exists(args.Path), Is.False);
			};
			
			processTask(indexingTask);
			Assert.That(File.Exists(indexingTask.Path), Is.False);
		}

		[Test]
		public void Currently_indexed_file_Can_not_be_opened_without_read_share()
		{
			// the way I see it, this fact prevents implementing file scanning procedure in a 
			// non intrusive way, i.e. not interfering with other procesess accessing the file
			const long contentId = 8L;
			var indexingTask = createAdditionTaskForNewFile(contentId, content: "textual file content");

			IOException ioException = null;

			_taskProcessor.FileOpened += (sender, args) =>
			{
				// The process cannot access the file ... because it is being used by another process
				ioException = Assert.Throws<IOException>(() =>
				{
					using (File.Open(args.Path, FileMode.Open, FileAccess.Write, FileShare.Write))
					{
					}
				});
			};

			processTask(indexingTask);
			Assert.That(ioException, Is.Not.Null);
		}

		[Test]
		public void When_delete_currently_indexed_file_Then_deletion_is_detected_before_indexing_completes()
		{
			const long contentId = 11L;

			_taskProcessor.FileOpened += (sender, args) =>
			{
				_util.Watch(EntryType.Directory);

				File.Delete(args.Path);
				_util.SmallDelay().Wait();

				_util.AssertDetected(WatcherChangeTypes.Deleted, args.Path);
			};

			var indexingTask = createAdditionTaskForNewFile(contentId, content: "textual file content");
			processTask(indexingTask);

			// to make sure FileOpened actually happened
			Assert.That(File.Exists(indexingTask.Path), Is.False);
		}

		[Test]
		public void When_addition_task_is_canceled_Then_index_is_not_changed()
		{
			const long contentId = 11L;

			_taskProcessor.FileOpened += (sender, task) => task.Cancel();

			var indexingTask = createAdditionTaskForNewFile(contentId, content: "textual file content");
			processTask(indexingTask);

			Assert.That(_indexEngine.Search("textual").ContentIds, Is.EquivalentTo(Enumerable.Empty<long>()));
		}



		private IndexingTask createAdditionTaskForNewFile(long contentId, string content)
		{
			var fileName = _util.CreateFile("file", content: content);
			var rootEntry = new RootEntry<Metadata>(() => new Metadata(SequentialId.None));
			var fileEntry = (FileEntry<Metadata>) rootEntry.Add(EntryType.File, fileName, new Metadata(contentId));
			
			var task = new IndexingTask(IndexingAction.AddContent, fileEntry, CancellationToken.None);
			return task;
		}

		private IndexingTask createRemovalTask(long contentId)
		{
			var fileName = _util.GetFileName("file");
			var rootEntry = new RootEntry<Metadata>(() => new Metadata(SequentialId.None));
			var fileEntry = (FileEntry<Metadata>) rootEntry.Add(EntryType.File, fileName, new Metadata(contentId));
			return new IndexingTask(IndexingAction.RemoveContent, fileEntry, CancellationToken.None);
		}

		private void processTask(IndexingTask task)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			_taskProcessor.ProcessTask(task);

			stopwatch.Stop();
			_log.Debug($"task {task.Action} #{task.ContentId} {task.Path} processed in {stopwatch.ElapsedMilliseconds}ms");
		}



		[SetUp]
		public void Setup()
		{
			_util = new WatchUtility();
			_indexEngine = new LuceneIndexEngine(Path.Combine(_util.WorkingDirectory, "lucene-index"));
			_indexEngine.Initialize();
			_taskProcessor = new IndexingTaskProcessor(_indexEngine);
		}

		[TearDown]
		public void Teardown()
		{
			_indexEngine.Dispose();
			_taskProcessor.Dispose();
			_util.Dispose();
		}

		private LuceneIndexEngine _indexEngine;
		private IndexingTaskProcessor _taskProcessor;
		private WatchUtility _util;

		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
	}
}