using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineGraphEventArgs : EventArgs
	{
		private ITimelineGraph graph;
		public ITimelineGraph Graph
		{
			get { return this.graph; }
		}
		public TimelineGraphEventArgs(ITimelineGraph graph)
		{
			this.graph = graph;
		}
	}
}
