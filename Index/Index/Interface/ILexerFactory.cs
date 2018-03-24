namespace IndexExercise.Index
{
	/// <summary>
	/// Creates <see cref="ILexer"/> instances to parse text into a sequence of <see cref="IToken"/>
	/// </summary>
	public interface ILexerFactory
	{
		ILexer CreateLexer();
	}
}