using System.IO;
using System.Threading;

namespace IndexExercise.Index
{
	public interface ILexer
	{
		void Reset(TextReader input);
		bool MoveNext();
		CancellationToken CancellationToken { get; set; }

		IToken Current { get; }
	}
}