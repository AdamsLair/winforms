using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AdamsLair.WinForms.TimelineControls;

namespace AdamsLair.WinForms.TestApp
{
	public class TimelineFunctionGraph : ITimelineGraph
	{
		private Func<float,float> func;
		private float begin;
		private float end;
		private float min;
		private float max;

		public event EventHandler<TimelineGraphRangeEventArgs> GraphChanged;

		public float EndTime
		{
			get { return this.end; }
			set
			{
				if (this.end != value)
				{
					float oldEnd = this.end;
					this.end = value;
					this.RaiseGraphChanged(Math.Min(oldEnd, this.end), Math.Max(oldEnd, this.end));
				}
			}
		}
		public float BeginTime
		{
			get { return this.begin; }
			set
			{
				if (this.begin != value)
				{
					float oldBegin = this.begin;
					this.begin = value;
					this.RaiseGraphChanged(Math.Min(oldBegin, this.begin), Math.Max(oldBegin, this.begin));
				}
			}
		}
		public float MinValue
		{
			get { return this.min; }
			set
			{
				if (this.min != value)
				{
					this.min = value;
					this.RaiseGraphChanged();
				}
			}
		}
		public float MaxValue
		{
			get { return this.max; }
			set
			{
				if (this.max != value)
				{
					this.max = value;
					this.RaiseGraphChanged();
				}
			}
		}
		public Func<float,float> Function
		{
			get { return this.func; }
			set
			{
				if (this.func != value)
				{
					this.func = value;
					this.RaiseGraphChanged(this.begin, this.end);
				}
			}
		}

		
		public TimelineFunctionGraph() : this(x => x, 0.0f, 1.0f, 0.0f, 1.0f) {}
		public TimelineFunctionGraph(Func<float,float> func, float begin, float end, float minVal, float maxVal)
		{
			this.func = func;
			this.begin = begin;
			this.end = end;
			this.min = minVal;
			this.max = maxVal;
		}

		public float GetValueAtX(float time)
		{
			return this.func(time);
		}

		private void RaiseGraphChanged()
		{
			this.RaiseGraphChanged(this.begin, this.end);
		}
		private void RaiseGraphChanged(float at)
		{
			this.RaiseGraphChanged(at, at);
		}
		private void RaiseGraphChanged(float from, float to)
		{
			if (this.GraphChanged != null)
				this.GraphChanged(this, new TimelineGraphRangeEventArgs(this, from, to));
		}
	}
}
