using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public interface ITimelineGraph
	{
		float EndTime { get; }
		float BeginTime { get; }

		float GetValueAtX(float units);

		event EventHandler<TimelineGraphRangeEventArgs> GraphChanged;
	}
}
