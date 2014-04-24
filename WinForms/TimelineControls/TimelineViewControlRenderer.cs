using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineViewControlRenderer : ControlRenderer
	{
		public Color ColorRulerMarkMajor
		{
			get { return this.ColorVeryDarkBackground; }
		}
		public Color ColorRulerMarkRegular
		{
			get { return Color.FromArgb(162, this.ColorVeryDarkBackground); }
		}
		public Color ColorRulerMarkMinor
		{
			get { return Color.FromArgb(96, this.ColorVeryDarkBackground); }
		}
	}
}
