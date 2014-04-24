using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
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

			Rectangle rect = e.TargetRect;
			string verticalTopText = string.Format("{0}", (float)Math.Round(this.verticalUnitTop, 2));
			string verticalBottomText = string.Format("{0}", (float)Math.Round(this.verticalUnitBottom, 2));
			SizeF verticalTopTextSize = e.Graphics.MeasureString(verticalTopText, e.Renderer.FontRegular);
			SizeF verticalBottomTextSize = e.Graphics.MeasureString(verticalBottomText, e.Renderer.FontRegular);

			// Draw background
			Rectangle borderRect;
			if (this.ParentView.BorderStyle != System.Windows.Forms.BorderStyle.None)
			{
				borderRect = new Rectangle(
					rect.X - 1,
					rect.Y,
					rect.Width + 1,
					rect.Height);
			}
			else
			{
				borderRect = rect;
			}
			e.Graphics.FillRectangle(new SolidBrush(e.Renderer.ColorVeryLightBackground), rect);
			e.Renderer.DrawBorder(e.Graphics, borderRect, Drawing.BorderStyle.Simple, BorderState.Normal);

			// Determine drawing geometry
			Rectangle rectTrackName;
			Rectangle rectUnitMarkings;
			Rectangle rectUnitRuler;
			{
				float markingRatio = 0.5f + 0.5f * (1.0f - Math.Max(Math.Min((float)rect.Height / 32.0f, 1.0f), 0.0f));
				rectTrackName = new Rectangle(
					rect.X, 
					rect.Y, 
					Math.Min(rect.Width, e.Renderer.FontRegular.Height + 2), 
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
			}

			// Draw track name
			{
				Rectangle overlap = rectUnitMarkings;
				overlap.Intersect(rectTrackName);
				float overlapAmount = Math.Max(Math.Min((float)overlap.Width / (float)rectTrackName.Width, 1.0f), 0.0f);
				float textOverlapAlpha = (1.0f - (overlapAmount));

				StringFormat format = new StringFormat(StringFormat.GenericDefault);
				format.Trimming = StringTrimming.EllipsisCharacter;

				SizeF textSize = e.Graphics.MeasureString(this.Name, e.Renderer.FontRegular, rectTrackName.Height, format);

				var state = e.Graphics.Save();
				e.Graphics.TranslateTransform(
					rectTrackName.X + (int)textSize.Height + 2, 
					rectTrackName.Y);
				e.Graphics.RotateTransform(90);
				e.Graphics.DrawString(
					this.Name, 
					e.Renderer.FontRegular, 
					new SolidBrush(Color.FromArgb((int)(textOverlapAlpha * 255), e.Renderer.ColorText)), 
					new Rectangle(0, 0, rectTrackName.Height, rectTrackName.Width), 
					format);
				e.Graphics.Restore(state);
			}

			// Draw vertical unit markings
			{
				Pen bigLinePen = new Pen(new SolidBrush(e.Renderer.ColorRulerMarkMajor));
				Pen medLinePen = new Pen(new SolidBrush(e.Renderer.ColorRulerMarkRegular));
				Pen minLinePen = new Pen(new SolidBrush(e.Renderer.ColorRulerMarkMinor));

				// Static Top and Bottom marks
				SizeF textSize;
				textSize = e.Graphics.MeasureString(verticalTopText, e.Renderer.FontRegular);
				e.Graphics.DrawString(verticalTopText, e.Renderer.FontRegular, new SolidBrush(e.Renderer.ColorText), rectUnitMarkings.Right - textSize.Width, rectUnitMarkings.Top);
				textSize = e.Graphics.MeasureString(verticalBottomText, e.Renderer.FontRegular);
				e.Graphics.DrawString(verticalBottomText, e.Renderer.FontRegular, new SolidBrush(e.Renderer.ColorText), rectUnitMarkings.Right - textSize.Width, rectUnitMarkings.Bottom - textSize.Height);

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

					int borderDistInner = e.Renderer.FontRegular.Height / 2;
					int borderDistOuter = e.Renderer.FontRegular.Height / 2 + 15;
					float markTopX = rectUnitMarkings.Right - markLen * rectUnitMarkings.Width;
					float markBottomX = rectUnitMarkings.Right;
					float borderDist = (float)Math.Min(Math.Abs(mark.PixelValue - rect.Top), Math.Abs(mark.PixelValue - rect.Bottom));

					if (borderDist > borderDistInner)
					{
						float alpha = Math.Min(1.0f, (float)(borderDist - borderDistInner) / (float)(borderDistOuter - borderDistInner));
						Color markColor = Color.FromArgb((int)(alpha * markPen.Color.A), markPen.Color);
						Color textColor = Color.FromArgb((int)(alpha * markPen.Color.A), e.Renderer.ColorText);

						e.Graphics.DrawLine(new Pen(markColor), (int)markTopX, (int)mark.PixelValue, (int)markBottomX, (int)mark.PixelValue);

						if (bigMark)
						{
							string text = string.Format("{0}", (float)Math.Round(mark.UnitValue, 2));
							textSize = e.Graphics.MeasureString(text, e.Renderer.FontRegular);
							e.Graphics.DrawString(
								text, 
								e.Renderer.FontRegular, 
								new SolidBrush(textColor), 
								markTopX - textSize.Width, 
								mark.PixelValue - textSize.Height * 0.5f);
						}
					}
				}
			}
		}
		protected internal override void OnPaintRightSidebar(TimelineViewTrackPaintEventArgs e)
		{
			base.OnPaintRightSidebar(e);

			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.CornflowerBlue)), e.TargetRect);
			e.Graphics.DrawRectangle(new Pen(Color.FromArgb(64, Color.Black)), 
				e.TargetRect.X,
				e.TargetRect.Y,
				e.TargetRect.Width - 1,
				e.TargetRect.Height - 1);
			e.Graphics.DrawRectangle(new Pen(Color.FromArgb(64, Color.White)), 
				e.TargetRect.X + 1,
				e.TargetRect.Y + 1,
				e.TargetRect.Width - 3,
				e.TargetRect.Height - 3);

			StringFormat format = StringFormat.GenericDefault.Clone() as StringFormat;
			format.Alignment = StringAlignment.Center;
			format.LineAlignment = StringAlignment.Center;
			e.Graphics.DrawString("RightSidebar", e.Renderer.FontRegular, new SolidBrush(e.Renderer.ColorText), e.TargetRect, format);
		}
	}
}
