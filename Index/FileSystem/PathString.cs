using System;
using System.Collections.Generic;
using System.IO;

namespace IndexExercise.Index.FileSystem
{
	/// <summary>
	/// Assumes file system is case INsensitive like in MS Windows
	/// </summary>
	public static class PathString
	{
		public static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
		public const StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

		public static IList<string> Split(string path)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("Expecting a rooted path", nameof(path));

			var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return parts;
		}

		public static string Combine(IEnumerable<string> pathSegments)
		{
			return string.Join(new string(Path.DirectorySeparatorChar, count: 1), pathSegments);
		}
	}
}