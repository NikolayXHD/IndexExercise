using System;
using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	/// <summary>
	/// A collection of unique <see cref="TElement"/>s that maintains them ordered by the order in 
	/// which  they were added i.e. the keys are ordered in a queue. In difference from a regular 
	/// queue <see cref="TryRemove"/> of arbitrary element is supported while the queue would only 
	/// support removing the first <see cref="TElement"/>.
	/// </summary>
	public class RandomAccessQueue<TElement>
	{
		/// <summary>
		/// A collection of unique <see cref="TElement"/>s that maintains them ordered by the order in 
		/// which  they were added i.e. the keys are ordered in a queue. In difference from a regular 
		/// queue <see cref="TryRemove"/> of arbitrary element is supported while the queue would only 
		/// support removing the first <see cref="TElement"/>.
		/// </summary>
		public RandomAccessQueue()
		{
			_elements = new SortedSet<TElement>(Comparer<TElement>.Create((el1, el2) =>
				Comparer<long>.Default.Compare(_priorities[el1], _priorities[el2])));
		}

		public bool TryEnqueue(TElement element)
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

		public TElement TryDequeue()
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

		public TElement TryPeek()
		{
			lock (Sync)
			{
				if (_elements.Count == 0)
					return default(TElement);

				return _elements.Min;
			}
		}

		public bool TryRemove(TElement element)
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