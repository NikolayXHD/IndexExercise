using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndexExercise.Index.Lucene;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture]
	public class LuceneIndexEngineTests
	{
		[TestCase("phrase number one", "second sentence", "number", 1L)]
		[TestCase("phrase number one", "second sentence", "second", 2L)]
		[TestCase("phrase number one", "second sentence", "second OR phrase", 1L, 2L)]
		public void Index_engine_finds_indexed_word(string content1, string content2, string searchQuery, params long[] expectedSearchResult)
		{
			const long contentId1 = 1;
			const long contentId2 = 2;

			_indexEngine.Update(contentId1, content1);
			_indexEngine.Update(contentId2, content2);

			var contentIds = _indexEngine.Search(searchQuery).ContentIds.ToArray();
			Assert.That(contentIds, Is.EquivalentTo(expectedSearchResult));
		}

		[Test]
		public void When_search_query_is_invalid_Then_search_result_is_syntax_error()
		{
			var searchResult = _indexEngine.Search("phrase (");
			Assert.That(searchResult.IsSyntaxError, Is.True);
		}

		[Test]
		public void When_content_is_removed_Then_search_result_becomes_empty()
		{
			const long contentId = 11;

			_indexEngine.Update(contentId, content: "phrase to be searched");

			Assert.That(_indexEngine.Search("phrase").ContentIds, Is.EquivalentTo(Enumerable.Repeat(contentId, 1)));

			_indexEngine.Remove(contentId);

			Assert.That(_indexEngine.Search("phrase").ContentIds, Is.EquivalentTo(Enumerable.Empty<long>()));
		}

		[Test]
		public void When_content_is_changed_Then_search_result_changes()
		{
			const long contentId = 1;

			_indexEngine.Update(contentId, content: "original phrase");

			Assert.That(_indexEngine.Search("original").ContentIds, Is.EquivalentTo(Enumerable.Repeat(contentId, 1)));
			Assert.That(_indexEngine.Search("changed").ContentIds, Is.EquivalentTo(Enumerable.Empty<long>()));
			Assert.That(_indexEngine.Search("phrase").ContentIds, Is.EquivalentTo(Enumerable.Repeat(contentId, 1)));

			_indexEngine.Update(contentId, content: "changed phrase");

			Assert.That(_indexEngine.Search("original").ContentIds, Is.EquivalentTo(Enumerable.Empty<long>()));
			Assert.That(_indexEngine.Search("changed").ContentIds, Is.EquivalentTo(Enumerable.Repeat(contentId, 1)));
			Assert.That(_indexEngine.Search("phrase").ContentIds, Is.EquivalentTo(Enumerable.Repeat(contentId, 1)));
		}

		[Explicit("probabilistic")]
		[TestCase( /* updateCyclesCount */ 10)]
		public void When_concurrently_reading_and_writing_Then_all_and_only_correct_results_are_returned(int updateCyclesCount)
		{
			var queries = readAndWriteConcurrently(updateCyclesCount);

			foreach (var query in queries)
			{
				assertAllActualResultsAreCorrect(query.Query, query.ExpectedResults, query.ActualResults);

				// higly probable but not guaranteed
				assertAllPossibleCorrectResultOccured(query.Query, query.ExpectedResults, query.ActualResults);
			}
		}

		[TestCase( /* updateCyclesCount */ 10)]
		public void When_concurrently_reading_and_writing_Then_all_search_results_are_correct(int updateCyclesCount)
		{
			var queries = readAndWriteConcurrently(updateCyclesCount);

			foreach (var query in queries)
				assertAllActualResultsAreCorrect(query.Query, query.ExpectedResults, query.ActualResults);
		}

		private (string Query, HashSet<long>[] ExpectedResults, List<HashSet<long>> ActualResults)[] readAndWriteConcurrently(int updateCyclesCount)
		{
			const long id1 = 1L;
			const long id2 = 2L;

			var empty = new HashSet<long>();
			var id1Only = new HashSet<long> { id1 };
			var id2Only = new HashSet<long> { id2 };
			var both = new HashSet<long> { id1, id2 };

			var id1OrEmpty = new[] { empty, id1Only };
			var id2OrEmpty = new[] { empty, id2Only };
			var anyResult = new[] { empty, id1Only, id2Only, both };
			var alwaysEmpty = new[] { empty };

			var queries = new (string Query, HashSet<long>[] ExpectedResults, List<HashSet<long>> ActualResults)[]
			{
				("firstword1", id1OrEmpty, new List<HashSet<long>>()),
				("firstword1 AND secondword1", id1OrEmpty, new List<HashSet<long>>()),
				("firstword1 OR secondword1", id1OrEmpty, new List<HashSet<long>>()),

				("firstword2", id2OrEmpty, new List<HashSet<long>>()),
				("firstword2 AND secondword2", id2OrEmpty, new List<HashSet<long>>()),
				("firstword2 OR secondword2", id2OrEmpty, new List<HashSet<long>>()),

				("firstword1 OR secondword2", anyResult, new List<HashSet<long>>()),
				("firstword1 AND secondword2", alwaysEmpty, new List<HashSet<long>>())
			};

			var parallelActions = new List<Action>();

			for (int i = 0; i < updateCyclesCount; i++)
			{
				parallelActions.Add(() => _indexEngine.Update(id1, new StringReader(string.Join(" ", "firstword1 secondword1 secondword1")), CancellationToken.None));
				parallelActions.Add(() => _indexEngine.Remove(id1, CancellationToken.None));
				parallelActions.Add(() => _indexEngine.Update(id2, new StringReader(string.Join(" ", "firstword2 firstword2 secondword2")), CancellationToken.None));
				parallelActions.Add(() => _indexEngine.Remove(id2, CancellationToken.None));

				foreach (var query in queries)
					parallelActions.Add(() => query.ActualResults.Add(new HashSet<long>(_indexEngine.Search(query.Query).ContentIds)));
			}

			shuffle(parallelActions);

			Parallel.Invoke(parallelActions.ToArray());
			return queries;
		}

		private static void assertAllPossibleCorrectResultOccured(string query, HashSet<long>[] expectedResults, List<HashSet<long>> actualResults)
		{
			foreach (var expectedResult in expectedResults)
			{
				var matchingActualResult = actualResults.FirstOrDefault(r => expectedResult.SetEquals(r));

				Assert.That(
					matchingActualResult,
					Is.Not.Null,
					() => $"\"{query}\" never returned expected result [{string.Join(",", expectedResult)}]");
			}
		}

		private static void assertAllActualResultsAreCorrect(string query, HashSet<long>[] expectedResults, List<HashSet<long>> actualResults)
		{
			foreach (var actualResult in actualResults)
			{
				var matchingExpectedResult = expectedResults.FirstOrDefault(r => actualResult.SetEquals(r));

				Assert.That(
					matchingExpectedResult,
					Is.Not.Null,
					() => $"\"{query}\" returned unexpected result [{string.Join(",", actualResult)}]");
			}
		}

		private void shuffle<T>(IList<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = _random.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}


		[SetUp]
		public void Setup()
		{
			_util = new FileSystemUtility();
			_indexEngine = new LuceneIndexEngine(Path.Combine(_util.WorkingDirectory, "lucene-net-index"));
			_indexEngine.Initialize();
			_random = new Random();
		}

		[TearDown]
		public void Teardown()
		{
			_indexEngine.Dispose();
			_util.Dispose();
		}

		private FileSystemUtility _util;
		private LuceneIndexEngine _indexEngine;
		private Random _random;
	}
}