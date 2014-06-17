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
		private Graphics		graphics	= null;
		private	Rectangle		targetRect	= Rectangle.Empty;
		private	float			beginTime	= 0.0f;
		private	float			endTime		= 0.0f;
		private	QualityLevel	qualityHint	= QualityLevel.High;


		public Graphics Graphics
		{
			get { return this.graphics; }
		}
		public TimelineViewControlRenderer Renderer
		{
			get { return this.View.Renderer; }
		}
		public QualityLevel QualityHint
		{
			get { return this.qualityHint; }
		}
		/// <summary>
		/// [GET] The rectangular area that is occupied by the track that is currently painted. It won't be
		/// altered due to clipping and partial repaints, and can be safely relied upon for determining
		/// drawing geometry.
		/// </summary>
		public Rectangle TargetRect
		{
			get { return this.targetRect; }
		}
		/// <summary>
		/// [GET] The begin of the currently painted timespan. This value may be altered due to clipping and
		/// partial repaints, and does not reflect the tracks or track contents actual begin.
		/// </summary>
		public float BeginTime
		{
			get { return this.beginTime; }
		}
		/// <summary>
		/// [GET] The end of the currently painted timespan. This value may be altered due to clipping and
		/// partial repaints, and does not reflect the tracks or track contents actual end.
		/// </summary>
		public float EndTime
		{
			get { return this.endTime; }
		}


		public TimelineViewTrackPaintEventArgs(TimelineViewTrack track, Graphics graphics, QualityLevel qualityHint, Rectangle targetRect) : this(track, graphics, qualityHint, targetRect, track.ContentBeginTime, track.ContentEndTime) {}
		public TimelineViewTrackPaintEventArgs(TimelineViewTrack track, Graphics graphics, QualityLevel qualityHint, Rectangle targetRect, float beginTime, float endTime) : base(track)
		{
			this.graphics = graphics;
			this.targetRect = targetRect;
			this.qualityHint = qualityHint;
			this.beginTime = beginTime;
			this.endTime = endTime;
		}

		public QualityLevel GetAdjustedQuality(QualityLevel baseLevel)
		{
			return (QualityLevel)Math.Min((int)baseLevel, (int)this.qualityHint);
		}
	}
}
