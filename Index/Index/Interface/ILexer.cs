using System.IO;
using System.Threading;

namespace IndexExercise.Index
{
	/// <summary>
	/// Parses a text into a sequence of <see cref="IToken"/>.
	/// Implementations are not required to be thread safe.
	/// </summary>
	public interface ILexer
	{
		void Reset(TextReader input);
		bool MoveNext();
		CancellationToken CancellationToken { get; set; }

		IToken Current { get; }
	}
}