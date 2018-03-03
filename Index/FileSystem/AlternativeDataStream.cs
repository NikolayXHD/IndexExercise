using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace IndexExercise.Index.FileSystem
{
	public class AlternativeDataStream
	{
		public static FileStream TryOpen(string filePath, string alternativeDataStreamName, FileAccess access, FileShare share)
		{
			uint fileAccess;

			switch (access)
			{
				case FileAccess.Write:
					fileAccess = GenericWrite;
					break;
				case FileAccess.Read:
					fileAccess = GenericRead;
					break;
				case FileAccess.ReadWrite:
					fileAccess = GenericRead | GenericWrite;
					break;
				default:
					throw new ArgumentException($"file access {access} is not supported", nameof(access));
			}

			var handle = CreateFileW(
				$"{filePath}:{alternativeDataStreamName}", // NTFS alternative data stream
				fileAccess,
				share,
				IntPtr.Zero,
				OpenAlways,
				0,
				IntPtr.Zero);

			if (handle == null || handle.IsInvalid)
				return null;

			return new FileStream(handle, access);
		}

		[DllImport("kernel32.dll", EntryPoint = "CreateFileW")]
		private static extern SafeFileHandle CreateFileW(
			[In] [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
			uint dwDesiredAccess,
			FileShare dwShareMode,
			[In] IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			[In] IntPtr hTemplateFile
		);

		private const uint GenericWrite = 0x40000000;
		private const uint GenericRead = 0x80000000;
		private const uint OpenAlways = 4;
	}
}