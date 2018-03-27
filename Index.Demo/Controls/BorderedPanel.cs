using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace IndexExercise.Index.Demo
{
	public class BorderedPanel : Panel
	{
		public BorderedPanel()
		{
			SetStyle(
				ControlStyles.UserPaint |
				ControlStyles.AllPaintingInWmPaint |
				ControlStyles.DoubleBuffer |
				ControlStyles.ResizeRedraw,
				true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var graphics = e.Graphics;
			var borders = VisibleBorders;
			graphics.Clear(BackColor);
			
			var pen = new Pen(BorderColor);

			if (BackgroundImage != null)
				graphics.DrawImage(BackgroundImage, new Rectangle(Point.Empty, BackgroundImage.Size));

			if ((borders & AnchorStyles.Top) > 0)
				graphics.DrawLine(pen, 0, 0, Width - 1, 0);

			if ((borders & AnchorStyles.Bottom) > 0)
				graphics.DrawLine(pen, 0, Height - 1, Width - 1, Height - 1);

			if ((borders & AnchorStyles.Left) > 0)
				graphics.DrawLine(pen, 0, 0, 0, Height - 1);

			if ((borders & AnchorStyles.Right) > 0)
				graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height - 1);
		}

		[Category("Settings"), DefaultValue(typeof(Color), "DarkGray")]
		public Color BorderColor { get; set; } = Color.DarkGray;

		[Category("Settings"), DefaultValue(typeof (AnchorStyles), "Top|Right|Bottom|Left")]
		public AnchorStyles VisibleBorders { get; set; } = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Left;
	}
}