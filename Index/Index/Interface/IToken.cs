namespace IndexExercise.Index
{
	/// <summary>
	/// A unit of analyzed text
	/// </summary>
	public interface IToken
	{
		/// <summary>
		/// A char from analyzed value
		/// </summary>
		/// <param name="index">Position relative to this token</param>
		char this[int index] { get; }
		
		/// <summary>
		/// Length of analyzed value
		/// </summary>
		int Length { get; }

		/// <summary>
		/// Position of analyzed value relative to analyzed text
		/// </summary>
		int StartOffset { get; }

		/// <summary>
		/// A part of this token
		/// </summary>
		/// <param name="start">Inclusive left boundary (position) of a part relative to this token</param>
		/// <param name="end">Non inclusive right boundary of a part relative to this token</param>
		/// <returns></returns>
		IToken SubSequence(int start, int end);
	}
}