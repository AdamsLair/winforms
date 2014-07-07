using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;

using AdamsLair.WinForms.Drawing;
using AdamsLair.WinForms.NativeWinAPI;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineView : Panel
	{
		private struct SubAreaInfo
		{
			public bool Active;
			public int DesiredSize;

			public int Size
			{
				get { return this.Active ? this.DesiredSize : 0; }
				set { this.DesiredSize = value; this.Active = true; }
			}

			public SubAreaInfo(int size)
			{
				this.Active = size > 0;
				this.DesiredSize = size;
			}
		}
		public enum NiceMultipleMode
		{
			Nearest,
			Higher,
			Lower
		}
		public enum NiceMultipleGranularity
		{
			High,
			Medium,
			Low
		}
		public enum MouseAction
		{
			None,
			Scroll,
			Select
		}


		private const float DefaultPixelsPerUnit = 5.0f;
		private static Type[] availableViewTrackTypes = null;

		private	ITimelineModel				model				= new TimelineModel();
		private	TimelineViewControlRenderer	renderer			= new TimelineViewControlRenderer();
		private	List<TimelineViewTrack>		trackList			= new List<TimelineViewTrack>();
		private	int							defaultTrackHeight	= 150;
		private	float						unitOffset			= 0.0f;
		private	float						unitZoom			= 1.0f;
		private	bool						fitZoom				= true;
		private	int							trackSpacing		= -1;
		private SubAreaInfo					areaTopRuler		= new SubAreaInfo(30);
		private SubAreaInfo					areaBottomRuler		= new SubAreaInfo(30);
		private SubAreaInfo					areaLeftSidebar		= new SubAreaInfo(50);
		private SubAreaInfo					areaRightSidebar	= new SubAreaInfo(100);
		private	bool						adaptiveQuality		= true;

		private	float				selectionTimeA	= 0.0f;
		private	float				selectionTimeB	= 0.0f;

		private	bool				mouseoverContent	= false;
		private	float				mouseoverTime		= 0.0f;
		private	TimelineViewTrack	mouseoverTrack		= null;
		private	MouseAction			mouseAction			= MouseAction.None;
		private	Point				mouseActionOrigin	= Point.Empty;
		private	Timer				mouseActionTimer	= null;
		private	PointF				mouseScrollAcc		= PointF.Empty;

		private	bool		paintLowQuality		= false;
		private	TimeSpan	lastPaintHqTime		= TimeSpan.Zero;
		private	Timer		paintHqTimer		= null;

		private List<Rectangle>	drawBufferBigRuler	= new List<Rectangle>();
		private List<Rectangle>	drawBufferMedRuler	= new List<Rectangle>();
		private List<Rectangle>	drawBufferMinRuler	= new List<Rectangle>();

		private	Rectangle	rectTopRuler;
		private	Rectangle	rectBottomRuler;
		private	Rectangle	rectLeftSidebar;
		private	Rectangle	rectRightSidebar;
		private	Rectangle	rectContentArea;

		public event EventHandler UnitZoomChanged = null;
		public event EventHandler UnitChanged = null;
		public event EventHandler ViewScrolled = null;

		
		public ITimelineModel Model
		{
			get { return this.model; }
			set
			{
				if (this.model != value)
				{
					this.model.UnitChanged		-= this.model_UnitChanged;
					this.model.TracksAdded		-= this.model_TracksAdded;
					this.model.TracksRemoved	-= this.model_TracksRemoved;

					if (this.model.Tracks.Any())
					{
						this.OnModelTracksRemoved(new TimelineTrackModelCollectionEventArgs(this.model.Tracks));
					}

					this.model = value ?? new TimelineModel();
					
					this.OnUnitChanged(EventArgs.Empty);
					if (this.model.Tracks.Any())
					{
						this.OnModelTracksAdded(new TimelineTrackModelCollectionEventArgs(this.model.Tracks));
					}

					this.model.UnitChanged		+= this.model_UnitChanged;
					this.model.TracksAdded		+= this.model_TracksAdded;
					this.model.TracksRemoved	+= this.model_TracksRemoved;
				}
			}
		}
		public TimelineViewControlRenderer Renderer
		{
			get { return this.renderer; }
		}
		public IEnumerable<TimelineViewTrack> Tracks
		{
			get { return this.trackList; }
		}
		[DefaultValue(0.0f)]
		public float UnitOriginOffset
		{
			get { return this.unitOffset; }
			set { this.unitOffset = value; }
		}
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float UnitScroll
		{
			get { return this.ConvertPixelsToUnits(this.AutoScrollPosition.X); }
			set
			{
				this.AutoScrollPosition = new Point(-(int)this.ConvertUnitsToPixels(value), -this.AutoScrollPosition.Y);
				this.OnViewScrolled(EventArgs.Empty);
			}
		}
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float VisibleUnitWidth
		{
			get { return this.ConvertPixelsToUnits(this.rectContentArea.Width); }
		}
		[DefaultValue(1.0f)]
		public float UnitZoom
		{
			get { return this.unitZoom; }
			set
			{
				this.fitZoom = false;
				this.SetUnitZoomInternal(value);
			}
		}
		[DefaultValue(150)]
		public int DefaultTrackHeight
		{
			get { return this.defaultTrackHeight; }
			set { this.defaultTrackHeight = value; }
		}
		[DefaultValue(-1)]
		public int TrackSpacing
		{
			get { return this.trackSpacing; }
			set { this.trackSpacing = value; }
		}
		[DefaultValue(true)]
		public bool HasTopRuler
		{
			get { return this.areaTopRuler.Active; }
			set { this.areaTopRuler.Active = value; this.UpdateGeometry(); }
		}
		[DefaultValue(true)]
		public bool HasBottomRuler
		{
			get { return this.areaBottomRuler.Active; }
			set { this.areaBottomRuler.Active = value; this.UpdateGeometry(); }
		}
		[DefaultValue(true)]
		public bool HasLeftSidebar
		{
			get { return this.areaLeftSidebar.Active; }
			set { this.areaLeftSidebar.Active = value; this.UpdateGeometry(); }
		}
		[DefaultValue(true)]
		public bool HasRightSidebar
		{
			get { return this.areaRightSidebar.Active; }
			set { this.areaRightSidebar.Active = value; this.UpdateGeometry(); }
		}
		[DefaultValue(30)]
		public int TopRulerSize
		{
			get { return this.areaTopRuler.DesiredSize; }
			set { this.areaTopRuler.DesiredSize = value; this.UpdateGeometry(); }
		}
		[DefaultValue(30)]
		public int BottomRulerSize
		{
			get { return this.areaBottomRuler.DesiredSize; }
			set { this.areaBottomRuler.DesiredSize = value; this.UpdateGeometry(); }
		}
		[DefaultValue(30)]
		public int LeftSidebarSize
		{
			get { return this.areaLeftSidebar.DesiredSize; }
			set { this.areaLeftSidebar.DesiredSize = value; this.UpdateGeometry(); }
		}
		[DefaultValue(30)]
		public int RightSidebarSize
		{
			get { return this.areaRightSidebar.DesiredSize; }
			set { this.areaRightSidebar.DesiredSize = value; this.UpdateGeometry(); }
		}
		/// <summary>
		/// [GET / SET] If true, the TimelineView will automatically adjust its drawing quality while scrolling and resizing in order to achieve maximum performance.
		/// </summary>
		public bool AdaptiveDrawingQuality
		{
			get { return this.adaptiveQuality; }
			set { this.adaptiveQuality = value; }
		}
		protected override CreateParams CreateParams
		{
			get
			{
				// We're dealing with both static and dynamic areas, so we'll need composited repaint operations
				CreateParams p = base.CreateParams;
				p.ExStyle |= (int)ExtendedWindowStyles.Composited;
				return p;
			}
		}
		public bool MouseoverContent
		{
			get { return this.mouseoverContent; }
		}
		public float MouseoverTime
		{
			get { return this.mouseoverTime; }
		}
		public TimelineViewTrack MouseoverTrack
		{
			get { return this.mouseoverTrack; }
		}
		public MouseAction ActiveMouseAction
		{
			get { return this.mouseAction; }
		}
		public float SelectionBeginTime
		{
			get { return Math.Min(this.selectionTimeA, this.selectionTimeB); }
			set
			{
				float lastBegin = this.SelectionBeginTime;
				float lastEnd = this.SelectionEndTime;

				if (this.selectionTimeA < this.selectionTimeB)
					this.selectionTimeA = value;
				else
					this.selectionTimeB = value;

				this.RaiseSelectionChanged(lastBegin, lastEnd);
			}
		}
		public float SelectionEndTime
		{
			get { return Math.Max(this.selectionTimeA, this.selectionTimeB); }
			set
			{
				float lastBegin = this.SelectionBeginTime;
				float lastEnd = this.SelectionEndTime;

				if (this.selectionTimeA >= this.selectionTimeB)
					this.selectionTimeA = value;
				else
					this.selectionTimeB = value;

				this.RaiseSelectionChanged(lastBegin, lastEnd);
			}
		}


		public TimelineView()
		{
			this.AutoScroll = true;
			this.TabStop = true;

			this.SetStyle(ControlStyles.ResizeRedraw, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.Opaque, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.SetStyle(ControlStyles.Selectable, true);

			this.paintHqTimer = new Timer();
			this.paintHqTimer.Enabled = false;
			this.paintHqTimer.Interval = 50;
			this.paintHqTimer.Tick += this.paintHqTimer_Tick;

			this.mouseActionTimer = new Timer();
			this.mouseActionTimer.Enabled = false;
			this.mouseActionTimer.Interval = 16;
			this.mouseActionTimer.Tick += this.mouseActionTimer_Tick;
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				if (this.paintHqTimer != null)
				{
					this.paintHqTimer.Dispose();
					this.paintHqTimer = null;
				}
				if (this.trackList != null)
				{
					foreach (TimelineViewTrack track in this.trackList)
					{
						track.ParentView = null;
					}
					this.trackList.Clear();
				}
			}
		}
		
		public TimelineViewTrack GetTrackByModel(ITimelineTrackModel trackModel)
		{
			return this.trackList.FirstOrDefault(t => t.Model == trackModel);
		}

		public float ConvertUnitsToPixels(float units)
		{
			return units * (this.model.UnitBaseScale * DefaultPixelsPerUnit * this.unitZoom);
		}
		public float ConvertPixelsToUnits(float pixels)
		{
			return pixels / (this.model.UnitBaseScale * DefaultPixelsPerUnit * this.unitZoom);
		}
		public float GetUnitAtPos(float x)
		{
			return this.unitOffset - this.UnitScroll + this.ConvertPixelsToUnits(x - this.rectContentArea.X);
		}
		public float GetPosAtUnit(float unit)
		{
			return this.rectContentArea.X + this.ConvertUnitsToPixels(unit - this.unitOffset + this.UnitScroll);
		}
		public Rectangle GetTrackRectangle(TimelineViewTrack track, bool scrolled = true)
		{
			int baseY = 0;
			foreach (TimelineViewTrack t in this.trackList)
			{
				if (t == track) break;
				baseY += t.Height + this.trackSpacing;
			}

			Rectangle rect = new Rectangle(
				this.rectLeftSidebar.Left,
				this.rectLeftSidebar.Y + baseY,
				this.rectRightSidebar.Right - this.rectLeftSidebar.Left,
				track.Height);

			if (scrolled) rect.Y += this.AutoScrollPosition.Y;

			return rect;
		}
		public TimelineViewTrack GetTrackAtPos(int x, int y, bool scrolled = true, bool allowNearest = false)
		{
			y -= this.rectContentArea.Y;

			if (allowNearest)
			{
				if (x < 0) x = 0;
				if (y < 0) y = 0;
				if (x >= this.ClientSize.Width) x = this.ClientSize.Width - 1;
				if (y >= this.rectContentArea.Height) y = this.rectContentArea.Height - 1;
			}
			else
			{
				if (x < 0) return null;
				if (y < 0) return null;
				if (x >= this.ClientSize.Width) return null;
				if (y >= this.rectContentArea.Height) return null;
			}
			
			if (scrolled) y -= this.AutoScrollPosition.Y;

			foreach (TimelineViewTrack t in this.trackList)
			{
				y -= t.Height + this.trackSpacing;
				if (y < 0) return t;
			}

			return null;
		}
		public IEnumerable<TimelineViewRulerMark> GetVisibleRulerMarks(int clipLeftPixelCoord, int clipRightPixelCoord)
		{
			float rulerStep = GetNiceMultiple(this.ConvertPixelsToUnits(100.0f)) / 10.0f;
			float unitScroll = this.UnitScroll;
			float beginTime = this.GetUnitAtPos(Math.Max(this.rectTopRuler.Left, clipLeftPixelCoord)) + this.UnitScroll;
			float endTime = this.GetUnitAtPos(Math.Min(this.rectTopRuler.Right, clipRightPixelCoord)) + this.UnitScroll;

			int lineIndex = 0;
			foreach (float markTime in EnumerateRulerMarks(rulerStep, unitScroll, beginTime, endTime, 10))
			{
				float markX = this.GetPosAtUnit(markTime);

				TimelineViewRulerMarkWeight weight;
				if ((lineIndex % 10) == 0)
					weight = TimelineViewRulerMarkWeight.Major;
				else if ((lineIndex % 5) == 0)
					weight = TimelineViewRulerMarkWeight.Regular;
				else
					weight = TimelineViewRulerMarkWeight.Minor;

				yield return new TimelineViewRulerMark(markTime, markX, weight);

				lineIndex++;
			}

			yield break;
		}

		public void AdjustZoomLevel(float amount)
		{
			// Prepare position adjustment values
			Point targetPos = this.PointToClient(Cursor.Position);
			if (!this.ClientRectangle.Contains(targetPos)) targetPos = new Point(this.ClientRectangle.Width / 2, this.ClientRectangle.Height / 2);
			float oldCursorUnits = this.GetUnitAtPos(targetPos.X);

			// Apply Zoom
			this.fitZoom = false;
			this.SetUnitZoomInternal(this.unitZoom * (float)Math.Pow(1.5f, amount));

			// If the cursor is within the Timeline area, zoom to the cursor. Otherwise, zoom to the center.
			float newCursorUnits = this.GetUnitAtPos(targetPos.X);
			this.UnitScroll -= oldCursorUnits - newCursorUnits;
		}
		public void ZoomToFit()
		{
			if (!this.model.Tracks.Any()) return;

			float minTime = float.MaxValue;
			float maxTime = float.MinValue;

			foreach (ITimelineTrackModel track in this.model.Tracks)
			{
				minTime = Math.Min(minTime, track.BeginTime);
				maxTime = Math.Max(maxTime, track.EndTime);
			}

			float duration = maxTime - minTime;
			float availableSpace = this.rectContentArea.Width - 1;
			float targetZoom = availableSpace / (duration * DefaultPixelsPerUnit * this.model.UnitBaseScale);

			this.fitZoom = true;
			this.SetUnitZoomInternal(targetZoom);
			this.UnitScroll = minTime;
		}

		protected void InvalidateLowQuality()
		{
			this.Invalidate();
			if (this.adaptiveQuality)
			{
				this.paintLowQuality = true;
				this.paintHqTimer.Stop();
				this.paintHqTimer.Start();
			}
		}
		protected void InvalidateContent(float fromUnits, float toUnits)
		{
			if (fromUnits > toUnits)
			{
				float temp = fromUnits;
				fromUnits = toUnits;
				toUnits = temp;
			}

			Rectangle invalidateRect = new Rectangle(this.rectContentArea.X, 0, this.rectContentArea.Width, this.ClientRectangle.Height);

			float fromPixels = Math.Max(this.GetPosAtUnit(fromUnits), invalidateRect.Left) - 1;
			float toPixels = Math.Min(this.GetPosAtUnit(toUnits), invalidateRect.Right) + 2;

			Rectangle targetRect = new Rectangle(
				(int)fromPixels,
				invalidateRect.Y,
				(int)(toPixels - fromPixels),
				invalidateRect.Height);
			invalidateRect.Intersect(targetRect);
			invalidateRect.Intersect(this.ClientRectangle);
			if (invalidateRect.IsEmpty) return;

			this.Invalidate(invalidateRect);
		}

		private void SetUnitZoomInternal(float newZoom)
		{
			newZoom = Math.Min(Math.Max(newZoom, 0.0001f), 10000.0f);
			if (this.unitZoom == newZoom) return;

			this.unitZoom = newZoom;
			this.UpdateContentWidth();

			if (this.UnitZoomChanged != null)
				this.UnitZoomChanged(this, EventArgs.Empty);

			this.InvalidateLowQuality();
		}
		private void UpdateGeometry()
		{
			Rectangle lastRectTopRuler		= this.rectTopRuler;
			Rectangle lastRectBottomRuler	= this.rectBottomRuler;
			Rectangle lastRectLeftSidebar	= this.rectLeftSidebar;
			Rectangle lastRectRightSidebar	= this.rectRightSidebar;
			Rectangle lastRectContentArea	= this.rectContentArea;

			this.rectTopRuler = new Rectangle(
				this.areaLeftSidebar.Size,
				0,
				this.ClientRectangle.Width - this.areaLeftSidebar.Size - this.areaRightSidebar.Size,
				this.areaTopRuler.Size);
			this.rectBottomRuler = new Rectangle(
				this.areaLeftSidebar.Size,
				this.ClientRectangle.Height - this.areaBottomRuler.Size,
				this.ClientRectangle.Width - this.areaLeftSidebar.Size - this.areaRightSidebar.Size,
				this.areaBottomRuler.Size);
			this.rectLeftSidebar = new Rectangle(
				0,
				this.areaTopRuler.Size - 1,
				this.areaLeftSidebar.Size + 1,
				this.ClientRectangle.Height - this.areaTopRuler.Size - this.areaBottomRuler.Size + 2);
			this.rectRightSidebar = new Rectangle(
				this.ClientRectangle.Width - this.areaRightSidebar.Size - 1,
				this.areaTopRuler.Size - 1,
				this.areaRightSidebar.Size + 1,
				this.ClientRectangle.Height - this.areaTopRuler.Size - this.areaBottomRuler.Size + 2);
			this.rectContentArea = new Rectangle(
				this.areaLeftSidebar.Size,
				this.areaTopRuler.Size - 1,
				this.ClientRectangle.Width - this.areaLeftSidebar.Size - this.areaRightSidebar.Size,
				this.ClientRectangle.Height - this.areaTopRuler.Size - this.areaBottomRuler.Size + 2);

			if (this.rectTopRuler != lastRectTopRuler ||
				this.rectBottomRuler != lastRectBottomRuler ||
				this.rectLeftSidebar != lastRectLeftSidebar ||
				this.rectRightSidebar != lastRectRightSidebar ||
				this.rectContentArea != lastRectContentArea)
			{
				this.Invalidate();
			}
		}
		private void UpdateContentHeight()
		{
			int contentBaseHeight = this.areaTopRuler.Size + this.areaBottomRuler.Size + this.trackList.Sum(t => t.BaseHeight);
			int additionalHeight = Math.Max(0, this.ClientSize.Height - contentBaseHeight - this.trackSpacing * (this.trackList.Count - 1) + 2);
			int availHeight = additionalHeight;
			int totalFill = this.trackList.Sum(t => t.FillHeight);
			TimelineViewTrack lastFillTrack = this.trackList.LastOrDefault(t => t.FillHeight > 0);
			foreach (TimelineViewTrack track in this.trackList)
			{
				int growHeight;
				if (track.FillHeight > 0)
				{
					if (track == lastFillTrack)
						growHeight = availHeight;
					else
						growHeight = Math.Min(availHeight, (int)Math.Round((float)additionalHeight * (float)track.FillHeight / (float)totalFill));
				}
				else
				{
					growHeight = 0;
				}
				track.Height = track.BaseHeight + growHeight;
				availHeight -= growHeight;
			}
			
			int contentHeight = this.areaTopRuler.Size + this.areaBottomRuler.Size + this.trackList.Sum(t => t.Height) + this.trackSpacing * (this.trackList.Count - 1) - 2;
			Size autoScrollSize;
			if (this.ClientSize.Height >= contentBaseHeight)
				autoScrollSize = new Size(this.AutoScrollMinSize.Width, 0);
			else
				autoScrollSize = new Size(this.AutoScrollMinSize.Width, contentHeight);
			if (autoScrollSize != this.AutoScrollMinSize)
				this.AutoScrollMinSize = autoScrollSize;
		}
		private void UpdateContentWidth()
		{
			float contentBeginTime = 0.0f;
			float contentEndTime = 0.0f;
			if (this.trackList.Count > 0)
			{
				contentBeginTime = this.trackList.Min(t => t.ContentBeginTime);
				contentEndTime = this.trackList.Max(t => t.ContentEndTime);
			}

			Size autoScrollSize;
			if (this.fitZoom)
			{
				autoScrollSize = new Size(
					0, 
					this.AutoScrollMinSize.Height);
			}
			else
			{
				autoScrollSize = new Size(
					1 + (int)this.ConvertUnitsToPixels(contentEndTime - this.unitOffset) + this.areaLeftSidebar.Size + this.areaRightSidebar.Size, 
					this.AutoScrollMinSize.Height);
			}
			if (autoScrollSize != this.AutoScrollMinSize)
				this.AutoScrollMinSize = autoScrollSize;
		}

		protected void UpdateMouseoverState()
		{
			float unitsPerPixel = this.ConvertPixelsToUnits(1.0f);
			Point mousePos = this.PointToClient(Cursor.Position);
			
			// Mind the old mouseover state
			TimelineViewTrack oldHoverTrack = this.mouseoverTrack;
			bool oldHoverContent = this.mouseoverContent;
			float oldUnits = this.mouseoverTime;

			// Determine new mouseover state
			this.mouseoverContent = (this.mouseAction != MouseAction.Scroll) && this.rectContentArea.Contains(mousePos);
			if (this.mouseoverContent)
			{
				this.mouseoverTime = this.GetUnitAtPos(mousePos.X);
				if (this.mouseAction == MouseAction.None)
					this.mouseoverTrack = this.GetTrackAtPos(mousePos.X, mousePos.Y);
				else
					this.mouseoverTrack = null;
			}
			else
			{
				this.mouseoverTime = 0.0f;
				this.mouseoverTrack = null;
			}

			float oldUnitsDrawing = oldHoverContent ? oldUnits : this.mouseoverTime;
			float unitsDrawing = this.mouseoverContent ? this.mouseoverTime : oldUnits;

			// Do a selective repaint due to the moved mouseover line
			if (oldHoverContent != this.mouseoverContent || oldUnits != this.mouseoverTime)
			{
				float unitSpeed = Math.Abs(unitsDrawing - oldUnitsDrawing);
				this.InvalidateContent(
					Math.Min(oldUnitsDrawing, unitsDrawing) - unitsPerPixel - unitSpeed, 
					Math.Max(oldUnitsDrawing, unitsDrawing) + unitsPerPixel + unitSpeed);
			}

			// Fire mouse location events for tracks
			if (oldHoverTrack != this.mouseoverTrack)
			{
				if (oldHoverTrack != null)
				{
					oldHoverTrack.OnMouseLeave(EventArgs.Empty);
				}
				if (this.mouseoverTrack != null)
				{
					this.mouseoverTrack.OnMouseEnter(EventArgs.Empty);
				}
			}
			if (this.mouseoverTrack != null)
			{
				Rectangle trackRect = this.GetTrackRectangle(this.mouseoverTrack);
				this.mouseoverTrack.OnMouseMove(new MouseEventArgs(Control.MouseButtons, 0, mousePos.X - trackRect.X, mousePos.Y - trackRect.Y, 0));
			}
			if (!oldHoverContent && this.mouseoverContent)
			{
				this.OnCursorEnter(new TimelineViewCursorEventArgs(this, unitsDrawing, oldUnitsDrawing));
			}
			if (oldHoverContent != this.mouseoverContent || oldUnits != this.mouseoverTime)
			{
				this.OnCursorMove(new TimelineViewCursorEventArgs(this, unitsDrawing, oldUnitsDrawing));
			}
			if (oldHoverContent && !this.mouseoverContent)
			{
				this.OnCursorLeave(new TimelineViewCursorEventArgs(this, unitsDrawing, oldUnitsDrawing));
			}
		}

		protected virtual void OnUnitChanged(EventArgs e)
		{
			this.Invalidate();
			this.UpdateContentWidth();

			if (this.UnitChanged != null)
				this.UnitChanged(this, e);
		}
		protected virtual void OnModelTracksRemoved(TimelineTrackModelCollectionEventArgs e)
		{
			foreach (ITimelineTrackModel trackModel in e.Tracks)
			{
				TimelineViewTrack track = this.GetTrackByModel(trackModel);
				if (track == null) continue;

				track.ParentView = null;
				track.Model = null;
				track.HeightSettingsChanged	-= this.track_HeightSettingsChanged;
				track.ContentWidthChanged	-= this.track_ContentWidthChanged;
				this.trackList.Remove(track);
			}
			this.OnContentWidthChanged(EventArgs.Empty);
			this.OnContentHeightChanged(EventArgs.Empty);
		}
		protected virtual void OnModelTracksAdded(TimelineTrackModelCollectionEventArgs e)
		{
			foreach (ITimelineTrackModel trackModel in e.Tracks)
			{
				TimelineViewTrack track = this.GetTrackByModel(trackModel);
				if (track != null) continue;

				// Determine Type of the TimelineViewTrack matching the TimelineTrackModel
				if (availableViewTrackTypes == null)
				{
					availableViewTrackTypes = ReflectionHelper.FindConcreteTypes(typeof(TimelineViewTrack));
				}
				Type viewTrackType = null;
				foreach (Type trackType in availableViewTrackTypes)
				{
					foreach (TimelineModelViewAssignmentAttribute attrib in trackType.GetCustomAttributes(true).OfType<TimelineModelViewAssignmentAttribute>())
					{
						foreach (Type validModelType in attrib.ValidModelTypes)
						{
							if (validModelType.IsInstanceOfType(trackModel))
							{
								viewTrackType = trackType;
								break;
							}
						}
						if (viewTrackType != null) break;
					}
					if (viewTrackType != null) break;
				}
				if (viewTrackType == null) continue;

				// Create TimelineViewTrack accordingly
				track = viewTrackType.CreateInstanceOf() as TimelineViewTrack;
				track.Model = trackModel;
				track.BaseHeight = this.defaultTrackHeight;
				track.FillHeight = 100;

				this.trackList.Add(track);
				track.HeightSettingsChanged	+= this.track_HeightSettingsChanged;
				track.ContentWidthChanged	+= this.track_ContentWidthChanged;
				track.ParentView = this;
			}

			this.OnContentWidthChanged(EventArgs.Empty);
			this.OnContentHeightChanged(EventArgs.Empty);
		}
		protected override void OnForeColorChanged(EventArgs e)
		{
			base.OnForeColorChanged(e);
			this.renderer.ColorText = this.ForeColor;
		}
		protected override void OnBackColorChanged(EventArgs e)
		{
			base.OnBackColorChanged(e);
			this.renderer.ColorBackground = this.BackColor;
		}
		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			this.renderer.FontRegular = this.Font;
		}
		protected virtual void OnContentHeightChanged(EventArgs e)
		{
			this.UpdateContentHeight();
			this.Invalidate();
		}
		protected virtual void OnContentWidthChanged(EventArgs e)
		{
			this.UpdateContentWidth();
		}

		protected virtual void OnViewScrolled(EventArgs e)
		{
			if (this.ViewScrolled != null)
				this.ViewScrolled(this, e);
		}
		protected override void OnScroll(ScrollEventArgs se)
		{
			base.OnScroll(se);
			this.UpdateContentWidth();
			this.InvalidateLowQuality();
			if (se.ScrollOrientation == ScrollOrientation.HorizontalScroll)
			{
				this.fitZoom = false;
			}
			this.OnViewScrolled(EventArgs.Empty);
		}
		protected override void OnResize(EventArgs eventargs)
		{
			base.OnResize(eventargs);
			this.UpdateGeometry();
			this.UpdateContentHeight();
			this.InvalidateLowQuality();
			if (this.fitZoom) this.ZoomToFit();
		}

		protected override bool IsInputKey(Keys keyData)
		{
			if (keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down) return true;
			return base.IsInputKey(keyData);
		}
		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);
			this.Focus();
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.Add)
			{
				this.AdjustZoomLevel(1.0f);
			}
			else if (e.KeyCode == Keys.Subtract)
			{
				this.AdjustZoomLevel(-1.0f);
			}
			else if (e.KeyCode == Keys.Left)
			{
				this.UnitScroll += this.ConvertPixelsToUnits(this.rectContentArea.Width) / 50.0f;
				this.InvalidateLowQuality();
				this.OnViewScrolled(EventArgs.Empty);
			}
			else if (e.KeyCode == Keys.Right)
			{
				this.UnitScroll -= this.ConvertPixelsToUnits(this.rectContentArea.Width) / 50.0f;
				this.InvalidateLowQuality();
				this.OnViewScrolled(EventArgs.Empty);
			}
			else if (e.KeyCode == Keys.Up)
			{
				this.AutoScrollPosition = new Point(-this.AutoScrollPosition.X, -this.AutoScrollPosition.Y - this.ClientSize.Height / 50);
				this.InvalidateLowQuality();
				this.OnViewScrolled(EventArgs.Empty);
			}
			else if (e.KeyCode == Keys.Down)
			{
				this.AutoScrollPosition = new Point(-this.AutoScrollPosition.X, -this.AutoScrollPosition.Y + this.ClientSize.Height / 50);
				this.InvalidateLowQuality();
				this.OnViewScrolled(EventArgs.Empty);
			}
			else if (e.KeyCode == Keys.F)
			{
				this.ZoomToFit();
			}
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (this.mouseAction == MouseAction.None)
			{
				if (e.Button == MouseButtons.Middle)
				{
					this.mouseAction = MouseAction.Scroll;
					this.mouseActionOrigin = e.Location;
					this.mouseActionTimer.Enabled = true;
					this.UpdateMouseoverState();
				}
				else if (e.Button == MouseButtons.Left)
				{
					this.mouseAction = MouseAction.Select;
					this.mouseActionOrigin = e.Location;
					this.UpdateMouseoverState();
					
					float lastBegin = this.SelectionBeginTime;
					float lastEnd = this.SelectionEndTime;
					this.selectionTimeA = this.mouseoverTime;
					this.selectionTimeB = this.mouseoverTime;
					if (lastBegin != lastEnd) this.selectionTimeB += this.ConvertPixelsToUnits(0.1f);
					this.RaiseSelectionChanged(lastBegin, lastEnd);
				}
			}
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (this.mouseAction != MouseAction.None)
			{
				if (e.Button == MouseButtons.Middle)
				{
					this.mouseAction = MouseAction.None;
					this.mouseActionTimer.Enabled = false;
					this.UpdateMouseoverState();

					this.Invalidate();
				}
				else if (e.Button == MouseButtons.Left)
				{
					this.mouseAction = MouseAction.None;
					this.UpdateMouseoverState();

					if (this.ConvertUnitsToPixels(Math.Abs(this.selectionTimeA - this.selectionTimeB)) <= 1)
					{
						float lastBegin = this.SelectionBeginTime;
						float lastEnd = this.SelectionEndTime;
						this.selectionTimeA = 0.0f;
						this.selectionTimeB = 0.0f;
						this.RaiseSelectionChanged(lastBegin, lastEnd);
					}
				}
			}
		}
		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			this.UpdateMouseoverState();
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (this.mouseAction == MouseAction.Scroll)
			{
				this.InvalidateLowQuality();
			}
			else
			{
				this.UpdateMouseoverState();
				if (this.mouseAction == MouseAction.Select)
				{
					float lastBegin = this.SelectionBeginTime;
					float lastEnd = this.SelectionEndTime;
					this.selectionTimeB = this.mouseoverTime;
					this.RaiseSelectionChanged(lastBegin, lastEnd);
				}
			}
		}
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			this.UpdateMouseoverState();
		}
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (this.VerticalScroll.Visible && this.VerticalScroll.Enabled)
			{
				base.OnMouseWheel(e);
				this.OnViewScrolled(EventArgs.Empty);
			}
			else
			{
				this.AdjustZoomLevel(e.Delta / 120.0f);
			}
		}
		protected virtual void OnCursorMove(TimelineViewCursorEventArgs e)
		{
			foreach (TimelineViewTrack track in this.trackList)
			{
				track.OnCursorMove(e);
			}
		}
		protected virtual void OnCursorEnter(TimelineViewCursorEventArgs e)
		{
			foreach (TimelineViewTrack track in this.trackList)
			{
				track.OnCursorEnter(e);
			}
		}
		protected virtual void OnCursorLeave(TimelineViewCursorEventArgs e)
		{
			foreach (TimelineViewTrack track in this.trackList)
			{
				track.OnCursorLeave(e);
			}
		}
		protected virtual void OnSelectionChanged(TimelineViewSelectionEventArgs e)
		{
			if (e.IsEmpty != e.WasEmpty)
			{
				this.Invalidate(this.rectContentArea);
			}
			else
			{
				if (e.BeginTime != e.LastBeginTime)
				{
					this.InvalidateContent(e.BeginTime, e.LastBeginTime);
				}
				if (e.EndTime != e.LastEndTime)
				{
					this.InvalidateContent(e.EndTime, e.LastEndTime);
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Apply dynamic quality level
			QualityLevel qualityHint = QualityLevel.High;
			if (this.paintLowQuality)
			{
				// If the last high quality paint didn't take too long, always paint in high quality
				if (this.lastPaintHqTime.TotalMilliseconds < 20.0d)
					qualityHint = QualityLevel.High;
				else
					qualityHint = QualityLevel.Low;
			}

			// Measure how long it takes
			Stopwatch paintWatch = new Stopwatch();
			paintWatch.Restart();

			// Paint the background
			e.Graphics.FillRectangle(new SolidBrush(this.renderer.ColorBackground), this.ClientRectangle);
			e.Graphics.FillRectangle(new SolidBrush(this.renderer.ColorLightBackground), this.rectContentArea);
			
			GraphicsState state;

			// Draw extended ruler markings in the background
			{
				state = e.Graphics.Save();
				e.Graphics.SetClip(this.rectContentArea, CombineMode.Intersect);

				Brush bigLineBrush = new SolidBrush(this.renderer.ColorRulerMarkMajor.ScaleAlpha(0.25f));
				Brush medLineBrush = new SolidBrush(this.renderer.ColorRulerMarkRegular.ScaleAlpha(0.25f));
				Brush minLineBrush = new SolidBrush(this.renderer.ColorRulerMarkMinor.ScaleAlpha(0.25f));
				this.drawBufferBigRuler.Clear();
				this.drawBufferMedRuler.Clear();
				this.drawBufferMinRuler.Clear();

				// Horizontal ruler marks
				foreach (TimelineViewRulerMark mark in this.GetVisibleRulerMarks((int)e.Graphics.ClipBounds.Left, (int)e.Graphics.ClipBounds.Right))
				{
					if (mark.PixelValue < e.Graphics.ClipBounds.Left) continue;
					if (mark.PixelValue > e.Graphics.ClipBounds.Right) break;

					Rectangle lineRect = new Rectangle((int)mark.PixelValue, (int)this.rectContentArea.Y, 1, (int)this.rectContentArea.Height);

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

				e.Graphics.Restore(state);
			}

			int y;

			// Draw all the tracks
			y = 0;
			foreach (TimelineViewTrack track in this.trackList)
			{
				if (this.rectContentArea.Y + y + this.AutoScrollPosition.Y + track.Height <= e.Graphics.ClipBounds.Top + 1)
				{
					y += track.Height + this.trackSpacing;
					continue;
				}
				if (this.rectContentArea.Y + y + this.AutoScrollPosition.Y >= e.Graphics.ClipBounds.Bottom - 1) break;

				// Content
				{
					Rectangle targetRect = new Rectangle(
						this.rectContentArea.X + 1,
						this.rectContentArea.Y + y + this.AutoScrollPosition.Y,
						this.rectContentArea.Width - 2,
						track.Height);
					state = e.Graphics.Save();
					e.Graphics.SetClip(this.rectContentArea, CombineMode.Intersect);
					e.Graphics.SetClip(targetRect, CombineMode.Intersect);
					if (!e.Graphics.ClipBounds.IsEmpty)
					{
						track.OnPaint(new TimelineViewTrackPaintEventArgs(track, e.Graphics, qualityHint, targetRect));
					}
					e.Graphics.Restore(state);
				}

				// Left Sidebar
				{
					Rectangle targetRect = new Rectangle(
						this.rectLeftSidebar.X,
						this.rectLeftSidebar.Y + y + this.AutoScrollPosition.Y,
						this.rectLeftSidebar.Width,
						track.Height);
					state = e.Graphics.Save();
					e.Graphics.SetClip(this.rectLeftSidebar, CombineMode.Intersect);
					e.Graphics.SetClip(targetRect, CombineMode.Intersect);
					if (!e.Graphics.ClipBounds.IsEmpty)
					{
						track.OnPaintLeftSidebar(new TimelineViewTrackPaintEventArgs(track, e.Graphics, qualityHint, targetRect));
					}
					e.Graphics.Restore(state);
				}

				// Right Sidebar
				{
					Rectangle targetRect = new Rectangle(
						this.rectRightSidebar.X,
						this.rectRightSidebar.Y + y + this.AutoScrollPosition.Y,
						this.rectRightSidebar.Width,
						track.Height);
					state = e.Graphics.Save();
					e.Graphics.SetClip(this.rectRightSidebar, CombineMode.Intersect);
					e.Graphics.SetClip(targetRect, CombineMode.Intersect);
					if (!e.Graphics.ClipBounds.IsEmpty)
					{
						track.OnPaintRightSidebar(new TimelineViewTrackPaintEventArgs(track, e.Graphics, qualityHint, targetRect));
					}
					e.Graphics.Restore(state);
				}

				y += track.Height + this.trackSpacing;
			}

			// Draw the selection indicator
			if (this.selectionTimeA != this.selectionTimeB)
			{
				float selectionPixelsBegin = this.GetPosAtUnit(this.SelectionBeginTime);
				float selectionPixelsEnd = this.GetPosAtUnit(this.SelectionEndTime);
				Pen pen = new Pen(Color.FromArgb(128, this.Renderer.ColorText));
				SolidBrush brush = new SolidBrush(Color.FromArgb(32, this.Renderer.ColorText));
				e.Graphics.FillRectangle(
					brush,
					this.rectContentArea.Left,
					this.rectContentArea.Top,
					selectionPixelsBegin - this.rectContentArea.Left,
					this.rectContentArea.Height);
				e.Graphics.FillRectangle(
					brush,
					selectionPixelsEnd,
					this.rectContentArea.Top,
					this.rectContentArea.Right - selectionPixelsEnd,
					this.rectContentArea.Height);
				e.Graphics.DrawLine(pen, selectionPixelsBegin, this.rectContentArea.Top, selectionPixelsBegin, this.rectContentArea.Bottom);
				e.Graphics.DrawLine(pen, selectionPixelsEnd, this.rectContentArea.Top, selectionPixelsEnd, this.rectContentArea.Bottom);
			}

			// Draw the mouseover time indicator
			if (this.mouseoverContent)
			{
				float mouseoverPixels = this.GetPosAtUnit(this.mouseoverTime);
				Pen mouseoverTimePen = new Pen(Color.FromArgb(128, this.Renderer.ColorText));
				mouseoverTimePen.DashStyle = DashStyle.Dot;
				e.Graphics.DrawLine(mouseoverTimePen, mouseoverPixels, this.rectContentArea.Top, mouseoverPixels, this.rectContentArea.Bottom);
			}

			// Draw all the track overlays and borders
			{
				y = 0;
				foreach (TimelineViewTrack track in this.trackList)
				{
					if (this.rectContentArea.Y + y + this.AutoScrollPosition.Y + track.Height <= e.Graphics.ClipBounds.Top + 1)
					{
						y += track.Height + this.trackSpacing;
						continue;
					}
					if (this.rectContentArea.Y + y + this.AutoScrollPosition.Y >= e.Graphics.ClipBounds.Bottom - 1) break;
				
					Rectangle targetRect = new Rectangle(
						this.rectContentArea.X + 1,
						this.rectContentArea.Y + y + this.AutoScrollPosition.Y,
						this.rectContentArea.Width - 2,
						track.Height);

					// Overlay
					{
						state = e.Graphics.Save();
						e.Graphics.SetClip(this.rectContentArea, CombineMode.Intersect);
						e.Graphics.SetClip(targetRect, CombineMode.Intersect);
						if (!e.Graphics.ClipBounds.IsEmpty)
						{
							track.OnPaintOverlay(new TimelineViewTrackPaintEventArgs(track, e.Graphics, qualityHint, targetRect));
						}
						e.Graphics.Restore(state);
					}

					y += track.Height + this.trackSpacing;
				}
			}

			// Draw the content area drop shadow
			if (!this.rectContentArea.IsEmpty)
			{
				LinearGradientBrush shadowBrush;
				Color shadowColor = Color.Black;
				ColorBlend shadowBlend = new ColorBlend();
				shadowBlend.Positions	= new[] {	0.0f,								0.5f,								1.0f };
				shadowBlend.Colors		= new[] {	Color.FromArgb(64, shadowColor),	Color.FromArgb(16, shadowColor),	Color.FromArgb(0, shadowColor) };

				Rectangle dropShadowH = new Rectangle(this.rectContentArea.Left, this.rectContentArea.Top, this.rectContentArea.Width, Math.Min(12, this.rectContentArea.Height));
				Rectangle dropShadowV = new Rectangle(this.rectContentArea.Left, this.rectContentArea.Top, Math.Min(12, this.rectContentArea.Width), this.rectContentArea.Height);

				shadowBrush = new LinearGradientBrush(dropShadowH, Color.Black, Color.Black, LinearGradientMode.Vertical);
				shadowBrush.InterpolationColors = shadowBlend;
				e.Graphics.FillRectangle(shadowBrush, dropShadowH);
				shadowBrush = new LinearGradientBrush(dropShadowV, Color.Black, Color.Black, LinearGradientMode.Horizontal);
				shadowBrush.InterpolationColors = shadowBlend;
				e.Graphics.FillRectangle(shadowBrush, dropShadowV);
			}

			// Draw the Top Ruler
			if (!this.rectTopRuler.IsEmpty)
			{
				state = e.Graphics.Save();
				e.Graphics.SetClip(this.rectTopRuler, CombineMode.Intersect);
				if (!e.Graphics.ClipBounds.IsEmpty)
				{
					this.OnPaintTopRuler(new TimelineViewPaintEventArgs(this, e.Graphics, this.rectTopRuler));
				}
				e.Graphics.Restore(state);
			}

			// Draw the Bottom Ruler
			if (!this.rectBottomRuler.IsEmpty)
			{
				state = e.Graphics.Save();
				e.Graphics.SetClip(this.rectBottomRuler, CombineMode.Intersect);
				if (!e.Graphics.ClipBounds.IsEmpty)
				{
					this.OnPaintBottomRuler(new TimelineViewPaintEventArgs(this, e.Graphics, this.rectBottomRuler));
				}
				e.Graphics.Restore(state);
			}

			// Draw mouse action indicators
			if (this.mouseAction == MouseAction.Scroll)
			{
				e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
				e.Graphics.FillEllipse(new SolidBrush(this.renderer.ColorText.ScaleAlpha(0.5f)), this.mouseActionOrigin.X - 3, this.mouseActionOrigin.Y - 3, 6, 6);
				e.Graphics.DrawLine(new Pen(this.renderer.ColorText.ScaleAlpha(0.5f)), this.mouseActionOrigin, this.PointToClient(Cursor.Position));
				e.Graphics.SmoothingMode = SmoothingMode.Default;
			}

			paintWatch.Stop();
			if (qualityHint == QualityLevel.High) this.lastPaintHqTime = paintWatch.Elapsed;

			Console.WriteLine("{1}\t{0:F}\t{2}", paintWatch.Elapsed.TotalMilliseconds, qualityHint, e.ClipRectangle);
		}
		protected virtual void OnPaintTopRuler(TimelineViewPaintEventArgs e)
		{
			this.DrawRuler(e.Graphics, e.TargetRect, true);
		}
		protected virtual void OnPaintBottomRuler(TimelineViewPaintEventArgs e)
		{
			this.DrawRuler(e.Graphics, e.TargetRect, false);
		}
		protected void DrawRuler(Graphics g, Rectangle rect, bool top)
		{
			// Draw background
			Rectangle borderRect;
			if (this.BorderStyle != System.Windows.Forms.BorderStyle.None)
			{
				borderRect = new Rectangle(
					rect.X,
					top ? rect.Y - 1 : rect.Y,
					rect.Width,
					rect.Height + 1);
			}
			else
			{
				borderRect = rect;
			}
			g.FillRectangle(new SolidBrush(this.renderer.ColorVeryLightBackground), rect);
			this.renderer.DrawBorder(g, borderRect, Drawing.BorderStyle.Simple, BorderState.Normal);

			// Determine drawing geometry
			Rectangle rectUnitName;
			Rectangle rectUnitMarkings;
			{
				SizeF unitNameSize = g.MeasureString(this.model.UnitName, this.renderer.FontRegular, rect.Width);
				float markingRatio = 0.5f + 0.5f * (1.0f - Math.Max(Math.Min((float)rect.Height / 32.0f, 1.0f), 0.0f));
				if (unitNameSize.IsEmpty) unitNameSize = new SizeF(1.0f, 1.0f);
				if (top)
				{
					rectUnitName = new Rectangle(
						rect.X, 
						rect.Y, 
						(int)Math.Ceiling(unitNameSize.Width), 
						(int)Math.Ceiling(unitNameSize.Height));
					rectUnitMarkings = new Rectangle(
						rect.X,
						rect.Bottom - Math.Min((int)(rect.Height * markingRatio), 16),
						rect.Width,
						Math.Min((int)(rect.Height * markingRatio), 16));
				}
				else
				{
					rectUnitName = new Rectangle(
						rect.X, 
						rect.Bottom - (int)Math.Ceiling(unitNameSize.Height), 
						(int)Math.Ceiling(unitNameSize.Width), 
						(int)Math.Ceiling(unitNameSize.Height));
					rectUnitMarkings = new Rectangle(
						rect.X,
						rect.Top,
						rect.Width,
						Math.Min((int)(rect.Height * markingRatio), 16));
				}
			}

			// Draw unit name
			{
				Rectangle overlap = rectUnitMarkings;
				overlap.Intersect(rectUnitName);
				float overlapAmount = Math.Max(Math.Min((float)overlap.Height / (float)rectUnitName.Height, 1.0f), 0.0f);
				float textOverlapAlpha = (1.0f - (overlapAmount));

				string unitText = null;
				{
					string unitTextPrimary = !string.IsNullOrEmpty(this.model.UnitDescription) ? this.model.UnitDescription : null;
					string unitTextSecondary = !string.IsNullOrEmpty(this.model.UnitName) ? this.model.UnitName : null;

					if (unitTextPrimary != null && unitTextSecondary != null)
					{
						unitText = string.Format("{0} ({1})", unitTextPrimary, unitTextSecondary);
					}
					else
						unitText = (unitTextPrimary ?? unitTextSecondary) ?? "Units";
				}

				StringFormat format = new StringFormat(StringFormat.GenericDefault);
				format.Alignment = StringAlignment.Near;
				format.LineAlignment = top ? StringAlignment.Near : StringAlignment.Far;

				g.DrawString(
					unitText, 
					this.renderer.FontRegular, 
					new SolidBrush(Color.FromArgb((int)(textOverlapAlpha * 255), this.renderer.ColorText)), 
					rect, 
					format);
			}

			// Draw ruler markings
			{
				int timeDecimals = Math.Max(0, -(int)Math.Log10(this.VisibleUnitWidth) + 2);

				Pen bigLinePen = new Pen(new SolidBrush(this.renderer.ColorRulerMarkMajor));
				Pen medLinePen = new Pen(new SolidBrush(this.renderer.ColorRulerMarkRegular));
				Pen minLinePen = new Pen(new SolidBrush(this.renderer.ColorRulerMarkMinor));

				foreach (TimelineViewRulerMark mark in this.GetVisibleRulerMarks((int)g.ClipBounds.Left, (int)g.ClipBounds.Right))
				{
					bool drawMark = (mark.PixelValue - rectUnitMarkings.Left >= 1.0f) && (rectUnitMarkings.Right - mark.PixelValue >= 1.0f);

					float markLen;
					Pen markPen;
					switch (mark.Weight)
					{
						case TimelineViewRulerMarkWeight.Major:
							markLen = 1.0f;
							markPen = bigLinePen;
							break;
						default:
						case TimelineViewRulerMarkWeight.Regular:
							markLen = 0.5f;
							markPen = medLinePen;
							break;
						case TimelineViewRulerMarkWeight.Minor:
							markLen = 0.25f;
							markPen = minLinePen;
							break;
					}

					float markTopY;
					float markBottomY;
					float markTextY;
					if (top)
					{
						markTopY = rectUnitMarkings.Bottom - markLen * rectUnitMarkings.Height;
						markBottomY = rectUnitMarkings.Bottom;
						markTextY = rectUnitMarkings.Bottom - this.renderer.FontRegular.Height - Math.Max(Math.Min(3 + rectUnitMarkings.Top - rectUnitName.Bottom, rectUnitMarkings.Height / 2), 0);
					}
					else
					{
						markTopY = rectUnitMarkings.Top;
						markBottomY = rectUnitMarkings.Top + markLen * rectUnitMarkings.Height;
						markTextY = rectUnitMarkings.Top + Math.Max(Math.Min(3 + rectUnitName.Top - rectUnitMarkings.Bottom, rectUnitMarkings.Height / 2), 0);
					}

					if (drawMark)
					{
						g.DrawLine(markPen, (int)mark.PixelValue, (int)markTopY, (int)mark.PixelValue, (int)markBottomY);
					}

					if (mark.Weight == TimelineViewRulerMarkWeight.Major)
					{
						string timeString = string.Format(
							System.Globalization.CultureInfo.InvariantCulture, 
							"{0:F" + timeDecimals + "}", 
							mark.UnitValue);
						g.DrawString(
							timeString, 
							this.renderer.FontSmall, 
							new SolidBrush(this.renderer.ColorText), 
							mark.PixelValue, 
							markTextY);
					}
				}
			}
		}
		
		private void RaiseSelectionChanged(float lastBegin, float lastEnd)
		{
			float begin = this.SelectionBeginTime;
			float end = this.SelectionEndTime;

			if (lastBegin == begin && lastEnd == end) return;
			if (begin == end && lastBegin == lastEnd) return;

			this.OnSelectionChanged(new TimelineViewSelectionEventArgs(this, this.SelectionBeginTime, this.SelectionEndTime, lastBegin, lastEnd));
		}

		private void track_HeightSettingsChanged(object sender, EventArgs e)
		{
			this.OnContentHeightChanged(EventArgs.Empty);
		}
		private void track_ContentWidthChanged(object sender, EventArgs e)
		{
			this.OnContentWidthChanged(EventArgs.Empty);
		}
		private void model_TracksRemoved(object sender, TimelineTrackModelCollectionEventArgs e)
		{
			this.OnModelTracksRemoved(e);
		}
		private void model_TracksAdded(object sender, TimelineTrackModelCollectionEventArgs e)
		{
			this.OnModelTracksAdded(e);
		}
		private void model_UnitChanged(object sender, EventArgs e)
		{
			this.OnUnitChanged(e);
		}
		private void paintHqTimer_Tick(object sender, EventArgs e)
		{
			if (Control.MouseButtons != MouseButtons.None) return;
			this.paintLowQuality = false;
			this.Invalidate();
			this.paintHqTimer.Enabled = false;
		}
		private void mouseActionTimer_Tick(object sender, EventArgs e)
		{
			Point mousePos = this.PointToClient(Cursor.Position);
			const float ScrollSpeed = 0.5f;
			this.mouseScrollAcc.X += (mousePos.X - this.mouseActionOrigin.X) * ScrollSpeed;
			this.mouseScrollAcc.Y += (mousePos.Y - this.mouseActionOrigin.Y) * ScrollSpeed;

			if (!this.HorizontalScroll.Visible || !this.HorizontalScroll.Enabled) this.mouseScrollAcc.X = 0.0f;
			if (!this.VerticalScroll.Visible || !this.VerticalScroll.Enabled) this.mouseScrollAcc.Y = 0.0f;

			Point quantizedMovement = new Point((int)this.mouseScrollAcc.X, (int)this.mouseScrollAcc.Y);
			if (quantizedMovement != Point.Empty)
			{
				this.AutoScrollPosition = new Point(
					-this.AutoScrollPosition.X + quantizedMovement.X,
					-this.AutoScrollPosition.Y + quantizedMovement.Y);
				this.mouseScrollAcc.X -= quantizedMovement.X;
				this.mouseScrollAcc.Y -= quantizedMovement.Y;

				this.OnViewScrolled(EventArgs.Empty);
			}
		}

		public static float GetNiceMultiple(float rawMultiple, NiceMultipleMode mode = NiceMultipleMode.Nearest, NiceMultipleGranularity granularity = NiceMultipleGranularity.Medium)
		{
			float[] allowedFactors;
			switch (granularity)
			{
				case NiceMultipleGranularity.Low:
					allowedFactors = new float[] { 1.0f, 5.0f, 10.0f };
					break;
				default:
				case NiceMultipleGranularity.Medium:
					allowedFactors = new float[] { 1.0f, 2.0f, 5.0f, 10.0f };
					break;
				case NiceMultipleGranularity.High:
					allowedFactors = new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f };
					break;
			}

			float absMultiple = Math.Abs(rawMultiple);
			float magnitude = (float)Math.Floor(Math.Log10(absMultiple));
			float baseValue = (float)Math.Pow(10.0f, magnitude);
			float factor = absMultiple / baseValue;

			if (Math.Sign(rawMultiple) < 0.0f)
			{
				if (mode == NiceMultipleMode.Higher)
					mode = NiceMultipleMode.Lower;
				else if (mode == NiceMultipleMode.Lower)
					mode = NiceMultipleMode.Higher;
			}

			switch (mode)
			{
				case NiceMultipleMode.Higher:
				{
					for (int i = 0; i < allowedFactors.Length; i++)
					{
						if (i == allowedFactors.Length - 1 || factor <= allowedFactors[i])
						{
							factor = allowedFactors[i];
							break;
						}
					}
					break;
				}
				case NiceMultipleMode.Lower:
				{
					for (int i = 0; i < allowedFactors.Length; i++)
					{
						if (i == allowedFactors.Length - 1 || factor < allowedFactors[i + 1])
						{
							factor = allowedFactors[i];
							break;
						}
					}
					break;
				}
				default:
				case NiceMultipleMode.Nearest:
				{
					for (int i = 0; i < allowedFactors.Length; i++)
					{
						if (i == allowedFactors.Length - 1 || factor < (allowedFactors[i] + allowedFactors[i + 1]) * 0.5f)
						{
							factor = allowedFactors[i];
							break;
						}
					}
					break;
				}
			}

			return baseValue * factor * Math.Sign(rawMultiple);
		}
		public static IEnumerable<float> EnumerateRulerMarks(float stepSize, float unitScroll, float beginUnits, float endUnits, int stepCountMultiple)
		{
			float stepSign = Math.Sign(stepSize);
			int roundDecimals = 3;
			{
				decimal stepSizeDec = (decimal)stepSize;
				while (Math.Round(stepSizeDec, roundDecimals) != stepSizeDec && roundDecimals < 10)
				{
					roundDecimals++;
				}
			}

			double stepSizeBig = stepSize * stepCountMultiple;
			double scrollStepOffset = stepSizeBig * Math.Floor(-unitScroll / stepSizeBig);
			double rangeBegin = stepSizeBig * Math.Floor(beginUnits / stepSizeBig) + scrollStepOffset;
			double rangeEnd = stepSizeBig * (Math.Ceiling(endUnits / stepSizeBig) + 1) + scrollStepOffset;
			double unitValue = rangeBegin;

			int stepCount = 0;
			while (unitValue * stepSign < rangeEnd * stepSign)
			{
				unitValue = Math.Round(rangeBegin + stepCount * (double)stepSize, roundDecimals);
				yield return (float)unitValue;
				stepCount++;
			}
			yield break;
		}
	}
}
