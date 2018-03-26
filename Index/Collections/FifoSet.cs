using System;
using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	/// <summary>
	/// A collection of unique <see cref="TValue"/>s that maintains them ordered by the order in
	/// which  they were added i.e. in a queue. In difference from a regular queue
	/// <see cref="TryRemove"/> of arbitrary element is supported while the queue would only support
	/// removing the first <see cref="TValue"/>.
	/// </summary>
	internal class FifoSet<TValue>
	{
		/// <summary>
		/// A collection of unique <see cref="TValue"/>s that maintains them ordered by the order in
		/// which  they were added i.e. in a queue. In difference from a regular queue
		/// <see cref="TryRemove"/> of arbitrary element is supported while the queue would only support
		/// removing the first <see cref="TValue"/>.
		/// </summary>
		public FifoSet(IEqualityComparer<TValue> comparer = null)
		{
			_values = new SortedSet<TValue>(Comparer<TValue>.Create((el1, el2) =>
				Comparer<long>.Default.Compare(_order[el1], _order[el2])));

			_order = new Dictionary<TValue, long>(comparer ?? EqualityComparer<TValue>.Default);
		}

		public bool TryEnqueue(TValue element)
		{
			lock (_sync)
			{
				if (_order.ContainsKey(element))
					return false;

				_order.Add(element, _counter++);
				_values.Add(element);
			}

			Enqueued?.Invoke(this, element);
			return true;
		}

		public TValue TryDequeue()
		{
			lock (_sync)
			{
				if (_values.Count == 0)
					return default(TValue);

				var element = _values.Min;
				_values.Remove(element);
				_order.Remove(element);

				return element;
			}
		}

		public bool TryRemove(TValue element)
		{
			lock (_sync)
			{
				if (!_order.ContainsKey(element))
					return false;

				_values.Remove(element);
				_order.Remove(element);
			}

			return true;
		}

		public event EventHandler<TValue> Enqueued;



		private long _counter;

		private readonly SortedSet<TValue> _values;
		private readonly Dictionary<TValue, long> _order;
		private readonly object _sync = new object();
	}
}