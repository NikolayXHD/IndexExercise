using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	/// <summary>
	/// A <see cref="TKey"/> - <see cref="TValue"/> map that maintains <see cref="TKey"/>s ordered by
	/// the order in which they were added i.e. the keys are ordered in a queue. In difference from
	/// a regular queue <see cref="Remove"/> of arbitrary element is supported while the queue would
	/// only support removing the first <see cref="TValue"/>.
	/// </summary>
	public class RandomAccessQueueMap<TKey, TValue>
	{
		/// <summary>
		/// A <see cref="TKey"/> - <see cref="TValue"/> map that maintains <see cref="TKey"/>s ordered by
		/// the order in which they were added i.e. the keys are ordered in a queue. In difference from
		/// a regular queue <see cref="Remove"/> of arbitrary element is supported while the queue would
		/// only support removing the first <see cref="TValue"/>.
		/// </summary>
		public RandomAccessQueueMap()
		{
			_elements = new SortedSet<TKey>(Comparer<TKey>.Create((el1, el2) =>
				Comparer<long>.Default.Compare(_priorities[el1], _priorities[el2])));
		}

		public void Enqueue(TKey key, TValue value)
		{
			lock (_sync)
			{
				_priorities.Add(key, _counter++);
				_elements.Add(key);
				_state.Add(key, value);
			}
		}

		public (TKey key, TValue value) TryDequeue()
		{
			lock (_sync)
			{
				if (_elements.Count == 0)
					return (default(TKey), default(TValue));

				var key = _elements.Min;
				var value = _state[key];

				_elements.Remove(key);
				_priorities.Remove(key);
				_state.Remove(key);

				return (key, value);
			}
		}

		public void Remove(TKey key)
		{
			lock (_sync)
			{
				_elements.Remove(key);
				_priorities.Remove(key);
				_state.Remove(key);
			}
		}

		private readonly SortedSet<TKey> _elements;
		private readonly Dictionary<TKey, long> _priorities = new Dictionary<TKey, long>();
		private readonly Dictionary<TKey, TValue> _state = new Dictionary<TKey, TValue>();
		private long _counter;

		private readonly object _sync = new object();
	}
}