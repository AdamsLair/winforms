using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms
{
	public class EmptyListModel : IListModel
	{
		public int Count
		{
			get { return 0; }
		}
		public object GetItemAt(int index)
		{
			return null;
		}
		public int GetIndexOf(object item)
		{
			return -1;
		}
		public event EventHandler<EventArgs> CountChanged;
		public event EventHandler<ListModelItemsEventArgs> IndicesChanged;
	}
}
