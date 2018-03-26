namespace IndexExercise.Index
{
	public static class QueryBuilderExtension
	{
		/// <summary>
		/// A query searching words that occure in a specified sequence withtin a document
		/// </summary>
		public static IQuery PhraseQuery(this IQueryBuilder builder, params string[] phrase) =>
			builder.PhraseQuery(phrase);

		public static IQuery Boolean(this IQueryBuilder builder, params (BoolOperator Operator, IQuery Subquery)[] clauses) =>
			builder.Boolean(clauses);
	}
}