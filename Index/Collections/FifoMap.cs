using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	/// <summary>
	/// A <see cref="TKey"/> - <see cref="TValue"/> map that maintains <see cref="TKey"/>s ordered by
	/// the order in which they were added i.e. the keys are ordered in a queue. In difference from
	/// a regular queue <see cref="Remove"/> of arbitrary element is supported while the queue would
	/// only support removing the first <see cref="TValue"/>.
	/// </summary>
	internal class FifoMap<TKey, TValue>
	{
		/// <summary>
		/// A <see cref="TKey"/> - <see cref="TValue"/> map that maintains <see cref="TKey"/>s ordered by
		/// the order in which they were added i.e. the keys are ordered in a queue. In difference from
		/// a regular queue <see cref="Remove"/> of arbitrary element is supported while the queue would
		/// only support removing the first <see cref="TValue"/>.
		/// </summary>
		public FifoMap()
		{
			_keys = new SortedSet<TKey>(Comparer<TKey>.Create((el1, el2) =>
				Comparer<long>.Default.Compare(_order[el1], _order[el2])));
		}

		public void Enqueue(TKey key, TValue value)
		{
			lock (_sync)
			{
				_order.Add(key, _counter++);
				_keys.Add(key);
				_map.Add(key, value);
			}
		}

		public (TKey key, TValue value) TryDequeue()
		{
			lock (_sync)
			{
				if (_keys.Count == 0)
					return (default(TKey), default(TValue));

				var key = _keys.Min;
				var value = _map[key];

				_keys.Remove(key);
				_order.Remove(key);
				_map.Remove(key);

				return (key, value);
			}
		}

		public void Remove(TKey key)
		{
			lock (_sync)
			{
				_keys.Remove(key);
				_order.Remove(key);
				_map.Remove(key);
			}
		}

		private readonly SortedSet<TKey> _keys;
		private readonly Dictionary<TKey, long> _order = new Dictionary<TKey, long>();
		private readonly Dictionary<TKey, TValue> _map = new Dictionary<TKey, TValue>();
		private long _counter;

		private readonly object _sync = new object();
	}
}