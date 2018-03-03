using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace IndexExercise.Index.Lucene
{
	/// <summary>
	/// Splits the text by words separated by non-word & non-digit & non-in-<see cref="AdditionalWordChars"/>.
	/// Chinese and japanese chars are considered one word each.
	/// </summary>
	public class DefaultLexer : ILexer
	{
		/// <summary>
		/// Splits the text by words separated by non-word & non-digit & non-in-<see cref="AdditionalWordChars"/>.
		/// Chinese and japanese chars are considered one word each.
		/// </summary>
		public DefaultLexer()
		{
			MaxWordLength = 256;
		}



		public void Reset(TextReader input)
		{
			_input = input;
			_offset = _bufferIndex = _dataLen = 0;
		}

		public bool MoveNext()
		{
			Current = default(Token);

			_length = 0;
			_start = _offset;

			while (true)
			{
				_offset++;

				if (_bufferIndex >= _dataLen)
				{
					if (CancellationToken.IsCancellationRequested)
						_dataLen = 0;
					else
						_dataLen = _input.Read(_buffer, 0, _buffer.Length);

					_bufferIndex = 0;
				}

				if (_dataLen <= 0)
				{
					_offset--;
					return flush();
				}

				char c = _buffer[_bufferIndex++];

				bool isSeparator = !char.IsLetterOrDigit(c) && !AdditionalWordChars.Contains(c);

				if (isSeparator)
				{
					if (_length > 0)
						return flush();

					continue;
				}

				if (c.IsCj())
				{
					// Chinese and Japanese words do not require whitespace separator
					if (_length > 0)
					{
						_bufferIndex--;
						_offset--;
						return flush();
					}

					push(c);
					return flush();
				}

				push(c);
				if (_length == _wordBuffer.Length)
					return flush();
			}
		}

		public IToken Current { get; private set; }



		private void push(char c)
		{
			if (_length == 0)
				_start = _offset - 1;

			if (!IsCaseSensitive)
				c = char.ToLower(c);

			_wordBuffer[_length++] = c;
		}

		private bool flush()
		{
			if (_length == 0)
				return false;

			Current = new Token(
				new string(_wordBuffer, 0, _length),
				_start,
				_length);

			return true;
		}



		public CancellationToken CancellationToken { get; set; }
		public HashSet<char> AdditionalWordChars { get; set; } = new HashSet<char>("_");

		public int MaxWordLength
		{
			get => _wordBuffer.Length;
			set => _wordBuffer = new char[value];
		}

		public bool IsCaseSensitive { get; set; }


		private TextReader _input;

		private int _offset;
		private int _bufferIndex;
		private int _dataLen;

		private int _length;
		private int _start;

		private char[] _wordBuffer;
		private readonly char[] _buffer = new char[BufferSize];

		private const int BufferSize = 1024;
	}
}