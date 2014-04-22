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
		private	int				height		= 100;


		public event EventHandler HeightChanged = null;
		

		public TimelineView ParentView
		{
			get { return this.parentView; }
			internal set { this.parentView = value; }
		}
		public int Height
		{
			get { return this.height; }
			set
			{
				if (this.height != value)
				{
					this.height = value;
					this.OnHeightChanged();
				}
			}
		}


		internal protected abstract void OnPaint(TimelineViewTrackPaintEventArgs e);
		internal protected abstract void OnPaintLeftSidebar(TimelineViewTrackPaintEventArgs e);
		internal protected abstract void OnPaintRightSidebar(TimelineViewTrackPaintEventArgs e);
		protected virtual void OnHeightChanged()
		{
			if (this.HeightChanged != null)
				this.HeightChanged(this, EventArgs.Empty);
		}
	}
}
