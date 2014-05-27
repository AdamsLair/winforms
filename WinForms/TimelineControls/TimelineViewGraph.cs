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
		private	float					envelopeOpacity	= 0.35f;


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

			// Determine sample points
			List<PointF> curvePoints = null;
			List<PointF> curvePointsEnvMax = null;
			List<PointF> curvePointsEnvMin = null;
			if (this.curveOpacity > 0.0f)
			{
				curvePoints = this.GetCurvePoints(rect, this.model.GetValueAtX, minPixelStep, e.BeginTime, e.EndTime);
			}
			if (this.envelopeOpacity > 0.0f)
			{
				const float EnvelopeBasePixelRadius = 5.0f;
				float envelopeUnitRadius = this.ParentView.ConvertPixelsToUnits(EnvelopeBasePixelRadius);
				float minEnvelopeStepFactor;
				switch (this.parentTrack.EnvelopePrecision)
				{
					case TimelineViewGraphTrack.PrecisionLevel.High:
						minEnvelopeStepFactor = 0.1f;
						break;
					default:
					case TimelineViewGraphTrack.PrecisionLevel.Medium:
						minEnvelopeStepFactor = 0.5f;
						break;
					case TimelineViewGraphTrack.PrecisionLevel.Low:
						minEnvelopeStepFactor = 1.0f;
						break;
				}
				float envelopePixelStep = minEnvelopeStepFactor * EnvelopeBasePixelRadius;
				float envelopeUnitStep = minEnvelopeStepFactor * envelopeUnitRadius;
				curvePointsEnvMax = this.GetCurvePoints(
					rect, 
					x => this.model.GetMaxValueInRange(envelopeUnitStep * (int)(x / envelopeUnitStep) - envelopeUnitRadius, envelopeUnitStep * (int)(x / envelopeUnitStep) + envelopeUnitRadius), 
					envelopePixelStep, 
					e.BeginTime, 
					e.EndTime);
				curvePointsEnvMin = this.GetCurvePoints(
					rect, 
					x => this.model.GetMinValueInRange(envelopeUnitStep * (int)(x / envelopeUnitStep) - envelopeUnitRadius, envelopeUnitStep * (int)(x / envelopeUnitStep) + envelopeUnitRadius), 
					envelopePixelStep, 
					e.BeginTime, 
					e.EndTime);
			}

			// Draw the envelope area
			LinearGradientBrush curveGradient = null;
			LinearGradientBrush envelopeGradient = null;
			if (curvePointsEnvMax != null && curvePointsEnvMin != null && curvePointsEnvMax.Count + curvePointsEnvMin.Count >= 3)
			{
				PointF[] envelopeVertices = new PointF[curvePointsEnvMax.Count + curvePointsEnvMin.Count];
				for (int i = 0; i < curvePointsEnvMax.Count; i++)
				{
					envelopeVertices[i] = curvePointsEnvMax[i];
				}
				for (int i = 0; i < curvePointsEnvMin.Count; i++)
				{
					envelopeVertices[curvePointsEnvMax.Count + i] = curvePointsEnvMin[curvePointsEnvMin.Count - i - 1];
				}

				if (curvePoints != null)
				{
					float varianceUnitRadius = this.ParentView.ConvertPixelsToUnits(0.5f);
					KeyValuePair<float,float>[] baseBlend = new KeyValuePair<float,float>[Math.Max(curvePoints.Count, 2)];
					for (int i = 0; i < baseBlend.Length; i++)
					{
						float relativeX = (float)(curvePoints[(int)((float)i * (curvePoints.Count - 1) / (float)(baseBlend.Length - 1))].X - rect.X) / (float)rect.Width;
						float unitX = this.ParentView.GetUnitAtPos(rect.X + relativeX * rect.Width);
						float variance = this.parentTrack.ConvertUnitsToPixels(
							this.model.GetMaxValueInRange(unitX - varianceUnitRadius, unitX + varianceUnitRadius) - 
							this.model.GetMinValueInRange(unitX - varianceUnitRadius, unitX + varianceUnitRadius)) / 80.0f;
						float localOpacity = Math.Max(Math.Min(variance * variance, 1.0f), 0.0f);
						baseBlend[i] = new KeyValuePair<float,float>(relativeX, localOpacity);
					}

					envelopeGradient = new LinearGradientBrush(rect, Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal);
					curveGradient = new LinearGradientBrush(rect, Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal);

					const int Samples = 21;
					const int SamplesHalf = Samples / 2;
					const int BlendSamplesPerChunk = 4;
					float highestOpacity = 0.0f;
					ColorBlend envelopeBlend = new ColorBlend(Math.Max(baseBlend.Length * BlendSamplesPerChunk / Samples, 2));
					ColorBlend curveBlend = new ColorBlend(Math.Max(baseBlend.Length * BlendSamplesPerChunk / Samples, 2));
					for (int i = 0; i < envelopeBlend.Colors.Length; i++)
					{
						int firstIndex = Math.Min(Math.Max(i * Samples / BlendSamplesPerChunk - SamplesHalf, 0), baseBlend.Length - 1);
						int lastIndex = Math.Min(Math.Max(i * Samples / BlendSamplesPerChunk + SamplesHalf, 0), baseBlend.Length - 1);
						float sum = 0.0f;
						for (int j = firstIndex; j <= lastIndex; j++)
						{
							sum += baseBlend[j].Value;
						}
						float localOpacity = sum / (float)(1 + lastIndex - firstIndex);
						highestOpacity = Math.Max(highestOpacity, localOpacity);
						envelopeBlend.Colors[i] = Color.FromArgb((int)(localOpacity * this.envelopeOpacity * 255.0f), this.baseColor);
						envelopeBlend.Positions[i] = baseBlend[firstIndex + (lastIndex - firstIndex) / 2].Key;
						curveBlend.Colors[i] = Color.FromArgb((int)((1.0f - localOpacity) * (1.0f - localOpacity) * this.curveOpacity * 255.0f), this.baseColor);
						curveBlend.Positions[i] = baseBlend[firstIndex + (lastIndex - firstIndex) / 2].Key;
					}

					if (highestOpacity <= 0.01f)
					{
						envelopeGradient = null;
						curveGradient = null;
					}
					else
					{
						envelopeBlend.Positions[0] = 0.0f;
						envelopeBlend.Positions[envelopeBlend.Positions.Length - 1] = 1.0f;
						envelopeGradient.InterpolationColors = envelopeBlend;
						curveBlend.Positions[0] = 0.0f;
						curveBlend.Positions[curveBlend.Positions.Length - 1] = 1.0f;
						curveGradient.InterpolationColors = curveBlend;
					}
				}

				if (envelopeGradient != null)
				{
					e.Graphics.FillPolygon(envelopeGradient, envelopeVertices);
				}
			}

			// Draw the graph
			if (curvePoints != null && curvePoints.Count >= 2)
			{
				Pen linePen;
				if (curveGradient != null)
				{
					linePen = new Pen(curveGradient);
				}
				else
				{
					linePen = new Pen(Color.FromArgb((int)(this.curveOpacity * 255.0f), this.baseColor));
				}
				e.Graphics.DrawLines(linePen, curvePoints.ToArray());
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
			if (beginUnitX > endUnitX) return null;

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
				float errorThreshold = Math.Abs(this.parentTrack.VerticalUnitTop - this.parentTrack.VerticalUnitBottom) / trackArea.Height;
				switch (this.parentTrack.CurvePrecision)
				{
					case TimelineViewGraphTrack.PrecisionLevel.High:
						errorThreshold *= 1.0f;
						break;
					default:
					case TimelineViewGraphTrack.PrecisionLevel.Medium:
						errorThreshold *= 2.5f;
						break;
					case TimelineViewGraphTrack.PrecisionLevel.Low:
						errorThreshold *= 5.0f;
						break;
				}
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
							curveFunc);
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
			const double Samples = 2.0d;
			double stepSize = (endX - beginX) / Samples;
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
			return (float)(error / Samples);
		}
	}
}
