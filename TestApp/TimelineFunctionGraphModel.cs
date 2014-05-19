using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AdamsLair.WinForms.TimelineControls;

namespace AdamsLair.WinForms.TestApp
{
	public class TimelineFunctionGraphModel : ITimelineGraphModel
	{
		public delegate float EnvelopeFunc(float begin, float end);
		public delegate float ValueFunc(float x);

		private ValueFunc func;
		private EnvelopeFunc maxFunc;
		private EnvelopeFunc minFunc;
		private float begin;
		private float end;

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
		public ValueFunc Function
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
		public EnvelopeFunc EnvelopeMaxFunction
		{
			get { return this.maxFunc; }
			set
			{
				if (this.maxFunc != value)
				{
					this.maxFunc = value;
					this.RaiseGraphChanged(this.begin, this.end);
				}
			}
		}
		public EnvelopeFunc EnvelopeMinFunction
		{
			get { return this.minFunc; }
			set
			{
				if (this.minFunc != value)
				{
					this.minFunc = value;
					this.RaiseGraphChanged(this.begin, this.end);
				}
			}
		}

		
		public TimelineFunctionGraphModel(ValueFunc func, EnvelopeFunc minFunc, EnvelopeFunc maxFunc, float begin, float end)
		{
			this.func = func;
			this.minFunc = minFunc;
			this.maxFunc = maxFunc;
			this.begin = begin;
			this.end = end;
		}

		public float GetValueAtX(float time)
		{
			return this.func(time);
		}
		public float GetMaxValueInRange(float begin, float end)
		{
			return this.maxFunc(begin, end);
		}
		public float GetMinValueInRange(float begin, float end)
		{
			return this.minFunc(begin, end);
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
