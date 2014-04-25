using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineModel : ITimelineModel
	{
		private	string	unitName			= "Seconds";
		private	string	unitDescription		= "Time";
		private	float	unitBaseScale		= 1.0f;
		private	List<ITimelineTrackModel>	trackList	= new List<ITimelineTrackModel>();

		public event EventHandler<EventArgs> UnitChanged;
		public event EventHandler<TimelineModelTracksEventArgs> TracksAdded;
		public event EventHandler<TimelineModelTracksEventArgs> TracksRemoved;

		public string UnitName
		{
			get { return this.unitName; }
			set
			{
				if (this.unitName != value)
				{
					this.unitName = value;
					if (this.UnitChanged != null)
						this.UnitChanged(this, EventArgs.Empty);
				}
			}
		}
		public string UnitDescription
		{
			get { return this.unitDescription; }
			set
			{
				if (this.unitDescription != value)
				{
					this.unitDescription = value;
					if (this.UnitChanged != null)
						this.UnitChanged(this, EventArgs.Empty);
				}
			}
		}
		public float UnitBaseScale
		{
			get { return this.unitBaseScale; }
			set
			{
				if (this.unitBaseScale != value)
				{
					this.unitBaseScale = value;
					if (this.UnitChanged != null)
						this.UnitChanged(this, EventArgs.Empty);
				}
			}
		}
		public IEnumerable<ITimelineTrackModel> Tracks
		{
			get { return this.trackList; }
		}
		
		public void AddTrack(ITimelineTrackModel track)
		{
			this.AddTracks(new[] { track });
		}
		public void AddTracks(IEnumerable<ITimelineTrackModel> tracks)
		{
			tracks = tracks.Where(t => !this.trackList.Contains(t)).Distinct().ToArray();
			if (!tracks.Any()) return;

			this.trackList.AddRange(tracks);

			if (this.TracksAdded != null)
				this.TracksAdded(this, new TimelineModelTracksEventArgs(tracks));
		}
		public void RemoveTrack(ITimelineTrackModel track)
		{
			this.RemoveTracks(new[] { track });
		}
		public void RemoveTracks(IEnumerable<ITimelineTrackModel> tracks)
		{
			tracks = tracks.Where(t => this.trackList.Contains(t)).Distinct().ToArray();
			if (!tracks.Any()) return;

			foreach (ITimelineTrackModel track in tracks)
			{
				this.trackList.Remove(track);
			}

			if (this.TracksRemoved != null)
				this.TracksRemoved(this, new TimelineModelTracksEventArgs(tracks));
		}
	}
}
