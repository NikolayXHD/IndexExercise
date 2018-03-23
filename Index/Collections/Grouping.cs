using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	/// <summary>
	/// Groups added <see cref="TValue"/>s by <see cref="TKey"/>
	/// to a sets of unique <see cref="TValue"/>s
	/// </summary>
	internal class Grouping<TKey, TValue>
	{
		/// <summary>
		/// Groups added <see cref="TValue"/>s by <see cref="TKey"/>
		/// to a sets of unique <see cref="TValue"/>s
		/// </summary>
		public Grouping()
		{
		}

		/// <summary>
		/// Adds a <see cref="value"/> if it is still not associated with a <see cref="key"/>
		/// </summary>
		/// <returns>Returns false if the <see cref="value"/> is already associated with the <see cref="key"/></returns>
		public bool Add(TKey key, TValue value)
		{
			lock (_sync)
			{
				if (!_values.TryGetValue(key, out var values))
				{
					values = new HashSet<TValue>();
					_values.Add(key, values);
				}

				return values.Add(value);
			}
		}

		/// <summary>
		/// Removes the <see cref="value"/> from a set accociated with a <see cref="key"/>
		/// </summary>
		/// <returns>Returns false if there was no <see cref="value"/> to remove</returns>
		public bool Remove(TKey key, TValue value)
		{
			lock (_sync)
			{
				if (!_values.TryGetValue(key, out var values))
					return false;

				bool result = values.Remove(value);

				if (values.Count == 0)
					_values.Remove(key);

				return result;
			}
		}

		public bool ContainsKey(TKey key)
		{
			lock (_sync)
				return _values.ContainsKey(key);
		}

		public IEnumerable<TValue> this[TKey key]
		{
			get
			{
				lock (_sync)
				{
					if (!_values.TryGetValue(key, out var values))
						yield break;

					foreach (var value in values)
						yield return value;
				}
			}
		}


		private readonly Dictionary<TKey, HashSet<TValue>> _values = new Dictionary<TKey, HashSet<TValue>>();
		private readonly object _sync = new object();
	}
}