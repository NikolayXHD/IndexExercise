using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = Lucene.Net.Store.Directory;

namespace IndexExercise.Index.Lucene
{
	public class LuceneIndexEngine : IIndexEngine
	{
		public LuceneIndexEngine(
			string indexDirectory = null,
			ILexerFactory lexerFactory = null)
		{
			_lexerFactory = lexerFactory ?? new DefaultLexerFactory();
			_indexDirectory = indexDirectory ?? "lucene-index";
			_writerLexer = _lexerFactory.CreateLexer();
		}

		public void Initialize()
		{
			System.IO.Directory.CreateDirectory(_indexDirectory);

			_index = FSDirectory.Open(_indexDirectory);
			clear(_index);

			var writerConfig = new IndexWriterConfig(LuceneVersion.LUCENE_48, new GenericAnalyzer(_writerLexer));
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

		public ContentSearchResult Search(string searchQuery)
		{
			var queryParser = createQueryParser();
			Query query;

			try
			{
				query = queryParser.Parse(searchQuery);
			}
			catch (ParseException ex)
			{
				return ContentSearchResult.Error(ex.Message);
			}

			return ContentSearchResult.Success(findContentIds(query));
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



		private QueryParser createQueryParser()
		{
			return new QueryParser(LuceneVersion.LUCENE_48, ContentFieldName, createAnalyzer());
		}

		private GenericAnalyzer createAnalyzer()
		{
			var lexer = _lexerFactory.CreateLexer();
			return new GenericAnalyzer(lexer);
		}

		private void clear(Directory index)
		{
			var writer = new IndexWriter(index,
				new IndexWriterConfig(LuceneVersion.LUCENE_48, createAnalyzer())
				{
					OpenMode = OpenMode.CREATE
				});

			using (writer)
			{
				writer.DeleteAll();
				writer.Commit();
			}
		}

		private static Term getContentIdTerm(long contentId)
		{
			var contentIdBytes = new BytesRef();
			NumericUtils.Int64ToPrefixCoded(contentId, 0, contentIdBytes);
			var idTerm = new Term(IdFieldName, contentIdBytes);
			return idTerm;
		}

		private FSDirectory _index;

		private readonly ILexerFactory _lexerFactory;
		private readonly string _indexDirectory;

		private readonly object _syncWrite = new object();
		private readonly ILexer _writerLexer;
		private IndexWriter _indexWriter;

		private const string IdFieldName = "id";
		private const string ContentFieldName = "content";
	}
}