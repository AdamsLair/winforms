using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms
{
	public class TiledViewEventArgs : EventArgs
	{
		private TiledView view;
		public TiledView View
		{
			get { return this.view; }
		}
		public TiledViewEventArgs(TiledView view)
		{
			this.view = view;
		}
	}
}
