using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineViewTrackEventArgs : TimelineViewEventArgs
	{
		private TimelineViewTrack track = null;

		public TimelineViewTrack Track
		{
			get { return this.track; }
		}

		public TimelineViewTrackEventArgs(TimelineViewTrack track) : base(track.ParentView)
		{
			this.track = track;
		}
	}
}
