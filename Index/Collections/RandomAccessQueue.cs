using System;
using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	public class RandomAccessQueue<TElement>
	{
		public RandomAccessQueue()
		{
			_elements = new SortedSet<TElement>(Comparer<TElement>.Create((el1, el2) =>
				Comparer<long>.Default.Compare(_priorities[el1], _priorities[el2])));
		}

		public bool Add(TElement element)
		{
			lock (Sync)
			{
				if (_priorities.ContainsKey(element))
					return false;
				
				_priorities.Add(element, _counter++);
				_elements.Add(element);
			}

			Enqueued?.Invoke(this, element);
			return true;
		}

		public TElement Dequeue()
		{
			lock (Sync)
			{
				if (_elements.Count == 0)
					return default(TElement);

				var element = _elements.Min;
				_elements.Remove(element);
				_priorities.Remove(element);

				return element;
			}
		}

		public TElement Peek()
		{
			lock (Sync)
			{
				if (_elements.Count == 0)
					return default(TElement);

				var element = _elements.Min;

				return element;
			}
		}

		public bool Remove(TElement element)
		{
			lock (Sync)
			{
				if (!_priorities.ContainsKey(element))
					return false;

				_elements.Remove(element);
				_priorities.Remove(element);
			}

			return true;
		}

		public bool Contains(TElement element)
		{
			lock (Sync)
				return _priorities.ContainsKey(element);
		}

		public object Sync { get; } = new object();

		public event EventHandler<TElement> Enqueued;

		private readonly SortedSet<TElement> _elements;
		private readonly Dictionary<TElement, long> _priorities = new Dictionary<TElement, long>();
		private long _counter;
	}
}