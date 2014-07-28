using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AdamsLair.WinForms.ItemModels
{
	public class MenuModel : IMenuModel
	{
		private List<MenuModelItem> items	= new List<MenuModelItem>();
		
		public event EventHandler<MenuModelItemsEventArgs> ItemsAdded = null;
		public event EventHandler<MenuModelItemsEventArgs> ItemsRemoved = null;
		public event EventHandler<MenuModelItemsEventArgs> ItemsChanged = null;

		public IEnumerable<MenuModelItem> Items
		{
			get { return this.items; }
		}
		public IEnumerable<MenuModelItem> ItemsDeep
		{
			get 
			{
				if (this.items == null) yield break;
				foreach (MenuModelItem i in this.items)
				{
					yield return i;
					foreach (MenuModelItem j in i.ItemsDeep)
					{
						yield return j;
					}
				}
			}
		}
		
		public MenuModelItem FindItem(string fullItemName)
		{
			fullItemName = fullItemName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			return this.ItemsDeep.FirstOrDefault(i => string.Equals(i.FullName, fullItemName, StringComparison.InvariantCultureIgnoreCase));
		}
		public void AddItem(MenuModelItem item)
		{
			this.AddItems(new[] { item });
		}
		public void AddItems(IEnumerable<MenuModelItem> items)
		{
			List<MenuModelItem> actual = new List<MenuModelItem>();
			foreach (MenuModelItem item in items)
			{
				if (item.Parent != null) continue;
				if (item.Model != null) continue;
				item.Model = this;
				actual.Add(item);
			}
			this.items.AddRange(actual);
			this.RaiseItemsAdded(actual);
		}
		public void RemoveItem(MenuModelItem item)
		{
			this.RemoveItems(new[] { item });
		}
		public void RemoveItems(IEnumerable<MenuModelItem> items)
		{
			List<MenuModelItem> actual = new List<MenuModelItem>();
			foreach (MenuModelItem item in items)
			{
				if (item.Parent != null) continue;
				if (item.Model != this) continue;
				actual.Add(item);
				this.items.Remove(item);
			}
			this.RaiseItemsRemoved(actual);
			foreach (MenuModelItem item in actual)
			{
				item.Model = null;
			}
		}
		public void ClearItems()
		{
			IMenuModelItem[] oldItems = this.items.ToArray();
			foreach (MenuModelItem item in this.items)
			{
				item.Model = null;
			}
			this.items.Clear();
			this.RaiseItemsChanged(oldItems);
		}
		
		internal void RaiseItemsAdded(IEnumerable<IMenuModelItem> items)
		{
			if (this.ItemsAdded != null)
				this.ItemsAdded(this, new MenuModelItemsEventArgs(items));
		}
		internal void RaiseItemsRemoved(IEnumerable<IMenuModelItem> items)
		{
			if (this.ItemsRemoved != null)
				this.ItemsRemoved(this, new MenuModelItemsEventArgs(items));
		}
		internal void RaiseItemsChanged(IEnumerable<IMenuModelItem> items, bool affectsSorting = false)
		{
			if (this.ItemsChanged != null)
				this.ItemsChanged(this, new MenuModelItemsEventArgs(items, affectsSorting));
		}

		IEnumerable<IMenuModelItem> IMenuModel.Items
		{
			get { return this.items; }
		}
	}
}
