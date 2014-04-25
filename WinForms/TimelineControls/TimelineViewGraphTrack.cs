using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	[TimelineTrackAssignment(typeof(ITimelineGraphTrackModel))]
	public class TimelineViewGraphTrack : TimelineViewTrack
	{
		private	float					verticalUnitTop		= 1.0f;
		private	float					verticalUnitBottom	= -1.0f;


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
			return this.verticalUnitTop + ((float)y * (this.verticalUnitBottom - this.verticalUnitTop) / (float)this.Height);
		}
		public float GetPosAtUnit(float unit)
		{
			return (float)this.Height * ((unit - this.verticalUnitTop) / (this.verticalUnitBottom - this.verticalUnitTop));
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
	}
}
