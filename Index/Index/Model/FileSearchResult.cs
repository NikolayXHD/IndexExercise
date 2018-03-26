using System;
using System.Collections.Generic;
using System.Linq;
using IndexExercise.Index.Collections;

namespace IndexExercise.Index
{
	public class FileSearchResult
	{
		public static FileSearchResult Error(IList<string> syntaxErrors)
		{
			if (syntaxErrors == null || syntaxErrors.Count == 0)
				throw new ArgumentException($"{nameof(syntaxErrors)} must not be empty");

			return new FileSearchResult
			{
				SyntaxErrors = syntaxErrors,
				FileNames = Enumerable.Empty<string>()
			};
		}

		public static FileSearchResult Success(IEnumerable<string> fileNames) => new FileSearchResult
		{
			FileNames = fileNames,
			SyntaxErrors = Array.Empty<string>()
		};

		public IList<string> SyntaxErrors { get; private set; }
		public IEnumerable<string> FileNames { get; private set; }

		public bool HasSyntaxErrors => SyntaxErrors.Count > 0;
	}
}