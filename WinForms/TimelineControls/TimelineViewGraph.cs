using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	[TimelineModelViewAssignment(typeof(ITimelineGraphModel))]
	public class TimelineViewGraph
	{
		public const float EnvelopeBasePixelRadius = 5.0f;

		private	TimelineViewGraphTrack	parentTrack		= null;
		private	ITimelineGraphModel		model			= null;
		private	Color					baseColor		= Color.Red;
		private	float					curveOpacity	= 1.0f;
		private	float					envelopeOpacity	= 0.35f;

		private	bool				skipEnvelope			= false;
		private	bool				curveCacheDirty			= true;
		private	PointF[]			cacheCurveVertices		= null;
		private	PointF[]			cacheEnvelopeVertices	= null;
		private	LinearGradientBrush	cacheCurveGradient		= null;
		private	LinearGradientBrush	cacheEnvelopeGradient	= null;


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
					this.curveCacheDirty = true;
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
					this.curveCacheDirty = true;
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

		public float GetEnvelopeVisibility(float unitYRange)
		{
			float variance = this.parentTrack.ConvertUnitsToPixels(unitYRange) / 80.0f;
			return Math.Max(Math.Min(variance * variance, 1.0f), 0.0f);
		}

		protected virtual void OnModelChanged(TimelineGraphChangedEventArgs e)
		{
			if (e.OldGraph != null)
			{
				e.OldGraph.GraphChanged -= this.Model_GraphChanged;
			}
			this.skipEnvelope = false;
			this.curveCacheDirty = true;
			if (e.Graph != null)
			{
				e.Graph.GraphChanged += this.Model_GraphChanged;
			}
			return;
		}
		protected internal virtual void OnPaint(TimelineViewTrackPaintEventArgs e)
		{
			Rectangle rect = e.TargetRect;

			// Draw curve
			switch (e.GetAdjustedQuality(this.parentTrack.CurveQuality))
			{
				case QualityLevel.High:
					e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
					break;
				default:
				case QualityLevel.Medium:
				case QualityLevel.Low:
					e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
					break;
			}
			float pixelWidth = this.ParentView.ConvertUnitsToPixels(this.model.EndTime - this.model.BeginTime);
			if (pixelWidth < 5) e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
			if (pixelWidth > 2)
			{
				this.OnPaintCurve(e);
			}
			e.Graphics.SmoothingMode = SmoothingMode.Default;

			// Draw overlay
			this.OnPaintBoundaries(e);
		}
		protected virtual void OnPaintCurve(TimelineViewTrackPaintEventArgs e)
		{
			// Ignore e.BeginTime and e.EndTime for sampling, as we're heavily dependent on rounding errors, etc. while undersampling. Instead, always sample the whole curve.
			float beginUnitX = Math.Max(this.ParentView.UnitOriginOffset - this.ParentView.UnitScroll, this.model.BeginTime);
			float endUnitX = Math.Min(this.ParentView.UnitOriginOffset - this.ParentView.UnitScroll + this.ParentView.VisibleUnitWidth, this.model.EndTime);

			// Determine graph parameters
			float minPixelStep;
			switch (e.GetAdjustedQuality(this.parentTrack.CurvePrecision))
			{
				case QualityLevel.High:
					minPixelStep = 0.25f;
					break;
				default:
				case QualityLevel.Medium:
					minPixelStep = 0.5f;
					break;
				case QualityLevel.Low:
					minPixelStep = 1.0f;
					break;
			}
			float visibleMax = this.model.GetMaxValueInRange(beginUnitX, endUnitX);
			float visibleMin = this.model.GetMinValueInRange(beginUnitX, endUnitX);
			float visiblePixelHeight = this.ParentTrack.ConvertUnitsToPixels(Math.Abs(visibleMax - visibleMin));
			if (visiblePixelHeight < 10.0f)			minPixelStep *= 8.0f;
			else if (visiblePixelHeight < 20.0f)	minPixelStep *= 4.0f;
			else if (visiblePixelHeight < 40.0f)	minPixelStep *= 2.0f;
			else if (visiblePixelHeight < 70.0f)	minPixelStep *= 1.5f;
			minPixelStep = Math.Min(minPixelStep, 4);
			float minUnitStep = this.ParentView.ConvertPixelsToUnits(minPixelStep);
			Rectangle rect = e.TargetRect;
			
			if (this.curveCacheDirty)
			{
				// Begin a little sooner, so interpolation / error checking can gather some data
				beginUnitX -= minUnitStep * 5.0f;

				// Determine sample points
				PointF[] curvePointsEnvMax = null;
				PointF[] curvePointsEnvMin = null;
				if (this.curveOpacity > 0.0f)
				{
					this.cacheCurveVertices = this.GetCurvePoints(rect, e.GetAdjustedQuality(this.parentTrack.CurvePrecision), this.model.GetValueAtX, minPixelStep, beginUnitX, endUnitX);
				}
				if (this.envelopeOpacity > 0.0f && !this.skipEnvelope)
				{
					float envelopeUnitRadius = this.ParentView.ConvertPixelsToUnits(EnvelopeBasePixelRadius);
					float minEnvelopeStepFactor;
					switch (e.GetAdjustedQuality(this.parentTrack.EnvelopePrecision))
					{
						case QualityLevel.High:
							minEnvelopeStepFactor = 0.1f;
							break;
						default:
						case QualityLevel.Medium:
							minEnvelopeStepFactor = 0.5f;
							break;
						case QualityLevel.Low:
							minEnvelopeStepFactor = 1.0f;
							break;
					}
					float envelopePixelStep = minEnvelopeStepFactor * EnvelopeBasePixelRadius;
					float envelopeUnitStep = minEnvelopeStepFactor * envelopeUnitRadius;
					curvePointsEnvMax = this.GetCurvePoints(
						rect,
 						e.GetAdjustedQuality(this.parentTrack.CurvePrecision),
						x => this.model.GetMaxValueInRange(x - envelopeUnitRadius, x + envelopeUnitRadius), 
						envelopePixelStep, 
						envelopeUnitStep * (int)(beginUnitX / envelopeUnitStep), 
						envelopeUnitStep * ((int)(endUnitX / envelopeUnitStep) + 1));
					curvePointsEnvMin = this.GetCurvePoints(
						rect, 
 						e.GetAdjustedQuality(this.parentTrack.CurvePrecision),
						x => this.model.GetMinValueInRange(x - envelopeUnitRadius, x + envelopeUnitRadius), 
						envelopePixelStep,
						envelopeUnitStep * (int)(beginUnitX / envelopeUnitStep), 
						envelopeUnitStep * ((int)(endUnitX / envelopeUnitStep) + 1));

					if (curvePointsEnvMax == null || curvePointsEnvMin == null || curvePointsEnvMax.Length + curvePointsEnvMin.Length < 3)
						this.skipEnvelope = true;
				}

				if (curvePointsEnvMax != null && curvePointsEnvMin != null && curvePointsEnvMax.Length + curvePointsEnvMin.Length >= 3)
				{
					// Calculate the visible envelope polygon
					this.cacheEnvelopeVertices = new PointF[curvePointsEnvMax.Length + curvePointsEnvMin.Length];
					for (int i = 0; i < curvePointsEnvMax.Length; i++)
					{
						this.cacheEnvelopeVertices[i] = curvePointsEnvMax[i];
					}
					for (int i = 0; i < curvePointsEnvMin.Length; i++)
					{
						this.cacheEnvelopeVertices[curvePointsEnvMax.Length + i] = curvePointsEnvMin[curvePointsEnvMin.Length - i - 1];
					}

					// Calculate the envelope and curve gradients
					if (this.cacheCurveVertices != null)
					{
						float varianceUnitRadius = this.ParentView.ConvertPixelsToUnits(0.5f);
						KeyValuePair<float,float>[] baseBlend = new KeyValuePair<float,float>[Math.Max(this.cacheCurveVertices.Length, 2)];
						for (int i = 0; i < baseBlend.Length; i++)
						{
							float relativeX = (float)(this.cacheCurveVertices[(int)((float)i * (this.cacheCurveVertices.Length - 1) / (float)(baseBlend.Length - 1))].X - rect.X) / (float)rect.Width;
							float unitX = this.ParentView.GetUnitAtPos(rect.X + relativeX * rect.Width);
							float localOpacity = this.GetEnvelopeVisibility(
								this.model.GetMaxValueInRange(unitX - varianceUnitRadius, unitX + varianceUnitRadius) - 
								this.model.GetMinValueInRange(unitX - varianceUnitRadius, unitX + varianceUnitRadius));
							baseBlend[i] = new KeyValuePair<float,float>(relativeX, localOpacity);
						}

						this.cacheEnvelopeGradient = new LinearGradientBrush(rect, Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal);
						this.cacheCurveGradient = new LinearGradientBrush(rect, Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal);

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

						if (highestOpacity <= 0.05f)
						{
							this.cacheEnvelopeGradient = null;
							this.cacheCurveGradient = null;
							this.skipEnvelope = true;
						}
						else
						{
							envelopeBlend.Positions[0] = 0.0f;
							envelopeBlend.Positions[envelopeBlend.Positions.Length - 1] = 1.0f;
							this.cacheEnvelopeGradient.InterpolationColors = envelopeBlend;
							curveBlend.Positions[0] = 0.0f;
							curveBlend.Positions[curveBlend.Positions.Length - 1] = 1.0f;
							this.cacheCurveGradient.InterpolationColors = curveBlend;
						}
					}
				}
			}
			
			// Draw the envelope area
			if (this.cacheEnvelopeGradient != null && this.cacheEnvelopeVertices != null && this.cacheEnvelopeVertices.Length >= 3)
			{
				e.Graphics.FillPolygon(this.cacheEnvelopeGradient, this.cacheEnvelopeVertices);
			}

			// Draw the graph
			if (this.cacheCurveVertices != null && this.cacheCurveVertices.Length >= 2)
			{
				Pen linePen;
				if (this.cacheCurveGradient != null)
				{
					linePen = new Pen(this.cacheCurveGradient);
				}
				else
				{
					linePen = new Pen(Color.FromArgb((int)(this.curveOpacity * 255.0f), this.baseColor));
				}
				e.Graphics.DrawLines(linePen, this.cacheCurveVertices);
			}

			// Keep in mind that our cache is no valid again
			if (e.GetAdjustedQuality(this.parentTrack.CurvePrecision) == this.parentTrack.CurvePrecision && 
				e.GetAdjustedQuality(this.parentTrack.EnvelopePrecision) == this.parentTrack.EnvelopePrecision)
			{
				this.curveCacheDirty = false;
			}
		}
		protected virtual void OnPaintBoundaries(TimelineViewTrackPaintEventArgs e)
		{
			Rectangle rect = e.TargetRect;
			float beginX = this.ParentView.GetPosAtUnit(this.model.BeginTime);
			float endX = this.ParentView.GetPosAtUnit(this.model.EndTime);

			// Draw boundaries
			{
				Pen boundaryPen = new Pen(Color.FromArgb(128, this.baseColor));
				boundaryPen.DashStyle = DashStyle.Dash;
				e.Graphics.DrawLine(boundaryPen, beginX, rect.Top, beginX, rect.Bottom);
				e.Graphics.DrawLine(boundaryPen, endX, rect.Top, endX, rect.Bottom);
			}
		}
		protected internal virtual void OnViewportChanged()
		{
			this.skipEnvelope = false;
			this.curveCacheDirty = true;
		}

		protected PointF[] GetCurvePoints(Rectangle trackArea, QualityLevel sampleQuality, Func<float,float> curveFunc, float minPixelStep, float beginUnitX, float endUnitX)
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
				explicitSamples.RemoveAll(s => s < beginUnitX || s < this.model.BeginTime);
				explicitSamples.RemoveAll(s => s > endUnitX || s > this.model.EndTime);
				explicitSamples.Sort();

				// Gather dynamic samples based on graph function fluctuation
				float errorThreshold = 1.0f * Math.Abs(this.parentTrack.VerticalUnitTop - this.parentTrack.VerticalUnitBottom) / trackArea.Height;
				switch (sampleQuality)
				{
					case QualityLevel.High:
						errorThreshold *= 1.0f;
						break;
					default:
					case QualityLevel.Medium:
						errorThreshold *= 2.0f;
						break;
					case QualityLevel.Low:
						errorThreshold *= 4.0f;
						break;
				}
				float curUnitX = MinUnitStep * (int)(beginUnitX / MinUnitStep) - MinUnitStep;
				float curUnitY;
				float curPixelX = this.ParentView.GetPosAtUnit(beginUnitX);
				float curPixelY = trackArea.Y + this.parentTrack.GetPosAtUnit(curveFunc(beginUnitX - MinUnitStep));
				float lastPixelX;
				float lastPixelY;
				float lastSampleUnitX = 0.0f;
				float lastSampleUnitY = 0.0f;
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
						if (curUnitX >= this.model.BeginTime && curUnitX <= this.model.EndTime)
						{
							curvePoints.Add(new PointF(curPixelX, curPixelY));
						}
						lastSampleUnitX = curUnitX;
						lastSampleUnitY = curUnitY;
						explicitSample = false;
					}
					else
					{
						float error = 0.0f;
						if (skipCount > 0)
						{
							error = GetLinearInterpolationError(
								lastSampleUnitX, 
								lastSampleUnitY, 
								curUnitX, 
								curUnitY, 
								curveFunc);
						}
						if (error * skipCount >= errorThreshold)
						{
							skipCount = 0;
							if (curUnitX >= this.model.BeginTime && curUnitX <= this.model.EndTime)
							{
								curvePoints.Add(new PointF(curPixelX, curPixelY));
							}
							lastSampleUnitX = curUnitX;
							lastSampleUnitY = curUnitY;
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

			return curvePoints != null ? curvePoints.ToArray() : null;
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

		private void Model_GraphChanged(object sender, TimelineGraphRangeEventArgs e)
		{
			this.skipEnvelope = false;
		}
	}
}
