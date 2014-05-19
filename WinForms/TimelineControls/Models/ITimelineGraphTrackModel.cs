using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public interface ITimelineGraphTrackModel : ITimelineTrackModel
	{
		IEnumerable<ITimelineGraphModel> Graphs { get; }

		event EventHandler<TimelineGraphCollectionEventArgs> GraphsAdded;
		event EventHandler<TimelineGraphCollectionEventArgs> GraphsRemoved;
		event EventHandler<TimelineGraphRangeEventArgs> GraphChanged;
	}
}
