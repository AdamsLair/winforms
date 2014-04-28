using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineTrackModelChangedEventArgs : TimelineTrackModelEventArgs
	{
		private ITimelineTrackModel oldModel = null;
		public ITimelineTrackModel OldModel
		{
			get { return this.oldModel; }
		}
		public TimelineTrackModelChangedEventArgs(ITimelineTrackModel oldModel, ITimelineTrackModel model) : base(model)
		{
			this.oldModel = oldModel;
		}
	}
}
