using System;
using System.Windows.Forms;

namespace IndexExercise.Index.Demo
{
	public class FixedRichTextBox : RichTextBox
	{
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			
			if (AutoWordSelection)
				return;
			
			AutoWordSelection = true;
			AutoWordSelection = false;
		}
	}
}