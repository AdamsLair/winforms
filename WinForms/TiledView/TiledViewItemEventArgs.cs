using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Renderer;

namespace AdamsLair.WinForms
{
	public class TiledViewItemEventArgs : TiledViewEventArgs
	{
		private	int modelIndex;
		private object item;

		public int ModelIndex
		{
			get { return this.modelIndex; }
		}
		public object Item
		{
			get { return this.item; }
		}

		public TiledViewItemEventArgs(TiledView view, int modelIndex, object item) : base(view)
		{
			this.modelIndex = modelIndex;
			this.item = item;
		}
	}
}
