using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineView : Panel
	{
		public TimelineView()
		{
			this.SetStyle(ControlStyles.ResizeRedraw, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.Opaque, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.SetStyle(ControlStyles.Selectable, true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
		}
	}
}
