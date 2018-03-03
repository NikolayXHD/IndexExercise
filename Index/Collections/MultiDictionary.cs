using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	public class MultiDictionary<TKey, TValue>
	{
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