using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace AdamsLair.WinForms.ItemModels
{
	public class MenuModelItem : IMenuModelItem
	{
		public static readonly int SortValue_Top		= -1000000;
		public static readonly int SortValue_UnderTop	= -100000;
		public static readonly int SortValue_Main		= 0;
		public static readonly int SortValue_OverBottom	= 100000;
		public static readonly int SortValue_Bottom		= 1000000;

		public static MenuModelItem Separator
		{
			get
			{
				return new MenuModelItem
				{ 
					Name = "Separator", 
					TypeHint = MenuItemTypeHint.Separator
				};
			}
		}

		private string					name			= "Item";
		private	Image					icon			= null;
		private	int						sortValue		= 0;
		private	object					tag				= null;
		private	bool					visible			= true;
		private	bool					enabled			= true;
		private	bool					checkable		= false;
		private	bool					isChecked		= false;
		private	Keys					shortcutKeys	= Keys.None;
		private	MenuItemTypeHint		typeHint		= MenuItemTypeHint.Item;
		private	MenuModelItem			parent			= null;
		private	List<MenuModelItem>		items			= new List<MenuModelItem>();
		private	MenuModel				model			= null;

		public event EventHandler PerformAction = null;

		public string FullName
		{
			get
			{
				if (this.parent == null)
					return this.name;
				else
					return Path.Combine(this.parent.FullName, this.name);
			}
		}
		public string Name
		{
			get { return this.name; }
			set
			{
				if (this.name != value)
				{
					this.name = value;
					this.RaiseItemChanged();
				}
			}
		}
		public Image Icon
		{
			get { return this.icon; }
			set
			{
				if (this.icon != value)
				{
					this.icon = value;
					this.RaiseItemChanged();
				}
			}
		}
		public int SortValue
		{
			get { return this.sortValue; }
			set
			{
				if (this.sortValue != value)
				{
					this.sortValue = value;
					this.RaiseItemChanged(true);
				}
			}
		}
		public object Tag
		{
			get { return this.tag; }
			set
			{
				if (this.tag != value)
				{
					this.tag = value;
					this.RaiseItemChanged();
				}
			}
		}
		public bool Visible
		{
			get { return this.visible; }
			set
			{
				if (this.visible != value)
				{
					this.visible = value;
					this.RaiseItemChanged();
				}
			}
		}
		public bool Enabled
		{
			get { return this.enabled; }
			set
			{
				if (this.enabled != value)
				{
					this.enabled = value;
					this.RaiseItemChanged();
				}
			}
		}
		public bool Checkable
		{
			get { return this.checkable; }
			set
			{
				if (this.checkable != value)
				{
					this.checkable = value;
					this.RaiseItemChanged();
				}
			}
		}
		public bool Checked
		{
			get { return this.isChecked; }
			set
			{
				if (this.isChecked != value)
				{
					this.isChecked = value;
					this.RaiseItemChanged();
				}
			}
		}
		public Keys ShortcutKeys
		{
			get { return this.shortcutKeys; }
			set
			{
				if (this.shortcutKeys != value)
				{
					this.shortcutKeys = value;
					this.RaiseItemChanged();
				}
			}
		}
		public EventHandler ActionHandler
		{
			get { return this.PerformAction; }
			set { this.PerformAction = value; }
		}
		public MenuItemTypeHint TypeHint
		{
			get { return this.typeHint; }
			set
			{
				if (this.typeHint != value)
				{
					this.RaiseItemsRemoved(new[] { this });
					this.typeHint = value;
					this.RaiseItemsAdded(new[] { this });
				}
			}
		}
		public MenuModelItem Parent
		{
			get { return this.parent; }
		}
		public IEnumerable<MenuModelItem> Items
		{
			get { return this.items; }
			set
			{
				MenuModelItem[] addedItems = value.Except(this.items).ToArray();
				MenuModelItem[] removedItems = this.items.Except(value).ToArray();
				if (removedItems.Length > 0) this.RemoveItems(removedItems);
				if (addedItems.Length > 0) this.AddItems(addedItems);
			}
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
		public MenuModel Model
		{
			get { return this.model; }
			internal set
			{
				if (this.model != value as MenuModel)
				{
					this.model = value as MenuModel;
					foreach (MenuModelItem item in this.items)
					{
						item.Model = this.model;
					}
				}
			}
		}
		
		public MenuModelItem() : this("Item", null, (EventHandler)null) {}
		public MenuModelItem(string name) : this(name, null, (EventHandler)null) {}
		public MenuModelItem(string name, EventHandler actionHandler) : this(name, null, actionHandler) {}
		public MenuModelItem(string name, Image icon) : this(name, icon, (EventHandler)null) {}
		public MenuModelItem(string name, Image icon, EventHandler actionHandler) : this(name, icon, (IEnumerable<MenuModelItem>)null)
		{
			this.PerformAction = actionHandler;
		}
		public MenuModelItem(string name, Image icon, IEnumerable<MenuModelItem> items)
		{
			this.name = name;
			this.icon = icon;
			if (items != null)
			{
				this.AddItems(items);
			}
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
				if (item.parent != null) continue;
				item.parent = this;
				item.model = this.model;
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
				if (item.parent != this) continue;
				actual.Add(item);
				this.items.Remove(item);
			}
			this.RaiseItemsRemoved(actual);
			foreach (MenuModelItem item in actual)
			{
				item.parent = null;
				item.model = null;
			}
		}
		public void ClearItems()
		{
			if (this.items.Count == 0) return;

			foreach (MenuModelItem item in this.items)
			{
				item.parent = null;
				item.model = null;
			}
			this.items.Clear();
			this.RaiseItemChanged();
		}

		public void RaisePerformAction()
		{
			if (this.PerformAction != null)
				this.PerformAction(this, EventArgs.Empty);
		}
		private void RaiseItemsAdded(IEnumerable<IMenuModelItem> items)
		{
			if (this.model == null) return;
			this.model.RaiseItemsAdded(items);
		}
		private void RaiseItemsRemoved(IEnumerable<IMenuModelItem> items)
		{
			if (this.model == null) return;
			this.model.RaiseItemsRemoved(items);
		}
		private void RaiseItemChanged(bool affectsSorting = false)
		{
			if (this.model == null) return;
			this.model.RaiseItemsChanged(new[] { this }, affectsSorting);
		}

		public override string ToString()
		{
			return string.Format("MenuModelItem: {0}", this.FullName);
		}

		IMenuModelItem IMenuModelItem.Parent
		{
			get { return this.parent; }
		}
		IEnumerable<IMenuModelItem> IMenuModelItem.Items
		{
			get { return this.items; }
		}
		IMenuModel IMenuModelItem.Model
		{
			get { return this.model; }
		}
	}
}
