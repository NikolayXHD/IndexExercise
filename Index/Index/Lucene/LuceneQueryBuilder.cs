using System;
using System.Collections.Generic;
using System.Linq;
using IndexExercise.Index.Collections;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace IndexExercise.Index.Lucene
{
	/// <inheritdoc />
	public class LuceneQueryBuilder : IQueryBuilder
	{
		public LuceneQueryBuilder(ILexerFactory lexerFactory, string contentFieldName)
		{
			_parser = createQueryParser(lexerFactory, contentFieldName);
			_contentField = contentFieldName;
		}

		/// <inheritdoc />
		public IQuery Boolean(IEnumerable<(BoolOperator Operator, IQuery Subquery)> clauses)
		{
			var booleanQuery = new BooleanQuery();
			var errors = Enumerable.Empty<string>();
			var warnings = Enumerable.Empty<string>();

			var orSubquery = new BooleanQuery();

			foreach (var clause in clauses)
			{
				var subquery = (QueryWrapper) clause.Subquery;

				errors = errors.Concat(subquery.Errors);
				warnings = warnings.Concat(subquery.Warnings);

				if (subquery.LuceneQuery == null)
					continue;

				switch (clause.Operator)
				{
					case BoolOperator.Or:
						orSubquery.Add(subquery.LuceneQuery, Occur.SHOULD);
						break;
					case BoolOperator.And:
						booleanQuery.Add(subquery.LuceneQuery, Occur.MUST);
						break;
					case BoolOperator.Not:
						booleanQuery.Add(subquery.LuceneQuery, Occur.MUST_NOT);
						break;
					default:
						throw new NotSupportedException($"{clause.Operator} is not supported");
				}
			}

			if (orSubquery.Clauses.Count > 0)
				booleanQuery.Add(orSubquery, Occur.MUST);

			if (booleanQuery.Clauses.Count == 0)
				return new QueryWrapper(null, warnings, errors);

			if (booleanQuery.Clauses.All(_ => _.Occur == Occur.MUST_NOT))
			{
				warnings = warnings.Append(getNegativeClauseWarning(booleanQuery));
				booleanQuery.Add(new MatchAllDocsQuery(), Occur.MUST);
			}

			return new QueryWrapper(booleanQuery, warnings, errors);
		}

		/// <inheritdoc />
		public IQuery ValueQuery(string word)
		{
			var query = new TermQuery(new Term(_contentField, word));
			return new QueryWrapper(query);
		}

		/// <inheritdoc />
		public IQuery PhraseQuery(IEnumerable<string> phrase)
		{
			var query = new PhraseQuery();

			foreach (string word in phrase)
				query.Add(new Term(_contentField, word));

			return new QueryWrapper(query);
		}

		/// <inheritdoc />
		public IQuery PrefixQuery(string prefix)
		{
			var query = new PrefixQuery(new Term(_contentField, prefix));
			return new QueryWrapper(query);
		}

		/// <inheritdoc />
		public IQuery EngineSpecificQuery(string query)
		{
			try
			{
				lock (_parser)
				{
					var parsedQuery = _parser.Parse(query);

					var warnings = new List<string>();

					fixNegativeClauses(parsedQuery, warnings);
						
					return new QueryWrapper(parsedQuery, warnings);
				}
			}
			catch (ParseException ex)
			{
				return new QueryWrapper(null, errors: Unit.Sequence(ex.Message));
			}
		}



		private static QueryParser createQueryParser(ILexerFactory lexerFactory, string contentFieldName)
		{
			var lexer = lexerFactory.CreateLexer();
			var analyzer = new GenericAnalyzer(lexer);
			return new QueryParser(LuceneVersion.LUCENE_48, contentFieldName, analyzer)
			{
				AllowLeadingWildcard = true
			};
		}

		private static void fixNegativeClauses(Query query, IList<string> warnings)
		{
			if (!(query is BooleanQuery boolean))
				return;
			
			bool existsPositive = false;
			foreach (var clause in boolean.Clauses)
			{
				if (clause.Occur != Occur.MUST_NOT)
					existsPositive = true;

				fixNegativeClauses(clause.Query, warnings);
			}

			if (!existsPositive)
			{
				warnings.Add(getNegativeClauseWarning(query));
				boolean.Add(new MatchAllDocsQuery(), Occur.MUST);
			}
		}

		private static string getNegativeClauseWarning(Query query)
		{
			return $"{NegativeClauseWarning}: {query}";
		}

		private const string NegativeClauseWarning = "Negative clause may require a full index scan";

		private readonly QueryParser _parser;
		private readonly string _contentField;
	}
}