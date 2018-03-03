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
			Log = LogManager.GetLogger(name: GetType().Name);
			TempDirectory = CreateDirectory(parent: TestContext.CurrentContext.TestDirectory);
			WorkingDirectory = CreateDirectory("working", parent: TempDirectory);
		}



		public string CreateFile(string name = null, string parent = null, string content = null, bool empty = false)
		{
			var fileName = GetFileName(name, parent);

			using (var writer = File.CreateText(fileName))
			{
				if (!empty)
					writer.Write(content ?? "some text content");
			}

			Log.Debug($"create{(empty ? " empty" : string.Empty)} file {fileName}");

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
			File.Copy(fileName, destFileName, overwrite: true);
			Log.Debug($"copy file {fileName} -> {destFileName}");
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
			File.Move(fileName, destFileName);
			Log.Debug($"move file {fileName} -> {destFileName}");
		}

		public void MoveDirectory(string fromDirectoryName, string toDirectoryName)
		{
			Directory.Move(fromDirectoryName, toDirectoryName);
			Log.Debug($"move directory {fromDirectoryName} -> {toDirectoryName}");
		}



		public void DeleteDirectory(string directory)
		{
			try
			{
				Directory.Delete(directory, recursive: true);
			}
			catch (DirectoryNotFoundException)
			{
			}

			Log.Debug($"delete directory {directory}");
		}

		public void DeleteFile(string fileName)
		{
			File.Delete(fileName);
			Log.Debug($"delete file {fileName}");
		}



		public async Task SmallDelay(int times = 1)
		{
			var duration = TimeSpan.FromMilliseconds(DelayDuration.TotalMilliseconds * times);
			Log.Debug($"delay start {(int) duration.TotalMilliseconds} ms");

			await Task.Delay(duration);
			Log.Debug("delay end");
		}

		public virtual void Dispose()
		{
			DeleteDirectory(WorkingDirectory);
		}



		protected TimeSpan DelayDuration { get; } = TimeSpan.FromMilliseconds(100);
		protected Logger Log { get; }
		public string WorkingDirectory { get; }
		public string TempDirectory { get; }
	}
}