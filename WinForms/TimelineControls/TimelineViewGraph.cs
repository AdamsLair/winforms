using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AdamsLair.WinForms.TimelineControls
{
	[TimelineModelViewAssignment(typeof(ITimelineGraphModel))]
	public class TimelineViewGraph
	{
		private	TimelineViewGraphTrack	parentTrack		= null;
		private	ITimelineGraphModel		model			= null;
		private	Color					baseColor		= Color.Red;
		private	float					curveOpacity	= 1.0f;
		private	float					envelopeOpacity	= 0.25f;


		public TimelineViewGraphTrack ParentTrack
		{
			get { return this.parentTrack; }
			internal set { this.parentTrack = value; }
		}
		public TimelineView ParentView
		{
			get { return this.parentTrack != null ? this.parentTrack.ParentView : null; }
		}
		public ITimelineGraphModel Model
		{
			get { return this.model; }
			internal set
			{
				if (this.model != value)
				{
					TimelineGraphChangedEventArgs args = new TimelineGraphChangedEventArgs(this.model, value);
					this.model = value;
					this.OnModelChanged(args);
				}
			}
		}
		public Color BaseColor
		{
			get { return this.baseColor; }
			set
			{
				if (this.baseColor != value)
				{
					this.baseColor = value;
					this.Invalidate();
				}
			}
		}
		public float CurveOpacity
		{
			get { return this.curveOpacity; }
			set
			{
				if (this.curveOpacity != value)
				{
					this.curveOpacity = Math.Max(Math.Min(value, 1.0f), 0.0f);
					this.Invalidate();
				}
			}
		}
		public float EnvelopeOpacity
		{
			get { return this.envelopeOpacity; }
			set
			{
				if (this.envelopeOpacity != value)
				{
					this.envelopeOpacity = Math.Max(Math.Min(value, 1.0f), 0.0f);
					this.Invalidate();
				}
			}
		}


		public void Invalidate()
		{
			if (this.parentTrack == null) return;
			this.Invalidate(this.model.BeginTime, this.model.EndTime);
		}
		public void Invalidate(float fromUnits, float toUnits)
		{
			if (this.parentTrack == null) return;
			this.parentTrack.Invalidate(fromUnits, toUnits);
		}

		protected virtual void OnModelChanged(TimelineGraphChangedEventArgs e)
		{
			return;
		}
		protected internal virtual void OnPaint(TimelineViewTrackPaintEventArgs e)
		{
			Rectangle rect = e.TargetRect;

			// Draw curve
			switch (this.parentTrack.CurveQuality)
			{
				default:
				case TimelineViewGraphTrack.DrawingQuality.High:
					e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
					break;
				case TimelineViewGraphTrack.DrawingQuality.Low:
					e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
					break;
			}
			this.OnPaintCurve(e);
			e.Graphics.SmoothingMode = SmoothingMode.Default;

			// Draw overlay
			this.OnPaintOverlay(e);
		}
		protected virtual void OnPaintCurve(TimelineViewTrackPaintEventArgs e)
		{
			// Determine graph parameters
			float minPixelStep;
			switch (this.parentTrack.CurvePrecision)
			{
				case TimelineViewGraphTrack.PrecisionLevel.High:
					minPixelStep = 0.25f;
					break;
				default:
				case TimelineViewGraphTrack.PrecisionLevel.Medium:
					minPixelStep = 0.5f;
					break;
				case TimelineViewGraphTrack.PrecisionLevel.Low:
					minPixelStep = 1.0f;
					break;
			}
			float minUnitStep = this.ParentView.ConvertPixelsToUnits(minPixelStep);
			Rectangle rect = e.TargetRect;

			// Draw the envelope area
			if (this.envelopeOpacity > 0.0f)
			{
				float envelopeRadius = this.ParentView.ConvertPixelsToUnits(10.0f); // 10 Pixel envelope Radius
				float minEnvelopeStepFactor;
				switch (this.parentTrack.EnvelopePrecision)
				{
					case TimelineViewGraphTrack.PrecisionLevel.High:
						minEnvelopeStepFactor = 2.0f;
						break;
					default:
					case TimelineViewGraphTrack.PrecisionLevel.Medium:
						minEnvelopeStepFactor = 5.0f;
						break;
					case TimelineViewGraphTrack.PrecisionLevel.Low:
						minEnvelopeStepFactor = 10.0f;
						break;
				}
				List<PointF> curvePointsEnvMax = this.GetCurvePoints(
					rect, 
					x => this.model.GetMaxValueInRange(minEnvelopeStepFactor * (int)(x / minEnvelopeStepFactor) - envelopeRadius, minEnvelopeStepFactor * (int)(x / minEnvelopeStepFactor) + envelopeRadius), 
					minPixelStep * minEnvelopeStepFactor, 
					e.BeginTime, 
					e.EndTime);
				List<PointF> curvePointsEnvMin = this.GetCurvePoints(
					rect, 
					x => this.model.GetMinValueInRange(minEnvelopeStepFactor * (int)(x / minEnvelopeStepFactor) - envelopeRadius, minEnvelopeStepFactor * (int)(x / minEnvelopeStepFactor) + envelopeRadius), 
					minPixelStep * minEnvelopeStepFactor, 
					e.BeginTime, 
					e.EndTime);
				PointF[] envelopeVertices = new PointF[curvePointsEnvMax.Count + curvePointsEnvMin.Count];
				if (envelopeVertices.Length >= 3)
				{
					for (int i = 0; i < curvePointsEnvMax.Count; i++)
					{
						envelopeVertices[i] = curvePointsEnvMax[i];
					}
					for (int i = 0; i < curvePointsEnvMin.Count; i++)
					{
						envelopeVertices[curvePointsEnvMax.Count + i] = curvePointsEnvMin[curvePointsEnvMin.Count - i - 1];
					}
					e.Graphics.FillPolygon(new SolidBrush(Color.FromArgb((int)(this.envelopeOpacity * 255.0f), this.baseColor)), envelopeVertices);
				}
			}

			// Draw the graph
			if (this.curveOpacity > 0.0f)
			{
				List<PointF> curvePoints = this.GetCurvePoints(rect, this.model.GetValueAtX, minPixelStep, e.BeginTime, e.EndTime);
				if (curvePoints.Count >= 2)
				{
					e.Graphics.DrawLines(new Pen(Color.FromArgb((int)(this.curveOpacity * 255.0f), this.baseColor)), curvePoints.ToArray());
				}
			}
		}
		protected virtual void OnPaintOverlay(TimelineViewTrackPaintEventArgs e)
		{
			Rectangle rect = e.TargetRect;
			float beginX = this.ParentView.GetPosAtUnit(e.BeginTime);
			float endX = this.ParentView.GetPosAtUnit(e.EndTime);

			// Draw boundaries
			{
				Pen boundaryPen = new Pen(Color.FromArgb(128, Color.Red));
				boundaryPen.DashStyle = DashStyle.Dash;
				e.Graphics.DrawLine(boundaryPen, beginX, rect.Top, beginX, rect.Bottom);
				e.Graphics.DrawLine(boundaryPen, endX, rect.Top, endX, rect.Bottom);
			}
		}

		protected List<PointF> GetCurvePoints(Rectangle trackArea, Func<float,float> curveFunc, float minPixelStep, float beginUnitX, float endUnitX)
		{
			// Determine graph parameters
			float MinUnitStep = this.ParentView.ConvertPixelsToUnits(minPixelStep);
			if (beginUnitX > endUnitX) return new List<PointF>();

			// Determine sample points
			List<PointF> curvePoints = new List<PointF>(1 + (int)((endUnitX - beginUnitX) / MinUnitStep));
			{
				// Gather explicit samples (in units, x)
				List<float> explicitSamples = new List<float>();
				explicitSamples.Add(this.model.BeginTime);
				explicitSamples.Add(this.model.EndTime);
				explicitSamples.Add(endUnitX);
				// Add more explicit samples here, when necessary
				explicitSamples.RemoveAll(s => s < beginUnitX);
				explicitSamples.RemoveAll(s => s > endUnitX);
				explicitSamples.Sort();

				// Gather dynamic samples based on graph function fluctuation
				float errorThreshold = 0.2f * Math.Abs(this.parentTrack.VerticalUnitTop - this.parentTrack.VerticalUnitBottom) / trackArea.Height;
				float curUnitX = beginUnitX;
				float curUnitY;
				float curPixelX = this.ParentView.GetPosAtUnit(beginUnitX);
				float curPixelY = trackArea.Y + this.parentTrack.GetPosAtUnit(curveFunc(beginUnitX - MinUnitStep));
				float lastPixelX;
				float lastPixelY;
				float lastSamplePixelX = 0.0f;
				float lastSamplePixelY = 0.0f;
				int nextExplicitIndex = 0;
				bool explicitSample = true;
				int skipCount = 0;
				while (curUnitX <= endUnitX)
				{
					lastPixelY = curPixelY;
					curUnitY = curveFunc(curUnitX);
					curPixelY = trackArea.Y + this.parentTrack.GetPosAtUnit(curUnitY);

					if (explicitSample)
					{
						curvePoints.Add(new PointF(curPixelX, curPixelY));
						lastSamplePixelX = curPixelX;
						lastSamplePixelY = curPixelY;
						explicitSample = false;
					}
					else
					{
						float error = GetLinearInterpolationError(
							this.ParentView.GetUnitAtPos(lastSamplePixelX), 
							this.parentTrack.GetUnitAtPos(lastSamplePixelY - trackArea.Y), 
							curUnitX, 
							curUnitY, 
							x => curveFunc(x));
						if (error * skipCount >= errorThreshold)
						{
							skipCount = 0;
							curvePoints.Add(new PointF(curPixelX, curPixelY));
							lastSamplePixelX = curPixelX;
							lastSamplePixelY = curPixelY;
						}
						else
						{
							skipCount++;
						}
					}

					lastPixelX = curPixelX;
					curPixelX += minPixelStep;
					curUnitX = this.ParentView.GetUnitAtPos(curPixelX);

					// Look out for explicit samples
					if (nextExplicitIndex < explicitSamples.Count && curUnitX >= explicitSamples[nextExplicitIndex])
					{
						curUnitX = explicitSamples[nextExplicitIndex];
						curPixelX = this.ParentView.GetPosAtUnit(curUnitX);
						nextExplicitIndex++;
						explicitSample = true;
					}
				}
			}

			return curvePoints;
		}
		protected static float GetLinearInterpolationError(float beginX, float beginY, float endX, float endY, Func<float,float> func)
		{
			double stepSize = (endX - beginX) / 10.0d;
			double curX = beginX + stepSize;
			double error = 0.0f;
			while (curX < endX)
			{
				double alpha = (curX - beginX) / (endX - beginX);
				double interpolatedY = beginY + (endY - beginY) * alpha;
				double realY = func((float)curX);
				error += Math.Abs(interpolatedY - realY);
				curX += stepSize;
			}
			return (float)(error / 10.0d);
		}
	}
}
