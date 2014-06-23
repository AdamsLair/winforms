using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineViewCursorEventArgs : TimelineViewEventArgs
	{
		private	float			cursorUnits		= 0.0f;
		private	float			lastCursorUnits	= 0.0f;

		public float CursorUnits
		{
			get { return this.cursorUnits; }
		}
		public float LastCursorUnits
		{
			get { return this.lastCursorUnits; }
		}
		public float CursorUnitSpeed
		{
			get { return this.cursorUnits - this.lastCursorUnits; }
		}

		public TimelineViewCursorEventArgs(TimelineView view, float cursorUnits, float lastCursorUnits) : base(view)
		{
			this.cursorUnits = cursorUnits;
			this.lastCursorUnits = lastCursorUnits;
		}
	}
}
