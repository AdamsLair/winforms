using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace AdamsLair.WinForms.ItemModels
{
	public interface IMenuModelItem
	{
		string FullName { get; }
		string Name { get; }
		Image Icon { get; }
		int SortValue { get; }
		object Tag { get; }
		Keys ShortcutKeys { get; }
		bool Visible { get; }
		bool Enabled { get; }
		bool Checkable { get; }
		bool Checked { get; set; }
		MenuItemTypeHint TypeHint { get; }
		IMenuModelItem Parent { get; }
		IEnumerable<IMenuModelItem> Items { get; }
		IMenuModel Model { get; }

		void RaisePerformAction();
	}
}
