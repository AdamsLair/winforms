using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

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


		private const float DefaultPixelsPerUnit = 5.0f;
		private static List<Type> availableViewTrackTypes = null;

		private	ITimelineModel				model				= new TimelineModel();
		private	TimelineViewControlRenderer	renderer			= new TimelineViewControlRenderer();
		private	List<TimelineViewTrack>		trackList			= new List<TimelineViewTrack>();
		private	bool						test	= false;
		private	int							defaultTrackHeight	= 150;
		private	float						unitOffset			= 0.0f;
		private	float						unitZoom			= 1.0f;
		private	int							trackSpacing		= -1;
		private SubAreaInfo					areaTopRuler		= new SubAreaInfo(30);
		private SubAreaInfo					areaBottomRuler		= new SubAreaInfo(30);
		private SubAreaInfo					areaLeftSidebar		= new SubAreaInfo(30);
		private SubAreaInfo					areaRightSidebar	= new SubAreaInfo(30);

		private	Rectangle	rectTopRuler;
		private	Rectangle	rectBottomRuler;
		private	Rectangle	rectLeftSidebar;
		private	Rectangle	rectRightSidebar;
		private	Rectangle	rectContentArea;

		
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

					this.OnModelTracksRemoved(new TimelineModelTracksEventArgs(this.model.Tracks));

					this.model = value ?? new TimelineModel();
					
					this.OnModelUnitChanged(EventArgs.Empty);
					this.OnModelTracksAdded(new TimelineModelTracksEventArgs(this.model.Tracks));

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
		public float UnitOffset
		{
			get { return this.unitOffset; }
			set { this.unitOffset = value; }
		}
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float UnitScroll
		{
			get { return this.ConvertPixelsToUnits(this.AutoScrollPosition.X) * this.unitZoom; }
		}
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float VisibleUnitWidth
		{
			get { return this.ConvertPixelsToUnits(this.rectContentArea.Width) * this.unitZoom; }
		}
		[DefaultValue(1.0f)]
		public float UnitZoom
		{
			get { return this.unitZoom; }
			set
			{
				this.unitZoom = Math.Max(value, 0.00000001f);
				this.Invalidate();
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


		public TimelineView()
		{
			this.AutoScroll = true;

			this.SetStyle(ControlStyles.ResizeRedraw, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.Opaque, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.SetStyle(ControlStyles.Selectable, true);
		}
		
		public TimelineViewTrack GetTrackByModel(ITimelineTrackModel trackModel)
		{
			return this.trackList.FirstOrDefault(t => t.Model == trackModel);
		}

		public float ConvertUnitsToPixels(float units)
		{
			return units * this.model.UnitBaseScale * DefaultPixelsPerUnit;
		}
		public float ConvertPixelsToUnits(float pixels)
		{
			return pixels / (this.model.UnitBaseScale * DefaultPixelsPerUnit);
		}
		public float GetUnitAtPos(float x)
		{
			return this.unitOffset + this.ConvertPixelsToUnits(x - this.rectContentArea.X) * this.unitZoom;
		}
		public float GetPosAtUnit(float unit)
		{
			return this.rectContentArea.X + this.ConvertUnitsToPixels(unit - this.unitOffset) / this.unitZoom;
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

			rect.X += this.ClientRectangle.X;
			rect.Y += this.ClientRectangle.Y;

			if (scrolled) rect.Y += this.AutoScrollPosition.Y;

			return rect;
		}
		public TimelineViewTrack GetTrackAtPos(int x, int y, bool scrolled = true, bool allowNearest = false)
		{
			if (scrolled) y -= this.AutoScrollPosition.Y;

			x -= this.ClientRectangle.X;
			y -= this.ClientRectangle.Y;

			if (allowNearest)
			{
				if (x < 0) x = 0;
				if (y < 0) y = 0;
				if (x >= this.ClientSize.Width) x = this.ClientSize.Width - 1;
				if (y >= this.ClientSize.Height) y = this.ClientSize.Height - 1;
			}
			else
			{
				if (x < 0) return null;
				if (y < 0) return null;
				if (x >= this.ClientSize.Width) return null;
				if (y >= this.ClientSize.Height) return null;
			}
			
			foreach (TimelineViewTrack t in this.trackList)
			{
				y -= t.Height + this.trackSpacing;
				if (y < 0) return t;
			}

			return null;
		}
		public IEnumerable<TimelineViewRulerMark> GetVisibleRulerMarks()
		{
			float rulerStep = GetNiceMultiple(this.ConvertPixelsToUnits(100.0f * this.unitZoom)) / 10.0f;
			float unitScroll = this.UnitScroll;
			float beginTime = this.GetUnitAtPos(this.rectTopRuler.Left);
			float endTime = this.GetUnitAtPos(this.rectTopRuler.Right);

			int lineIndex = 0;
			foreach (float markTime in EnumerateRulerMarks(rulerStep, unitScroll, beginTime, endTime, 10))
			{
				float markX = this.GetPosAtUnit(markTime + unitScroll);

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

		private void UpdateGeometry()
		{
			Rectangle lastRectTopRuler		= this.rectTopRuler;
			Rectangle lastRectBottomRuler	= this.rectBottomRuler;
			Rectangle lastRectLeftSidebar	= this.rectLeftSidebar;
			Rectangle lastRectRightSidebar	= this.rectRightSidebar;
			Rectangle lastRectContentArea	= this.rectContentArea;

			this.rectTopRuler = new Rectangle(
				this.ClientRectangle.X + this.areaLeftSidebar.Size,
				this.ClientRectangle.Y + 0,
				this.ClientRectangle.Width - this.areaLeftSidebar.Size - this.areaRightSidebar.Size,
				this.areaTopRuler.Size);
			this.rectBottomRuler = new Rectangle(
				this.ClientRectangle.X + this.areaLeftSidebar.Size,
				this.ClientRectangle.Y + this.ClientRectangle.Height - this.areaBottomRuler.Size,
				this.ClientRectangle.Width - this.areaLeftSidebar.Size - this.areaRightSidebar.Size,
				this.areaBottomRuler.Size);
			this.rectLeftSidebar = new Rectangle(
				this.ClientRectangle.X,
				this.ClientRectangle.Y + this.areaTopRuler.Size - 1,
				this.areaLeftSidebar.Size + 1,
				this.ClientRectangle.Height - this.areaTopRuler.Size - this.areaBottomRuler.Size + 2);
			this.rectRightSidebar = new Rectangle(
				this.ClientRectangle.X + this.ClientRectangle.Width - this.areaRightSidebar.Size - 1,
				this.ClientRectangle.Y + this.areaTopRuler.Size - 1,
				this.areaRightSidebar.Size + 1,
				this.ClientRectangle.Height - this.areaTopRuler.Size - this.areaBottomRuler.Size + 2);
			this.rectContentArea = new Rectangle(
				this.ClientRectangle.X + this.areaLeftSidebar.Size,
				this.ClientRectangle.Y + this.areaTopRuler.Size - 1,
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
			
			Size contentSize = new Size(1500, this.areaTopRuler.Size + this.areaBottomRuler.Size + this.trackList.Sum(t => t.Height) + this.trackSpacing * (this.trackList.Count - 1) - 2);
			Size autoScrollSize;
			if (this.ClientSize.Height >= contentBaseHeight)
				autoScrollSize = new Size(contentSize.Width, 0);
			else
				autoScrollSize = contentSize;
			this.AutoScrollMinSize = autoScrollSize;
		}

		protected virtual void OnModelUnitChanged(EventArgs e)
		{
			this.Invalidate();
		}
		protected virtual void OnModelTracksRemoved(TimelineModelTracksEventArgs e)
		{
			foreach (ITimelineTrackModel trackModel in e.Tracks)
			{
				TimelineViewTrack track = this.GetTrackByModel(trackModel);
				if (track == null) continue;

				track.ParentView = null;
				track.Model = null;
				track.HeightSettingsChanged -= this.track_HeightSettingsChanged;
				this.trackList.Remove(track);
			}
			this.OnContentHeightChanged(EventArgs.Empty);
		}
		protected virtual void OnModelTracksAdded(TimelineModelTracksEventArgs e)
		{
			foreach (ITimelineTrackModel trackModel in e.Tracks)
			{
				TimelineViewTrack track = this.GetTrackByModel(trackModel);
				if (track != null) continue;

				// Determine Type of the TimelineViewTrack matching the TimelineTrackModel
				if (availableViewTrackTypes == null)
				{
					availableViewTrackTypes = 
						AppDomain.CurrentDomain.GetAssemblies().
						SelectMany(a => a.GetExportedTypes()).
						Where(t => !t.IsAbstract && !t.IsInterface && typeof(TimelineViewTrack).IsAssignableFrom(t)).
						ToList();
				}
				Type viewTrackType = null;
				foreach (Type trackType in availableViewTrackTypes)
				{
					foreach (TimelineTrackAssignmentAttribute attrib in trackType.GetCustomAttributes(true).OfType<TimelineTrackAssignmentAttribute>())
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
				track.HeightSettingsChanged += this.track_HeightSettingsChanged;
				track.ParentView = this;
			}

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
		protected override void OnScroll(ScrollEventArgs se)
		{
			base.OnScroll(se);
			this.Invalidate();
		}
		protected override void OnResize(EventArgs eventargs)
		{
			base.OnResize(eventargs);
			this.UpdateGeometry();
			this.UpdateContentHeight();
		}
		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);
			this.Focus();
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			//var w = new System.Diagnostics.Stopwatch();
			//w.Restart();

			e.Graphics.FillRectangle(new SolidBrush(this.renderer.ColorBackground), this.ClientRectangle);
			e.Graphics.FillRectangle(new SolidBrush(this.renderer.ColorLightBackground), this.rectContentArea);

			GraphicsState state;

			// Draw all the tracks
			int y = 0;
			foreach (TimelineViewTrack track in this.trackList)
			{
				if (y + track.Height < e.Graphics.ClipBounds.Top) continue;
				if (y > e.Graphics.ClipBounds.Bottom) break;

				// Content
				{
					Rectangle targetRect = new Rectangle(
						this.rectContentArea.X,
						this.rectContentArea.Y + y + this.AutoScrollPosition.Y,
						this.rectContentArea.Width,
						track.Height);
					state = e.Graphics.Save();
					e.Graphics.SetClip(this.rectContentArea, CombineMode.Intersect);
					e.Graphics.SetClip(targetRect, CombineMode.Intersect);
					if (!e.Graphics.ClipBounds.IsEmpty)
					{
						track.OnPaint(new TimelineViewTrackPaintEventArgs(track, e.Graphics, targetRect));
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
						track.OnPaintLeftSidebar(new TimelineViewTrackPaintEventArgs(track, e.Graphics, targetRect));
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
						track.OnPaintRightSidebar(new TimelineViewTrackPaintEventArgs(track, e.Graphics, targetRect));
					}
					e.Graphics.Restore(state);
				}

				y += track.Height + this.trackSpacing;
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

			//w.Stop();
			//Console.WriteLine("{0:F}", w.Elapsed.TotalMilliseconds);
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
						unitText = string.Format("{0} ({1})", unitTextPrimary, unitTextSecondary);
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
				Pen bigLinePen = new Pen(new SolidBrush(this.renderer.ColorRulerMarkMajor));
				Pen medLinePen = new Pen(new SolidBrush(this.renderer.ColorRulerMarkRegular));
				Pen minLinePen = new Pen(new SolidBrush(this.renderer.ColorRulerMarkMinor));

				foreach (TimelineViewRulerMark mark in this.GetVisibleRulerMarks())
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
						string timeString = string.Format("{0}", mark.UnitValue);
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
		
		private void track_HeightSettingsChanged(object sender, EventArgs e)
		{
			this.OnContentHeightChanged(EventArgs.Empty);
		}
		private void model_TracksRemoved(object sender, TimelineModelTracksEventArgs e)
		{
			this.OnModelTracksRemoved(e);
		}
		private void model_TracksAdded(object sender, TimelineModelTracksEventArgs e)
		{
			this.OnModelTracksAdded(e);
		}
		private void model_UnitChanged(object sender, EventArgs e)
		{
			this.OnModelUnitChanged(e);
		}

		public static float GetNiceMultiple(float rawMultiple)
		{
			float magnitude = (float)Math.Floor(Math.Log10(rawMultiple));
			float baseValue = (float)Math.Pow(10.0f, magnitude);
			float factor = rawMultiple / baseValue;

			if (factor < 1.25f) factor = 1.0f;
			else if (factor < 3.75f) factor = 2.5f;
			else if (factor < 7.5f) factor = 5.0f;
			else factor = 10.0f;

			return baseValue * factor;
		}
		public static IEnumerable<float> EnumerateRulerMarks(float stepSize, float unitScroll, float beginUnits, float endUnits, int stepCountMultiple)
		{
			float stepSign = Math.Sign(stepSize);

			float stepSizeBig = stepSize * stepCountMultiple;
			float scrollStepOffset = stepSizeBig * (float)Math.Floor(-unitScroll / stepSizeBig);
			float rangeBegin = stepSizeBig * (float)Math.Floor(beginUnits / stepSizeBig) + scrollStepOffset;
			float rangeEnd = stepSizeBig * ((float)Math.Ceiling(endUnits / stepSizeBig) + 1) + scrollStepOffset;
			float unitValue = rangeBegin;

			while (unitValue * stepSign < rangeEnd * stepSign)
			{
				yield return unitValue;
				unitValue += stepSize;
			}
			yield break;
		}
	}
}
