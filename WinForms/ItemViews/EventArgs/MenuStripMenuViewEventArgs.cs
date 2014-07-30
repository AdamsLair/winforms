using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AdamsLair.WinForms.ItemViews
{
	public class MenuStripMenuViewEventArgs : EventArgs
	{
		private MenuStripMenuView	view	= null;

		public MenuStripMenuView View
		{
			get { return this.view; }
		}

		public MenuStripMenuViewEventArgs(MenuStripMenuView view)
		{
			this.view = view;
		}
	}
}
