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
			switch (task.Action)
			{
				case IndexingAction.AddContent:
					updateFile(task);
					break;

				case IndexingAction.RemoveContent:
					_indexEngine.Remove(task.ContentId, task.CancellationToken);
					break;

				default:
					throw new NotSupportedException($"{nameof(IndexingAction)} {task.Action} is not supported");
			}
		}

		private void updateFile(IndexingTask task)
		{
			if (task.Length >= MaxFileLength)
				_indexEngine.Remove(task.ContentId, task.CancellationToken);

			var (stream, encoding) = openFile(task, task.Path);

			if (stream == null)
				return;

			FileOpened?.Invoke(this, task);

			using (stream)
			using (var input = new StreamReader(stream, encoding))
				_indexEngine.Update(task.ContentId, input, task.CancellationToken);
		}

		private (Stream stream, Encoding encoding) openFile(IndexingTask task, string path)
		{
			string hardlinkPath = tryCreateHardlink(path);

			Encoding encoding;
			if (_encodingDetector != null)
				encoding = _encodingDetector(new FileInfo(path));
			else
				encoding = Encoding.UTF8;

			string pathToOpen = path;
			var fileOptions = FileOptions.SequentialScan;

			if (hardlinkPath != null)
			{
				pathToOpen = hardlinkPath;
				fileOptions |= FileOptions.DeleteOnClose;
			}

			try
			{
				var stream = new FileStream(
					pathToOpen,
					FileMode.Open,
					FileAccess.Read,
					FileShare.Read | FileShare.Write | FileShare.Delete,
					BufferSize,
					fileOptions);

				return (stream, encoding);
			}
			catch (Exception ex)
			{
				if (hardlinkPath != null)
					File.Delete(hardlinkPath);

				switch (ex)
				{
					case DirectoryNotFoundException _:
					case FileNotFoundException _:
						return (null, null);

					case SecurityException _:
					case UnauthorizedAccessException _:
					case IOException _:
						if (task.Attempts < MaxReadAttempts)
							task.HasToBeRepeated = true;
						FileAccessError?.Invoke(this, new EntryAccessError(EntryType.File, path, ex));
						return (null, null);

					default:
						throw;
				}
			}
		}

		/// <summary>
		/// <see cref="FileShare.Delete"/> does not actually let other processes delete the opened file,
		/// see https://stackoverflow.com/a/19876245/6656775.
		///
		/// To workaraund this we access the file through a temporarily created hardlink.
		/// Now other processes can delete the file. When it happens we can detect it e.g. by using <see cref="FileSystemWatcher"/>
		/// in order to gracefully handle currently inexed file deletion.
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