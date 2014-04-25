using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public abstract class TimelineTrackModel : ITimelineTrackModel
	{
		private	string	trackName	= "A Timeline Track";

		public string TrackName
		{
			get { return this.trackName; }
			set
			{
				if (this.trackName != value)
				{
					this.trackName = value;
					if (this.TrackNameChanged != null)
						this.TrackNameChanged(this, EventArgs.Empty);
				}
			}
		}

		public event EventHandler TrackNameChanged;
	}
}
