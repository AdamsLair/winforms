using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.ItemModels
{
	public interface IMenuModel
	{
		IEnumerable<IMenuModelItem> Items { get; }
		
		event EventHandler<MenuModelItemsEventArgs> ItemsAdded;
		event EventHandler<MenuModelItemsEventArgs> ItemsRemoved;
		event EventHandler<MenuModelItemsEventArgs> ItemsChanged; 
	}
}
