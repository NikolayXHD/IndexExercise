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


		public string ToString(Action<StringBuilder, Entry<TData>> onAppend)
		{
			int nesting = this is RootEntry<TData> ? -1 : 0;

			var result = new StringBuilder();
			write(this, result, nesting, onAppend);
			return result.ToString();
		}

		public override string ToString()
		{
			return ToString((sb, entry) => sb.Append(" ").Append(entry.Data.ToString()));
		}

		private static void write(
			FileEntry<TData> entry,
			StringBuilder builder,
			int nesting,
			Action<StringBuilder, Entry<TData>> onAppend)
		{
			builder.Append('\t', repeatCount: nesting).Append(entry.Name);
			onAppend?.Invoke(builder, entry);
			builder.AppendLine();
		}

		private static void write(
			UnclassifiedEntry<TData> entry,
			StringBuilder builder,
			int nesting,
			Action<StringBuilder, Entry<TData>> onAppend)
		{
			builder.Append('\t', repeatCount: nesting).Append(entry.Name).Append("?");
			onAppend?.Invoke(builder, entry);
			builder.AppendLine();
		}

		private static void write(
			DirectoryEntry<TData> entry,
			StringBuilder builder,
			int nesting,
			Action<StringBuilder, Entry<TData>> onAppend)
		{
			if (nesting >= 0)
			{
				builder
					.Append('\t', repeatCount: nesting).Append(entry.Name)
					.Append("/");

				onAppend?.Invoke(builder, entry);
				builder.AppendLine();
			}

			nesting++;

			foreach (var file in entry.Files.Values.OrderBy(f => f.Name, PathString.Comparer))
				write(file, builder, nesting, onAppend);

			foreach (var unclassified in entry.UnclassifiedEntries.Values.OrderBy(u => u.Name, PathString.Comparer))
				write(unclassified, builder, nesting, onAppend);

			foreach (var directory in entry.Directories.Values.OrderBy(u => u.Name, PathString.Comparer))
				write(directory, builder, nesting, onAppend);
		}
	}
}