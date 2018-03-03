using System;

namespace IndexExercise.Index.FileSystem
{
	public struct WatchTarget : IEquatable<WatchTarget>
	{
		public WatchTarget(EntryType type, string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentException($"{nameof(path)} must be non empty", nameof(path));

			if (!System.IO.Path.IsPathRooted(path))
				throw new ArgumentException($"{nameof(path)} must be absolute", nameof(path));

			switch (type)
			{
				case EntryType.Directory:
					break;

				case EntryType.File:
					if (string.IsNullOrEmpty(System.IO.Path.GetDirectoryName(path)))
						throw new ArgumentException($"File path is pointing to a root directory {path}");
					break;

				default:
					throw new ArgumentException($"Invalid type {type}", nameof(type));
			}

			Path = path;
			Type = type;
		}

		public string Path { get; }

		public EntryType Type { get; }



		public bool Equals(WatchTarget other)
		{
			return string.Equals(Path, other.Path, PathString.Comparison) && Type == other.Type;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(objA: null, objB: obj))
				return false;

			return obj is WatchTarget target && Equals(target);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (StringComparer.OrdinalIgnoreCase.GetHashCode(Path) * 397) ^ (int) Type;
			}
		}

		public static bool operator ==(WatchTarget left, WatchTarget right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(WatchTarget left, WatchTarget right)
		{
			return !left.Equals(right);
		}



		public override string ToString()
		{
			return $"{Type} {Path}";
		}
	}
}