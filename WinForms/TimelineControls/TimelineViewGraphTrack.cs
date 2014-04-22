using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineViewGraphTrack : TimelineViewTrack
	{
		private	float	verticalUnitTop		= 1.0f;
		private	float	verticalUnitBottom	= -1.0f;


		public float VerticalUnitTop
		{
			get { return this.verticalUnitTop; }
			set { this.verticalUnitTop = value; }
		}
		public float VerticalUnitBottom
		{
			get { return this.verticalUnitBottom; }
			set { this.verticalUnitBottom = value; }
		}


		protected internal override void OnPaintLeftSidebar(TimelineViewTrackPaintEventArgs e)
		{
			base.OnPaintLeftSidebar(e);
		}
	}
}
