using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

using AdamsLair.WinForms.Drawing;
using AdamsLair.WinForms.ItemModels;

namespace AdamsLair.WinForms.ItemViews
{
	public interface ITiledViewItemEditor
	{
		event EventHandler StopEditing;

		bool IsAcceptingValue { get; }
		Control MainControl { get; }

		void GetValueFromItem(object item);
		bool ApplyValueToItem(object item);
	}
}
