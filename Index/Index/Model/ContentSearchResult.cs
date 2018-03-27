using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexExercise.Index
{
	public class ContentSearchResult
	{
		public static ContentSearchResult Error(IList<string> syntaxErrors)
		{
			if (syntaxErrors == null || syntaxErrors.Count == 0)
				throw new ArgumentException($"{nameof(syntaxErrors)} must not be empty");

			return new ContentSearchResult
			{
				SyntaxErrors = syntaxErrors,
				ContentIds = Enumerable.Empty<long>()
			};
		}

		public static ContentSearchResult Success(IEnumerable<long> contentIds) => new ContentSearchResult
		{
			ContentIds = contentIds,
			SyntaxErrors = Array.Empty<string>()
		};
		
		public IList<string> SyntaxErrors { get; private set; }
		public IEnumerable<long> ContentIds { get; private set; }

		public bool HasSyntaxErrors => SyntaxErrors.Count > 0;
	}
}