using System;
using System.IO;
using System.Linq;
using IndexExercise.Index.Lucene;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture]
	public class LuceneEngineQueriesTests
	{
		[Test]
		public void When_search_query_is_invalid_Then_search_result_is_syntax_error()
		{
			var searchResult = _indexEngine.Search("phrase (");
			Assert.That(searchResult.HasSyntaxErrors, Is.True);
		}

		[TestCase("phrase number one", "second sentence", "number", 1L)]
		[TestCase("phrase number one", "second sentence", "second", 2L)]
		[TestCase("phrase number one", "second sentence", "second OR phrase", 1L, 2L)]
		public void Index_engine_finds_indexed_word(string content1, string content2, string searchQuery, params long[] expectedSearchResult)
		{
			const long contentId1 = 1L;
			const long contentId2 = 2L;

			_indexEngine.Update(contentId1, content1);
			_indexEngine.Update(contentId2, content2);

			var contentIds = _indexEngine.Search(searchQuery).ContentIds.ToArray();
			Assert.That(contentIds, Is.EquivalentTo(expectedSearchResult));
		}

		[TestCase("1", 1L, 2L, 3L, 4L)]
		[TestCase("2", 1L, 2L, 3L)]
		[TestCase("3", 2L, 3L)]
		[TestCase("4", 3L, 4L)]
		public void Value_query_returns_expected_result(string queriedValue, params long[] expectedResult)
		{
			_indexEngine.Update(1L, content: "1 2    ");
			_indexEngine.Update(2L, content: "1 2 3  ");
			_indexEngine.Update(3L, content: "1 2 3 4");
			_indexEngine.Update(4L, content: "1     4");

			var query = _indexEngine.QueryBuilder.ValueQuery(queriedValue);

			Assert.That(_indexEngine.Search(query).ContentIds, Is.EquivalentTo(expectedResult));
		}

		[TestCase("12", 1L, 2L, 3L)]
		[TestCase("14", 4L)]
		[TestCase("2")]
		public void Prefix_query_returns_expected_result(string queriedValue, params long[] expectedResult)
		{
			_indexEngine.Update(1L, content: "12  ");
			_indexEngine.Update(2L, content: "123 ");
			_indexEngine.Update(3L, content: "1234");
			_indexEngine.Update(4L, content: "14  ");

			var query = _indexEngine.QueryBuilder.PrefixQuery(queriedValue);
			Assert.That(_indexEngine.Search(query).ContentIds, Is.EquivalentTo(expectedResult));
		}

		[TestCase("1  ", 1L, 2L, 3L, 4L)]
		[TestCase("1 2", 1L, 2L, 3L)]
		[TestCase("2 3", 2L, 3L)]
		[TestCase("1 4", 4L)]
		[TestCase("1 3")]
		public void Phrase_query_returns_expected_result(string phraseQuery, params long[] expectedResult)
		{
			_indexEngine.Update(1L, content: "1 2    ");
			_indexEngine.Update(2L, content: "1 2 3  ");
			_indexEngine.Update(3L, content: "1 2 3 4");
			_indexEngine.Update(4L, content: "1     4");

			var phraseValues = phraseQuery.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			var query = _indexEngine.QueryBuilder.PhraseQuery(phraseValues);
			Assert.That(_indexEngine.Search(query).ContentIds, Is.EquivalentTo(expectedResult));
		}

		[Test]
		public void Conjunction_query_returns_expected_result()
		{
			_indexEngine.Update(1L, content: "1 2    ");
			_indexEngine.Update(2L, content: "1 2 3  ");
			_indexEngine.Update(3L, content: "1 2 3 4");
			_indexEngine.Update(4L, content: "1     4");

			var builder = _indexEngine.QueryBuilder;

			var query = builder.Boolean(
				(BoolOperator.And, builder.ValueQuery("3")),
				(BoolOperator.And, builder.ValueQuery("4")));

			Assert.That(_indexEngine.Search(query).ContentIds, Is.EquivalentTo(new[] { 3L }));
		}

		[Test]
		public void Disjunction_query_returns_expected_result()
		{
			_indexEngine.Update(1L, content: "1 2    ");
			_indexEngine.Update(2L, content: "1 2 3  ");
			_indexEngine.Update(3L, content: "1 2 3 4");
			_indexEngine.Update(4L, content: "1     4");

			var builder = _indexEngine.QueryBuilder;

			var query = builder.Boolean(
					(BoolOperator.Or, builder.ValueQuery("3")),
					(BoolOperator.Or, builder.ValueQuery("4")));

			Assert.That(_indexEngine.Search(query).ContentIds, Is.EquivalentTo(new[] { 2L, 3L, 4L }));
		}

		[Test]
		public void Negation_query_returns_expected_result()
		{
			_indexEngine.Update(1L, content: "1 2    ");
			_indexEngine.Update(2L, content: "1 2 3  ");
			_indexEngine.Update(3L, content: "1 2 3 4");
			_indexEngine.Update(4L, content: "1     4");

			var builder = _indexEngine.QueryBuilder;

			var query = builder.Boolean(
					(BoolOperator.And, builder.ValueQuery("2")),
					(BoolOperator.Not, builder.ValueQuery("4")));

			Assert.That(_indexEngine.Search(query).ContentIds, Is.EquivalentTo(new[] { 1L, 2L }));
		}

		[Test]
		public void Purely_negative_query_contains_warning()
		{
			var builder = _indexEngine.QueryBuilder;

			var query = builder.Boolean((BoolOperator.Not, builder.ValueQuery("1")));

			Assert.That(query.Warnings.Count > 0);
		}

		[Test]
		public void Purely_negative_query_from_engine_specific_syntax_contains_warning()
		{
			var query = _indexEngine.QueryBuilder
				.EngineSpecificQuery("NOT 1");

			Assert.That(query.Warnings.Count > 0);
		}

		[Test]
		public void Purely_negative_query_returns_expected_result()
		{
			_indexEngine.Update(1L, content: "1 2    ");
			_indexEngine.Update(2L, content: "1 2 3  ");
			_indexEngine.Update(3L, content: "1 2 3 4");
			_indexEngine.Update(4L, content: "1     4");

			var builder = _indexEngine.QueryBuilder;

			var query = builder.Boolean((BoolOperator.Not, builder.ValueQuery("2")));

			Assert.That(_indexEngine.Search(query).ContentIds, Is.EquivalentTo(new[] { 4L }));
		}

		[Test]
		public void Single_disjunction_subquery_is_interpreted_as_conjunction()
		{
			_indexEngine.Update(1L, content: "1 2    ");
			_indexEngine.Update(2L, content: "1 2 3  ");
			_indexEngine.Update(3L, content: "1 2 3 4");
			_indexEngine.Update(4L, content: "1     4");

			var builder = _indexEngine.QueryBuilder;

			var query = builder.Boolean(
				(BoolOperator.And, builder.ValueQuery("3")),
				(BoolOperator.Or, builder.ValueQuery("4")));

			Assert.That(_indexEngine.Search(query).ContentIds, Is.EquivalentTo(new[] { 3L }));
		}

		[Test]
		public void Multiple_disjunction_subqueries_are_conjuncted_as_a_group_to_others()
		{
			_indexEngine.Update(1L, content: "1 2    ");
			_indexEngine.Update(2L, content: "1 2 3  ");
			_indexEngine.Update(3L, content: "1 2 3 4");
			_indexEngine.Update(4L, content: "1     4");

			var builder = _indexEngine.QueryBuilder;

			var query = builder.Boolean(
					(BoolOperator.And, builder.ValueQuery("2")),
					(BoolOperator.Or, builder.ValueQuery("3")),
					(BoolOperator.Or, builder.ValueQuery("4")));

			Assert.That(_indexEngine.Search(query).ContentIds, Is.EquivalentTo(new[] { 2L, 3L }));
		}

		[Test]
		public void Regular_expression_query_returns_expected_result()
		{
			_indexEngine.Update(1L, content: "first one unit");
			_indexEngine.Update(2L, content: "second two pair");

			Assert.That(_indexEngine.Search("/f?irst/").ContentIds, Is.EquivalentTo(new[] { 1L }));
			Assert.That(_indexEngine.Search("/pa.+/").ContentIds, Is.EquivalentTo(new[] { 2L }));
			Assert.That(_indexEngine.Search("/.irst|p[ao]ir/").ContentIds, Is.EquivalentTo(new[] { 1L, 2L }));
		}

		[SetUp]
		public void Setup()
		{
			_util = new FileSystemUtility();
			_indexEngine = new LuceneIndexEngine(Path.Combine(_util.WorkingDirectory, "lucene-index"));
			_indexEngine.Initialize();
		}

		[TearDown]
		public void Teardown()
		{
			_indexEngine.Dispose();
			_util.Dispose();
		}

		private FileSystemUtility _util;
		private LuceneIndexEngine _indexEngine;
	}
}