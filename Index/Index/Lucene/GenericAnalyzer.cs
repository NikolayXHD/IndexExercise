using System.IO;
using Lucene.Net.Analysis;

namespace IndexExercise.Index.Lucene
{
	public class GenericAnalyzer : Analyzer
	{
		public GenericAnalyzer(ILexer lexer)
		{
			_lexer = lexer;
		}

		protected override TokenStreamComponents CreateComponents(string fieldName, TextReader input)
		{
			var tokenizer = new GenericTokenizer(_lexer, input);
			return new TokenStreamComponents(tokenizer);
		}

		private readonly ILexer _lexer;
	}
}