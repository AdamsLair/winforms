using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	[TimelineModelViewAssignment(typeof(ITimelineGraphTrackModel))]
	public class TimelineViewGraphTrack : TimelineViewTrack
	{
		public enum AdjustVerticalMode
		{
			GrowAndShrink,
			Grow,
			Shrink
		}
		private struct GraphAreaInfo
		{
			public	float	BeginTime;
			public	float	EndTime;
			public	float	MinValue;
			public	float	MaxValue;
			public	float	AverageValue;
			public	float	EnvelopeVisibility;
		}
		private struct GraphValueTextInfo
		{
			public	string		Text;
			public	Rectangle	TargetRect;
			public	Rectangle	ActualRect;
			public	Color		Color;
		}


		private static Type[] availableViewGraphTypes = null;
		private const int MaxGraphValueTextWidth = 100;

		private	float					verticalUnitTop		= 1.0f;
		private	float					verticalUnitBottom	= -1.0f;
		private	List<TimelineViewGraph>	graphList			= new List<TimelineViewGraph>();
		private	QualityLevel			curveQuality		= QualityLevel.High;
		private	QualityLevel			curvePrecision		= QualityLevel.Medium;
		private	QualityLevel			envelopePrecision	= QualityLevel.Medium;

		private	TimelineViewGraph							mouseoverGraph		= null;
		private	Dictionary<TimelineViewGraph,GraphAreaInfo>	graphDisplayedInfo	= new Dictionary<TimelineViewGraph,GraphAreaInfo>();

		private List<Rectangle>		drawBufferBigRuler	= new List<Rectangle>();
		private List<Rectangle>		drawBufferMedRuler	= new List<Rectangle>();
		private List<Rectangle>		drawBufferMinRuler	= new List<Rectangle>();


		public new ITimelineGraphTrackModel Model
		{
			get { return base.Model as ITimelineGraphTrackModel; }
		}
		public IEnumerable<TimelineViewGraph> Graphs
		{
			get { return this.graphList; }
		}
		public float VerticalUnitTop
		{
			get { return this.verticalUnitTop; }
			set
			{
				if (this.verticalUnitTop != value)
				{
					this.verticalUnitTop = value;
					this.Invalidate();
				}
			}
		}
		public float VerticalUnitBottom
		{
			get { return this.verticalUnitBottom; }
			set
			{
				if (this.verticalUnitBottom != value)
				{
					this.verticalUnitBottom = value;
					this.Invalidate();
				}
			}
		}
		public QualityLevel CurveQuality
		{
			get { return this.curveQuality; }
			set
			{
				if (this.curveQuality != value)
				{
					this.curveQuality = value;
					this.Invalidate();
				}
			}
		}
		public QualityLevel CurvePrecision
		{
			get { return this.curvePrecision; }
			set
			{
				if (this.curvePrecision != value)
				{
					this.curvePrecision = value;
					this.Invalidate();
				}
			}
		}
		public QualityLevel EnvelopePrecision
		{
			get { return this.envelopePrecision; }
			set
			{
				if (this.envelopePrecision != value)
				{
					this.envelopePrecision = value;
					this.Invalidate();
				}
			}
		}
		
		
		public TimelineViewGraph GetGraphByModel(ITimelineGraphModel graphModel)
		{
			return this.graphList.FirstOrDefault(t => t.Model == graphModel);
		}
		
		public float ConvertUnitsToPixels(float units)
		{
			return units * (float)(this.Height - 1) / (this.verticalUnitTop - this.verticalUnitBottom);
		}
		public float ConvertPixelsToUnits(float pixels)
		{
			return pixels * (this.verticalUnitTop - this.verticalUnitBottom) / (float)(this.Height - 1);
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
		public void AdjustVerticalUnits(AdjustVerticalMode adjustMode, bool niceMultiple = true)
		{
			float targetTop;
			float targetBottom;

			if (this.graphList.Count == 0)
			{
				targetTop = 1.0f;
				targetBottom = -1.0f;
			}
			else
			{
				float minUnits = float.MaxValue;
				float maxUnits = float.MinValue;
				foreach (TimelineViewGraph graph in this.graphList)
				{
					ITimelineGraphModel graphModel = graph.Model;
					minUnits = Math.Min(minUnits, graphModel.GetMinValueInRange(graphModel.BeginTime, graphModel.EndTime));
					maxUnits = Math.Max(maxUnits, graphModel.GetMaxValueInRange(graphModel.BeginTime, graphModel.EndTime));
				}
				if (niceMultiple)
				{
					targetTop = TimelineView.GetNiceMultiple(maxUnits, TimelineView.NiceMultipleMode.Higher, TimelineView.NiceMultipleGranularity.High);
					targetBottom = TimelineView.GetNiceMultiple(minUnits, TimelineView.NiceMultipleMode.Lower, TimelineView.NiceMultipleGranularity.High);
				}
				else
				{
					targetTop = maxUnits;
					targetBottom = minUnits;
				}
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

			if (Math.Abs(this.verticalUnitBottom - this.verticalUnitTop) <= 0.00000001f)
			{
				this.verticalUnitBottom -= 0.5f;
				this.verticalUnitTop += 0.5f;
			}

			this.Invalidate();
		}

		protected void UpdateMouseoverState()
		{
			Rectangle trackRect = this.ParentView.GetTrackRectangle(this);
			Point cursorPos = this.ParentView.PointToClient(Cursor.Position);
			PointF unitsPerPixel = new PointF(
				this.ParentView.ConvertPixelsToUnits(1.0f), 
				this.ConvertPixelsToUnits(1.0f));
			PointF cursorUnits = new PointF(
				this.ParentView.GetUnitAtPos(cursorPos.X),
				this.GetUnitAtPos(cursorPos.Y - trackRect.Y));

			// Mind the old mouseover state
			TimelineViewGraph oldMouseoverGraph = this.mouseoverGraph;

			// Select the graph that is located nearest to the cursor
			this.mouseoverGraph = null;
			float minDistance = float.MaxValue;
			foreach (TimelineViewGraph graph in this.graphList)
			{
				if (graph.Model.BeginTime > cursorUnits.X) continue;
				if (graph.Model.EndTime < cursorUnits.X) continue;

				float maxValue = graph.Model.GetMaxValueInRange(cursorUnits.X - unitsPerPixel.X * 2.5f, cursorUnits.X + unitsPerPixel.X * 2.5f);
				float minValue = graph.Model.GetMinValueInRange(cursorUnits.X - unitsPerPixel.X * 2.5f, cursorUnits.X + unitsPerPixel.X * 2.5f);
				float midValue = (maxValue + minValue) * 0.5f;
				float valueSpan = maxValue - minValue;
				valueSpan = Math.Max(valueSpan, unitsPerPixel.Y * 10);
				minValue = midValue - valueSpan * 0.5f;
				maxValue = midValue + valueSpan * 0.5f;
				if (cursorUnits.Y < minValue || cursorUnits.Y > maxValue) continue;

				float distance = Math.Abs(cursorUnits.Y - midValue);
				if (distance < minDistance)
				{
					this.mouseoverGraph = graph;
					minDistance = distance;
				}
			}
		}
		protected void UpdateDisplayedGraphInfo()
		{
			this.graphDisplayedInfo.Clear();

			float unitPixelRadius		= this.ParentView.ConvertPixelsToUnits(1.0f);
			float unitEnvelopeRadius	= unitPixelRadius * TimelineViewGraph.EnvelopeBasePixelRadius;
			float mouseoverTime			= this.ParentView.MouseoverTime;
			float selectionBeginTime	= this.ParentView.SelectionBeginTime;
			float selectionEndTime		= this.ParentView.SelectionEndTime;

			// Update graph mouseover visualizations
			foreach (TimelineViewGraph graph in this.graphList)
			{
				GraphAreaInfo info = new GraphAreaInfo();

				// Determine selection data
				if (selectionBeginTime != selectionEndTime)
				{
					if (graph.Model.BeginTime > selectionEndTime) continue;
					if (graph.Model.EndTime < selectionBeginTime) continue;

					info.BeginTime = selectionBeginTime;
					info.EndTime = selectionEndTime;
					info.MinValue = graph.Model.GetMinValueInRange(selectionBeginTime, selectionEndTime);
					info.MaxValue = graph.Model.GetMaxValueInRange(selectionBeginTime, selectionEndTime);
					info.EnvelopeVisibility = 1.0f;
					info.AverageValue = 0.0f;
					const int SampleCount = 10;
					float sampleBegin = Math.Max(selectionBeginTime, graph.Model.BeginTime);
					float sampleEnd = Math.Min(selectionEndTime, graph.Model.EndTime);
					float timeRadius = 0.5f * (sampleEnd - sampleBegin);
					float sampleRadius = timeRadius / (float)SampleCount;
					for (int i = 0; i < SampleCount; i++)
					{
						float sampleTime = sampleBegin + ((float)i / (float)(SampleCount - 1)) * 2.0f * timeRadius;
						float localMin = graph.Model.GetMinValueInRange(sampleTime - sampleRadius, sampleTime + sampleRadius);
						float localMax = graph.Model.GetMaxValueInRange(sampleTime - sampleRadius, sampleTime + sampleRadius);
						info.AverageValue += (localMin + localMax) * 0.5f;
					}
					info.AverageValue /= (float)SampleCount;
				}
				// Determine mouseover data
				else if (this.ParentView.MouseoverContent)
				{
					if (graph.Model.BeginTime > mouseoverTime) continue;
					if (graph.Model.EndTime < mouseoverTime) continue;

					info.BeginTime = mouseoverTime;
					info.EndTime = mouseoverTime;
					info.MinValue = graph.Model.GetMinValueInRange(mouseoverTime - unitEnvelopeRadius, mouseoverTime + unitEnvelopeRadius);
					info.MaxValue = graph.Model.GetMaxValueInRange(mouseoverTime - unitEnvelopeRadius, mouseoverTime + unitEnvelopeRadius);
					info.EnvelopeVisibility = Math.Min(1.0f, 10.0f * graph.GetEnvelopeVisibility(
						graph.Model.GetMaxValueInRange(mouseoverTime - unitPixelRadius * 0.5f, mouseoverTime + unitPixelRadius * 0.5f) - 
						graph.Model.GetMinValueInRange(mouseoverTime - unitPixelRadius * 0.5f, mouseoverTime + unitPixelRadius * 0.5f)));
					if (info.EnvelopeVisibility > 0.05f)
					{
						info.AverageValue = 0.0f;
						const int SampleCount = 10;
						float sampleRadius = unitEnvelopeRadius / (float)SampleCount;
						for (int i = 0; i < SampleCount; i++)
						{
							float sampleTime = mouseoverTime - unitEnvelopeRadius + ((float)i / (float)(SampleCount - 1)) * 2.0f * unitEnvelopeRadius;
							float localMin = graph.Model.GetMinValueInRange(sampleTime - sampleRadius, sampleTime + sampleRadius);
							float localMax = graph.Model.GetMaxValueInRange(sampleTime - sampleRadius, sampleTime + sampleRadius);
							info.AverageValue += (localMin + localMax) * 0.5f;
						}
						info.AverageValue /= (float)SampleCount;
					}
					else
					{
						info.AverageValue = graph.Model.GetValueAtX(mouseoverTime);
					}
				}

				this.graphDisplayedInfo[graph] = info;
			}
		}

		protected Color GetDefaultGraphColor(int graphIndex)
		{
			const double GoldenRatio = 1.618033988749895d;
			return ExtMethodsColor.ColorFromHSV(
				(float)((graphIndex * GoldenRatio) % 1.0d), 
				0.75f, 
				(float)Math.Sqrt(1.0d - ((graphIndex * GoldenRatio) % 0.5d)));
		}
		protected override void CalculateContentWidth(out float beginTime, out float endTime)
		{
			base.CalculateContentWidth(out beginTime, out endTime);
			if (this.graphList.Count > 0)
			{
				beginTime = this.graphList.Min(g => g.Model.BeginTime);
				endTime = this.graphList.Max(g => g.Model.EndTime);
			}
			else
			{
				beginTime = 0.0f;
				endTime = 0.0f;
			}
		}

		protected override void OnModelChanged(TimelineTrackModelChangedEventArgs e)
		{
			if (e.OldModel != null)
			{
				ITimelineGraphTrackModel oldModel = e.OldModel as ITimelineGraphTrackModel;

				oldModel.GraphsAdded -= this.model_GraphsAdded;
				oldModel.GraphsRemoved -= this.model_GraphsRemoved;
				oldModel.GraphChanged -= this.model_GraphChanged;

				if (oldModel.Graphs.Any())
				{
					this.OnModelGraphsRemoved(new TimelineGraphCollectionEventArgs(oldModel.Graphs));
				}
			}
			if (e.Model != null)
			{
				ITimelineGraphTrackModel newModel = e.Model as ITimelineGraphTrackModel;

				if (newModel.Graphs.Any())
				{
					this.OnModelGraphsAdded(new TimelineGraphCollectionEventArgs(newModel.Graphs));
				}

				newModel.GraphsAdded += this.model_GraphsAdded;
				newModel.GraphsRemoved += this.model_GraphsRemoved;
				newModel.GraphChanged += this.model_GraphChanged;
			}
			base.OnModelChanged(e);
			this.AdjustVerticalUnits(AdjustVerticalMode.GrowAndShrink);
		}
		protected virtual void OnModelGraphsAdded(TimelineGraphCollectionEventArgs e)
		{
			foreach (ITimelineGraphModel graphModel in e.Graphs)
			{
				TimelineViewGraph graph = this.GetGraphByModel(graphModel);
				if (graph != null) continue;

				// Determine Type of the TimelineViewTrack matching the TimelineTrackModel
				if (availableViewGraphTypes == null)
				{
					availableViewGraphTypes = ReflectionHelper.FindConcreteTypes(typeof(TimelineViewGraph));
				}
				Type viewGraphType = null;
				foreach (Type graphType in availableViewGraphTypes)
				{
					foreach (TimelineModelViewAssignmentAttribute attrib in graphType.GetCustomAttributes(true).OfType<TimelineModelViewAssignmentAttribute>())
					{
						foreach (Type validModelType in attrib.ValidModelTypes)
						{
							if (validModelType.IsInstanceOfType(graphModel))
							{
								viewGraphType = graphType;
								break;
							}
						}
						if (viewGraphType != null) break;
					}
					if (viewGraphType != null) break;
				}
				if (viewGraphType == null) continue;

				// Create TimelineViewTrack accordingly
				graph = viewGraphType.CreateInstanceOf() as TimelineViewGraph;
				graph.Model = graphModel;
				graph.BaseColor = this.GetDefaultGraphColor(this.graphList.Count);

				this.graphList.Add(graph);
				graph.ParentTrack = this;
			}

			this.Invalidate();
			this.UpdateContentWidth();
			this.AdjustVerticalUnits(AdjustVerticalMode.Grow);
		}
		protected virtual void OnModelGraphsRemoved(TimelineGraphCollectionEventArgs e)
		{
			foreach (ITimelineGraphModel graphModel in e.Graphs)
			{
				TimelineViewGraph graph = this.GetGraphByModel(graphModel);
				if (graph == null) continue;

				graph.ParentTrack = null;
				graph.Model = null;
				this.graphList.Remove(graph);
			}

			this.Invalidate();
			this.UpdateContentWidth();
			this.AdjustVerticalUnits(AdjustVerticalMode.Shrink);
		}
		protected override void OnViewportChanged()
		{
			base.OnViewportChanged();
			foreach (TimelineViewGraph graph in this.graphList)
			{
				graph.OnViewportChanged();
			}
		}

		protected internal override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			this.UpdateMouseoverState();
		}
		protected internal override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			this.UpdateMouseoverState();
		}
		protected internal override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			this.UpdateMouseoverState();
		}
		protected internal override void OnCursorMove(TimelineViewCursorEventArgs e)
		{
			base.OnCursorMove(e);
			this.UpdateDisplayedGraphInfo();
			if (e.CursorUnitSpeed != 0.0f)
			{
				float unitsPerPixel = this.ParentView.ConvertPixelsToUnits(1.0f);
				this.Invalidate(e.CursorUnits - unitsPerPixel * 6, e.CursorUnits + unitsPerPixel * MaxGraphValueTextWidth);
				this.Invalidate(e.LastCursorUnits - unitsPerPixel * 6, e.LastCursorUnits + unitsPerPixel * MaxGraphValueTextWidth);
			}
		}
		protected internal override void OnCursorEnter(TimelineViewCursorEventArgs e)
		{
			base.OnCursorLeave(e);
			this.UpdateDisplayedGraphInfo();
			float unitsPerPixel = this.ParentView.ConvertPixelsToUnits(1.0f);
			this.Invalidate(e.CursorUnits - unitsPerPixel * 6, e.CursorUnits + unitsPerPixel * MaxGraphValueTextWidth);
		}
		protected internal override void OnCursorLeave(TimelineViewCursorEventArgs e)
		{
			base.OnCursorLeave(e);
			this.UpdateDisplayedGraphInfo();
			float unitsPerPixel = this.ParentView.ConvertPixelsToUnits(1.0f);
			this.Invalidate(e.LastCursorUnits - unitsPerPixel * 6, e.LastCursorUnits + unitsPerPixel * MaxGraphValueTextWidth);
		}
		protected internal override void OnTimeSelectionChanged(TimelineViewSelectionEventArgs e)
		{
			base.OnTimeSelectionChanged(e);
			this.UpdateDisplayedGraphInfo();
			float unitsPerPixel = this.ParentView.ConvertPixelsToUnits(1.0f);
			if (e.IsEmpty)
			{
				this.Invalidate(
					e.LastBeginTime - unitsPerPixel * MaxGraphValueTextWidth, 
					e.LastEndTime + unitsPerPixel * MaxGraphValueTextWidth);
			}
			else if (e.WasEmpty)
			{
				this.Invalidate(
					e.BeginTime - unitsPerPixel * MaxGraphValueTextWidth, 
					e.EndTime + unitsPerPixel * MaxGraphValueTextWidth);
			}
			else
			{
				this.Invalidate(
					Math.Min(e.BeginTime, e.LastBeginTime) - unitsPerPixel * MaxGraphValueTextWidth, 
					Math.Max(e.EndTime, e.LastEndTime) + unitsPerPixel * MaxGraphValueTextWidth);
			}
		}
		
		protected internal override void OnPaint(TimelineViewTrackPaintEventArgs e)
		{
			base.OnPaint(e);

			Rectangle rect = e.TargetRect;
			
			// Draw extended ruler markings in the background
			{
				Brush bigLineBrush = new SolidBrush(e.Renderer.ColorRulerMarkMajor.ScaleAlpha(0.25f));
				Brush medLineBrush = new SolidBrush(e.Renderer.ColorRulerMarkRegular.ScaleAlpha(0.25f));
				Brush minLineBrush = new SolidBrush(e.Renderer.ColorRulerMarkMinor.ScaleAlpha(0.25f));
				this.drawBufferBigRuler.Clear();
				this.drawBufferMedRuler.Clear();
				this.drawBufferMinRuler.Clear();

				// Vertical ruler marks
				foreach (TimelineViewRulerMark mark in this.GetVisibleRulerMarks())
				{
					if (mark.PixelValue < e.Graphics.ClipBounds.Top) continue;
					if (mark.PixelValue > e.Graphics.ClipBounds.Bottom) break;

					Rectangle lineRect = new Rectangle((int)rect.X, (int)mark.PixelValue, (int)rect.Width, 1);
					
					switch (mark.Weight)
					{
						case TimelineViewRulerMarkWeight.Major:
							this.drawBufferBigRuler.Add(lineRect);
							break;
						default:
						case TimelineViewRulerMarkWeight.Regular:
							this.drawBufferMedRuler.Add(lineRect);
							break;
						case TimelineViewRulerMarkWeight.Minor:
							this.drawBufferMinRuler.Add(lineRect);
							break;
					}
				}

				if (this.drawBufferBigRuler.Count > 0) e.Graphics.FillRectangles(bigLineBrush, this.drawBufferBigRuler.ToArray());
				if (this.drawBufferMedRuler.Count > 0) e.Graphics.FillRectangles(medLineBrush, this.drawBufferMedRuler.ToArray());
				if (this.drawBufferMinRuler.Count > 0) e.Graphics.FillRectangles(minLineBrush, this.drawBufferMinRuler.ToArray());
			}

			// Draw the graphs
			{
				float beginUnitX = Math.Max(this.ParentView.UnitOriginOffset - this.ParentView.UnitScroll, this.ContentBeginTime);
				float endUnitX = Math.Min(this.ParentView.UnitOriginOffset - this.ParentView.UnitScroll + this.ParentView.VisibleUnitWidth, this.ContentEndTime);

				beginUnitX = Math.Max(Math.Max(beginUnitX, this.ParentView.GetUnitAtPos(e.Graphics.ClipBounds.Left - 1)), e.BeginTime);
				endUnitX = Math.Min(Math.Min(endUnitX, this.ParentView.GetUnitAtPos(e.Graphics.ClipBounds.Right)), e.EndTime);

				if (beginUnitX <= endUnitX)
				{
					foreach (TimelineViewGraph graph in this.graphList)
					{
						graph.OnPaint(new TimelineViewTrackPaintEventArgs(this, e.Graphics, e.QualityHint, rect, beginUnitX, endUnitX));
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
			this.DrawLegend(e.Graphics, e.Renderer, e.TargetRect);
		}
		protected internal override void OnPaintOverlay(TimelineViewTrackPaintEventArgs e)
		{
			base.OnPaintOverlay(e);
			
			// Determine overlay data
			float unitEnvelopeRadius	= this.ParentView.ConvertPixelsToUnits(TimelineViewGraph.EnvelopeBasePixelRadius);
			float mouseoverTime			= this.ParentView.MouseoverTime;
			float selectionBeginTime	= this.ParentView.SelectionBeginTime;
			float selectionEndTime		= this.ParentView.SelectionEndTime;
			float mouseoverPixels		= this.ParentView.GetPosAtUnit(mouseoverTime);
			float selectionBeginPixels	= this.ParentView.GetPosAtUnit(selectionBeginTime);
			float selectionEndPixels	= this.ParentView.GetPosAtUnit(selectionEndTime);

			float visibleTimeSpan	= this.ParentView.VisibleUnitWidth;
			float visibleValueSpan	= Math.Abs(this.verticalUnitTop - this.verticalUnitBottom);
			int timeDecimals		= Math.Max(0, -(int)Math.Log10(visibleTimeSpan) + 2);
			int valueDecimals		= Math.Max(0, -(int)Math.Log10(visibleValueSpan) + 2);

			Font textFont = e.Renderer.FontSmall;
			StringFormat textFormat = new StringFormat();
			textFormat.FormatFlags |= StringFormatFlags.NoWrap;
			textFormat.Trimming = StringTrimming.EllipsisCharacter;

			// Display selection data
			if (this.ParentView.SelectionBeginTime != this.ParentView.SelectionEndTime)
			{
				// Draw begin and end time values
				{
					string timeText = string.Format(
						System.Globalization.CultureInfo.InvariantCulture, 
						"{0:F" + timeDecimals + "}", 
						selectionEndTime);
					e.Graphics.DrawString(
						timeText,
						textFont, 
						new SolidBrush(e.Renderer.ColorText), 
						(int)selectionEndPixels + 2, 
						e.TargetRect.Y + 1, 
						textFormat);
				}
				{
					string timeText = string.Format(
						System.Globalization.CultureInfo.InvariantCulture, 
						"{0:F" + timeDecimals + "}", 
						selectionBeginTime);
					SizeF textSize = e.Graphics.MeasureString(timeText, textFont);
					e.Graphics.DrawString(
						timeText,
						textFont, 
						new SolidBrush(e.Renderer.ColorText), 
						(int)selectionBeginPixels - 2 - textSize.Width, 
						e.TargetRect.Y + 1, 
						textFormat);
				}

				// Draw display value overlay
				foreach (TimelineViewGraph graph in this.graphList)
				{
					GraphAreaInfo info;
					if (!this.graphDisplayedInfo.TryGetValue(graph, out info)) continue;
					
					// Draw min, max and average values
					float averagePixels = e.TargetRect.Y + this.GetPosAtUnit(info.AverageValue);
					float minPixels = Math.Min(e.TargetRect.Y + this.GetPosAtUnit(info.MinValue), e.TargetRect.Bottom - 2);
					float maxPixels = Math.Max(e.TargetRect.Y + this.GetPosAtUnit(info.MaxValue), e.TargetRect.Top + 1);

					Color graphInfoColor = graph.BaseColor.ScaleBrightness(0.75f);
					{
						SolidBrush brush = new SolidBrush(graphInfoColor);
						e.Graphics.FillRectangle(new SolidBrush(graphInfoColor.ScaleAlpha(0.3f)), selectionBeginPixels, Math.Min(minPixels, maxPixels), selectionEndPixels - selectionBeginPixels, Math.Abs(maxPixels - minPixels));
						e.Graphics.FillRectangle(brush, selectionBeginPixels, minPixels, selectionEndPixels - selectionBeginPixels, 1);
						e.Graphics.FillRectangle(brush, selectionBeginPixels, maxPixels, selectionEndPixels - selectionBeginPixels, 1);
					}

					// Draw min and max texts
					{
						SolidBrush brush = new SolidBrush(graphInfoColor);
						string text = string.Format(
							System.Globalization.CultureInfo.InvariantCulture, 
							"{0:F" + valueDecimals + "}", 
							info.MinValue);
						SizeF textSize = e.Graphics.MeasureString(text, textFont);
						PointF textPos;
						textPos = new PointF((selectionBeginPixels + selectionEndPixels) * 0.5f - textSize.Width * 0.5f, minPixels + 2);
						if (textSize.Width > (selectionEndPixels - selectionBeginPixels) - 4)
							textPos.X = selectionEndPixels + 2;
						if (textSize.Height * 2 <= Math.Abs(maxPixels - minPixels) - 4)
							textPos.Y = minPixels - 2 - textSize.Height;
						e.Graphics.DrawString(text, textFont, brush, textPos.X, textPos.Y, textFormat);
					}
					{
						SolidBrush brush = new SolidBrush(graphInfoColor);
						string text = string.Format(
							System.Globalization.CultureInfo.InvariantCulture, 
							"{0:F" + valueDecimals + "}", 
							info.MaxValue);
						SizeF textSize = e.Graphics.MeasureString(text, textFont);
						PointF textPos;
						textPos = new PointF((selectionBeginPixels + selectionEndPixels) * 0.5f - textSize.Width * 0.5f, maxPixels - 2 - textSize.Height);
						if (textSize.Width > (selectionEndPixels - selectionBeginPixels) - 4)
							textPos.X = selectionEndPixels + 2;
						if (textSize.Height * 2 <= Math.Abs(maxPixels - minPixels) - 4)
							textPos.Y = maxPixels + 2;
						e.Graphics.DrawString(text, textFont, brush, textPos.X, textPos.Y, textFormat);
					}
				}
			}
			// Display mouseover data / effects
			else if (this.ParentView.MouseoverContent && this.ParentView.ActiveMouseAction == TimelineView.MouseAction.None)
			{
				// Accumulate graph value text information
				Rectangle totalTextRect = Rectangle.Empty;
				List<GraphValueTextInfo> textInfoList = new List<GraphValueTextInfo>();
				{
					int textYAdv = 0;

					// Time text
					{
						GraphValueTextInfo textInfo;
						textInfo.Text = string.Format(
							System.Globalization.CultureInfo.InvariantCulture, 
							"{0:F" + timeDecimals + "}", 
							mouseoverTime);
						textInfo.TargetRect = new Rectangle((int)mouseoverPixels + 2, e.TargetRect.Y + textYAdv + 1, MaxGraphValueTextWidth - 2, e.TargetRect.Height - textYAdv);
						SizeF textSize = e.Graphics.MeasureString(textInfo.Text, textFont, textInfo.TargetRect.Size, textFormat);
						textInfo.ActualRect = new Rectangle(textInfo.TargetRect.X, textInfo.TargetRect.Y, (int)textSize.Width, (int)textSize.Height);
						textInfo.Color = e.Renderer.ColorText;

						if (totalTextRect.IsEmpty)
						{
							totalTextRect = textInfo.ActualRect;
						}
						else
						{
							totalTextRect.X = Math.Min(totalTextRect.X, textInfo.ActualRect.X);
							totalTextRect.Y = Math.Min(totalTextRect.Y, textInfo.ActualRect.Y);
							totalTextRect.Width = Math.Max(totalTextRect.Width, textInfo.ActualRect.Right - totalTextRect.Left);
							totalTextRect.Height = Math.Max(totalTextRect.Height, textInfo.ActualRect.Bottom - totalTextRect.Top);
						}
						textInfoList.Add(textInfo);
						textYAdv += (int)textSize.Height;
					}

					// Graph texts
					foreach (TimelineViewGraph graph in this.graphList)
					{
						GraphAreaInfo info;
						if (!this.graphDisplayedInfo.TryGetValue(graph, out info)) continue;

						GraphValueTextInfo textInfo;
						if (info.EnvelopeVisibility < 0.25f)
						{
							textInfo.Text = string.Format(
								System.Globalization.CultureInfo.InvariantCulture, 
								"{0:F" + valueDecimals + "}", 
								Math.Round(info.AverageValue, 2));
						}
						else
						{
							textInfo.Text = string.Format(
								System.Globalization.CultureInfo.InvariantCulture, 
								"[{0:F" + valueDecimals + "}, {1:F" + valueDecimals + "}]", Math.Round(info.MinValue, 2), 
								Math.Round(info.MaxValue, 2));
						}
						textInfo.TargetRect = new Rectangle((int)mouseoverPixels + 2, e.TargetRect.Y + textYAdv + 1, MaxGraphValueTextWidth - 2, e.TargetRect.Height - textYAdv);
						SizeF textSize = e.Graphics.MeasureString(textInfo.Text, textFont, textInfo.TargetRect.Size, textFormat);
						textInfo.ActualRect = new Rectangle(textInfo.TargetRect.X, textInfo.TargetRect.Y, (int)textSize.Width, (int)textSize.Height);
						textInfo.Color = graph.BaseColor.ScaleBrightness(0.75f);

						if (totalTextRect.IsEmpty)
						{
							totalTextRect = textInfo.ActualRect;
						}
						else
						{
							totalTextRect.X = Math.Min(totalTextRect.X, textInfo.ActualRect.X);
							totalTextRect.Y = Math.Min(totalTextRect.Y, textInfo.ActualRect.Y);
							totalTextRect.Width = Math.Max(totalTextRect.Width, textInfo.ActualRect.Right - totalTextRect.Left);
							totalTextRect.Height = Math.Max(totalTextRect.Height, textInfo.ActualRect.Bottom - totalTextRect.Top);
						}

						textInfoList.Add(textInfo);
						textYAdv += (int)textSize.Height;
					}
				}

				// Draw the texts background rect
				if (!totalTextRect.IsEmpty)
				{
					e.Graphics.FillRectangle(
						new SolidBrush(e.Renderer.ColorLightBackground.ScaleAlpha(0.5f)), 
						totalTextRect.X,
						totalTextRect.Y,
						totalTextRect.Width + 2,
						totalTextRect.Height + 2);
				}

				// Draw graph mouseover visualizations
				foreach (TimelineViewGraph graph in this.graphList)
				{
					GraphAreaInfo info;
					if (!this.graphDisplayedInfo.TryGetValue(graph, out info)) continue;

					// Determine mouseover data
					float averagePixels = e.TargetRect.Y + this.GetPosAtUnit(info.AverageValue);
					float minEnvelopePixels = Math.Min(e.TargetRect.Y + this.GetPosAtUnit(info.MinValue), e.TargetRect.Bottom - 2);
					float maxEnvelopePixels = Math.Max(e.TargetRect.Y + this.GetPosAtUnit(info.MaxValue), e.TargetRect.Top + 1);
					
					Color valueBaseColor = graph.BaseColor.ScaleBrightness(0.75f);

					// Draw value range
					if (info.EnvelopeVisibility > 0.05f)
					{
						SolidBrush brush = new SolidBrush(valueBaseColor.ScaleAlpha(info.EnvelopeVisibility));
						e.Graphics.FillRectangle(brush, mouseoverPixels - 3, minEnvelopePixels, 7, 1);
						e.Graphics.FillRectangle(brush, mouseoverPixels - 3, maxEnvelopePixels, 7, 1);
						e.Graphics.FillRectangle(brush, mouseoverPixels, maxEnvelopePixels, 1, minEnvelopePixels - maxEnvelopePixels);
					}

					// Draw average / exact value knob
					if (info.EnvelopeVisibility < 0.95f)
					{
						e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
						e.Graphics.FillEllipse(
							new SolidBrush(valueBaseColor.ScaleAlpha(1.0f - info.EnvelopeVisibility)), 
							mouseoverPixels - 2.5f, 
							averagePixels - 2.5f, 
							5, 
							5);
						e.Graphics.SmoothingMode = SmoothingMode.Default;
					}
				}

				// Draw value information texts
				foreach (GraphValueTextInfo textInfo in textInfoList)
				{
					e.Graphics.DrawString(textInfo.Text, textFont, new SolidBrush(textInfo.Color), textInfo.TargetRect, textFormat);
				}
			}
		}
		protected void DrawRuler(Graphics g, TimelineViewControlRenderer r, Rectangle rect, bool left)
		{
			float visibleValueSpan = Math.Abs(this.verticalUnitTop - this.verticalUnitBottom);
			int valueDecimals = Math.Max(0, -(int)Math.Log10(visibleValueSpan) + 2);

			string verticalTopText = string.Format(
				System.Globalization.CultureInfo.InvariantCulture,
				"{0:F" + valueDecimals + "}", 
				this.verticalUnitTop);
			string verticalBottomText = string.Format(
				System.Globalization.CultureInfo.InvariantCulture,
				"{0:F" + valueDecimals + "}", 
				this.verticalUnitBottom);
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
				g.DrawString(verticalTopText, r.FontSmall, new SolidBrush(r.ColorText), left ? rectUnitMarkings.Right - textSize.Width : rectUnitMarkings.Left, rectUnitMarkings.Top);
				textSize = g.MeasureString(verticalBottomText, r.FontSmall);
				g.DrawString(verticalBottomText, r.FontSmall, new SolidBrush(r.ColorText), left ? rectUnitMarkings.Right - textSize.Width : rectUnitMarkings.Left, rectUnitMarkings.Bottom - textSize.Height - 1);

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
							string text = string.Format(
								System.Globalization.CultureInfo.InvariantCulture,
								"{0:F" + valueDecimals + "}", 
								mark.UnitValue);
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
		protected void DrawLegend(Graphics g, TimelineViewControlRenderer r, Rectangle rect)
		{
			// Draw background
			Rectangle borderRect;
			if (this.ParentView.BorderStyle != System.Windows.Forms.BorderStyle.None)
			{
				borderRect = new Rectangle(
					rect.X,
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
		}

		private void model_GraphChanged(object sender, TimelineGraphRangeEventArgs e)
		{
			this.Invalidate(e.BeginTime, e.EndTime);
			this.UpdateContentWidth();
		}
		private void model_GraphsAdded(object sender, TimelineGraphCollectionEventArgs e)
		{
			this.OnModelGraphsAdded(e);
		}
		private void model_GraphsRemoved(object sender, TimelineGraphCollectionEventArgs e)
		{
			this.OnModelGraphsRemoved(e);
		}
	}
}
