using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AdamsLair.WinForms.ColorControls
{
	public class ColorShowBox : Control
	{
		private	Color	upperColor	= Color.Transparent;
		private	Color	lowerColor	= Color.Transparent;


		public event EventHandler UpperClick = null;
		public event EventHandler LowerClick = null;

		
		public Rectangle ColorAreaRectangle
		{
			get { return new Rectangle(
				this.ClientRectangle.X + 2,
				this.ClientRectangle.Y + 2,
				this.ClientRectangle.Width - 4,
				this.ClientRectangle.Height - 4); }
		}
		public Color Color
		{
			get { return this.upperColor; }
			set { this.upperColor = this.lowerColor = value; this.Invalidate(); }
		}
		public Color UpperColor
		{
			get { return this.upperColor; }
			set { this.upperColor = value; this.Invalidate(); }
		}
		public Color LowerColor
		{
			get { return this.lowerColor; }
			set { this.lowerColor = value; this.Invalidate(); }
		}


		public ColorShowBox()
		{
			this.DoubleBuffered = true;
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.Opaque, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.SetStyle(ControlStyles.ResizeRedraw, true);
		}
		
		protected void OnUpperClick()
		{
			if (this.UpperClick != null)
				this.UpperClick(this, null);
		}
		protected void OnLowerClick()
		{
			if (this.LowerClick != null)
				this.LowerClick(this, null);
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (e.Y > (this.ClientRectangle.Top + this.ClientRectangle.Bottom) / 2)
				this.OnLowerClick();
			else
				this.OnUpperClick();
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			Rectangle colorBoxOuter = new Rectangle(
				this.ClientRectangle.X,
				this.ClientRectangle.Y,
				this.ClientRectangle.Width - 1,
				this.ClientRectangle.Height - 1);
			Rectangle colorBoxInner = new Rectangle(
				colorBoxOuter.X + 1,
				colorBoxOuter.Y + 1,
				colorBoxOuter.Width - 2,
				colorBoxOuter.Height - 2);
			Rectangle colorArea = this.ColorAreaRectangle;

			e.Graphics.FillRectangle(new HatchBrush(HatchStyle.LargeCheckerBoard, Color.LightGray, Color.Gray), colorArea);
			e.Graphics.FillRectangle(new SolidBrush(this.upperColor),
				colorArea.X,
				colorArea.Y,
				colorArea.Width,
				colorArea.Height / 2 + 1);
			e.Graphics.FillRectangle(new SolidBrush(this.lowerColor),
				colorArea.X,
				colorArea.Y + colorArea.Height / 2 + 1,
				colorArea.Width,
				colorArea.Height / 2);

			e.Graphics.DrawRectangle(SystemPens.ControlDark, colorBoxOuter);
			e.Graphics.DrawRectangle(SystemPens.ControlLightLight, colorBoxInner);
		}
	}
}
