using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace IndexExercise.Index.Lucene
{
	public class LuceneIndexEngine : IIndexEngine
	{
		public LuceneIndexEngine(
			string indexDirectory = null,
			ILexerFactory lexerFactory = null)
		{
			IndexDirectory = indexDirectory ?? "lucene-index";

			lexerFactory = lexerFactory ?? new DefaultLexerFactory();
			_writerLexer = lexerFactory.CreateLexer();
			_writerAnalyzer = new GenericAnalyzer(_writerLexer);

			QueryBuilder = new LuceneQueryBuilder(lexerFactory, ContentFieldName);
		}

		public void Initialize()
		{
			System.IO.Directory.CreateDirectory(IndexDirectory);

			_index = FSDirectory.Open(IndexDirectory);

			var writerConfig = new IndexWriterConfig(LuceneVersion.LUCENE_48, _writerAnalyzer);
			_indexWriter = new IndexWriter(_index, writerConfig);
		}

		public void Dispose()
		{
			if (_index == null)
				return;

			_indexWriter.Dispose();
			_index.Dispose();
		}

		public void Update(long contentId, TextReader input, CancellationToken cancellationToken)
		{
			var document = new Document();

			document.AddInt64Field(IdFieldName, contentId, Field.Store.YES);
			document.AddTextField(ContentFieldName, input);

			lock (_syncWrite)
			{
				_writerLexer.CancellationToken = cancellationToken;
				_indexWriter.UpdateDocument(getContentIdTerm(contentId), document);
			}
		}

		public void Remove(long contentId, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
				return;

			lock (_syncWrite)
			{
				_writerLexer.CancellationToken = cancellationToken;
				_indexWriter.DeleteDocuments(getContentIdTerm(contentId));
			}
		}

		public ContentSearchResult Search(IQuery query)
		{
			var queryWrapper = (QueryWrapper) query;

			if (queryWrapper.HasSyntaxErrors)
				return ContentSearchResult.Error(queryWrapper.SyntaxErrors);

			return ContentSearchResult.Success(findContentIds(queryWrapper.LuceneQuery));
		}

		private IEnumerable<long> findContentIds(Query query)
		{
			const int batchSize = 256;

			// ReSharper disable once InconsistentlySynchronizedField
			using (var reader = _indexWriter.GetReader(applyAllDeletes: true))
			{
				var searcher = new IndexSearcher(reader);
				var searchResult = searcher.Search(query, batchSize);

				foreach (var scoreDoc in searchResult.ScoreDocs)
				{
					var doc = searcher.Doc(scoreDoc.Doc);
					long contentId = doc.GetField<StoredField>(IdFieldName).GetInt64Value().Value;
					yield return contentId;
				}

				while (searchResult.ScoreDocs.Length > 0)
				{
					var lastDoc = searchResult.ScoreDocs[searchResult.ScoreDocs.Length - 1];
					searchResult = searcher.SearchAfter(lastDoc, query, batchSize);
				}
			}
		}

		private static Term getContentIdTerm(long contentId)
		{
			var contentIdBytes = new BytesRef();
			NumericUtils.Int64ToPrefixCoded(contentId, 0, contentIdBytes);
			var idTerm = new Term(IdFieldName, contentIdBytes);
			return idTerm;
		}



		public IQueryBuilder QueryBuilder { get; }



		private FSDirectory _index;
		private IndexWriter _indexWriter;

		private readonly ILexer _writerLexer;
		private readonly GenericAnalyzer _writerAnalyzer;

		public string IndexDirectory { get; }

		private readonly object _syncWrite = new object();

		private const string IdFieldName = "id";
		private const string ContentFieldName = "content";
	}
}