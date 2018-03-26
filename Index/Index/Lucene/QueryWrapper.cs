using System.Collections.Generic;
using System.Linq;
using IndexExercise.Index.Collections;
using Lucene.Net.Search;

namespace IndexExercise.Index.Lucene
{
	internal struct QueryWrapper : IQuery
	{
		public QueryWrapper(Query luceneQuery, IEnumerable<string> warnings = null, IEnumerable<string> errors = null)
		{
			warnings = warnings ?? Enumerable.Empty<string>();
			errors = errors ?? Enumerable.Empty<string>();

			LuceneQuery = luceneQuery;
			Errors = new List<string>(errors);
			Warnings = new List<string>(warnings);
		}

		public Query LuceneQuery { get; }
		public IList<string> Errors { get; }
		public IList<string> Warnings { get; }
	}
}