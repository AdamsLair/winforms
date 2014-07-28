using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.ItemModels
{
	public class MenuModelItemsEventArgs : EventArgs
	{
		private IMenuModelItem[]	items			= null;
		private bool				sortingAffected	= false;

		public IEnumerable<IMenuModelItem> Items
		{
			get { return this.items; }
		}
		public bool IsSortingAffected
		{
			get { return this.sortingAffected; }
		}

		public MenuModelItemsEventArgs(IEnumerable<IMenuModelItem> items, bool affectsSorting = false)
		{
			if (items == null) items = Enumerable.Empty<IMenuModelItem>();
			this.items = items.Distinct().ToArray();
			this.sortingAffected = affectsSorting;
		}
	}
}
