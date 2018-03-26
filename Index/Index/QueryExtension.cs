namespace IndexExercise.Index
{
	public static class QueryExtension
	{
		public static bool HasErrors(this IQuery query) => query.Errors.Count > 0;
	}
}