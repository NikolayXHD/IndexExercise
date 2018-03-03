using System.Collections.Generic;
using System.IO;

namespace IndexExercise.Index.Lucene
{
	/// <summary>
	/// Splits the text by words separated by non-word & non-digit & non-in-<see cref="AdditionalWordChars"/>.
	/// Chinese and japanese chars are considered one word each.
	/// </summary>
	public class DefaultLexerFactory : ILexerFactory
	{
		/// <summary>
		/// Splits the text by words separated by non-word & non-digit & non-in-<see cref="AdditionalWordChars"/>.
		/// Chinese and japanese chars are considered one word each.
		/// </summary>
		public DefaultLexerFactory()
		{
		}

		public ILexer CreateLexer()
		{
			return new DefaultLexer
			{
				AdditionalWordChars = AdditionalWordChars,
				MaxWordLength = MaxWordLength,
				IsCaseSensitive = IsCaseSensitive
			};
		}

		internal IEnumerable<IToken> Parse(TextReader input)
		{
			var lexer = CreateLexer();

			lexer.Reset(input);

			while (lexer.MoveNext())
				yield return lexer.Current;
		}

		public HashSet<char> AdditionalWordChars { get; } = new HashSet<char>("_");
		public int MaxWordLength { get; set; } = 256;
		public bool IsCaseSensitive { get; set; }
	}
}