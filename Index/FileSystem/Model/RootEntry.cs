using System;
using System.Collections.Generic;

namespace IndexExercise.Index.FileSystem
{
	public class RootEntry<TData> : DirectoryEntry<TData>
	{
		public RootEntry(Func<TData> defaultData)
			:base(root: null)
		{
			_defaultData = defaultData;
		}

		public Entry<TData> Find(string path)
		{
			lock (Sync)
			{
				var pathSegments = PathString.Split(path);
				DirectoryEntry<TData> current = this;

				for (int i = 0; i < pathSegments.Count - 1; i++)
				{
					if (!current.Directories.TryGetValue(pathSegments[i], out var subdirectory))
						return null;

					current = subdirectory;
				}

				string name = pathSegments[pathSegments.Count - 1];

				if (current.Directories.TryGetValue(name, out var directory))
					return directory;

				if (current.Files.TryGetValue(name, out var file))
					return file;

				if (current.UnclassifiedEntries.TryGetValue(name, out var unclassifiedEntry))
					return unclassifiedEntry;

				return null;
			}
		}

		/// <summary>
		/// Returns ancestors present in the tree starting from root
		/// </summary>
		public IEnumerable<Entry<TData>> FindAncestors(string path)
		{
			lock (Sync)
			{
				var pathSegments = PathString.Split(path);

				DirectoryEntry<TData> current = this;

				for (int i = 0; i < pathSegments.Count - 1; i++)
				{
					if (current.Directories.TryGetValue(pathSegments[i], out var subdirectory))
						yield return subdirectory;
					else
						yield break;

					current = subdirectory;
				}
			}
		}

		public void Move(Entry<TData> entry, string path)
		{
			lock (Sync)
			{
				remove(entry);
				add(path, entry);
			}
		}

		public Entry<TData> Add(EntryType type, string path, TData data)
		{
			var entry = create(type);
			entry.Data = data;

			lock (Sync)
				add(path, entry);

			return entry;
		}

		public void Remove(Entry<TData> entry)
		{
			lock (Sync)
				remove(entry);
		}

		public override EntryType Type => EntryType.Root;



		private static void remove(Entry<TData> entry)
		{
			switch (entry)
			{
				case DirectoryEntry<TData> _:
					entry.Parent.Directories.Remove(entry.Name);
					break;
				case FileEntry<TData> _:
					entry.Parent.Files.Remove(entry.Name);
					break;
				case UnclassifiedEntry<TData> _:
					entry.Parent.UnclassifiedEntries.Remove(entry.Name);
					break;
				default:
					throw new NotSupportedException($"{entry.GetType().FullName} is not supported");
			}

			entry.Parent = null;
		}

		private Entry<TData> create(EntryType entryType)
		{
			switch (entryType)
			{
				case EntryType.File:
					return new FileEntry<TData>(root: this);
				case EntryType.Directory:
					return new DirectoryEntry<TData>(root: this);
				case EntryType.Uncertain:
					return new UnclassifiedEntry<TData>(root: this);

				default:
					throw new NotSupportedException($"{nameof(EntryType)} {entryType} is not supported");
			}
		}

		private void add(string path, Entry<TData> entry)
		{
			var pathSegments = PathString.Split(path);
			var parent = addParent(pathSegments);

			entry.Name = pathSegments[pathSegments.Count - 1];
			entry.Parent = parent;

			add(parent, entry);
		}

		private static void add(DirectoryEntry<TData> parentDirectory, Entry<TData> entry)
		{
			switch (entry)
			{
				case DirectoryEntry<TData> directory:
					parentDirectory.Directories.Add(directory.Name, directory);
					break;
				case FileEntry<TData> file:
					parentDirectory.Files.Add(file.Name, file);
					break;
				case UnclassifiedEntry<TData> unclassified:
					parentDirectory.UnclassifiedEntries.Add(unclassified.Name, unclassified);
					break;

				default:
					throw new NotSupportedException($"{entry.GetType().FullName} is not supported");
			}
		}

		private DirectoryEntry<TData> addParent(IList<string> pathSegments)
		{
			DirectoryEntry<TData> parent = this;

			for (int i = 0; i < pathSegments.Count - 1; i++)
			{
				string pathSegment = pathSegments[i];
				if (!parent.Directories.TryGetValue(pathSegment, out var subdirectory))
				{
					subdirectory = new DirectoryEntry<TData>(this)
					{
						Name = pathSegment,
						Parent = parent
					};

					if (_defaultData != null)
						subdirectory.Data = _defaultData();

					parent.Directories.Add(pathSegment, subdirectory);
				}

				parent = subdirectory;
			}

			return parent;
		}



		private readonly Func<TData> _defaultData;

		internal readonly object Sync = new object();
	}
}