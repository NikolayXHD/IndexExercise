using System;
using IndexExercise.Index.FileSystem;
using IndexExercise.Index.Lucene;

namespace IndexExercise.Index.Test
{
	public class IndexFacadeUtility : MirrorUtility
	{
		public IndexFacadeUtility()
		{
			string indexDirectory = CreateDirectory("lucene-net-index", parent: TempDirectory);
			var indexEngine = new LuceneIndexEngine(indexDirectory);

			IndexFacade = new IndexFacade(Mirror, new IndexingTaskProcessor(indexEngine), indexEngine);

			IndexFacade.IdleDelay = TimeSpan.FromMilliseconds(10);
			IndexFacade.Idle += indexFacadeIdle;
			IndexFacade.BeginProcessingTask += beginProcessingTask;
			IndexFacade.EndProcessingTask += endProcessingTask;
		}

		public void StartIndexFacade()
		{
			IndexFacade.Watch(new WatchTarget(EntryType.Directory, WorkingDirectory));
			IndexFacade.Start();
		}

		public FileSearchResult Search(string query)
		{
			var parsedQuery = IndexFacade.QueryBuilder.EngineSpecificQuery(query).Build();
			return IndexFacade.Search(parsedQuery);
		}

		public override void Dispose()
		{
			IndexFacade.Dispose();
			base.Dispose();
		}

		private void beginProcessingTask(object sender, IndexingTask task)
		{
			Log.Debug($"begin processing index {task.Action} #{task.ContentId} length:{task.Length}b attempt:{task.Attempts} {task.Path}");
		}

		private void endProcessingTask(object sender, IndexingTask task)
		{
			Log.Debug($"end processing index {task.Action} #{task.ContentId} length:{task.Length}b attempt:{task.Attempts} {task.Path}");
		}

		private void indexFacadeIdle(object sender, TimeSpan delay)
		{
			Log.Debug($"indexer idle {(int) delay.TotalMilliseconds} ms");
		}


		private IndexFacade IndexFacade { get; }
	}
}