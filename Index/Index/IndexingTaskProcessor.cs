using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using IndexExercise.Index.FileSystem;

namespace IndexExercise.Index
{
	public class IndexingTaskProcessor : IDisposable
	{
		public IndexingTaskProcessor(IIndexEngine indexEngine, Func<FileInfo, Encoding> encodingDetector = null)
		{
			_indexEngine = indexEngine;
			_encodingDetector = encodingDetector;
		}

		public void ProcessTask(IndexingTask task)
		{
			task.BeginProcessing();

			switch (task.Action)
			{
				case IndexingAction.AddContent:
					updateFile(task);
					break;

				case IndexingAction.RemoveContent:
					_indexEngine.Remove(task.FileEntry.Data.ContentId, task.CancellationToken);
					break;

				default:
					throw new NotSupportedException($"{nameof(IndexingAction)} {task.Action} is not supported");
			}

			task.EndProcessing();
		}

		public void SetFilePropertiesOf(IndexingTask task)
		{
			task.Path = task.FileEntry.GetPath();

			// file was deleted
			if (task.Path == null)
			{
				handleFailedAccess(task, exception: null);
				return;
			}

			if (isIndexFileOrDirectory(task.Path))
				return;

			try
			{
				var fileInfo = new FileInfo(task.Path);
				task.FileEntry.Data.Length = fileInfo.Length;
			}
			catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException || ex is FileNotFoundException)
			{
				// FileNotFoundException may be ok if the file was renamed / moved after GetPath call
				task.FileEntry.Data.Length = long.MaxValue;
				handleFailedAccess(task, ex);
			}
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

			char separator = path[indexDirectory.Length];
			return separator == Path.DirectorySeparatorChar || separator == Path.AltDirectorySeparatorChar;
		}

		private void updateFile(IndexingTask task)
		{
			SetFilePropertiesOf(task);

			if (task.FileAccessException != null)
				return;
			
			if (task.Path == null)
				return;

			if (task.FileEntry.Data.Length >= MaxFileLength)
			{
				_indexEngine.Remove(task.FileEntry.Data.ContentId, task.CancellationToken);
				return;
			}

			var textReader = openFile(task);

			if (textReader == null)
				return;

			using (textReader)
			{
				FileOpened?.Invoke(this, task);
				_indexEngine.Update(task.FileEntry.Data.ContentId, textReader, task.CancellationToken);
			}
		}

		private StreamReader openFile(IndexingTask task)
		{
			var encoding = getEncoding(task);

			string hardlinkPath = task.HardlinkPath ?? tryCreateHardlink(task.Path);

			FileStream stream;

			if (hardlinkPath != null)
			{
				task.HardlinkPath = hardlinkPath;
				stream = openFileThroughHardLink(task);
			}
			else
			{
				stream = openFileDirectly(task);
			}

			if (stream == null)
				return null;

			return new StreamReader(stream, encoding);
		}

		private Encoding getEncoding(IndexingTask task)
		{
			Encoding encoding;
			if (_encodingDetector != null)
				encoding = _encodingDetector(new FileInfo(task.Path));
			else
				encoding = Encoding.UTF8;
			return encoding;
		}

		private FileStream openFileDirectly(IndexingTask task)
		{
			try
			{
				var stream = new FileStream(
					task.Path,
					FileMode.Open,
					FileAccess.Read,
					FileShare.Read | FileShare.Write | FileShare.Delete,
					BufferSize,
					FileOptions.SequentialScan);

				return stream;
			}
			catch (IOException ex)
			{
				handleFailedAccess(task, ex);
			}
			catch (SecurityException ex)
			{
				handleFailedAccess(task, ex);
			}
			catch (UnauthorizedAccessException ex)
			{
				handleFailedAccess(task, ex);
			}

			return null;
		}

		private FileStream openFileThroughHardLink(IndexingTask task)
		{
			try
			{
				var stream = new FileStream(
					task.HardlinkPath,
					FileMode.Open,
					FileAccess.Read,
					FileShare.Read | FileShare.Write | FileShare.Delete,
					BufferSize,
					FileOptions.SequentialScan | FileOptions.DeleteOnClose);

				return stream;
			}
			catch (IOException ex) when (!(ex is FileNotFoundException) && !(ex is DirectoryNotFoundException))
			{
				handleFailedAccess(task, ex);
			}

			return null;
		}

		private void handleFailedAccess(IndexingTask task, Exception exception)
		{
			task.HasToBeRepeated = task.Attempts < MaxReadAttempts;
			task.FileAccessException = exception;

			if (exception != null)
				FileAccessError?.Invoke(this, new EntryAccessError(EntryType.File, task.Path, exception));
		}

		/// <summary>
		/// Create a hardlink to a specified file.
		/// <para>
		/// Reading a file through a hardlink instead of it's original path enables other processes to 
		/// delete it and create a new one with the same name without getting a
		/// <see cref="UnauthorizedAccessException"/> on creation step.
		/// </para>
		/// <para>
		/// See https://stackoverflow.com/a/19876245/6656775.
		/// </para>
		/// <para>
		/// Despite the trick of using hardlink other processes still have a limitation on access to 
		/// the scanned file. If another process tries to open a <see cref="FileStream"/> withOUT 
		/// <see cref="FileShare.Read"/> it will get an <see cref="IOException"/>
		/// </para>
		/// <para>
		/// "The process cannot access the file ... because it is being used by another process"
		/// </para>
		/// <para>
		/// because we did open the file with <see cref="FileShare.Read"/>
		/// </para>
		/// </summary>
		private string tryCreateHardlink(string filePath)
		{
			string root = Path.GetPathRoot(filePath);

			if (string.IsNullOrEmpty(root))
				throw new ArgumentException($"A rooted path expected. Actual path {filePath}", nameof(filePath));

			var tempDirectoryPath = Path.Combine(root, TempDirectoryName);
			DirectoryInfo tempDirectory;

			try
			{
				tempDirectory = Directory.CreateDirectory(tempDirectoryPath);
			}
			catch (IOException)
			{
				return null;
			}
			catch (UnauthorizedAccessException)
			{
				return null;
			}

			tempDirectory.Attributes |= FileAttributes.Hidden;
			_tempDirectories.Add(tempDirectoryPath);

			string hardLinkPath = Path.Combine(tempDirectoryPath, Path.GetRandomFileName());

			if (!HardLinkUtility.TryCreateHardLink(hardLinkPath, existingFileName: filePath))
				return null;

			return hardLinkPath;
		}

		public void Dispose()
		{
			foreach (string tempDirectory in _tempDirectories)
			{
				try
				{
					Directory.Delete(tempDirectory, recursive: true);
				}
				catch (DirectoryNotFoundException)
				{
				}
			}
		}

		public event EventHandler<EntryAccessError> FileAccessError;
		public event EventHandler<IndexingTask> FileOpened;

		public long MaxFileLength { get; set; } = 1L << 26; // 64MB
		public int MaxReadAttempts { get; set; } = 3;
		public string TempDirectoryName { get; set; } = "Temp.IndexExercise";

		private readonly HashSet<string> _tempDirectories = new HashSet<string>(PathString.Comparer);

		/// <summary>
		/// Buffer size in bytes for a <see cref="FileStream"/> created to read the file content
		/// </summary>
		public int BufferSize { get; set; } = 4096;

		private readonly IIndexEngine _indexEngine;
		private readonly Func<FileInfo, Encoding> _encodingDetector;
	}
}