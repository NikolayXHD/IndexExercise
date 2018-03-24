using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using IndexExercise.Index.Collections;
using IndexExercise.Index.FileSystem;
using IndexExercise.Index.Lucene;

namespace IndexExercise.Index
{
	/// <summary>
	/// Maintains an up-to-date index of content of specified files and directories
	/// </summary>
	public class IndexFacade : BackgroundLoopOwner
	{
		/// <summary>
		/// Maintains an up-to-date index of content of specified files and directories
		/// </summary>
		/// <param name="indexDirectory">A directory to store index files</param>
		/// <param name="lexerFactory">Creates instances of <see cref="ILexer"/> to parse text into a 
		/// sequence of <see cref="IToken"/></param>
		/// <param name="filesFilter">A filtering callback to determine wich files need to be indexed.
		/// By default all files are indexed.</param>
		/// <param name="encodingDetector">Provides encoding detection functionality. By default 
		/// <see cref="Encoding.UTF8"/>is assumed.</param>
		public static IndexFacade Create(
			string indexDirectory = null,
			ILexerFactory lexerFactory = null,
			Mirror.FilesFilter filesFilter = null,
			Func<FileInfo, Encoding> encodingDetector = null)
		{
			var indexEngine = new LuceneIndexEngine(indexDirectory, lexerFactory);
			var mirror = new Mirror(new Watcher(), new SequentialId(), filesFilter);
			var taskProcessor = new IndexingTaskProcessor(indexEngine, encodingDetector);

			return new IndexFacade(mirror, taskProcessor, indexEngine);
		}

		/// <summary>
		/// Maintains an up-to-date index of content of specified files and directories
		/// </summary>
		public IndexFacade(Mirror mirror, IndexingTaskProcessor indexingTaskProcessor, IIndexEngine indexEngine)
		{
			_mirror = mirror;
			_indexingTaskProcessor = indexingTaskProcessor;
			_indexEngine = indexEngine;

			_mirror.FileCreated += fileCreatead;
			_mirror.FileDeleted += fileDeleted;
			_mirror.EntryAccessError += fileAccessError;
			_indexingTaskProcessor.FileAccessError += fileAccessError;
		}

		public override Task Start()
		{
			_indexEngine.Initialize();
			return Task.WhenAll(_mirror.Start(), base.Start());
		}

		public override void Dispose()
		{
			base.Dispose();

			_mirror.Dispose();
			_indexEngine.Dispose();
			_indexingTaskProcessor.Dispose();
		}

		protected override async Task BackgroundLoopIteration()
		{
			var fileToRemove = _removingFromIndexQueue.TryDequeue();
			if (fileToRemove != null)
			{
				processRemoveFromIndexTask(fileToRemove);
				return;
			}

			var fileToAdd = _addingToIndexQueue.TryRemoveMin();
			if (fileToAdd != null)
			{
				processAddToIndexTask(fileToAdd);
				return;
			}

			var (fileToRepeat, postponedTask) = _repetitionTasks.TryDequeue();
			if (fileToRepeat != null)
			{
				processAddToIndexTask(fileToRepeat, postponedTask);
				return;
			}

			await IdleDelayTask();
		}

		public void Watch(WatchTarget target) => _mirror.Watch(target);

		public FileSearchResult Search(IQuery query)
		{
			var contentSearchResult = _indexEngine.Search(query);

			if (contentSearchResult.HasSyntaxErrors)
				return FileSearchResult.Error(contentSearchResult.SyntaxErrors);

			var fileNames = contentSearchResult.ContentIds
				.SelectMany(findFiles,
					(contentId, fileEntry) => fileEntry.GetPath())
				.Where(path => path != null);

			return FileSearchResult.Success(fileNames);
		}



		private void fileCreatead(object sender, FileEntry<Metadata> file) => add(file);
		private void fileDeleted(object sender, FileEntry<Metadata> file) => remove(file);
		private void fileAccessError(object sender, EntryAccessError accessError) => EntryAccessError?.Invoke(this, accessError);

		private void add(FileEntry<Metadata> fileEntry)
		{
			string path = fileEntry.GetPath();

			if (path == null)
				return;

			if (isIndexFileOrDirectory(path))
				return;

			if (!tryGetFileLength(path, out long length))
				return;

			fileEntry.Data.Length = length;

			_filesByContentId.Add(fileEntry.Data.ContentId, fileEntry);
			_addingToIndexQueue.Add(fileEntry, fileEntry.Data.Length);
		}

		private bool isIndexFileOrDirectory(string path)
		{
			string indexDirectory = _indexEngine.IndexDirectory;

			if (indexDirectory == null)
				return false;

			if (!path.StartsWith(indexDirectory, PathString.Comparison))
				return false;

			if (path.Length == indexDirectory.Length)
				return true;

			char separator = path[indexDirectory.Length + 1];
			return separator == Path.DirectorySeparatorChar || separator == Path.AltDirectorySeparatorChar;
		}

		private void remove(FileEntry<Metadata> file)
		{
			_filesByContentId.Remove(file.Data.ContentId, file);

			// The file was removed so there is no need to index it.
			// If another file happens to be created with the same path,
			// it will be enqueued and processed normally by detecting and processing the creation.
			_addingToIndexQueue.Remove(file);
			_repetitionTasks.Remove(file);

			var currentTask = _currentTask;
			if (file == currentTask.Entry && currentTask.Task.Action == IndexingAction.AddContent)
				currentTask.Task.Cancel();

			if (!_filesByContentId.ContainsKey(file.Data.ContentId))
			{
				// after removing from _addingToIndexQueue so that
				// the task never coexists in both _addingToIndexQueue and _removingFromIndexQueue
				// such coexistence could lead to processing element creation after its deletion
				_removingFromIndexQueue.TryEnqueue(file);
			}
		}

		private IEnumerable<FileEntry<Metadata>> findFiles(long contentId)
		{
			return _filesByContentId[contentId];
		}



		private void processAddToIndexTask(FileEntry<Metadata> fileToAdd)
		{
			string path = fileToAdd.GetPath();

			if (path == null)
				return;

			var indexingTask = new IndexingTask(IndexingAction.AddContent, fileToAdd.Data.ContentId, CancellationToken, fileToAdd.Data.Length, path);

			processAddToIndexTask(fileToAdd, indexingTask);
		}

		private void processAddToIndexTask(FileEntry<Metadata> fileToAdd, IndexingTask indexingTask)
		{
			BeginProcessingTask?.Invoke(this, indexingTask);

			_currentTask = (indexingTask, fileToAdd);
			_indexingTaskProcessor.ProcessTask(indexingTask);
			_currentTask = (Task: null, Entry: null);

			EndProcessingTask?.Invoke(this, indexingTask);

			if (indexingTask.HasToBeRepeated)
				_repetitionTasks.Enqueue(fileToAdd, indexingTask.CreateRepetitionTask());
		}

		private void processRemoveFromIndexTask(FileEntry<Metadata> fileToRemove)
		{
			var indexingTask = new IndexingTask(IndexingAction.RemoveContent, fileToRemove.Data.ContentId, CancellationToken);

			BeginProcessingTask?.Invoke(this, indexingTask);

			_currentTask = (indexingTask, fileToRemove);
			_indexingTaskProcessor.ProcessTask(indexingTask);
			_currentTask = (Task: null, Entry: null);

			EndProcessingTask?.Invoke(this, indexingTask);
		}



		private bool tryGetFileLength(string path, out long length)
		{
			length = 0;

			try
			{
				var fileInfo = new FileInfo(path);
				length = fileInfo.Length;
				return true;
			}
			catch (SecurityException ex)
			{
				EntryAccessError?.Invoke(this, new EntryAccessError(EntryType.File, path, ex));
				return false;
			}
			catch (UnauthorizedAccessException ex)
			{
				EntryAccessError?.Invoke(this, new EntryAccessError(EntryType.File, path, ex));
				return false;
			}
			catch (FileNotFoundException ex)
			{
				EntryAccessError?.Invoke(this, new EntryAccessError(EntryType.File, path, ex));
				return false;
			}
		}

		public IQueryBuilder QueryBuilder => _indexEngine.QueryBuilder;

		public event EventHandler<EntryAccessError> EntryAccessError;
		public event EventHandler<IndexingTask> BeginProcessingTask;
		public event EventHandler<IndexingTask> EndProcessingTask;


		private (IndexingTask Task, FileEntry<Metadata> Entry) _currentTask;

		private readonly Mirror _mirror;
		private readonly IndexingTaskProcessor _indexingTaskProcessor;
		private readonly IIndexEngine _indexEngine;

		private readonly OrderedSet<FileEntry<Metadata>, long> _addingToIndexQueue =
			new OrderedSet<FileEntry<Metadata>, long>();

		private readonly FifoSet<FileEntry<Metadata>> _removingFromIndexQueue =
			new FifoSet<FileEntry<Metadata>>();

		private readonly FifoMap<FileEntry<Metadata>, IndexingTask> _repetitionTasks =
			new FifoMap<FileEntry<Metadata>, IndexingTask>();

		private readonly Grouping<long, FileEntry<Metadata>> _filesByContentId =
			new Grouping<long, FileEntry<Metadata>>();
	}
}