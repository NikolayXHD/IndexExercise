using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndexExercise.Index.FileSystem
{
	public class DirectoryEntry<TData> : Entry<TData>
	{
		public DirectoryEntry(RootEntry<TData> root) : base(root)
		{
		}

		public override EntryType Type => EntryType.Directory;

		protected internal Dictionary<string, DirectoryEntry<TData>> Directories { get; } =
			new Dictionary<string, DirectoryEntry<TData>>(PathString.Comparer);

		protected internal Dictionary<string, FileEntry<TData>> Files { get; } =
			new Dictionary<string, FileEntry<TData>>(PathString.Comparer);

		protected internal Dictionary<string, UnclassifiedEntry<TData>> UnclassifiedEntries { get; } =
			new Dictionary<string, UnclassifiedEntry<TData>>(PathString.Comparer);


		public string ToString(Action<StringBuilder, TData> printData)
		{
			int nesting = this is RootEntry<TData> ? -1 : 0;

			var result = new StringBuilder();
			write(this, result, nesting, printData);
			return result.ToString();
		}

		public override string ToString()
		{
			return ToString((sb, data) => sb.Append(data.ToString()));
		}

		private static void write(FileEntry<TData> entry, StringBuilder builder, int nesting, Action<StringBuilder, TData> printData)
		{
			builder.Append('\t', repeatCount: nesting).Append(entry.Name);

			if (printData != null)
			{
				builder.Append(" ");
				printData(builder, entry.Data);
			}
			
			builder.AppendLine();
		}

		private static void write(UnclassifiedEntry<TData> entry, StringBuilder builder, int nesting)
		{
			builder.Append('\t', repeatCount: nesting).Append(entry.Name).AppendLine("?");
		}

		private static void write(DirectoryEntry<TData> entry, StringBuilder builder, int nesting, Action<StringBuilder, TData> printData)
		{
			if (nesting >= 0)
				builder
					.Append('\t', repeatCount: nesting).Append(entry.Name)
					.AppendLine($"/ {entry.Directories.Count}+{entry.Files.Count}+{entry.UnclassifiedEntries.Count}");

			nesting++;

			foreach (var file in entry.Files.Values.OrderBy(f => f.Name, PathString.Comparer))
				write(file, builder, nesting, printData);

			foreach (var unclassified in entry.UnclassifiedEntries.Values.OrderBy(u => u.Name, PathString.Comparer))
				write(unclassified, builder, nesting);

			foreach (var directory in entry.Directories.Values.OrderBy(u => u.Name, PathString.Comparer))
				write(directory, builder, nesting, printData);
		}
	}
}