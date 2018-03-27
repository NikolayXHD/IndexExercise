using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using IndexExercise.Index.FileSystem;
using NConfiguration;
using NConfiguration.Joining;
using NConfiguration.Xml;
using NLog;

namespace IndexExercise.Index.Demo
{
	public class DemoApplication : IDisposable
	{
		public DemoApplication()
		{
			var settings = loadSettings();
			var config = settings.Get<IndexConfig>();

			FileNameRegex = config.FileNameRegex;
			var regex = new Regex(FileNameRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
			
			_indexFacade = IndexFacade.Create(
				fileNameFilter: fileName => regex.IsMatch(fileName),
				additionalWordChars: config.AdditionalWordChars,
				maxWordLength: config.MaxWordLength,
				caseSensitive: config.CaseSensitive,
				indexDirectory: config.IndexDirectory,
				maxFileLength: config.MaxFileLength,
				maxReadAttempts: config.MaxReadAttempts);

			foreach (var directory in config.Directories)
				_indexFacade.Watch(new WatchTarget(EntryType.Directory, Path.GetFullPath(directory.Path)));

			foreach (var file in config.Files)
				_indexFacade.Watch(new WatchTarget(EntryType.File, Path.GetFullPath(file.Path)));

			_indexFacade.ProcessingTaskStarted += processingTaskStarted;
			_indexFacade.ProcessingTaskFinished += processingTaskFinished;
			_indexFacade.FileSystemDeniedAccess += fileSystemDeniedAccess;
			_indexFacade.BackgroundLoopFailed += backgroundLoopFailed;

			_indexFacade.FileCreated += fileCreated;
			_indexFacade.FileDeleted += fileDeleted;
			_indexFacade.FileMoved += fileMoved;

			_indexFacade.Idle += indexFacadeIdle;
		}

		public void Start()
		{
			_indexFacade.RunAsync();
		}

		public void Dispose()
		{
			_indexFacade.Dispose();
		}

		private static IAppSettings loadSettings()
		{
			var systemSettings = new XmlSystemSettings(@"ExtConfigure");

			var settingsLoader = new SettingsLoader();
			settingsLoader.XmlFileByExtension();
			var settings = settingsLoader.LoadSettings(systemSettings).Joined.ToAppSettings();

			return settings;
		}

		public IQuery GetQuery(string luceneSearchQuery) => _indexFacade.QueryBuilder.EngineSpecificQuery(luceneSearchQuery);

		public FileSearchResult Search(IQuery query)
		{
			return _indexFacade.Search(query);
		}

		public string PrintDirectoryStructure(Action<StringBuilder, Entry<Metadata>> onAppend) => _indexFacade.PrintDirectoryStructure(onAppend);

		public string FileNameRegex { get; }



		private void fileCreated(object sender, FileEntry<Metadata> e)
		{
			IndexChangeTime = DateTime.UtcNow;
			IndexChanged?.Invoke(this, new EventArgs());
		}

		private void fileDeleted(object sender, FileEntry<Metadata> e)
		{
			IndexChangeTime = DateTime.UtcNow;
			IndexChanged?.Invoke(this, new EventArgs());
		}

		private void fileMoved(object sender, FileEntry<Metadata> e)
		{
			IndexChangeTime = DateTime.UtcNow;
			IndexChanged?.Invoke(this, new EventArgs());
		}

		private void processingTaskStarted(object sender, IndexingTask task)
		{
			_log.Info($"begin processing task {task.Action} #{task.ContentId} length:{task.FileLength}b attempt:{task.Attempts} {task.Path}");
			
			IndexChangeTime = DateTime.UtcNow;
			IndexChanged?.Invoke(this, new EventArgs());
		}

		private void processingTaskFinished(object sender, IndexingTask task)
		{
			_log.Info($"end processing task {task.Action} #{task.ContentId} length:{task.FileLength}b attempt:{task.Attempts} {task.Path} resolution: {task.GetState()}");

			IndexChangeTime = DateTime.UtcNow;
			IndexChanged?.Invoke(this, new EventArgs());
		}

		public DateTime IndexChangeTime { get; private set; }


		public event EventHandler IndexChanged;


		private static void indexFacadeIdle(object sender, TimeSpan delay)
		{
			_log.Debug($"idle {(int) delay.TotalMilliseconds} ms");
		}

		private static void backgroundLoopFailed(object sender, Exception e)
		{
			_log.Error(e, "background loop failed");
		}

		private static void fileSystemDeniedAccess(object sender, EntryAccessError e)
		{
			_log.Info(e.Exception, $"file system denied access to {e.EntryType} {e.Path}");
		}

		private readonly IndexFacade _indexFacade;

		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
	}
}
