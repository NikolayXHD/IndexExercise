using System.Collections.Generic;

namespace IndexExercise.Index.Demo
{
	public class FixedSearchResult
	{
		public HashSet<string> FileNames { get; }
		public IList<string> SyntaxErrors { get; }

		public bool HasSyntaxErrors => SyntaxErrors.Count > 0;

		public FixedSearchResult(FileSearchResult fileSearchResult)
		{
			FileNames = new HashSet<string>(fileSearchResult.FileNames);
			SyntaxErrors = fileSearchResult.SyntaxErrors;
		}

		public static FixedSearchResult Empty { get; } = new FixedSearchResult(FileSearchResult.Empty);
	}
}