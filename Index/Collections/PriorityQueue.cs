using System;
using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	/// <summary>
	/// An collection of <see cref="TElement"/>s ordered by a supplied <see cref="TPriority"/> value.
	/// <see cref="TryDequeue"/> returns an <see cref="TElement"/> with minumum <see cref="TPriority"/>.
	/// </summary>
	public class PriorityQueue<TElement, TPriority>
		where TPriority : IComparable<TPriority>
	{
		/// <summary>
		/// An collection of <see cref="TElement"/> ordered by a supplied <see cref="TPriority"/> value.
		/// <see cref="TryDequeue"/> returns an <see cref="TElement"/> with minumum <see cref="TPriority"/>.
		/// </summary>
		public PriorityQueue()
		{
			_elements = new SortedSet<TElement>(Comparer<TElement>.Create(compare));
		}

		/// <summary>
		/// Adds an <see cref="element"/>
		/// <see cref="TryDequeue"/> returns an <see cref="element"/> with minumum <see cref="priority"/>.
		/// </summary>
		public void Enqueue(TElement element, TPriority priority)
		{
			lock (_sync)
			{
				_priorities.Add(element, priority);
				_elements.Add(element);
			}
		}

		/// <summary>
		/// Removes an <see cref="element"/> if it exists
		/// </summary>
		public bool Remove(TElement element)
		{
			lock (_sync)
			{
				if (!_priorities.ContainsKey(element))
					return false;

				_elements.Remove(element);
				_priorities.Remove(element);

				return true;
			}
		}

		/// <summary>
		/// Removes and returns an <see cref="TElement"/> with minimum priority.
		/// When empty returns default(<see cref="TElement"/>).
		/// </summary>
		public TElement TryDequeue()
		{
			lock (_sync)
			{
				if (_elements.Count == 0)
					return default(TElement);

				var element = _elements.Min;
				
				_elements.Remove(element);
				_priorities.Remove(element);

				return element;
			}
		}

		private int compare(TElement el1, TElement el2)
		{
			var priorityResult = _priorities[el1].CompareTo(_priorities[el2]);

			if (priorityResult != 0)
				return priorityResult;

			if (el1.Equals(el2))
				return 0;

			int hashResult = el1.GetHashCode().CompareTo(el2.GetHashCode());

			if (hashResult != 0)
				return hashResult;

			return 1;
		}

		private readonly SortedSet<TElement> _elements;
		private readonly Dictionary<TElement, TPriority> _priorities = new Dictionary<TElement, TPriority>();
		private readonly object _sync = new object();
	}
}