using Lucene.Net.Support;

namespace IndexExercise.Index.Lucene
{
	public static class TokenExtension
	{
		public static ICharSequence ToCharSequence(this IToken token) => new CharSequenceWrapper(token);
	}
}