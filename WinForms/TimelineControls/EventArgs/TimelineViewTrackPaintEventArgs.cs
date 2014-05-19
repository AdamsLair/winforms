using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineViewTrackPaintEventArgs : TimelineViewTrackEventArgs
	{
		private Graphics	graphics	= null;
		private	Rectangle	targetRect	= Rectangle.Empty;
		private	float		beginTime	= 0.0f;
		private	float		endTime		= 0.0f;

		public Graphics Graphics
		{
			get { return this.graphics; }
		}
		public TimelineViewControlRenderer Renderer
		{
			get { return this.View.Renderer; }
		}
		public Rectangle TargetRect
		{
			get { return this.targetRect; }
		}
		public float BeginTime
		{
			get { return this.beginTime; }
		}
		public float EndTime
		{
			get { return this.endTime; }
		}

		public TimelineViewTrackPaintEventArgs(TimelineViewTrack track, Graphics graphics, Rectangle targetRect) : this(track, graphics, targetRect, track.ContentBeginTime, track.ContentEndTime) {}
		public TimelineViewTrackPaintEventArgs(TimelineViewTrack track, Graphics graphics, Rectangle targetRect, float beginTime, float endTime) : base(track)
		{
			this.graphics = graphics;
			this.targetRect = targetRect;
			this.beginTime = beginTime;
			this.endTime = endTime;
		}
	}
}
