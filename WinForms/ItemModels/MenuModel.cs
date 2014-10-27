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
		
		public MenuModelItem GetItem(string fullItemName)
		{
			fullItemName = fullItemName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			return this.ItemsDeep.FirstOrDefault(i => string.Equals(i.FullName, fullItemName, StringComparison.InvariantCultureIgnoreCase));
		}
		public MenuModelItem RequestItem(string fullItemName)
		{
			// Check if such item already exists. Returns it, if it does.
			{
				MenuModelItem existingItem = this.GetItem(fullItemName);
				if (existingItem != null) return existingItem;
			}

			// Create an item that matches the specified full name
			MenuModelItem resultItem = null;
			{
				fullItemName = fullItemName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
				string[] itemNames = fullItemName.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.None);

				// Create the root item
				MenuModelItem item = this.items.FirstOrDefault(c => string.Equals(c.Name, itemNames[0], StringComparison.InvariantCultureIgnoreCase));
				if (item == null)
				{
					item = new MenuModelItem(itemNames[0]);
					this.AddItem(item);
				}

				// Create subsequent items
				resultItem = item;
				for (int i = 1; i < itemNames.Length; i++)
				{
					resultItem = item.Items.FirstOrDefault(c => string.Equals(c.Name, itemNames[i], StringComparison.InvariantCultureIgnoreCase));
					if (resultItem == null)
					{
						resultItem = new MenuModelItem(itemNames[i]);
						item.AddItem(resultItem);
					}
					item = resultItem;
				}
			}

			return resultItem;
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
			this.RaiseItemsRemoved(oldItems);
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
