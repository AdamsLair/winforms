using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using AdamsLair.WinForms.ItemModels;

namespace AdamsLair.WinForms.ItemViews
{
	public class MenuStripMenuViewItemEventArgs : MenuStripMenuViewEventArgs
	{
		private IMenuModelItem	modelItem	= null;
		private ToolStripItem	viewItem	= null;

		public IMenuModelItem Modelitem
		{
			get { return this.modelItem; }
		}
		public ToolStripItem ViewItem
		{
			get { return this.viewItem; }
		}

		public MenuStripMenuViewItemEventArgs(IMenuModelItem modelItem, ToolStripItem viewItem, MenuStripMenuView view) : base(view)
		{
			this.modelItem = modelItem;
			this.viewItem = viewItem;
		}
	}
}
