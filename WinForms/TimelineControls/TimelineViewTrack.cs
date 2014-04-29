using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public abstract class TimelineViewTrack
	{
		private	TimelineView		parentView	= null;
		private	ITimelineTrackModel	model		= null;
		private	int					baseHeight	= 100;
		private	int					fillHeight	= 0;

		private	int	height;
		private	float contentBeginTime;
		private	float contentEndTime;


		public event EventHandler ContentWidthChanged = null;
		public event EventHandler HeightChanged = null;
		public event EventHandler HeightSettingsChanged = null;
		

		public TimelineView ParentView
		{
			get { return this.parentView; }
			internal set { this.parentView = value; }
		}
		public ITimelineTrackModel Model
		{
			get { return this.model; }
			internal set
			{
				if (this.model != value)
				{
					TimelineTrackModelChangedEventArgs args = new TimelineTrackModelChangedEventArgs(this.model, value);
					this.model = value;
					this.OnModelChanged(args);
				}
			}
		}
		public int BaseHeight
		{
			get { return this.baseHeight; }
			set
			{
				if (this.baseHeight != value)
				{
					this.baseHeight = value;
					this.OnHeightSettingsChanged();
				}
			}
		}
		public int FillHeight
		{
			get { return this.fillHeight; }
			set
			{
				if (this.fillHeight != value)
				{
					this.fillHeight = value;
					this.OnHeightSettingsChanged();
				}
			}
		}
		public int Height
		{
			get { return this.height; }
			internal set
			{
				if (this.height != value)
				{
					this.height = value;
					this.OnHeightChanged();
				}
			}
		}
		public float ContentBeginTime
		{
			get { return this.contentBeginTime; }
		}
		public float ContentEndTime
		{
			get { return this.contentEndTime; }
		}


		public void Invalidate()
		{
			if (this.parentView == null) return;

			this.Invalidate(this.parentView.UnitScroll, this.parentView.UnitScroll + this.parentView.VisibleUnitWidth);
		}
		public void Invalidate(float fromUnits, float toUnits)
		{
			if (this.parentView == null) return;

			Rectangle rectOnParent = this.parentView.GetTrackRectangle(this);

			float fromPixels = Math.Max(this.parentView.GetPosAtUnit(fromUnits + this.parentView.UnitScroll), rectOnParent.Left + this.parentView.LeftSidebarSize) - 1;
			float toPixels = Math.Min(this.parentView.GetPosAtUnit(toUnits + this.parentView.UnitScroll), rectOnParent.Right - this.parentView.RightSidebarSize) + 2;

			Rectangle targetRect = new Rectangle(
				(int)fromPixels,
				rectOnParent.Y,
				(int)(toPixels - fromPixels),
				rectOnParent.Height);
			rectOnParent.Intersect(targetRect);
			rectOnParent.Intersect(this.parentView.ClientRectangle);
			if (rectOnParent.IsEmpty) return;

			this.parentView.Invalidate(rectOnParent);
		}

		protected internal void UpdateContentWidth()
		{
			float newBegin;
			float newEnd;
			this.CalculateContentWidth(out newBegin, out newEnd);
			if (newBegin != this.contentBeginTime || newEnd != this.contentEndTime)
			{
				this.contentBeginTime = newBegin;
				this.contentEndTime = newEnd;
				this.OnContentWidthChanged();
			}
		}
		protected virtual void CalculateContentWidth(out float beginTime, out float endTime)
		{
			beginTime = 0.0f;
			endTime = 0.0f;
		}

		protected virtual void OnModelChanged(TimelineTrackModelChangedEventArgs e)
		{
			if (e.OldModel != null)
			{
				e.OldModel.TrackNameChanged -= this.model_TrackNameChanged;
			}
			if (e.Model != null)
			{
				e.Model.TrackNameChanged += this.model_TrackNameChanged;
			}
			this.UpdateContentWidth();
			this.Invalidate();
		}
		protected internal virtual void OnPaint(TimelineViewTrackPaintEventArgs e) {}
		protected internal virtual void OnPaintLeftSidebar(TimelineViewTrackPaintEventArgs e) {}
		protected internal virtual void OnPaintRightSidebar(TimelineViewTrackPaintEventArgs e) {}
		protected virtual void OnHeightSettingsChanged()
		{
			if (this.HeightSettingsChanged != null)
				this.HeightSettingsChanged(this, EventArgs.Empty);
		}
		protected virtual void OnHeightChanged()
		{
			if (this.HeightChanged != null)
				this.HeightChanged(this, EventArgs.Empty);
		}
		protected virtual void OnContentWidthChanged()
		{
			if (this.ContentWidthChanged != null)
				this.ContentWidthChanged(this, EventArgs.Empty);
		}

		private void model_TrackNameChanged(object sender, EventArgs e)
		{
			this.Invalidate();
		}
	}
}
