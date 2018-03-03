namespace IndexExercise.Index.Lucene
{
	public struct Token : IToken
	{
		/// <summary>
		/// A unit of analyzed text
		/// </summary>
		/// <param name="value">Analyzed value</param>
		/// <param name="startOffset">Position of token in analyzed text</param>
		/// <param name="length">Analyzed value length</param>
		public Token(string value, int startOffset, int length)
		{
			_value = value;
			StartOffset = startOffset;
			Length = length;
		}

		/// <inheritdoc />
		public int StartOffset { get; }

		/// <inheritdoc />
		public IToken SubSequence(int start, int end)
		{
			int length = end - start;
			return new Token(_value.Substring(start, length), StartOffset + start, length);
		}

		/// <inheritdoc />
		public char this[int index] => _value[index];

		/// <inheritdoc />
		public int Length { get; }

		public override string ToString()
		{
			return _value;
		}

		private readonly string _value;
	}
}