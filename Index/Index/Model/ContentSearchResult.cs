using System.Collections.Generic;
using System.Linq;

namespace IndexExercise.Index
{
	public class ContentSearchResult
	{
		public static ContentSearchResult Error(string searchQueryError) => new ContentSearchResult
		{
			SyntaxError = searchQueryError,
			ContentIds = Enumerable.Empty<long>()
		};

		public static ContentSearchResult Success(IEnumerable<long> contentIds) => new ContentSearchResult
		{
			ContentIds = contentIds
		};
		
		public string SyntaxError { get; private set; }
		public IEnumerable<long> ContentIds { get; private set; }

		public bool IsSyntaxError => SyntaxError != null;
	}
}