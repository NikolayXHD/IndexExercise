using Lucene.Net.Support;

namespace IndexExercise.Index.Lucene
{
	public struct CharSequenceWrapper : ICharSequence
	{
		public CharSequenceWrapper(IToken token)
		{
			_token = token;
		}

		public char this[int index] => _token[index];
		public int Length => _token.Length;

		public ICharSequence SubSequence(int start, int end) => new CharSequenceWrapper(_token.SubSequence(start, end));

		private readonly IToken _token;
	}
}