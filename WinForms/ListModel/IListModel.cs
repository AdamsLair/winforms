using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms
{
	public interface IListModel
	{
		int Count { get; }
		object GetItemAt(int index);
		int GetIndexOf(object item);

		event EventHandler<EventArgs> CountChanged; 
		event EventHandler<ListModelItemsEventArgs> IndicesChanged;
	}
}
