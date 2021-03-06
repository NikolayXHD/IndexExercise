﻿using System.Collections.Generic;

namespace IndexExercise.Index
{
	/// <summary>
	/// A query to be searched against by <see cref="IIndexEngine"/>
	/// </summary>
	public interface IQuery
	{
		IList<string> Errors { get; }
		IList<string> Warnings { get; }
	}
}