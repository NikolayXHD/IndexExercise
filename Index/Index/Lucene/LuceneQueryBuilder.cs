using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace IndexExercise.Index.Lucene
{
	/// <inheritdoc />
	public class LuceneQueryBuilder : IQueryBuilder
	{
		public LuceneQueryBuilder(ILexerFactory lexerFactory, string contentFieldName) : this(
			createMatchNoDocsQuery(),
			createQueryParser(lexerFactory, contentFieldName),
			contentFieldName,
			errors: Enumerable.Empty<string>())
		{
		}

		private LuceneQueryBuilder(Query query, QueryParser parser, string contentField, IEnumerable<string> errors)
		{
			Query = query;
			_parser = parser;
			_contentField = contentField;
			SyntaxErrors = new List<string>(errors);
		}

		/// <inheritdoc />
		public IQueryBuilder Boolean(params (BoolOperator BooleanOperator, IQueryBuilder Subquery)[] subqueries)
		{
			if (subqueries.Length == 0)
				throw new ArgumentException($"{nameof(subqueries)} must have at least 1 element", nameof(subqueries));

			var query = new BooleanQuery();
			var syntaxErrors = Enumerable.Empty<string>();

			var orSubquery = new BooleanQuery();

			foreach (var subquery in subqueries)
			{
				var subqueryBuilder = (LuceneQueryBuilder) subquery.Subquery;

				if (subqueryBuilder.HasSyntaxErrors)
					syntaxErrors = syntaxErrors.Concat(subqueryBuilder.SyntaxErrors);

				if (subqueryBuilder.Query == null)
					continue;

				switch (subquery.BooleanOperator)
				{
					case BoolOperator.Or:
						orSubquery.Add(subqueryBuilder.Query, Occur.SHOULD);
						break;
					case BoolOperator.And:
						query.Add(subqueryBuilder.Query, Occur.MUST);
						break;
					case BoolOperator.Not:
						query.Add(subqueryBuilder.Query, Occur.MUST_NOT);
						break;
					default:
						throw new NotSupportedException($"{subquery.BooleanOperator} is not supported");
				}
			}

			if (orSubquery.Clauses.Count > 0)
				query.Add(orSubquery, Occur.MUST);

			if (query.Clauses.Count == 0)
				return createBuilder(syntaxErrors);

			return createBuilder(query, syntaxErrors);
		}

		/// <inheritdoc />
		public IQueryBuilder ValueQuery(string word)
		{
			var query = new TermQuery(new Term(_contentField, word));
			return createBuilder(query);
		}

		/// <inheritdoc />
		public IQueryBuilder PhraseQuery(IEnumerable<string> phrase)
		{
			var query = new PhraseQuery();

			foreach (string word in phrase)
				query.Add(new Term(_contentField, word));

			return createBuilder(query);
		}

		/// <inheritdoc />
		public IQueryBuilder PrefixQuery(string prefix)
		{
			var query = new PrefixQuery(new Term(_contentField, prefix));
			return createBuilder(query);
		}

		/// <inheritdoc />
		public IQueryBuilder EngineSpecificQuery(string query)
		{
			try
			{
				lock (_parser)
				{
					var parsedQuery = _parser.Parse(query);
					return createBuilder(parsedQuery);
				}
			}
			catch (ParseException ex)
			{
				return createBuilder(ex.Message);
			}
		}

		/// <inheritdoc />
		public IQuery Build()
		{
			var warnings = new List<string>();
			if (fixNegativeClauses(Query))
				warnings.Add("Query contains purely negative boolean clauses. Search may require a full index scan.");

			return new QueryWrapper(Query, SyntaxErrors, warnings);
		}



		private LuceneQueryBuilder createBuilder(Query query)
		{
			return new LuceneQueryBuilder(query, _parser, _contentField, Enumerable.Empty<string>());
		}

		private LuceneQueryBuilder createBuilder(string syntaxError)
		{
			// ReSharper disable once InconsistentlySynchronizedField
			return new LuceneQueryBuilder(null, _parser, _contentField, Enumerable.Repeat(syntaxError, 1));
		}

		private LuceneQueryBuilder createBuilder(Query query, IEnumerable<string> syntaxErrors)
		{
			// ReSharper disable once InconsistentlySynchronizedField
			return new LuceneQueryBuilder(query, _parser, _contentField, syntaxErrors);
		}

		private LuceneQueryBuilder createBuilder(IEnumerable<string> syntaxErrors)
		{
			// ReSharper disable once InconsistentlySynchronizedField
			return new LuceneQueryBuilder(null, _parser, _contentField, syntaxErrors);
		}

		private static QueryParser createQueryParser(ILexerFactory lexerFactory, string contentFieldName)
		{
			var lexer = lexerFactory.CreateLexer();
			var analyzer = new GenericAnalyzer(lexer);
			return new QueryParser(LuceneVersion.LUCENE_48, contentFieldName, analyzer);
		}

		private static BooleanQuery createMatchNoDocsQuery()
		{
			// A Boolean query without any clause returns no documents
			return new BooleanQuery();
		}

		private static bool fixNegativeClauses(Query query)
		{
			bool queryFixed = false;

			if (!(query is BooleanQuery boolean))
			{
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				return queryFixed;
			}

			bool existsPositive = false;
			foreach (var clause in boolean.Clauses)
			{
				if (clause.Occur != Occur.MUST_NOT)
					existsPositive = true;

				queryFixed |= fixNegativeClauses(clause.Query);
			}

			if (!existsPositive)
			{
				boolean.Add(new MatchAllDocsQuery(), Occur.MUST);
				queryFixed = true;
			}

			return queryFixed;
		}



		private bool HasSyntaxErrors => SyntaxErrors.Count > 0;
		private IList<string> SyntaxErrors { get; }
		private Query Query { get; }

		private readonly QueryParser _parser;
		private readonly string _contentField;
	}
}