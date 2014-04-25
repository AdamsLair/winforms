using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class TimelineTrackAssignmentAttribute : Attribute
	{
		private Type[] validModelTypes;
		public Type[] ValidModelTypes
		{
			get { return this.validModelTypes; }
		}
		public TimelineTrackAssignmentAttribute(params Type[] validModelTypes)
		{
			this.validModelTypes = validModelTypes.Where(t => t != null && typeof(ITimelineTrackModel).IsAssignableFrom(t)).ToArray();
		}
	}
}
