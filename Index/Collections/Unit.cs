using System.Collections.Generic;
using System.Linq;

namespace IndexExercise.Index.Collections
{
	public static class Unit
	{
		public static IEnumerable<T> Sequence<T>(T element) => Enumerable.Repeat(element, 1);
	}
}