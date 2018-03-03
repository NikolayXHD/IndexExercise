using System.Collections.Generic;
using System.Linq;

namespace IndexExercise.Index.FileSystem
{
	public abstract class Entry<TData>
	{
		protected Entry(RootEntry<TData> root)
		{
			Root = root;
		}

		public abstract EntryType Type { get; }
		public TData Data { get; set; }
		public string Name { get; internal set; }
		public DirectoryEntry<TData> Parent { get; internal set; }
		public RootEntry<TData> Root { get; }



		public IEnumerable<Entry<TData>> GetSelfAndAncestors()
		{
			lock (Root.Sync)
			{
				var current = this;

				while (true)
				{
					yield return current;

					var parent = current.Parent;

					if (parent == null || parent is RootEntry<TData>)
						yield break;

					current = parent;
				}
			}
		}

		/// <summary>
		/// Returns entry path or null if entry was removed
		/// </summary>
		public string GetPath()
		{
			var entries = GetSelfAndAncestors()
				.Reverse()
				.ToArray();

			if (entries[0].Parent?.Type != EntryType.Root)
				return null;

			var path = PathString.Combine(entries.Select(_=>_.Name));
			return path;
		}
	}
}