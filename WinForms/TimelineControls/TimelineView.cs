using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using AdamsLair.WinForms.Drawing;
using AdamsLair.WinForms.NativeWinAPI;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineView : Panel
	{
		private struct SubAreaInfo
		{
			public bool Active;
			public int DesiredSize;

			public int Size
			{
				get { return this.Active ? this.DesiredSize : 0; }
				set { this.DesiredSize = value; this.Active = true; }
			}

			public SubAreaInfo(int size)
			{
				this.Active = size > 0;
				this.DesiredSize = size;
			}
		}


		private	ControlRenderer			renderer			= new ControlRenderer();
		private	TimelineViewUnitInfo	unitInfo			= new TimelineViewUnitInfo("Seconds", "s", 5);
		private	float					unitOffset			= 0.0f;
		private SubAreaInfo				areaTopRuler		= new SubAreaInfo(25);
		private SubAreaInfo				areaBottomRuler		= new SubAreaInfo(50);
		private SubAreaInfo				areaLeftSidebar		= new SubAreaInfo(100);
		private SubAreaInfo				areaRightSidebar	= new SubAreaInfo(75);

		private	Rectangle	rectTopRuler;
		private	Rectangle	rectBottomRuler;
		private	Rectangle	rectLeftSidebar;
		private	Rectangle	rectRightSidebar;
		private	Rectangle	rectContentArea;

		
		public ControlRenderer Renderer
		{
			get { return this.renderer; }
		}
		public TimelineViewUnitInfo UnitInfo
		{
			get { return this.unitInfo; }
			set { this.unitInfo = value; }
		}
		public float UnitOffset
		{
			get { return this.unitOffset; }
			set { this.unitOffset = value; }
		}
		public float UnitScroll
		{
			get { return this.unitInfo.ConvertToUnits(this.AutoScrollPosition.X); }
		}
		public bool HasTopRuler
		{
			get { return this.areaTopRuler.Active; }
			set { this.areaTopRuler.Active = value; this.UpdateGeometry(); }
		}
		public bool HasBottomRuler
		{
			get { return this.areaBottomRuler.Active; }
			set { this.areaBottomRuler.Active = value; this.UpdateGeometry(); }
		}
		public bool HasLeftSidebar
		{
			get { return this.areaLeftSidebar.Active; }
			set { this.areaLeftSidebar.Active = value; this.UpdateGeometry(); }
		}
		public bool HasRightSidebar
		{
			get { return this.areaRightSidebar.Active; }
			set { this.areaRightSidebar.Active = value; this.UpdateGeometry(); }
		}
		public int TopRulerSize
		{
			get { return this.areaTopRuler.DesiredSize; }
			set { this.areaTopRuler.DesiredSize = value; this.UpdateGeometry(); }
		}
		public int BottomRulerSize
		{
			get { return this.areaBottomRuler.DesiredSize; }
			set { this.areaBottomRuler.DesiredSize = value; this.UpdateGeometry(); }
		}
		public int LeftSidebarSize
		{
			get { return this.areaLeftSidebar.DesiredSize; }
			set { this.areaLeftSidebar.DesiredSize = value; this.UpdateGeometry(); }
		}
		public int RightSidebarSize
		{
			get { return this.areaRightSidebar.DesiredSize; }
			set { this.areaRightSidebar.DesiredSize = value; this.UpdateGeometry(); }
		}
		protected override CreateParams CreateParams
		{
			get
			{
				// We're dealing with both static and dynamic areas, so we'll need composited repaint operations
				CreateParams p = base.CreateParams;
				p.ExStyle |= (int)ExtendedWindowStyles.Composited;
				return p;
			}
		}


		public TimelineView()
		{
			this.AutoScroll = true;
			this.AutoScrollMinSize = new Size(1500, 800);

			this.SetStyle(ControlStyles.ResizeRedraw, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.Opaque, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.SetStyle(ControlStyles.Selectable, true);
		}

		private void UpdateGeometry()
		{
			Rectangle lastRectTopRuler		= this.rectTopRuler;
			Rectangle lastRectBottomRuler	= this.rectBottomRuler;
			Rectangle lastRectLeftSidebar	= this.rectLeftSidebar;
			Rectangle lastRectRightSidebar	= this.rectRightSidebar;
			Rectangle lastRectContentArea	= this.rectContentArea;

			this.rectTopRuler = new Rectangle(
				this.ClientRectangle.X + this.areaLeftSidebar.Size,
				this.ClientRectangle.Y + 0,
				this.ClientRectangle.Width - this.areaLeftSidebar.Size - this.areaRightSidebar.Size,
				this.areaTopRuler.Size);
			this.rectBottomRuler = new Rectangle(
				this.ClientRectangle.X + this.areaLeftSidebar.Size,
				this.ClientRectangle.Y + this.ClientRectangle.Height - this.areaBottomRuler.Size,
				this.ClientRectangle.Width - this.areaLeftSidebar.Size - this.areaRightSidebar.Size,
				this.areaBottomRuler.Size);
			this.rectLeftSidebar = new Rectangle(
				this.ClientRectangle.X,
				this.ClientRectangle.Y + this.areaTopRuler.Size,
				this.areaLeftSidebar.Size,
				this.ClientRectangle.Height - this.areaTopRuler.Size - this.areaBottomRuler.Size);
			this.rectRightSidebar = new Rectangle(
				this.ClientRectangle.X + this.ClientRectangle.Width - this.areaRightSidebar.Size,
				this.ClientRectangle.Y + this.areaTopRuler.Size,
				this.areaRightSidebar.Size,
				this.ClientRectangle.Height - this.areaTopRuler.Size - this.areaBottomRuler.Size);
			this.rectContentArea = new Rectangle(
				this.ClientRectangle.X + this.areaLeftSidebar.Size,
				this.ClientRectangle.Y + this.areaTopRuler.Size,
				this.ClientRectangle.Width - this.areaLeftSidebar.Size - this.areaRightSidebar.Size,
				this.ClientRectangle.Height - this.areaTopRuler.Size - this.areaBottomRuler.Size);

			if (this.rectTopRuler != lastRectTopRuler ||
				this.rectBottomRuler != lastRectBottomRuler ||
				this.rectLeftSidebar != lastRectLeftSidebar ||
				this.rectRightSidebar != lastRectRightSidebar ||
				this.rectContentArea != lastRectContentArea)
			{
				this.Invalidate();
			}
		}
		protected float GetUnitAtPos(float x)
		{
			return this.unitOffset + this.unitInfo.ConvertToUnits(x - this.rectContentArea.X);
		}
		protected float GetPosAtUnit(float unit)
		{
			return this.rectContentArea.X + this.unitInfo.ConvertToPixels(unit - this.unitOffset);
		}

		protected override void OnScroll(ScrollEventArgs se)
		{
			base.OnScroll(se);
			this.Invalidate();
		}
		protected override void OnResize(EventArgs eventargs)
		{
			base.OnResize(eventargs);
			this.UpdateGeometry();
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.FillRectangle(new SolidBrush(this.BackColor), this.ClientRectangle);

			GraphicsState state;

			// Draw the Top Ruler
			if (!this.rectTopRuler.IsEmpty)
			{
				state = e.Graphics.Save();
				e.Graphics.SetClip(this.rectTopRuler, CombineMode.Intersect);
				if (!e.Graphics.ClipBounds.IsEmpty)
				{
					this.OnPaintRuler(e.Graphics, this.rectTopRuler, true);
				}
				e.Graphics.Restore(state);
			}

			// Draw the Bottom Ruler
			if (!this.rectBottomRuler.IsEmpty)
			{
				state = e.Graphics.Save();
				e.Graphics.SetClip(this.rectBottomRuler, CombineMode.Intersect);
				if (!e.Graphics.ClipBounds.IsEmpty)
				{
					this.OnPaintRuler(e.Graphics, this.rectBottomRuler, false);
				}
				e.Graphics.Restore(state);
			}

			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Red)), this.rectContentArea);
			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Purple)), this.rectLeftSidebar);
			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.CornflowerBlue)), this.rectRightSidebar);

			StringFormat format = StringFormat.GenericDefault.Clone() as StringFormat;
			format.Alignment = StringAlignment.Center;
			format.LineAlignment = StringAlignment.Center;
			e.Graphics.DrawString("ContentArea", this.Font, new SolidBrush(this.ForeColor), this.rectContentArea, format);
			e.Graphics.DrawString("LeftSidebar", this.Font, new SolidBrush(this.ForeColor), this.rectLeftSidebar, format);
			e.Graphics.DrawString("RightSidebar", this.Font, new SolidBrush(this.ForeColor), this.rectRightSidebar, format);
		}
		protected virtual void OnPaintRuler(Graphics g, Rectangle rect, bool top)
		{
			// Draw background
			Rectangle borderRect;
			if (this.BorderStyle != System.Windows.Forms.BorderStyle.None)
			{
				borderRect = new Rectangle(
					rect.X,
					top ? rect.Y - 1 : rect.Y,
					rect.Width,
					rect.Height + 1);
			}
			else
			{
				borderRect = rect;
			}
			g.FillRectangle(new SolidBrush(this.renderer.ColorVeryLightBackground), rect);
			this.renderer.DrawBorder(g, borderRect, Drawing.BorderStyle.Simple, BorderState.Normal);

			// Determine drawing geometry
			Rectangle rectUnitName;
			Rectangle rectUnitMarkings;
			{
				SizeF unitNameSize = g.MeasureString(this.unitInfo.Name, this.renderer.DefaultFont, rect.Width);
				float markingRatio = 0.5f + 0.5f * (1.0f - Math.Max(Math.Min((float)rect.Height / 32.0f, 1.0f), 0.0f));
				if (top)
				{
					rectUnitName = new Rectangle(
						rect.X, 
						rect.Y, 
						(int)Math.Ceiling(unitNameSize.Width), 
						(int)Math.Ceiling(unitNameSize.Height));
					rectUnitMarkings = new Rectangle(
						rect.X,
						rect.Bottom - Math.Min((int)(rect.Height * markingRatio), 16),
						rect.Width,
						Math.Min((int)(rect.Height * markingRatio), 16));
				}
				else
				{
					rectUnitName = new Rectangle(
						rect.X, 
						rect.Bottom - (int)Math.Ceiling(unitNameSize.Height), 
						(int)Math.Ceiling(unitNameSize.Width), 
						(int)Math.Ceiling(unitNameSize.Height));
					rectUnitMarkings = new Rectangle(
						rect.X,
						rect.Top,
						rect.Width,
						Math.Min((int)(rect.Height * markingRatio), 16));
				}
			}

			// Draw unit name
			{
				Rectangle overlap = rectUnitMarkings;
				overlap.Intersect(rectUnitName);
				float overlapAmount = Math.Max(Math.Min((float)overlap.Height / (float)rectUnitName.Height, 1.0f), 0.0f);
				float textOverlapAlpha = (1.0f - (overlapAmount * overlapAmount));

				StringFormat format = new StringFormat(StringFormat.GenericDefault);
				format.Alignment = StringAlignment.Near;
				format.LineAlignment = top ? StringAlignment.Near : StringAlignment.Far;
				g.DrawString(this.unitInfo.Name, this.renderer.DefaultFont, new SolidBrush(Color.FromArgb((int)(textOverlapAlpha * 255), this.renderer.ColorText)), rect, format);
			}

			// Draw ruler markings
			float rulerStep = GetNiceMultiple(this.unitInfo.ConvertToUnits(10.0f));
			float unitScroll = this.UnitScroll;
			float beginTime = this.GetUnitAtPos(rect.Left);
			float endTime = this.GetUnitAtPos(rect.Right);
			{
				Pen bigLinePen = new Pen(new SolidBrush(this.renderer.ColorText));
				Pen medLinePen = new Pen(new SolidBrush(Color.FromArgb(128, this.renderer.ColorText)));
				Pen minLinePen = new Pen(new SolidBrush(Color.FromArgb(64, this.renderer.ColorText)));

				float timeValue;
				float maxTime;
				GetRulerRange(rulerStep * 4, unitScroll, beginTime, endTime, out timeValue, out maxTime);

				int lineIndex = 0;
				while (timeValue < maxTime)
				{
					float markX = this.GetPosAtUnit(timeValue + unitScroll);

					float markLen;
					Pen markPen;
					if ((lineIndex % 4) == 0)
					{
						markLen = 1.0f;
						markPen = bigLinePen;
					}
					else if ((lineIndex % 2) == 0)
					{
						markLen = 0.5f;
						markPen = medLinePen;
					}
					else
					{
						markLen = 0.25f;
						markPen = minLinePen;
					}

					float markTopY;
					float markBottomY;
					float markTextY;
					if (top)
					{
						markTopY = rectUnitMarkings.Bottom - markLen * rectUnitMarkings.Height;
						markBottomY = rectUnitMarkings.Bottom;
						markTextY = rectUnitMarkings.Bottom - this.renderer.DefaultFont.Height - Math.Max(Math.Min(3 + rectUnitMarkings.Top - rectUnitName.Bottom, rectUnitMarkings.Height / 2), 0);
					}
					else
					{
						markTopY = rectUnitMarkings.Top;
						markBottomY = rectUnitMarkings.Top + markLen * rectUnitMarkings.Height;
						markTextY = rectUnitMarkings.Top + Math.Max(Math.Min(3 + rectUnitName.Top - rectUnitMarkings.Bottom, rectUnitMarkings.Height / 2), 0);
					}

					g.DrawLine(markPen, markX, markTopY, markX, markBottomY);

					if ((lineIndex % 4) == 0)
					{
						string timeString = this.unitInfo.ConvertToString(timeValue, TimelineViewUnitInfo.NameMode.Short);
						g.DrawString(
							timeString, 
							this.renderer.DefaultFont, 
							new SolidBrush(markPen.Color), 
							markX, 
							markTextY);
					}

					timeValue += rulerStep;
					lineIndex++;
				}
			}
		}

		protected static float GetNiceMultiple(float rawMultiple)
		{
			float magnitude = (float)Math.Floor(Math.Log10(rawMultiple));
			float baseValue = (float)Math.Pow(10.0f, magnitude);
			float factor = rawMultiple / baseValue;

			if (factor < 1.25f) factor = 1.0f;
			else if (factor < 3.75f) factor = 2.5f;
			else if (factor < 7.5f) factor = 5.0f;
			else factor = 10.0f;

			return baseValue * factor;
		}
		protected static void GetRulerRange(float stepSize, float scroll, float minTime, float maxTime, out float rangeBegin, out float rangeEnd)
		{
			float scrollStepOffset = stepSize * (float)Math.Floor(-scroll / stepSize);
			rangeBegin = stepSize * (float)Math.Floor(minTime / stepSize) + scrollStepOffset;
			rangeEnd = stepSize * ((float)Math.Ceiling(maxTime / stepSize) + 1) + scrollStepOffset;
		}
	}
}
