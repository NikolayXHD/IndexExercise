using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;

namespace IndexExercise.Index.Lucene
{
	public sealed class GenericTokenizer : Tokenizer
	{
		public GenericTokenizer(ILexer lexer, TextReader input) : base(input)
		{
			_lexer = lexer;
			_termAttribute = AddAttribute<ICharTermAttribute>();
			_offsetAttribute = AddAttribute<IOffsetAttribute>();
		}

		public override bool IncrementToken()
		{
			ClearAttributes();
			
			if (!_lexer.MoveNext())
				return false;

			var token = _lexer.Current;

			_termAttribute.SetEmpty();
			_termAttribute.Append(token.ToCharSequence());
			_offsetAttribute.SetOffset(token.StartOffset, token.StartOffset + token.Length);

			return true;
		}

		public override void Reset()
		{
			base.Reset();
			_lexer.Reset(m_input);
		}



		private readonly ILexer _lexer;
		private readonly ICharTermAttribute _termAttribute;
		private readonly IOffsetAttribute _offsetAttribute;
	}
}