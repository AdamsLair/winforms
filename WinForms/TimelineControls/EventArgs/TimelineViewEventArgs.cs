using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineViewEventArgs : EventArgs
	{
		private TimelineView view = null;

		public TimelineView View
		{
			get { return this.view; }
		}

		public TimelineViewEventArgs(TimelineView view)
		{
			this.view = view;
		}
	}
}
