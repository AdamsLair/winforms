using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineLinearGraph : ITimelineGraph
	{
		public struct Key : IComparable<Key>, IEquatable<Key>
		{
			public float X;
			public float Y;

			public Key(float x, float y)
			{
				this.X = x;
				this.Y = y;
			}

			int IComparable<Key>.CompareTo(Key other)
			{
				return this.X.CompareTo(other.X);
			}
			bool IEquatable<Key>.Equals(Key other)
			{
				return this.X == other.X && this.Y == other.Y;
			}
		}

		private List<Key> values = new List<Key>();

		public event EventHandler<TimelineGraphRangeEventArgs> GraphChanged;

		public float EndTime
		{
			get { return this.values.Count > 0 ? this.values[this.values.Count - 1].X : 0.0f; }
		}
		public float BeginTime
		{
			get { return this.values.Count > 0 ? this.values[0].X : 0.0f; }
		}
		public float MinValue
		{
			get { return this.values.Count > 0 ? this.values.Min(k => k.Y) : 0.0f; }
		}
		public float MaxValue
		{
			get { return this.values.Count > 0 ? this.values.Max(k => k.Y) : 0.0f; }
		}
		public IEnumerable<Key> Samples
		{
			get { return this.values; }
			set
			{
				if (value != this.values)
				{
					List<Key> newSamples = value.ToList();
					this.Clear();
					this.AddRange(newSamples);
				}
			}
		}

		
		public TimelineLinearGraph() {}
		public TimelineLinearGraph(IEnumerable<Key> samples)
		{
			this.values = samples.ToList();
			this.values.Sort();
		}
		public TimelineLinearGraph(IEnumerable<float> samples, float sampleRate, float startX = 0.0f)
		{
			int sampleCount = samples.Count();
			this.values.Capacity = sampleCount;

			float x = startX;
			float xPerSample = 1.0f / sampleRate;
			foreach (float y in samples)
			{
				this.values.Add(new Key(x, y));
				x += xPerSample;
			}
		}

		public void AddRange(IEnumerable<Key> values)
		{
			this.values.AddRange(values);
			this.values.Sort();
			this.RaiseGraphChanged(values.Min(v => v.X), values.Max(v => v.X));
		}
		public void Add(Key frame)
		{
			int insertIndex = this.SearchIndexBelow(frame.X) + 1;

			if (insertIndex >= this.values.Count)
				this.values.Add(frame);
			else if (this.values[insertIndex].X == frame.X)
				this.values[insertIndex] = frame;
			else
				this.values.Insert(insertIndex, frame);

			this.RaiseGraphChanged(frame.X);
		}
		public void Add(float x, float y)
		{
			this.Add(new Key(x, y));
		}
		public void Remove(float x)
		{
			this.values.RemoveAll(f => f.X == x);
			this.RaiseGraphChanged(x);
		}
		public void Clear()
		{
			float begin = this.BeginTime;
			float end = this.EndTime;
			this.values.Clear();
			this.RaiseGraphChanged(begin, end);
		}

		public float GetValueAtX(float time)
		{
			int frameCount = this.values.Count;
			if (frameCount == 0) return 0.0f;

			int baseIndex = this.SearchIndexBelow(time);
			if (baseIndex < 0) return this.values[0].Y;
			if (baseIndex == frameCount - 1) return this.values[frameCount - 1].Y;

			int nextIndex = (baseIndex + 1) % frameCount;
			float nextX = this.values[nextIndex].X;
			if (nextX < this.values[baseIndex].X) nextX += this.EndTime;

			float factor = Math.Max(Math.Min((time - this.values[baseIndex].X) / (nextX - this.values[baseIndex].X), 1.0f), 0.0f);
			return this.values[baseIndex].Y * (1.0f - factor) + this.values[nextIndex].Y * factor;
		}
		private int SearchIndexBelow(float x)
		{
			int left = 0;
			int right = this.values.Count - 1;
			while (right >= left)
			{
				int mid = (left + right) / 2;
				float midTime = this.values[mid].X;
				if (midTime > x)
				{
					right = mid - 1;
				}
				else if (midTime <= x)
				{
					if (left != mid)
					{
						left = mid;
					}
					else
					{
						float rightTime = this.values[right].X;
						if (rightTime <= x)
							return right;
						else
							return left;
					}
				}
				else if (left == right)
				{
					break;
				}
			}
			return -1;
		}

		private void RaiseGraphChanged(float at)
		{
			this.RaiseGraphChanged(at, at);
		}
		private void RaiseGraphChanged(float from, float to)
		{
			// Expand the specified range due to linear interpolation changes
			int belowIndex = this.SearchIndexBelow(from);
			int aboveIndex = this.SearchIndexBelow(to);
			if (belowIndex != -1) from = this.values[belowIndex].X;
			if (aboveIndex != -1) to = (aboveIndex + 1 < this.values.Count) ? this.values[aboveIndex + 1].X : this.EndTime;

			if (this.GraphChanged != null)
				this.GraphChanged(this, new TimelineGraphRangeEventArgs(this, from, to));
		}
	}
}
