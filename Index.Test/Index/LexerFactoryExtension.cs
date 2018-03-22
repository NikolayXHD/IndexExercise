using System.Collections.Generic;
using System.IO;

namespace IndexExercise.Index.Test
{
	public static class LexerFactoryExtension
	{
		public static IEnumerable<IToken> Parse(this ILexerFactory factory, TextReader input)
		{
			var lexer = factory.CreateLexer();

			lexer.Reset(input);

			while (lexer.MoveNext())
				yield return lexer.Current;
		}
	}
}