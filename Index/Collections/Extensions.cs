using System;
using System.Collections.Generic;

namespace IndexExercise.Index.Collections
{
	internal static class Extensions
	{
		/// <summary>
		/// Performs a binary search of the first element that meets a specified <see cref="criterion"/>.
		/// Assumes that if some element meets the <see cref="criterion"/>. then all subsequent elements also do.
		/// </summary>
		/// <returns> Index of the first found element or -1 if none meets the <see cref="criterion"/>.</returns>
		public static int BinarySearchFirstIndexOf<T>(this IList<T> list, Func<T, bool> criterion)
		{
			if (list.Count == 0)
				return -1;

			return binarySearchFirstIndex(list, criterion, 0, list.Count);
		}

		/// <summary>
		/// Performs a binary search of the last element that meets a specified <see cref="criterion"/>..
		/// Assumes that if some element meets the <see cref="criterion"/>. then all previous elements also do.
		/// </summary>
		/// <returns> Index of the last found element or -1 if none meets the criterion.</returns>
		public static int BinarySearchLastIndexOf<T>(this IList<T> list, Func<T, bool> criterion)
		{
			var criterionStopsBeingTrueAt = BinarySearchFirstIndexOf(list, _ => !criterion(_));
			if (criterionStopsBeingTrueAt == -1)
				return list.Count - 1;

			return criterionStopsBeingTrueAt - 1;
		}

		private static int binarySearchFirstIndex<T>(this IList<T> list, Func<T, bool> criterion, int left, int count)
		{
			if (criterion(list[left]))
				return left;

			if (count == 1)
				return -1;

			var middle = left + count / 2;

			var searchRightHalfResult = binarySearchFirstIndex(list, criterion, middle, count - count / 2);

			if (searchRightHalfResult > middle)
				return searchRightHalfResult;

			if (searchRightHalfResult == -1)
				return -1;

			// searchRightHalfResult == middle

			var newCount = middle - left - 1;
			if (newCount == 0)
				return middle;

			var newLeft = left + 1;
			var searchLeftResult = binarySearchFirstIndex(list, criterion, newLeft, newCount);

			if (searchLeftResult == -1)
				return middle;

			return searchLeftResult;
		}
	}
}