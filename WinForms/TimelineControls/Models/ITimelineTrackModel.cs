using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public interface ITimelineTrackModel
	{
		string TrackName { get; }
		float EndTime { get; }
		float BeginTime { get; }

		event EventHandler TrackNameChanged;
	}
}
