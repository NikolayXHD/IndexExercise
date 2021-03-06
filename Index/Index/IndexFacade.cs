﻿using System;
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
		/// <param name="fileNameFilter">A filtering callback to determine wich files need to be indexed.
		///   By default all files are indexed.</param>
		/// <param name="additionalWordChars">Additional non-word-separator characters for <see cref="ILexer"/></param>
		/// <param name="maxWordLength">Maximum word length to be returned by <see cref="ILexer"/></param>
		/// <param name="caseSensitive">Whether index search is case sensitive</param>
		/// <param name="indexDirectory">A directory to store index files</param>
		/// <param name="maxFileLength">Maximum length of indexed file</param>
		/// <param name="maxReadAttempts">Maximum read attempts when file system denies access to the file</param>
		/// <param name="lexerFactory">Creates instances of <see cref="ILexer"/> to parse text into a 
		/// sequence of <see cref="IToken"/></param>
		/// <param name="encodingDetector">Provides encoding detection functionality. By default 
		///   <see cref="Encoding.UTF8"/>is assumed.</param>
		public static IndexFacade Create(
			Mirror.FileNameFilter fileNameFilter = null,
			IEnumerable<char> additionalWordChars = null,
			int? maxWordLength = null,
			bool? caseSensitive = null,
			string indexDirectory = null,
			long? maxFileLength = null,
			int? maxReadAttempts = null,
			ILexerFactory lexerFactory = null,
			Func<FileInfo, Encoding> encodingDetector = null)
		{
			if (additionalWordChars != null && lexerFactory != null)
				throw new ArgumentException($"setting both {nameof(additionalWordChars)} and {nameof(lexerFactory)} is not supported");

			if (maxWordLength.HasValue && lexerFactory != null)
				throw new ArgumentException($"setting both {nameof(maxWordLength)} and {nameof(lexerFactory)} is not supported");

			if (caseSensitive.HasValue && lexerFactory != null)
				throw new ArgumentException($"setting both {nameof(caseSensitive)} and {nameof(lexerFactory)} is not supported");

			DefaultLexerFactory defaultLexerFactory = null;

			if (additionalWordChars != null || maxWordLength.HasValue || caseSensitive.HasValue)
			{
				defaultLexerFactory = new DefaultLexerFactory();
				lexerFactory = defaultLexerFactory;
			}

			if (additionalWordChars != null)
				defaultLexerFactory.AdditionalWordChars.UnionWith(additionalWordChars);

			if (maxWordLength.HasValue)
				defaultLexerFactory.MaxWordLength = maxWordLength.Value;

			if (caseSensitive.HasValue)
				defaultLexerFactory.IsPreservingCase = caseSensitive.Value;

			var indexEngine = new LuceneIndexEngine(indexDirectory, lexerFactory);
			var watcher = new Watcher();
			var mirror = new Mirror(watcher, new SequentialId(), fileNameFilter);
			var taskProcessor = new IndexingTaskProcessor(indexEngine, encodingDetector);

			if (maxFileLength.HasValue)
				taskProcessor.MaxFileLength = maxFileLength.Value;

			if (maxReadAttempts.HasValue)
				taskProcessor.MaxReadAttempts = maxReadAttempts.Value;

			return new IndexFacade(watcher, mirror, taskProcessor, indexEngine);
		}

		/// <summary>
		/// Maintains an up-to-date index of content of specified files and directories
		/// </summary>
		public IndexFacade(
			Watcher watcher,
			Mirror mirror,
			IndexingTaskProcessor indexingTaskProcessor,
			IIndexEngine indexEngine)
		{
			_watcher = watcher;
			_mirror = mirror;
			_indexingTaskProcessor = indexingTaskProcessor;
			_indexEngine = indexEngine;

			_mirror.FileCreated += fileCreatead;
			_mirror.FileDeleted += fileDeleted;
			_mirror.FileMoved += fileMoved;

			_mirror.EntryAccessError += fileAccessError;
			_indexingTaskProcessor.FileAccessError += fileAccessError;
		}

		public override Task RunAsync()
		{
			_indexEngine.Initialize();
			return Task.WhenAll(_mirror.RunAsync(), base.RunAsync());
		}

		public override void Dispose()
		{
			base.Dispose();

			_mirror.Dispose();
			_watcher.Dispose();
			_indexEngine.Dispose();
			_indexingTaskProcessor.Dispose();
		}

		protected override async Task BackgroundLoopIteration()
		{
			var indexingTask = _delayedTasksQueue.TryPeek().Value;
			if (indexingTask != null)
			{
				advanceDelayedTask(indexingTask);
				return;
			}

			var fileToRemove = _removingFromIndexQueue.TryDequeue();
			if (fileToRemove != null)
			{
				processRemoveFromIndexTask(fileToRemove);
				return;
			}

			indexingTask = _addingToIndexQueue.TryRemoveMin().Value;
			if (indexingTask != null)
			{
				processAddToIndexTask(indexingTask);
				return;
			}

			var failedHardLink = _failedHardLinksQueue.TryDequeue();
			if (failedHardLink != null)
				processFailedHardLink(failedHardLink);

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

		public string PrintDirectoryStructure(Action<StringBuilder, Entry<Metadata>> onAppend) => _mirror.PrintDirectoryStructure(onAppend);



		private void fileCreatead(object sender, FileEntry<Metadata> file)
		{
			FileCreated?.Invoke(this, file);
			addDelayedTask(file);
		}

		private void fileDeleted(object sender, FileEntry<Metadata> file)
		{
			FileDeleted?.Invoke(this, file);
			remove(file);
		}

		private void fileMoved(object sender, FileEntry<Metadata> file)
		{
			FileMoved?.Invoke(this, file);
			move(file);
		}

		private void fileAccessError(object sender, EntryAccessError accessError) =>
			FileSystemDeniedAccess?.Invoke(this, accessError);

		private void addDelayedTask(FileEntry<Metadata> fileEntry)
		{
			var indexingTask = new IndexingTask(IndexingAction.AddContent, fileEntry, CancellationToken);

			_indexingTaskProcessor.SetFilePropertiesOf(indexingTask);

			// file was deleted
			if (indexingTask.Path == null)
				return;

			_delayedTasksQueue.TryEnqueue(fileEntry, indexingTask);
			_filesByContentId.TryAdd(fileEntry.Data.ContentId, fileEntry);
		}

		private void advanceDelayedTask(IndexingTask indexingTask)
		{
			if (indexingTask.ElapsedSinceCreation < ThrottleDelay)
				return;

			if (_delayedTasksQueue.TryRemove(indexingTask.FileEntry))
				_addingToIndexQueue.TryAdd(indexingTask.FileEntry, indexingTask, indexingTask.FileLength);
		}

		private void remove(FileEntry<Metadata> file)
		{
			_filesByContentId.TryRemove(file.Data.ContentId, file);

			// The file was removed so there is no need to index it.
			// If another file happens to be created with the same path,
			// it will be enqueued and processed normally by detecting and processing the creation.
			_delayedTasksQueue.TryRemove(file);
			_addingToIndexQueue.TryRemove(file);

			var currentTask = _currentTask;

			if (currentTask != null && currentTask.FileEntry == file && currentTask.Action == IndexingAction.AddContent)
				currentTask.Cancel();

			if (!_filesByContentId.ContainsKey(file.Data.ContentId) && file.Data.ScanStarted)
			{
				// after removing from _addingToIndexQueue so that
				// the task never coexists in both _addingToIndexQueue and _removingFromIndexQueue
				// such coexistence could lead to processing element creation after its deletion
				_removingFromIndexQueue.TryEnqueue(file);
			}
			else if (file.Data.ScanStarted)
			{
				foreach (var sameContentEntry in _filesByContentId[file.Data.ContentId])
					sameContentEntry.Data.CopyScanStatusFrom(file.Data);
			}
		}

		private void move(FileEntry<Metadata> fileEntry)
		{
			bool needToReindex = false;

			var currentTask = _currentTask;

			bool indexingInProgress =
				currentTask != null &&
				currentTask.FileEntry == fileEntry &&
				currentTask.Action == IndexingAction.AddContent;

			needToReindex |= indexingInProgress;

			if (indexingInProgress)
				currentTask.Cancel();

			needToReindex |= _addingToIndexQueue.TryRemove(fileEntry);
			needToReindex |= _delayedTasksQueue.TryRemove(fileEntry);

			var elapsedSinceLastScan = _filesByContentId[fileEntry.Data.ContentId]
				.Select(_ => _.Data.ElapsedSinceScanFinished)
				.DefaultIfEmpty(TimeSpan.MinValue)
				.Min();

			bool neverIndexed = elapsedSinceLastScan == TimeSpan.MinValue;

			needToReindex |= neverIndexed;

			// if the moved file was indexed just moments ago we cannot be sure we indexed the right one.
			// maybe we scanned its previous location and there was some other file
			needToReindex |= elapsedSinceLastScan < _mirror.AllowablePathSynchronizationLag;

			if (needToReindex)
				addDelayedTask(fileEntry);
		}

		private IEnumerable<FileEntry<Metadata>> findFiles(long contentId)
		{
			return _filesByContentId[contentId];
		}



		private void processAddToIndexTask(IndexingTask indexingTask)
		{
			ProcessingTaskStarted?.Invoke(this, indexingTask);

			_currentTask = indexingTask;
			_indexingTaskProcessor.ProcessTask(indexingTask);
			_currentTask = null;

			ProcessingTaskFinished?.Invoke(this, indexingTask);

			if (indexingTask.HasToBeRepeated)
				_delayedTasksQueue.TryEnqueue(indexingTask.FileEntry, indexingTask);

			if (indexingTask.FileAccessException != null && indexingTask.HardlinkPath != null)
				_failedHardLinksQueue.TryEnqueue(indexingTask.HardlinkPath);
		}

		private void processRemoveFromIndexTask(FileEntry<Metadata> fileToRemove)
		{
			var indexingTask = new IndexingTask(IndexingAction.RemoveContent, fileToRemove, CancellationToken);

			ProcessingTaskStarted?.Invoke(this, indexingTask);

			_currentTask = indexingTask;
			_indexingTaskProcessor.ProcessTask(indexingTask);
			_currentTask = null;

			ProcessingTaskFinished?.Invoke(this, indexingTask);
		}

		private void processFailedHardLink(string hardLinkPath)
		{
			var fileInfo = new FileInfo(hardLinkPath);

			if (!fileInfo.Exists)
				return;

			try
			{
				fileInfo.Delete();
			}
			catch (Exception ex) when (ex is IOException || ex is SecurityException || ex is UnauthorizedAccessException)
			{
				_failedHardLinksQueue.TryEnqueue(hardLinkPath);
			}
		}


		public IQueryBuilder QueryBuilder => _indexEngine.QueryBuilder;

		public event EventHandler<EntryAccessError> FileSystemDeniedAccess;
		public event EventHandler<IndexingTask> ProcessingTaskStarted;
		public event EventHandler<IndexingTask> ProcessingTaskFinished;
		public event EventHandler<FileEntry<Metadata>> FileCreated;
		public event EventHandler<FileEntry<Metadata>> FileDeleted;
		public event EventHandler<FileEntry<Metadata>> FileMoved;


		/// <summary>
		/// A delay between changed file content is detected and indexing task is enqueued.
		/// Helps avoid some indexing attempts interrupted due to a subsequent change.
		/// 
		/// Should be greater or equal than <see cref="FileSystem.Mirror.AllowablePathSynchronizationLag"/>
		/// </summary>
		public TimeSpan ThrottleDelay
		{
			get => _throttleDelay;
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentException($"{nameof(ThrottleDelay)} must be a positive {nameof(TimeSpan)}");

				_throttleDelay = value;
			}
		}

		private TimeSpan _throttleDelay = TimeSpan.FromMilliseconds(400);

		private IndexingTask _currentTask;

		private readonly Mirror _mirror;

		private static Watcher _watcher;
		private readonly IndexingTaskProcessor _indexingTaskProcessor;
		private readonly IIndexEngine _indexEngine;

		private readonly FifoMap<FileEntry<Metadata>, IndexingTask> _delayedTasksQueue =
			new FifoMap<FileEntry<Metadata>, IndexingTask>();

		private readonly OrderedMap<FileEntry<Metadata>, IndexingTask, long> _addingToIndexQueue =
			new OrderedMap<FileEntry<Metadata>, IndexingTask, long>();

		private readonly FifoSet<FileEntry<Metadata>> _removingFromIndexQueue =
			new FifoSet<FileEntry<Metadata>>();

		private readonly FifoSet<string> _failedHardLinksQueue =
			new FifoSet<string>(PathString.Comparer);

		private readonly SetGrouping<long, FileEntry<Metadata>> _filesByContentId =
			new SetGrouping<long, FileEntry<Metadata>>();
	}
}