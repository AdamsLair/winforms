using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineGraphRangeEventArgs : TimelineGraphEventArgs
	{
		private float beginTime;
		private float endTime;

		public float BeginTime
		{
			get { return this.beginTime; }
		}
		public float EndTime
		{
			get { return this.endTime; }
		}

		public TimelineGraphRangeEventArgs(ITimelineGraphModel graph, float beginTime, float endTime) : base(graph)
		{
			this.beginTime = beginTime;
			this.endTime = endTime;
		}
	}
}
