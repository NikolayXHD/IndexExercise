using System.IO;
using System.Threading;

namespace IndexExercise.Index.Test
{
	public static class IndexEngineExtension
	{
		public static void Update(this IIndexEngine engine, long contentId, string content)
		{
			engine.Update(contentId, new StringReader(content), CancellationToken.None);
		}

		public static void Remove(this IIndexEngine engine, long contentId)
		{
			engine.Remove(contentId, CancellationToken.None);
		}

		public static ContentSearchResult Search(this IIndexEngine engine, string engineSpecificQuery)
		{
			return engine.Search(engine.QueryBuilder.EngineSpecificQuery(engineSpecificQuery));
		}
	}
}