using IndexExercise.Index.Collections;
using NUnit.Framework;

namespace IndexExercise.Index.Test
{
	[TestFixture]
	public class BinarySearchTests
	{
		[TestCase(0, ExpectedResult = 0)]
		[TestCase(1, ExpectedResult = 1)]
		[TestCase(2, ExpectedResult = 2)]
		[TestCase(3, ExpectedResult = 3)]
		[TestCase(4, ExpectedResult = 4)]
		[TestCase(5, ExpectedResult = -1)]
		public int Binary_search_first_element_greater_than_or_equal(int value) =>
			_values.BinarySearchFirstIndexOf(v => v >= value);
		
		[TestCase(0, ExpectedResult = 0)]
		[TestCase(1, ExpectedResult = 1)]
		[TestCase(2, ExpectedResult = 2)]
		[TestCase(3, ExpectedResult = 3)]
		[TestCase(4, ExpectedResult = 4)]
		[TestCase(-1, ExpectedResult = -1)]
		public int Binary_search_last_element_less_than_or_equal(int value) =>
			_values.BinarySearchLastIndexOf(v => v <= value);

		[Test]
		public void Search_first_element_returns_minus_one_on_empty_list()
		{
			Assert.That(new int[] { }.BinarySearchFirstIndexOf(_ => true), Is.EqualTo(-1));
			Assert.That(new int[] { }.BinarySearchFirstIndexOf(_ => false), Is.EqualTo(-1));
		}

		[Test]
		public void Search_last_element_returns_minus_one_on_empty_list()
		{
			Assert.That(new int[] { }.BinarySearchLastIndexOf(_ => true), Is.EqualTo(-1));
			Assert.That(new int[] { }.BinarySearchLastIndexOf(_ => false), Is.EqualTo(-1));
		}

		private static readonly int[] _values = { 0, 1, 2, 3, 4 };
	}
}