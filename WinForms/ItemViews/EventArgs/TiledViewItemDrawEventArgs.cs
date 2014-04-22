using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.ItemViews
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
			internal set { this.graphics = value; }
		}
		public ControlRenderer Renderer
		{
			get { return this.View.Renderer; }
		}
		public Rectangle ItemRect
		{
			get { return this.itemRect; }
			internal set { this.itemRect = value; }
		}
		public bool IsSelected
		{
			get { return this.selected; }
			internal set { this.selected = value; }
		}
		public bool IsHovered
		{
			get { return this.hovered; }
			internal set { this.hovered = value; }
		}
		public bool Handled
		{
			get { return this.handled; }
			set { this.handled = true; }
		}

		internal TiledViewItemDrawEventArgs(TiledView view) : this(view, -1, null, null, Rectangle.Empty, false, false) {}
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
