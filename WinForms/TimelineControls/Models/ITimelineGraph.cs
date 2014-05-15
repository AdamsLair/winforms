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
		float GetMaxValueInRange(float begin, float end);
		float GetMinValueInRange(float begin, float end);

		event EventHandler<TimelineGraphRangeEventArgs> GraphChanged;
	}
}
