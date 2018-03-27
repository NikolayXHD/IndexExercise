using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NLog;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	public class FileSystemUtility : IDisposable
	{
		public FileSystemUtility()
		{
			TempDirectory = CreateDirectory(parent: TestContext.CurrentContext.TestDirectory);
			WorkingDirectory = CreateDirectory("working", parent: TempDirectory);
		}



		public string CreateFile(string name = null, string parent = null, string content = null, bool empty = false)
		{
			var fileName = GetFileName(name, parent);
			Log.Debug($"create{(empty ? " empty" : string.Empty)} file {fileName}");

			// Indexed files are opened with FileShare.Read|Write|Delete. File.CreateText attempts to
			// open with FileShare.Write which is denied.
			// The process cannot access the file ... because it is being used by another process
			// Therfore we cannot rely on opening FileStream on first attempt.

			// As unit test files are small, these IOExceptions happen quite unfrequently ~ 0.1% of
			// all write attempts
			retry(() =>
			{
				using (var writer = File.CreateText(fileName))
				{
					if (!empty)
						writer.Write(content ?? "some text content");
				}
			});

			return fileName;
		}

		public string CreateDirectory(string name = null, string parent = null)
		{
			var directoryName = GetFileName(name, parent);

			Directory.CreateDirectory(directoryName);
			Log.Debug($"create directory {directoryName}");

			return directoryName;
		}

		public List<string> CreateFiles(int count, string parent = null, bool empty = false)
		{
			var names = new List<string>();

			for (int i = 0; i < count; i++)
			{
				var name = CreateFile(parent: parent, empty: empty);
				names.Add(name);
			}

			return names;
		}

		public List<string> CreateDirectories(int count, string parent = null)
		{
			var names = new List<string>();

			for (int i = 0; i < count; i++)
			{
				var name = CreateDirectory(parent: parent);
				names.Add(name);
			}

			return names;
		}

		public string GetFileName(string name = null, string parent = null)
		{
			if (name != null && Path.IsPathRooted(name))
			{
				if (parent != null)
					throw new ArgumentException($"{name} is already a rooted path", nameof(parent));

				return name;
			}

			return Path.Combine(parent ?? WorkingDirectory, name ?? Path.GetRandomFileName());
		}



		public void CopyFile(string fileName, string destFileName)
		{
			Log.Debug($"copy file {fileName} -> {destFileName}");
			File.Copy(fileName, destFileName, overwrite: true);
		}

		public void ChangeFile(string fileName)
		{
			Log.Debug($"write to {fileName}");

			using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Write))
			using (var writer = new StreamWriter(stream))
			{
				writer.BaseStream.Seek(0, SeekOrigin.End);
				writer.Write("some text");
			}
		}

		public void MoveFile(string fileName, string destFileName)
		{
			Log.Debug($"move file {fileName} -> {destFileName}");
			File.Move(fileName, destFileName);
		}

		public void MoveDirectory(string fromDirectoryName, string toDirectoryName)
		{
			Log.Debug($"move directory {fromDirectoryName} -> {toDirectoryName}");
			
			// Did not have time to figure out why moving scanned directory
			// once thrown IOException Access to the path ... is denied
			// once means ~0.01% of all actual move directory attempts

			// I do know while DirectoryInfo.EnumerateFiles and DirectoryInfo.EnumerateDirectories
			// happens other processes can freely remove the scanned directory and even create a new
			// with the same path.
			retry(() => Directory.Move(fromDirectoryName, toDirectoryName));
		}



		public void DeleteDirectory(string directory)
		{
			Log.Debug($"delete directory {directory}");

			try
			{
				Directory.Delete(directory, recursive: true);
			}
			catch (DirectoryNotFoundException)
			{
			}
		}

		public void DeleteFile(string fileName)
		{
			Log.Debug($"delete file {fileName}");
			File.Delete(fileName);
		}



		public async Task SmallDelay(int times = 1)
		{
			var duration = TimeSpan.FromMilliseconds(DelayDuration.TotalMilliseconds * times);
			Log.Debug($"delay start {(int) duration.TotalMilliseconds} ms");

			await Task.Delay(duration);
			Log.Debug("delay end");
		}

		public Task AverageDelay()
		{
			return SmallDelay(times: 3);
		}

		public virtual void Dispose()
		{
			DeleteDirectory(TempDirectory);
		}



		private static void retry(Action action)
		{
			const int attempts = 5;
			for (int i = 0; i < attempts; i++)
			{
				try
				{
					action.Invoke();
					break;
				}
				catch (IOException) when (i < attempts - 1)
				{
					Task.Delay(TimeSpan.FromMilliseconds(10)).Wait();
				}
			}
		}

		protected TimeSpan DelayDuration { get; } = TimeSpan.FromMilliseconds(100);
		public string WorkingDirectory { get; }
		public string TempDirectory { get; }

		protected static readonly Logger Log = LogManager.GetCurrentClassLogger();
	}
}