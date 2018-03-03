using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace IndexExercise.Index.FileSystem
{
	/// <summary>
	/// <see cref="FileSystemWatcher"/> assumes the watched directory path does not change, which is
	/// not true after the watched directory is renamed.
	/// </summary>
	internal class WatcherPathInspector
	{
		public static string GetActualPath(FileSystemWatcher watcher)
		{
			var directoryHandle = getDirectoryHandle(watcher);

			if (directoryHandle == null || directoryHandle.IsInvalid || directoryHandle.IsClosed)
				return null;

			var path = getAbsolutePathByHandle(directoryHandle);
			return path;
		}

		private static string getAbsolutePathByHandle(SafeFileHandle handle)
		{
			const int size = 1 << 13; // 8kb;
			var buffer = Marshal.AllocCoTaskMem(size);

			try
			{
				int res = NtQueryObject(handle, 1, buffer, size, out int length);

				// result was longer than size
				if (res == -1073741820 /* NTSTATUS */)
				{
					Marshal.FreeCoTaskMem(buffer);
					buffer = Marshal.AllocCoTaskMem(length);
					res = NtQueryObject(handle, 1, buffer, length, out length);
				}

				if (res != 0)
					return null;

				int position = Environment.Is64BitProcess ? 16 : 8;
				const int charSize = 2;
				var chars = new char[(length - position) / charSize - 1];

				for (int i = 0; i < chars.Length; i++)
				{
					chars[i] = (char) Marshal.ReadInt16(buffer, position);
					position += charSize;
				}

				return new string(chars);
			}
			finally
			{
				Marshal.FreeCoTaskMem(buffer);
			}
		}

		private static SafeFileHandle getDirectoryHandle(FileSystemWatcher watcher)
		{
			if (_directoryHandleField == null)
			{
				throw new NotSupportedException(
					$"This functionality relies on presence of private field {DirectoryHandleFieldName} in {nameof(FileSystemWatcher)} class.");
			}

			return (SafeFileHandle) _directoryHandleField.GetValue(watcher);
		}

		
		
		[DllImport("ntdll.dll", CharSet = CharSet.Auto)]
		private static extern int NtQueryObject(
			SafeFileHandle handle,
			int objectInformationClass,
			IntPtr buffer,
			int structSize,
			out int returnLength);



		private const string DirectoryHandleFieldName = "directoryHandle";

		private static readonly FieldInfo _directoryHandleField = typeof(FileSystemWatcher).GetField(
			DirectoryHandleFieldName,
			BindingFlags.NonPublic | BindingFlags.Instance);
	}
}