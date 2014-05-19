using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class TimelineModelViewAssignmentAttribute : Attribute
	{
		private Type[] validModelTypes;
		public Type[] ValidModelTypes
		{
			get { return this.validModelTypes; }
		}
		public TimelineModelViewAssignmentAttribute(params Type[] validModelTypes)
		{
			this.validModelTypes = validModelTypes.Where(t => t != null && (typeof(ITimelineTrackModel).IsAssignableFrom(t) || typeof(ITimelineGraphModel).IsAssignableFrom(t))).ToArray();
		}
	}
}
