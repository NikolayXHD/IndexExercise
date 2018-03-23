namespace IndexExercise.Index.Test
{
	public static class IndexFacadeExtension
	{
		public static FileSearchResult Search(this IndexFacade facade, string engineSpecificQuery)
		{
			var query = facade.QueryBuilder
				.EngineSpecificQuery(engineSpecificQuery)
				.Build();

			return facade.Search(query);
		}
	}
}