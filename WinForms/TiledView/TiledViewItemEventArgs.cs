using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.ItemViews
{
	public class TiledViewItemEventArgs : TiledViewEventArgs
	{
		private	int modelIndex;
		private object item;

		public int ModelIndex
		{
			get { return this.modelIndex; }
			internal set { this.modelIndex = value; }
		}
		public object Item
		{
			get { return this.item; }
			internal set { this.item = value; }
		}

		public TiledViewItemEventArgs(TiledView view, int modelIndex, object item) : base(view)
		{
			this.modelIndex = modelIndex;
			this.item = item;
		}
	}
}
