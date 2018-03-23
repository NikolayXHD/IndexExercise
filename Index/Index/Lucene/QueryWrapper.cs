using System.Collections.Generic;
using Lucene.Net.Search;

namespace IndexExercise.Index.Lucene
{
	internal struct QueryWrapper : IQuery
	{
		public QueryWrapper(Query luceneQuery, IEnumerable<string> syntaxErrors, IEnumerable<string> warnings)
		{
			LuceneQuery = luceneQuery;
			SyntaxErrors = new List<string>(syntaxErrors);
			Warnings = new List<string>(warnings);
		}

		public Query LuceneQuery { get; }
		public IList<string> SyntaxErrors { get; }
		public IList<string> Warnings { get; }

		public bool HasSyntaxErrors => SyntaxErrors.Count > 0;
	}
}