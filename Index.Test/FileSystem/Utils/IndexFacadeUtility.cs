using System;
using System.Threading.Tasks;
using IndexExercise.Index.FileSystem;
using IndexExercise.Index.Lucene;

namespace IndexExercise.Index.Test
{
	public class IndexFacadeUtility : MirrorUtility
	{
		public IndexFacadeUtility()
		{
			string indexDirectory = CreateDirectory("lucene-index", parent: TempDirectory);
			var indexEngine = new LuceneIndexEngine(indexDirectory);
			var indexingTaskProcessor = new IndexingTaskProcessor(indexEngine);
			
			_indexFacade = new IndexFacade(Watcher, Mirror, indexingTaskProcessor, indexEngine)
			{
				IdleDelay = TimeSpan.FromMilliseconds(10),
				ThrottleDelay = TimeSpan.FromMilliseconds(100)
			};

			_indexFacade.Idle += indexFacadeIdle;
			_indexFacade.BeginProcessingTask += beginProcessingTask;
			_indexFacade.EndProcessingTask += endProcessingTask;

			indexingTaskProcessor.FileOpened += indexingTaskProcessorFileOpened;
		}

		private static void indexingTaskProcessorFileOpened(object sender, IndexingTask task)
		{
			if (task.HardlinkPath == null)
				Log.Error($"indexing failed to create hardlink to {task.Path}. Indexing in original location");
			else
				Log.Debug($"indexing {task.Path} at hardlink {task.HardlinkPath}");
		}

		public void StartIndexFacade()
		{
			_indexFacade.Watch(new WatchTarget(EntryType.Directory, WorkingDirectory));
			_indexFacade.RunAsync();
		}

		public FileSearchResult Search(string query)
		{
			return _indexFacade.Search(_indexFacade.QueryBuilder.EngineSpecificQuery(query));
		}

		public override void Dispose()
		{
			_indexFacade.Dispose();
			base.Dispose();
		}

		private static void beginProcessingTask(object sender, IndexingTask task)
		{
			Log.Debug($"facade begin processing {task.Action} #{task.FileEntry.Data.ContentId} length:{task.FileEntry.Data.Length}b attempt:{task.Attempts} {task.Path}");
		}

		private static void endProcessingTask(object sender, IndexingTask task)
		{
			Log.Debug($"facade end processing {task.Action} #{task.FileEntry.Data.ContentId} length:{task.FileEntry.Data.Length}b attempt:{task.Attempts} {task.Path}");
		}

		private static void indexFacadeIdle(object sender, TimeSpan delay)
		{
			Log.Debug($"facade idle {(int) delay.TotalMilliseconds} ms");
		}

		public async Task ThrottleDelay()
		{
			Log.Debug($"delay start {(int) _indexFacade.ThrottleDelay.TotalMilliseconds} ms (same as throttle)");
			await Task.Delay(_indexFacade.ThrottleDelay);
			Log.Debug("delay end (same as throttle)");
		}

		private readonly IndexFacade _indexFacade;
	}
}