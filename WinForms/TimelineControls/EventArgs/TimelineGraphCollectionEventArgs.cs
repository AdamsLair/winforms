using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineGraphCollectionEventArgs : EventArgs
	{
		private ITimelineGraphModel[] graphs;
		public IEnumerable<ITimelineGraphModel> Graphs
		{
			get { return this.graphs; }
		}
		public TimelineGraphCollectionEventArgs(IEnumerable<ITimelineGraphModel> graphs)
		{
			this.graphs = graphs.ToArray();
		}
	}
}
