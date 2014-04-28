using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineTrackModelEventArgs : EventArgs
	{
		private ITimelineTrackModel model = null;
		public ITimelineTrackModel Model
		{
			get { return this.model; }
		}
		public TimelineTrackModelEventArgs(ITimelineTrackModel model)
		{
			this.model = model;
		}
	}
}
