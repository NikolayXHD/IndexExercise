using System.Collections.Generic;

namespace IndexExercise.Index
{
	/// <summary>
	/// Creates index implementation - specific <see cref="IQuery"/> objects
	/// </summary>
	public interface IQueryBuilder
	{
		IQuery Boolean(IEnumerable<(BoolOperator Operator, IQuery Subquery)> clauses);

		/// <summary>
		/// A query searching for a specific word in a text
		/// </summary>
		IQuery ValueQuery(string word);

		/// <summary>
		/// A query searching words that occure in a specified sequence withtin a document
		/// </summary>
		IQuery PhraseQuery(IEnumerable<string> phrase);

		/// <summary>
		/// A query searching any word that begins with a specified <see cref="prefix"/>
		/// </summary>
		IQuery PrefixQuery(string prefix);

		/// <summary>
		/// A query expressed in engine - specific search syntax
		/// </summary>
		IQuery EngineSpecificQuery(string query);
	}
}