using System;
using System.Windows.Forms;

namespace IndexExercise.Index.Demo
{
	public static class ControlHelpers
	{
		public static void Invoke(this Control value, Action method)
		{
			if (value.IsDisposed || value.Disposing)
				return;

			try
			{
				value.Invoke(method);
			}
			catch (ObjectDisposedException)
			{
			}
		}
	}
}