using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineViewUnitInfo
	{
		public enum NameMode
		{
			None,
			Short,
			Full,
		}
		public delegate string FormatFunc(TimelineViewUnitInfo unitInfo, float units, NameMode mode);


		private	string		name;
		private	string		shortName;
		private	float		pixelsPerUnit;
		private	FormatFunc	formatFunc;


		public string Name
		{
			get { return this.name; }
			set { this.name = value; }
		}
		public string ShortName
		{
			get { return this.shortName; }
			set { this.shortName = value; }
		}
		public float PixelsPerUnit
		{
			get { return this.pixelsPerUnit; }
			set { this.pixelsPerUnit = value; }
		}
		public FormatFunc FormattingMethod
		{
			get { return this.formatFunc; }
			set { this.formatFunc = value ?? DefaultFormattingMethod; }
		}


		public TimelineViewUnitInfo(string name, string shortName, float pixelsPerUnit, FormatFunc formattingMethod = null)
		{
			this.formatFunc = formattingMethod ?? DefaultFormattingMethod;
			this.name = name;
			this.shortName = shortName;
			this.pixelsPerUnit = pixelsPerUnit;
		}


		public string ConvertToString(float units, NameMode name)
		{
			return this.formatFunc(this, units, name);
		}
		public float ConvertToPixels(float units)
		{
			return units * this.pixelsPerUnit;
		}
		public float ConvertToUnits(float pixels)
		{
			return pixels / this.pixelsPerUnit;
		}


		private static string DefaultFormattingMethod(TimelineViewUnitInfo unitInfo, float units, NameMode name)
		{
			units = (float)Math.Round(units, 2);
			if (name == NameMode.Full && !string.IsNullOrEmpty(unitInfo.name))
				return string.Format("{0} {1}", units, unitInfo.name);
			else if (name == NameMode.Short && !string.IsNullOrEmpty(unitInfo.shortName))
				return string.Format("{0} {1}", units, unitInfo.shortName);
			else
				return string.Format("{0}", units);
		}
	}
}
