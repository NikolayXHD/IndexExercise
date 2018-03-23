using System;
using System.IO;
using System.Threading;

namespace IndexExercise.Index
{
	/// <summary>
	/// The engine providing indexing and search functionality.
	/// Concurrent multiple <see cref="Search"/> operations must be supported simulteneously with
	/// at least one write opration <see cref="Update"/> or <see cref="Remove"/>.
	/// Concurrent multiple writes <see cref="Update"/> and <see cref="Remove"/> may not be supported.
	/// </summary>
	public interface IIndexEngine : IDisposable
	{
		void Update(long contentId, TextReader input, CancellationToken cancellationToken);
		void Remove(long contentId, CancellationToken cancellationToken);
		
		/// <summary>
		/// Searches indexed content based on <see cref="IQuery"/> created by <see cref="QueryBuilder"/>
		/// </summary>
		ContentSearchResult Search(IQuery query);

		IQueryBuilder QueryBuilder { get; }

		void Initialize();
	}
}