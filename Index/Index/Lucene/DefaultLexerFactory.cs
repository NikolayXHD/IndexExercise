using System.Collections.Generic;

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
				IsPreservingCase = IsPreservingCase
			};
		}

		public HashSet<char> AdditionalWordChars { get; } = new HashSet<char>("_");
		public int MaxWordLength { get; set; } = 256;
		public bool IsPreservingCase { get; set; }
	}
}