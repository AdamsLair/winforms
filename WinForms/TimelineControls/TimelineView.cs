using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

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


		private SubAreaInfo	areaTopRuler		= new SubAreaInfo(25);
		private SubAreaInfo	areaBottomRuler		= new SubAreaInfo(50);
		private SubAreaInfo	areaLeftSidebar		= new SubAreaInfo(100);
		private SubAreaInfo	areaRightSidebar	= new SubAreaInfo(75);

		private	Rectangle	rectTopRuler;
		private	Rectangle	rectBottomRuler;
		private	Rectangle	rectLeftSidebar;
		private	Rectangle	rectRightSidebar;
		private	Rectangle	rectContentArea;


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

			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Red)), this.rectContentArea);
			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Green)), this.rectTopRuler);
			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Blue)), this.rectBottomRuler);
			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Purple)), this.rectLeftSidebar);
			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.CornflowerBlue)), this.rectRightSidebar);

			StringFormat format = StringFormat.GenericDefault.Clone() as StringFormat;
			format.Alignment = StringAlignment.Center;
			format.LineAlignment = StringAlignment.Center;
			e.Graphics.DrawString("ContentArea", this.Font, new SolidBrush(this.ForeColor), this.rectContentArea, format);
			e.Graphics.DrawString("TopRuler", this.Font, new SolidBrush(this.ForeColor), this.rectTopRuler, format);
			e.Graphics.DrawString("BottomRuler", this.Font, new SolidBrush(this.ForeColor), this.rectBottomRuler, format);
			e.Graphics.DrawString("LeftSidebar", this.Font, new SolidBrush(this.ForeColor), this.rectLeftSidebar, format);
			e.Graphics.DrawString("RightSidebar", this.Font, new SolidBrush(this.ForeColor), this.rectRightSidebar, format);
		}
	}
}
