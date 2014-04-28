using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineModelTracksEventArgs : EventArgs
	{
		private ITimelineTrackModel[] tracks = null;

		public IEnumerable<ITimelineTrackModel> Tracks
		{
			get { return this.tracks; }
		}

		public TimelineModelTracksEventArgs(IEnumerable<ITimelineTrackModel> tracks)
		{
			this.tracks = tracks.ToArray();
		}
	}
}
