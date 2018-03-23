using System;
using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	/// <summary>
	/// A collection of unique <see cref="TValue"/>s that maintains them ordered by the order in
	/// which  they were added i.e. in a queue. In difference from a regular queue
	/// <see cref="Remove"/> of arbitrary element is supported while the queue would only support
	/// removing the first <see cref="TValue"/>.
	/// </summary>
	internal class FifoSet<TValue>
	{
		/// <summary>
		/// A collection of unique <see cref="TValue"/>s that maintains them ordered by the order in
		/// which  they were added i.e. in a queue. In difference from a regular queue
		/// <see cref="Remove"/> of arbitrary element is supported while the queue would only support
		/// removing the first <see cref="TValue"/>.
		/// </summary>
		public FifoSet()
		{
			_values = new SortedSet<TValue>(Comparer<TValue>.Create((el1, el2) =>
				Comparer<long>.Default.Compare(_order[el1], _order[el2])));
		}

		public bool TryEnqueue(TValue element)
		{
			lock (Sync)
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
			lock (Sync)
			{
				if (_values.Count == 0)
					return default(TValue);

				var element = _values.Min;
				_values.Remove(element);
				_order.Remove(element);

				return element;
			}
		}

		public TValue TryPeek()
		{
			lock (Sync)
			{
				if (_values.Count == 0)
					return default(TValue);

				return _values.Min;
			}
		}

		public bool TryRemove(TValue element)
		{
			lock (Sync)
			{
				if (!_order.ContainsKey(element))
					return false;

				remove(element);
			}

			return true;
		}

		public void Remove(TValue element)
		{
			lock (Sync)
				remove(element);
		}

		private void remove(TValue element)
		{
			_values.Remove(element);
			_order.Remove(element);
		}

		public bool Contains(TValue element)
		{
			lock (Sync)
				return _order.ContainsKey(element);
		}

		public object Sync { get; } = new object();

		public event EventHandler<TValue> Enqueued;

		private readonly SortedSet<TValue> _values;
		private readonly Dictionary<TValue, long> _order = new Dictionary<TValue, long>();
		private long _counter;
	}
}