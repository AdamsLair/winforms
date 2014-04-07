using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.ItemViews
{
	public class TiledViewItemMouseEventArgs : TiledViewItemEventArgs
	{
		private	Point location;
		private MouseButtons buttons;
		
		public MouseButtons Buttons
		{
			get { return this.buttons; }
		}
		public Point Location
		{
			get { return this.location; }
		}
		public int X
		{
			get { return this.location.X; }
		}
		public int Y
		{
			get { return this.location.Y; }
		}

		public TiledViewItemMouseEventArgs(TiledView view, int modelIndex, object item, Point location, MouseButtons buttons) : base(view, modelIndex, item)
		{
			this.location = location;
			this.buttons = buttons;
		}
	}
}
