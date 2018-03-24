using System;
using System.Runtime.InteropServices;

namespace IndexExercise.Index.FileSystem
{
	public class HardLinkUtility
	{
		public static bool TryCreateHardLink(string hardLinkName, string existingFileName)
		{
			return CreateHardLink(hardLinkName, existingFileName, IntPtr.Zero);
		}

		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool CreateHardLink(
			string lpFileName,
			string lpExistingFileName,
			IntPtr lpSecurityAttributes
		);
	}
}