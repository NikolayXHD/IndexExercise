using System.Collections.Generic;
using System.Linq;

namespace IndexExercise.Index
{
	public class FileSearchResult
	{
		public static FileSearchResult Error(string searchQueryError) => new FileSearchResult
		{
			SyntaxError = searchQueryError,
			FileNames = Enumerable.Empty<string>()
		};

		public static FileSearchResult Success(IEnumerable<string> fileNames) => new FileSearchResult
		{
			FileNames = fileNames
		};
		
		public string SyntaxError { get; private set; }
		public IEnumerable<string> FileNames { get; private set; }
		
		public bool IsSyntaxError => SyntaxError != null;
	}
}