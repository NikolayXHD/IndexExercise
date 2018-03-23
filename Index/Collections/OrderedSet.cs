using System;
using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	/// <summary>
	/// A collection of <see cref="TValue"/>s ordered by a supplied <see cref="TOrder"/> value.
	/// </summary>
	internal class OrderedSet<TValue, TOrder>
		where TOrder : IComparable<TOrder>
	{
		/// <summary>
		/// A collection of <see cref="TValue"/>s ordered by a supplied <see cref="TOrder"/> value.
		/// </summary>
		public OrderedSet()
		{
			_elements = new SortedSet<TValue>(Comparer<TValue>.Create(compare));
		}

		/// <summary>
		/// Adds an <see cref="element"/> to a set ordered by <see cref="order"/> value
		/// </summary>
		public void Add(TValue element, TOrder order)
		{
			lock (_sync)
			{
				_order.Add(element, order);
				_elements.Add(element);
			}
		}

		/// <summary>
		/// Removes an <see cref="element"/> if it exists
		/// </summary>
		public bool Remove(TValue element)
		{
			lock (_sync)
			{
				if (!_order.ContainsKey(element))
					return false;

				_elements.Remove(element);
				_order.Remove(element);

				return true;
			}
		}

		/// <summary>
		/// Removes and returns an <see cref="TValue"/> with minimum <see cref="TOrder"/>.
		/// When empty returns default(<see cref="TValue"/>).
		/// </summary>
		public TValue TryRemoveMin()
		{
			lock (_sync)
			{
				if (_elements.Count == 0)
					return default(TValue);

				var element = _elements.Min;
				
				_elements.Remove(element);
				_order.Remove(element);

				return element;
			}
		}

		private int compare(TValue el1, TValue el2)
		{
			var comparePriorityResult = _order[el1].CompareTo(_order[el2]);

			if (comparePriorityResult != 0)
				return comparePriorityResult;

			int compareElementsResult = Comparer<TValue>.Default.Compare(el1, el2);
			return compareElementsResult;
		}

		private readonly SortedSet<TValue> _elements;
		private readonly Dictionary<TValue, TOrder> _order = new Dictionary<TValue, TOrder>();
		private readonly object _sync = new object();
	}
}