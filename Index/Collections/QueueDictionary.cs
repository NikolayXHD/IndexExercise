using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	public class QueueDictionary<TKey, TValue>
	{
		public QueueDictionary()
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

		public (TKey key, TValue value) Dequeue()
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