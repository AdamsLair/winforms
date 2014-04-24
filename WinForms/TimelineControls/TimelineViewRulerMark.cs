using System;
using System.Collections.Generic;
using System.Linq;


namespace AdamsLair.WinForms.TimelineControls
{
	public struct TimelineViewRulerMark
	{
		public float UnitValue;
		public float PixelValue;
		public TimelineViewRulerMarkWeight Weight;

		public TimelineViewRulerMark(float units, float pixels, TimelineViewRulerMarkWeight weight)
		{
			this.UnitValue = units;
			this.PixelValue = pixels;
			this.Weight = weight;
		}
	}
}
