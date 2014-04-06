using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Renderer;

namespace AdamsLair.WinForms
{
	public class TiledViewItemDrawEventArgs : TiledViewItemEventArgs
	{
		private Graphics graphics;
		private Rectangle itemRect;
		private bool hovered;
		private bool selected;
		private bool handled;

		public Graphics Graphics
		{
			get { return this.graphics; }
		}
		public ControlRenderer ControlRenderer
		{
			get { return this.View.ControlRenderer; }
		}
		public Rectangle ItemRect
		{
			get { return this.itemRect; }
		}
		public bool IsSelected
		{
			get { return this.selected; }
		}
		public bool IsHovered
		{
			get { return this.hovered; }
		}
		public bool Handled
		{
			get { return this.handled; }
			set { this.handled = true; }
		}

		public TiledViewItemDrawEventArgs(TiledView view, int modelIndex, object item, Graphics graphics, Rectangle itemRect, bool selected, bool hovered) : base(view, modelIndex, item)
		{
			this.graphics = graphics;
			this.itemRect = itemRect;
			this.selected = selected;
			this.hovered = hovered;
			this.handled = false;
		}
	}
}
