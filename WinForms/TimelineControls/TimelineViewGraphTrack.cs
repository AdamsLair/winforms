using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	[TimelineTrackAssignment(typeof(ITimelineGraphTrackModel))]
	public class TimelineViewGraphTrack : TimelineViewTrack
	{
		public enum AdjustVerticalMode
		{
			GrowAndShrink,
			Grow,
			Shrink
		}


		private	float	verticalUnitTop		= 1.0f;
		private	float	verticalUnitBottom	= -1.0f;


		public new ITimelineGraphTrackModel Model
		{
			get { return base.Model as ITimelineGraphTrackModel; }
		}
		public float VerticalUnitTop
		{
			get { return this.verticalUnitTop; }
			set { this.verticalUnitTop = value; }
		}
		public float VerticalUnitBottom
		{
			get { return this.verticalUnitBottom; }
			set { this.verticalUnitBottom = value; }
		}
		

		public float GetUnitAtPos(float y)
		{
			return this.verticalUnitTop + ((float)y * (this.verticalUnitBottom - this.verticalUnitTop) / (float)(this.Height - 1));
		}
		public float GetPosAtUnit(float unit)
		{
			return (float)(this.Height - 1) * ((unit - this.verticalUnitTop) / (this.verticalUnitBottom - this.verticalUnitTop));
		}
		public IEnumerable<TimelineViewRulerMark> GetVisibleRulerMarks()
		{
			const float BigMarkDelta = 0.00001f;
			float bigMarkRange = TimelineView.GetNiceMultiple(Math.Abs(this.verticalUnitTop - this.verticalUnitBottom) * 0.5f);
			float rulerStep = -TimelineView.GetNiceMultiple(Math.Abs(this.verticalUnitTop - this.verticalUnitBottom)) / 10.0f;

			Rectangle trackRect = this.ParentView.GetTrackRectangle(this);
			int lineIndex = 0;
			foreach (float unitValue in TimelineView.EnumerateRulerMarks(rulerStep, 0.0f, this.verticalUnitTop, this.verticalUnitBottom, 1))
			{
				float markY = this.GetPosAtUnit(unitValue) + trackRect.Y;

				TimelineViewRulerMarkWeight weight;
				if ((((unitValue + BigMarkDelta) % bigMarkRange) + bigMarkRange) % bigMarkRange <= BigMarkDelta * 2.0f)
					weight = TimelineViewRulerMarkWeight.Major;
				else
					weight = TimelineViewRulerMarkWeight.Regular;

				if (Math.Abs(unitValue - this.verticalUnitTop) >= rulerStep && Math.Abs(unitValue - this.verticalUnitBottom) >= rulerStep)
				{
					yield return new TimelineViewRulerMark(unitValue, markY, weight);
				}

				lineIndex++;
			}

			yield break;
		}
		public void AdjustVerticalUnits(AdjustVerticalMode adjustMode)
		{
			float targetTop;
			float targetBottom;

			if (!this.Model.Graphs.Any())
			{
				targetTop = 1.0f;
				targetBottom = -1.0f;
			}
			else
			{
				float minUnits = float.MaxValue;
				float maxUnits = float.MinValue;
				foreach (ITimelineGraph graph in this.Model.Graphs)
				{
					minUnits = Math.Min(minUnits, graph.GetMinValueInRange(graph.BeginTime, graph.EndTime));
					maxUnits = Math.Max(maxUnits, graph.GetMaxValueInRange(graph.BeginTime, graph.EndTime));
				}
				targetTop = TimelineView.GetNiceMultiple(maxUnits);
				targetBottom = TimelineView.GetNiceMultiple(minUnits);
			}

			switch (adjustMode)
			{
				default:
				case AdjustVerticalMode.GrowAndShrink:
					this.verticalUnitTop = targetTop;
					this.verticalUnitBottom = targetBottom;
					break;
				case AdjustVerticalMode.Grow:
					this.verticalUnitTop = Math.Max(this.verticalUnitTop, targetTop);
					this.verticalUnitBottom = Math.Min(this.verticalUnitBottom, targetBottom);
					break;
				case AdjustVerticalMode.Shrink:
					this.verticalUnitTop = Math.Min(this.verticalUnitTop, targetTop);
					this.verticalUnitBottom = Math.Max(this.verticalUnitBottom, targetBottom);
					break;
			}

			if (this.verticalUnitBottom == this.verticalUnitTop)
				this.verticalUnitTop += 1.0f;
		}

		protected override void CalculateContentWidth(out float beginTime, out float endTime)
		{
			base.CalculateContentWidth(out beginTime, out endTime);
			if (this.Model.Graphs.Any())
			{
				beginTime = this.Model.Graphs.Min(g => g.BeginTime);
				endTime = this.Model.Graphs.Max(g => g.EndTime);
			}
			else
			{
				beginTime = 0.0f;
				endTime = 0.0f;
			}
		}

		protected override void OnModelChanged(TimelineTrackModelChangedEventArgs e)
		{
			base.OnModelChanged(e);
			if (e.OldModel != null)
			{
				(e.OldModel as ITimelineGraphTrackModel).GraphCollectionChanged -= this.model_GraphCollectionChanged;
				(e.OldModel as ITimelineGraphTrackModel).GraphChanged -= this.model_GraphChanged;
			}
			if (e.Model != null)
			{
				(e.Model as ITimelineGraphTrackModel).GraphCollectionChanged += this.model_GraphCollectionChanged;
				(e.Model as ITimelineGraphTrackModel).GraphChanged += this.model_GraphChanged;
			}
			this.AdjustVerticalUnits(AdjustVerticalMode.GrowAndShrink);
		}
		protected internal override void OnPaint(TimelineViewTrackPaintEventArgs e)
		{
			base.OnPaint(e);

			Rectangle rect = e.TargetRect;

			// Draw extended ruler markings in the background
			{
				Pen bigLinePen = new Pen(new SolidBrush(e.Renderer.ColorRulerMarkMajor.ScaleAlpha(0.25f)));
				Pen medLinePen = new Pen(new SolidBrush(e.Renderer.ColorRulerMarkRegular.ScaleAlpha(0.25f)));
				Pen minLinePen = new Pen(new SolidBrush(e.Renderer.ColorRulerMarkMinor.ScaleAlpha(0.25f)));

				// Horizontal ruler marks
				foreach (TimelineViewRulerMark mark in this.ParentView.GetVisibleRulerMarks())
				{
					Pen markPen;
					switch (mark.Weight)
					{
						case TimelineViewRulerMarkWeight.Major:
							markPen = bigLinePen;
							break;
						default:
						case TimelineViewRulerMarkWeight.Regular:
							markPen = medLinePen;
							break;
						case TimelineViewRulerMarkWeight.Minor:
							markPen = minLinePen;
							break;
					}

					e.Graphics.DrawLine(markPen, (int)mark.PixelValue, (int)rect.Top, (int)mark.PixelValue, (int)rect.Bottom);
				}

				// Vertical ruler marks
				foreach (TimelineViewRulerMark mark in this.GetVisibleRulerMarks())
				{
					Pen markPen;
					switch (mark.Weight)
					{
						case TimelineViewRulerMarkWeight.Major:
							markPen = bigLinePen;
							break;
						default:
						case TimelineViewRulerMarkWeight.Regular:
							markPen = medLinePen;
							break;
						case TimelineViewRulerMarkWeight.Minor:
							markPen = minLinePen;
							break;
					}

					e.Graphics.DrawLine(markPen, (int)rect.Left, (int)mark.PixelValue, (int)rect.Right, (int)mark.PixelValue);
				}
			}

			// Draw the graphs
			{
				foreach (ITimelineGraph graph in this.Model.Graphs)
				{
					// Draw curve
					e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
					this.DrawCurve(e.Graphics, rect, graph);
					e.Graphics.SmoothingMode = SmoothingMode.Default;

					// Draw boundaries
					{
						float beginX = this.ParentView.GetPosAtUnit(this.ContentBeginTime);
						float endX = this.ParentView.GetPosAtUnit(this.ContentEndTime);
						Pen boundaryPen = new Pen(Color.FromArgb(128, Color.Red));
						boundaryPen.DashStyle = DashStyle.Dash;
						e.Graphics.DrawLine(boundaryPen, beginX, rect.Top, beginX, rect.Bottom);
						e.Graphics.DrawLine(boundaryPen, endX, rect.Top, endX, rect.Bottom);
					}
				}
			}

			// Draw top and bottom borders
			e.Graphics.DrawLine(new Pen(e.Renderer.ColorVeryDarkBackground), rect.Left, rect.Top, rect.Right, rect.Top);
			e.Graphics.DrawLine(new Pen(e.Renderer.ColorVeryDarkBackground), rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
		}
		protected internal override void OnPaintLeftSidebar(TimelineViewTrackPaintEventArgs e)
		{
			base.OnPaintLeftSidebar(e);
			this.DrawRuler(e.Graphics, e.Renderer, e.TargetRect, true);
		}
		protected internal override void OnPaintRightSidebar(TimelineViewTrackPaintEventArgs e)
		{
			base.OnPaintRightSidebar(e);
			this.DrawRuler(e.Graphics, e.Renderer, e.TargetRect, false);
		}
		protected void DrawRuler(Graphics g, TimelineViewControlRenderer r, Rectangle rect, bool left)
		{
			string verticalTopText = string.Format("{0}", (float)Math.Round(this.verticalUnitTop, 2));
			string verticalBottomText = string.Format("{0}", (float)Math.Round(this.verticalUnitBottom, 2));
			SizeF verticalTopTextSize = g.MeasureString(verticalTopText, r.FontSmall);
			SizeF verticalBottomTextSize = g.MeasureString(verticalBottomText, r.FontSmall);

			// Draw background
			Rectangle borderRect;
			if (this.ParentView.BorderStyle != System.Windows.Forms.BorderStyle.None)
			{
				borderRect = new Rectangle(
					rect.X - (left ? 1 : 0),
					rect.Y,
					rect.Width + 1,
					rect.Height);
			}
			else
			{
				borderRect = rect;
			}
			g.FillRectangle(new SolidBrush(r.ColorVeryLightBackground), rect);
			r.DrawBorder(g, borderRect, Drawing.BorderStyle.Simple, BorderState.Normal);

			// Determine drawing geometry
			Rectangle rectTrackName;
			Rectangle rectUnitMarkings;
			Rectangle rectUnitRuler;
			{
				float markingRatio = 0.5f + 0.5f * (1.0f - Math.Max(Math.Min((float)rect.Height / 32.0f, 1.0f), 0.0f));
				rectTrackName = new Rectangle(
					rect.X, 
					rect.Y, 
					Math.Min(rect.Width, r.FontRegular.Height + 2), 
					rect.Height);
				rectUnitMarkings = new Rectangle(
					rect.Right - Math.Min((int)(rect.Width * markingRatio), 16),
					rect.Y,
					Math.Min((int)(rect.Width * markingRatio), 16),
					rect.Height);
				int maxUnitWidth = Math.Max(Math.Max(rectUnitMarkings.Width, (int)verticalTopTextSize.Width + 2), (int)verticalBottomTextSize.Width + 2);
				rectUnitRuler = new Rectangle(
					rect.Right - maxUnitWidth,
					rect.Y,
					maxUnitWidth,
					rect.Height);

				if (!left)
				{
					rectTrackName.X		= rect.Right - (rectTrackName	.X	- rect.Left) - rectTrackName	.Width;
					rectUnitMarkings.X	= rect.Right - (rectUnitMarkings.X	- rect.Left) - rectUnitMarkings	.Width;
					rectUnitRuler.X		= rect.Right - (rectUnitRuler	.X	- rect.Left) - rectUnitRuler	.Width;
				}
			}

			// Draw track name
			{
				Rectangle overlap = rectUnitMarkings;
				overlap.Intersect(rectTrackName);
				float overlapAmount = Math.Max(Math.Min((float)overlap.Width / (float)rectTrackName.Width, 1.0f), 0.0f);
				float textOverlapAlpha = (1.0f - (overlapAmount));

				StringFormat format = new StringFormat(StringFormat.GenericDefault);
				format.Trimming = StringTrimming.EllipsisCharacter;

				SizeF textSize = g.MeasureString(this.Model.TrackName, r.FontRegular, rectTrackName.Height, format);

				var state = g.Save();
				g.TranslateTransform(
					rectTrackName.X + (int)textSize.Height + 2, 
					rectTrackName.Y);
				g.RotateTransform(90);
				g.DrawString(
					this.Model.TrackName, 
					r.FontRegular, 
					new SolidBrush(Color.FromArgb((int)(textOverlapAlpha * 255), r.ColorText)), 
					new Rectangle(0, 0, rectTrackName.Height, rectTrackName.Width), 
					format);
				g.Restore(state);
			}

			// Draw vertical unit markings
			{
				Pen bigLinePen = new Pen(new SolidBrush(r.ColorRulerMarkMajor));
				Pen medLinePen = new Pen(new SolidBrush(r.ColorRulerMarkRegular));
				Pen minLinePen = new Pen(new SolidBrush(r.ColorRulerMarkMinor));

				// Static Top and Bottom marks
				SizeF textSize;
				textSize = g.MeasureString(verticalTopText, r.FontSmall);
				g.DrawString(verticalTopText, r.FontSmall, new SolidBrush(r.ColorText), rectUnitMarkings.Right - textSize.Width, rectUnitMarkings.Top);
				textSize = g.MeasureString(verticalBottomText, r.FontSmall);
				g.DrawString(verticalBottomText, r.FontSmall, new SolidBrush(r.ColorText), rectUnitMarkings.Right - textSize.Width, rectUnitMarkings.Bottom - textSize.Height - 1);

				// Dynamic Inbetween marks
				foreach (TimelineViewRulerMark mark in this.GetVisibleRulerMarks())
				{
					float markLen;
					Pen markPen;
					bool bigMark;
					switch (mark.Weight)
					{
						case TimelineViewRulerMarkWeight.Major:
							markLen = 0.5f;
							markPen = bigLinePen;
							bigMark = true;
							break;
						default:
						case TimelineViewRulerMarkWeight.Regular:
						case TimelineViewRulerMarkWeight.Minor:
							markLen = 0.25f;
							markPen = medLinePen;
							bigMark = false;
							break;
					}

					int borderDistInner = r.FontSmall.Height / 2;
					int borderDistOuter = r.FontSmall.Height / 2 + 15;
					float borderDist = (float)Math.Min(Math.Abs(mark.PixelValue - rect.Top), Math.Abs(mark.PixelValue - rect.Bottom));
					
					float markTopX;
					float markBottomX;
					if (left)
					{
						markTopX = rectUnitMarkings.Right - markLen * rectUnitMarkings.Width;
						markBottomX = rectUnitMarkings.Right;
					}
					else
					{
						markTopX = rectUnitMarkings.Left;
						markBottomX = rectUnitMarkings.Left + markLen * rectUnitMarkings.Width;
					}

					if (borderDist > borderDistInner)
					{
						float alpha = Math.Min(1.0f, (float)(borderDist - borderDistInner) / (float)(borderDistOuter - borderDistInner));
						Color markColor = Color.FromArgb((int)(alpha * markPen.Color.A), markPen.Color);
						Color textColor = Color.FromArgb((int)(alpha * markPen.Color.A), r.ColorText);

						g.DrawLine(new Pen(markColor), (int)markTopX, (int)mark.PixelValue, (int)markBottomX, (int)mark.PixelValue);

						if (bigMark)
						{
							string text = string.Format("{0}", (float)Math.Round(mark.UnitValue, 2));
							textSize = g.MeasureString(text, r.FontSmall);
							g.DrawString(
								text, 
								r.FontSmall, 
								new SolidBrush(textColor), 
								left ? markTopX - textSize.Width : markBottomX, 
								mark.PixelValue - textSize.Height * 0.5f);
						}
					}
				}
			}
		}
		protected void DrawCurve(Graphics g, Rectangle rect, ITimelineGraph graph)
		{
			// Determine graph parameters
			const float MinPixelStep = 0.5f;
			float MinUnitStep = this.ParentView.ConvertPixelsToUnits(MinPixelStep);
			float beginUnitX = Math.Max(-this.ParentView.UnitScroll, this.ContentBeginTime);
			float endUnitX = Math.Min(-this.ParentView.UnitScroll + this.ParentView.VisibleUnitWidth, this.ContentEndTime);

			var w = new System.Diagnostics.Stopwatch();
			w.Restart();

			// Determine sample points
			List<PointF> curvePoints = this.GetCurvePoints(rect, graph.GetValueAtX, MinPixelStep);
			List<PointF> curvePoints2 = this.GetCurvePoints(rect, x => graph.GetMaxValueInRange(x - MinUnitStep * 20.0f, x + MinUnitStep * 20.0f), MinPixelStep * 5);
			List<PointF> curvePoints3 = this.GetCurvePoints(rect, x => graph.GetMinValueInRange(x - MinUnitStep * 20.0f, x + MinUnitStep * 20.0f), MinPixelStep * 5);
			
			w.Stop();
			Console.WriteLine("Calc: {0:F}", w.Elapsed.TotalMilliseconds);
			w.Restart();

			// Draw the graph
			PointF[] envelopeVertices = new PointF[curvePoints2.Count + curvePoints3.Count];
			for (int i = 0; i < curvePoints2.Count; i++)
				envelopeVertices[i] = curvePoints2[i];
			for (int i = 0; i < curvePoints3.Count; i++)
				envelopeVertices[curvePoints2.Count + i] = curvePoints3[curvePoints3.Count - i - 1];
			if (envelopeVertices.Length >= 3) g.FillPolygon(new SolidBrush(Color.FromArgb(48, Color.Red)), envelopeVertices);
			if (curvePoints.Count >= 2) g.DrawLines(Pens.Red, curvePoints.ToArray());
			if (curvePoints2.Count >= 2) g.DrawLines(new Pen(Color.FromArgb(128, Color.Red)), curvePoints2.ToArray());
			if (curvePoints3.Count >= 2) g.DrawLines(new Pen(Color.FromArgb(128, Color.Red)), curvePoints3.ToArray());

			w.Stop();
			Console.WriteLine("Draw: {0:F}", w.Elapsed.TotalMilliseconds);
		}

		private List<PointF> GetCurvePoints(Rectangle rect, Func<float,float> func, float minPixelStep)
		{
			// Determine graph parameters
			float MinUnitStep = this.ParentView.ConvertPixelsToUnits(minPixelStep);
			float beginUnitX = Math.Max(-this.ParentView.UnitScroll, this.ContentBeginTime);
			float endUnitX = Math.Min(-this.ParentView.UnitScroll + this.ParentView.VisibleUnitWidth, this.ContentEndTime);

			// Determine sample points
			List<PointF> curvePoints = new List<PointF>();
			{
				// Gather explicit samples (in units, x)
				List<float> explicitSamples = new List<float>();
				explicitSamples.Add(this.ContentEndTime);
				explicitSamples.Add(endUnitX);
				// Add more explicit samples here, when necessary
				explicitSamples.Sort();

				// Gather dynamic samples based on graph function fluctuation
				float errorThreshold = 0.2f * Math.Abs(this.verticalUnitTop - this.verticalUnitBottom) / rect.Height;
				float curUnitX = beginUnitX;
				float curUnitY;
				float curPixelX = this.ParentView.GetPosAtUnit(beginUnitX);
				float curPixelY = rect.Y + rect.Height * 0.5f;
				float lastPixelX = 0.0f;
				float lastPixelY = 0.0f;
				float lastSamplePixelX = 0.0f;
				float lastSamplePixelY = 0.0f;
				int nextExplicitIndex = 0;
				bool explicitSample = true;
				int skipCount = 0;
				while (curUnitX <= endUnitX)
				{
					lastPixelY = curPixelY;
					curUnitY = func(curUnitX);
					curPixelY = rect.Y + this.GetPosAtUnit(curUnitY);

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
							this.GetUnitAtPos(lastSamplePixelY - rect.Y), 
							curUnitX, 
							curUnitY, 
							x => func(x));
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

		private void model_GraphChanged(object sender, TimelineGraphRangeEventArgs e)
		{
			this.Invalidate(e.BeginTime, e.EndTime);
			this.UpdateContentWidth();
		}
		private void model_GraphCollectionChanged(object sender, EventArgs e)
		{
			this.Invalidate();
			this.UpdateContentWidth();
			this.AdjustVerticalUnits(AdjustVerticalMode.Grow);
		}

		private static float GetLinearInterpolationError(float beginX, float beginY, float endX, float endY, Func<float,float> func)
		{
			float stepSize = (endX - beginX) / 10.0f;
			float curX = beginX + stepSize;
			float error = 0.0f;
			while (curX < endX)
			{
				float alpha = (curX - beginX) / (endX - beginX);
				float interpolatedY = beginY + (endY - beginY) * alpha;
				float realY = func(curX);
				error += Math.Abs(interpolatedY - realY);
				curX += stepSize;
			}
			return error / 10.0f;
		}
	}
}
