using System.Collections.Generic;

namespace IndexExercise.Index
{
	/// <summary>
	/// Creates index implementation - specific <see cref="IQuery"/> objects
	/// </summary>
	public interface IQueryBuilder
	{
		IQueryBuilder Boolean(params (BoolOperator BooleanOperator, IQueryBuilder Subquery)[] subqueries);

		/// <summary>
		/// A query searching for a specific word in a text
		/// </summary>
		IQueryBuilder ValueQuery(string word);

		/// <summary>
		/// A query searching words that occure in a specified sequence withtin a document
		/// </summary>
		IQueryBuilder PhraseQuery(IEnumerable<string> phrase);

		/// <summary>
		/// A query searching any word that begins with a specified <see cref="prefix"/>
		/// </summary>
		IQueryBuilder PrefixQuery(string prefix);

		/// <summary>
		/// A query expressed in engine - specific search syntax
		/// </summary>
		IQueryBuilder EngineSpecificQuery(string query);

		/// <summary>
		/// Create a query from the current state of <see cref="IQueryBuilder"/>
		/// </summary>
		IQuery Build();
	}
}