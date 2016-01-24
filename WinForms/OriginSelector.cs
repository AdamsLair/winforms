using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

namespace AdamsLair.WinForms
{
	public class OriginSelector : Control
	{
		public enum Origin
		{
			None = -1,

			TopLeft,
			Top,
			TopRight,
			Left,
			Center,
			Right,
			BottomLeft,
			Bottom,
			BottomRight
		}


		protected Origin	hoverOrigin		= Origin.None;
		protected Origin	selOrigin		= Origin.None;
		protected Color		selColor		= SystemColors.Highlight;
		protected bool		invertArrowsH	= false;
		protected bool		invertArrowsV	= false;
		protected Image[]	arrows			= new Image[9];


		[DefaultValue(Origin.None)]
		public Origin SelectedOrigin
		{
			get { return this.selOrigin; }
			set 
			{
				if (this.selOrigin != value)
				{
					this.selOrigin = value;
					this.Invalidate();
				}
			}
		}
		[DefaultValue(false)]
		public bool InvertArrowsHorizontal
		{
			get { return this.invertArrowsH; }
			set 
			{
				if (this.invertArrowsH != value)
				{
					this.invertArrowsH = value;
					this.Invalidate();
				}
			}
		}
		[DefaultValue(false)]
		public bool InvertArrowsVertical
		{
			get { return this.invertArrowsV; }
			set 
			{
				if (this.invertArrowsV != value)
				{
					this.invertArrowsV = value;
					this.Invalidate();
				}
			}
		}
		public Color SelectionColor
		{
			get { return this.selColor; }
			set { this.selColor = value; }
		}


		public OriginSelector()
		{
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.ResizeRedraw, true);
			this.SetStyle(ControlStyles.Opaque, true);
			this.DoubleBuffered = true;

			this.arrows[0] = Properties.ResourcesCache.ArrowUpLeft;
			this.arrows[1] = Properties.ResourcesCache.ArrowUp;
			this.arrows[2] = Properties.ResourcesCache.ArrowUpRight;
			this.arrows[3] = Properties.ResourcesCache.ArrowLeft;
			this.arrows[4] = null;
			this.arrows[5] = Properties.ResourcesCache.ArrowRight;
			this.arrows[6] = Properties.ResourcesCache.ArrowDownLeft;
			this.arrows[7] = Properties.ResourcesCache.ArrowDown;
			this.arrows[8] = Properties.ResourcesCache.ArrowDownRight;
		}

		protected Size GetButtonSize()
		{
			return new Size(
				(int)Math.Floor((float)this.ClientRectangle.Width / 3.0f),
				(int)Math.Floor((float)this.ClientRectangle.Height / 3.0f));
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics g = e.Graphics;

			Size buttonSize = this.GetButtonSize();
			Pen gridPen = new Pen(Color.FromArgb(96, this.ForeColor));
			ColorMatrix arrowColorMatrix = new ColorMatrix(new float[][] {
				new float[] {this.ForeColor.R / 255.0f, 0.0f, 0.0f, 0.0f, 0.0f},
				new float[] {0.0f, this.ForeColor.G / 255.0f, 0.0f, 0.0f, 0.0f},
				new float[] {0.0f, 0.0f, this.ForeColor.B / 255.0f, 0.0f, 0.0f},
				new float[] {0.0f, 0.0f, 0.0f, this.ForeColor.A / 255.0f, 0.0f},
				new float[] {0.0f, 0.0f, 0.0f, 0.0f, 1.0f}});
			ImageAttributes arrowAttributes = new ImageAttributes();
			arrowAttributes.SetColorMatrix(arrowColorMatrix);

			// Background and border
			g.FillRectangle(new SolidBrush(this.BackColor), this.ClientRectangle);
			g.DrawRectangle(gridPen, this.ClientRectangle.X, this.ClientRectangle.Y, this.ClientRectangle.Width - 1, this.ClientRectangle.Height - 1);

			for (int x = 1; x < 3; x++)
			{
				g.DrawLine(gridPen,
					x * buttonSize.Width, this.ClientRectangle.Top,
					x * buttonSize.Width, this.ClientRectangle.Bottom);
			}
			for (int y = 1; y < 3; y++)
			{
				g.DrawLine(gridPen,
					this.ClientRectangle.Left, y * buttonSize.Height,
					this.ClientRectangle.Right, y * buttonSize.Height);
			} 

			Origin origTemp	= Origin.None;
			Point posUL		= new Point();
			Point posLR		= new Point();
			for (int y = 0; y < 3; y++)
			{
				for (int x = 0; x < 3; x++)
				{
					origTemp = (Origin)(x + y * 3);

					posUL.X = x * buttonSize.Width;
					posUL.Y = y * buttonSize.Width;
					posLR.X = -1 + (x + 1) * buttonSize.Width;
					posLR.Y = -1 + (y + 1) * buttonSize.Height;

					Image imgTemp = null;

					if (((int)origTemp + 1) % 3 != 0 && origTemp == this.selOrigin - 1)
						imgTemp = this.arrows[(int)Origin.Left];
					else if (((int)origTemp - 3) % 3 != 0 && origTemp == this.selOrigin + 1)
						imgTemp = this.arrows[(int)Origin.Right];
					else if ((int)origTemp < 6 && origTemp == this.selOrigin - 3)
						imgTemp = this.arrows[(int)Origin.Top];
					else if ((int)origTemp > 2 && origTemp == this.selOrigin + 3)
						imgTemp = this.arrows[(int)Origin.Bottom];
					else if (((int)origTemp + 1) % 3 != 0 && (int)origTemp < 6 && origTemp == this.selOrigin - 4)
						imgTemp = this.arrows[(int)Origin.TopLeft];
					else if (((int)origTemp - 3) % 3 != 0 && (int)origTemp < 6 && origTemp == this.selOrigin - 2)
						imgTemp = this.arrows[(int)Origin.TopRight];
					else if (((int)origTemp + 1) % 3 != 0 && (int)origTemp > 2 && origTemp == this.selOrigin + 2)
						imgTemp = this.arrows[(int)Origin.BottomLeft];
					else if (((int)origTemp - 3) % 3 != 0 && (int)origTemp > 2 && origTemp == this.selOrigin + 4)
						imgTemp = this.arrows[(int)Origin.BottomRight];

					if (this.invertArrowsH)
					{
						if (imgTemp == this.arrows[(int)Origin.Left])
							imgTemp = this.arrows[(int)Origin.Right];
						else if (imgTemp == this.arrows[(int)Origin.Right])
							imgTemp = this.arrows[(int)Origin.Left];
						else if (imgTemp == this.arrows[(int)Origin.TopRight])
							imgTemp = this.arrows[(int)Origin.TopLeft];
						else if (imgTemp == this.arrows[(int)Origin.BottomRight])
							imgTemp = this.arrows[(int)Origin.BottomLeft];
						else if (imgTemp == this.arrows[(int)Origin.TopLeft])
							imgTemp = this.arrows[(int)Origin.TopRight];
						else if (imgTemp == this.arrows[(int)Origin.BottomLeft])
							imgTemp = this.arrows[(int)Origin.BottomRight];
					}
					if (this.invertArrowsV)
					{
						if (imgTemp == this.arrows[(int)Origin.Top])
							imgTemp = this.arrows[(int)Origin.Bottom];
						else if (imgTemp == this.arrows[(int)Origin.Bottom])
							imgTemp = this.arrows[(int)Origin.Top];
						else if (imgTemp == this.arrows[(int)Origin.TopLeft])
							imgTemp = this.arrows[(int)Origin.BottomLeft];
						else if (imgTemp == this.arrows[(int)Origin.TopRight])
							imgTemp = this.arrows[(int)Origin.BottomRight];
						else if (imgTemp == this.arrows[(int)Origin.BottomLeft])
							imgTemp = this.arrows[(int)Origin.TopLeft];
						else if (imgTemp == this.arrows[(int)Origin.BottomRight])
							imgTemp = this.arrows[(int)Origin.TopRight];
					}

					if (imgTemp != null)
					{
						Size displayedSize = new Size(
							Math.Min(Math.Min(imgTemp.Width, buttonSize.Width - 2), buttonSize.Width * 3 / 5),
							Math.Min(Math.Min(imgTemp.Height, buttonSize.Height - 2), buttonSize.Height * 3 / 5));
						g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
						g.DrawImage(imgTemp,
							new Rectangle(
								posUL.X + buttonSize.Width / 2 - displayedSize.Width / 2, 
								posUL.Y + buttonSize.Height / 2 - displayedSize.Width / 2,
								displayedSize.Width,
								displayedSize.Height),
							0, 0, imgTemp.Width, imgTemp.Height,
							GraphicsUnit.Pixel,
							arrowAttributes);
					}
				}
			}
			
			// Highlight selected origin
			if (this.selOrigin != Origin.None)
			{
				int x = ((int)this.selOrigin) % 3;
				int y = ((int)this.selOrigin) / 3;

				posUL.X = x * buttonSize.Width;
				posUL.Y = y * buttonSize.Width;
				posLR.X = -1 + (x + 1) * buttonSize.Width;
				posLR.Y = -1 + (y + 1) * buttonSize.Height;

				g.FillRectangle(new SolidBrush(Color.FromArgb(64, this.selColor)), 
					posUL.X, 
					posUL.Y, 
					posLR.X - posUL.X + 1, 
					posLR.Y - posUL.Y + 1);
				g.DrawRectangle(new Pen(this.selColor), 
					posUL.X, 
					posUL.Y, 
					posLR.X - posUL.X + 1, 
					posLR.Y - posUL.Y + 1);
			}
			
			// Highlight hovered origin
			if (this.hoverOrigin != Origin.None)
			{
				int x = ((int)this.hoverOrigin) % 3;
				int y = ((int)this.hoverOrigin) / 3;

				posUL.X = x * buttonSize.Width;
				posUL.Y = y * buttonSize.Width;
				posLR.X = -1 + (x + 1) * buttonSize.Width;
				posLR.Y = -1 + (y + 1) * buttonSize.Height;

				g.FillRectangle(new SolidBrush(Color.FromArgb(32, this.selColor)), 
					posUL.X, 
					posUL.Y, 
					posLR.X - posUL.X + 1, 
					posLR.Y - posUL.Y + 1);
				g.DrawRectangle(new Pen(Color.FromArgb(96, this.selColor)),
					posUL.X, 
					posUL.Y, 
					posLR.X - posUL.X + 1, 
					posLR.Y - posUL.Y + 1);
			}

			if (!this.Enabled)
			{
				g.FillRectangle(new SolidBrush(Color.FromArgb(128, this.BackColor)), this.ClientRectangle);
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			
			Size buttonSize = GetButtonSize();
			int hoverInt = 0;
			hoverInt += Math.Min(2, e.X / buttonSize.Width);
			hoverInt += 3 * Math.Min(2, e.Y / buttonSize.Height);

			Origin lastHover = this.hoverOrigin;
			this.hoverOrigin = (Origin)hoverInt;

			if (lastHover != this.hoverOrigin)
			{
				this.Invalidate();
			}
		}
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			this.hoverOrigin = Origin.None;
			this.Invalidate();
		}
		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			this.SelectedOrigin = this.hoverOrigin;
		}
		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			this.Invalidate();
		}

		private bool ShouldSerializeSelectionColor()
		{
			return this.selColor != SystemColors.Highlight;
		}
	}
}
