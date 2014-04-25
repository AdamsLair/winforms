using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public interface ITimelineModel
	{
		string UnitName { get; }
		string UnitDescription { get; }
		float UnitBaseScale { get; }
		IEnumerable<ITimelineTrackModel> Tracks { get; }

		event EventHandler<EventArgs> UnitChanged; 
		event EventHandler<TimelineModelTracksEventArgs> TracksAdded;
		event EventHandler<TimelineModelTracksEventArgs> TracksRemoved;
	}
}
