using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public abstract class TimelineViewTrack
	{
		private	TimelineView	parentView	= null;
		private	int				baseHeight	= 100;
		private	int				fillHeight	= 0;
		private	string			name		= "TimelineViewTrack";

		private	int	height;


		public event EventHandler HeightChanged = null;
		public event EventHandler HeightSettingsChanged = null;
		

		public TimelineView ParentView
		{
			get { return this.parentView; }
			internal set { this.parentView = value; }
		}
		public string Name
		{
			get { return this.name; }
			set
			{
				this.name = value;
				this.Invalidate();
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


		public void Invalidate()
		{
			if (this.parentView == null) return;

			Rectangle rectOnParent = this.parentView.GetTrackRectangle(this);
			rectOnParent.Intersect(this.parentView.ClientRectangle);
			if (rectOnParent.IsEmpty) return;

			this.parentView.Invalidate(rectOnParent);
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
	}
}
