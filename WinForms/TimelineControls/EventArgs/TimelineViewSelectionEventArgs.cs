using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineViewSelectionEventArgs : TimelineViewEventArgs
	{
		private	float			beginTime		= 0.0f;
		private	float			endTime			= 0.0f;
		private	float			lastBeginTime	= 0.0f;
		private	float			lastEndTime		= 0.0f;

		public float BeginTime
		{
			get { return this.beginTime; }
		}
		public float EndTime
		{
			get { return this.endTime; }
		}
		public bool IsEmpty
		{
			get { return this.beginTime == this.endTime; }
		}
		public float LastBeginTime
		{
			get { return this.lastBeginTime; }
		}
		public float LastEndTime
		{
			get { return this.lastEndTime; }
		}
		public bool WasEmpty
		{
			get { return this.lastBeginTime == this.lastEndTime; }
		}

		public TimelineViewSelectionEventArgs(TimelineView view, float begin, float end, float lastBegin, float lastEnd) : base(view)
		{
			this.beginTime = begin;
			this.endTime = end;
			this.lastBeginTime = lastBegin;
			this.lastEndTime = lastEnd;
		}
	}
}
