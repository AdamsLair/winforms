using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineGraphChangedEventArgs : TimelineGraphEventArgs
	{
		private ITimelineGraphModel oldGraph = null;
		public ITimelineGraphModel OldGraph
		{
			get { return this.oldGraph; }
		}
		public TimelineGraphChangedEventArgs(ITimelineGraphModel oldGraph, ITimelineGraphModel graph) : base(graph)
		{
			this.oldGraph = oldGraph;
		}
	}
}
