﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndexExercise.Index.Lucene;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture]
	public class DefaultLexerTests
	{
		[SetUp]
		public void Setup()
		{
			_lexerFactory = new DefaultLexerFactory();
		}

		[TestCase("Spears were 1.5 metres long.", "spears", "were", "1", "5", "metres", "long")]
		[TestCase("Пасхалия на 312—412 годы;", "пасхалия", "на", "312", "412", "годы")] // Russian
		[TestCase("末尾の年表も参照。", "末", "尾", "の", "年", "表", "も", "参", "照")] // Japanese
		[TestCase("術：變化術", "術", "變", "化", "術")] // Chinese
		public void Lexer_splits_text_by_words(string inputString, params string[] expectedTokens)
		{
			var input = new StringReader(inputString);
			var parsedTokens = _lexerFactory.Parse(input).Select(_ => _.ToString()).ToArray();

			Assert.That(parsedTokens, Is.EquivalentTo(expectedTokens));
		}

		[TestCase( /* caseSensitive */ true, "Speed: 210 MPH", "Speed", "210", "MPH")]
		[TestCase( /* caseSensitive */ false, "Speed: 210 MPH", "speed", "210", "mph")]
		public void Lexer_honors_preserving_case_property(bool caseSensitive, string inputString, params string[] expectedTokens)
		{
			var input = new StringReader(inputString);

			_lexerFactory.IsPreservingCase = caseSensitive;
			var parsedToken = _lexerFactory.Parse(input).Select(_ => _.ToString()).ToArray();

			Assert.That(parsedToken, Is.EquivalentTo(expectedTokens));
		}

		[TestCase("some@email.ru", /* additionalWordChars */ "", "some", "email", "ru")]
		[TestCase("some@email.ru", /* additionalWordChars */ "@.", "some@email.ru")]
		public void Lexer_honors_additional_word_chars_property(string inputString, IEnumerable<char> additionalWordChars, params string[] expectedTokens)
		{
			var input = new StringReader(inputString);

			_lexerFactory.AdditionalWordChars.UnionWith(additionalWordChars);
			var tokens = _lexerFactory.Parse(input).Select(_ => _.ToString()).ToArray();

			Assert.That(tokens, Is.EquivalentTo(expectedTokens));
		}



		private DefaultLexerFactory _lexerFactory;
	}
}