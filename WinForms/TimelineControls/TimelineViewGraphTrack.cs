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
				e.Graphics.TranslateTransform(rectTrackName.X + (int)textSize.Height + 2, rectTrackName.Y + rectTrackName.Height / 2 - textSize.Width / 2);
				e.Graphics.RotateTransform(90);
				e.Graphics.DrawString(this.Name, e.Renderer.FontRegular, new SolidBrush(Color.FromArgb((int)(textOverlapAlpha * 255), e.Renderer.ColorText)), new Rectangle(0, 0, rectTrackName.Height, rectTrackName.Width), format);
				e.Graphics.Restore(state);
			}

			// Draw vertical unit markings
			{
				Pen bigLinePen = new Pen(new SolidBrush(e.Renderer.ColorText));
				Pen medLinePen = new Pen(new SolidBrush(Color.FromArgb(128, e.Renderer.ColorText)));
				Pen minLinePen = new Pen(new SolidBrush(Color.FromArgb(64, e.Renderer.ColorText)));

				// Top mark
				{
					SizeF textSize = e.Graphics.MeasureString(verticalTopText, e.Renderer.FontRegular);
					e.Graphics.DrawString(verticalTopText, e.Renderer.FontRegular, new SolidBrush(e.Renderer.ColorText), rectUnitMarkings.Right - textSize.Width, rectUnitMarkings.Top);
					e.Graphics.DrawLine(bigLinePen, rectUnitMarkings.Left, rectUnitMarkings.Top, rectUnitMarkings.Right, rectUnitMarkings.Top);
				}
				// Bottom mark
				{
					SizeF textSize = e.Graphics.MeasureString(verticalBottomText, e.Renderer.FontRegular);
					e.Graphics.DrawString(verticalBottomText, e.Renderer.FontRegular, new SolidBrush(e.Renderer.ColorText), rectUnitMarkings.Right - textSize.Width, rectUnitMarkings.Bottom - textSize.Height);
					e.Graphics.DrawLine(bigLinePen, rectUnitMarkings.Left, rectUnitMarkings.Bottom - 1, rectUnitMarkings.Right, rectUnitMarkings.Bottom - 1);
				}

				// Inbetween marks
				{
					const float BigMarkDelta = 0.00001f;
					float bigMarkRange = TimelineView.GetNiceMultiple(Math.Abs(this.verticalUnitTop - this.verticalUnitBottom) * 0.5f);
					float rulerStep = -TimelineView.GetNiceMultiple(Math.Abs(this.verticalUnitTop - this.verticalUnitBottom)) / 10.0f;

					float beginUnit = this.verticalUnitTop;
					float endUnit = this.verticalUnitBottom;
					float unitValue;
					float maxUnit;
					TimelineView.GetRulerRange(rulerStep, 0.0f, beginUnit, endUnit, out unitValue, out maxUnit);

					int lineIndex = 0;
					while (unitValue * Math.Sign(rulerStep) < maxUnit * Math.Sign(rulerStep))
					{
						float markY = this.GetPosAtUnit(unitValue) + rect.Y;

						float markLen;
						Pen markPen;
						bool bigMark;
						if ((((unitValue + BigMarkDelta) % bigMarkRange) + bigMarkRange) % bigMarkRange <= BigMarkDelta * 2.0f)
						{
							markLen = 0.5f;
							markPen = medLinePen;
							bigMark = true;
						}
						else
						{
							markLen = 0.25f;
							markPen = minLinePen;
							bigMark = false;
						}

						if (Math.Abs(unitValue - this.verticalUnitTop) >= rulerStep && Math.Abs(unitValue - this.verticalUnitBottom) >= rulerStep)
						{
							int borderDistInner = e.Renderer.FontRegular.Height / 2;
							int borderDistOuter = e.Renderer.FontRegular.Height / 2 + 15;
							float markTopX = rectUnitMarkings.Right - markLen * rectUnitMarkings.Width;
							float markBottomX = rectUnitMarkings.Right;
							float borderDist = (float)Math.Min(Math.Abs(markY - rect.Top), Math.Abs(markY - rect.Bottom));

							if (borderDist > borderDistInner)
							{
								float alpha = Math.Min(1.0f, (float)(borderDist - borderDistInner) / (float)(borderDistOuter - borderDistInner));
								Color markColor = Color.FromArgb((int)(alpha * markPen.Color.A), markPen.Color);

								e.Graphics.DrawLine(new Pen(markColor), markTopX, markY, markBottomX, markY);

								if (bigMark)
								{
									string text = string.Format("{0}", (float)Math.Round(unitValue, 2));
									SizeF textSize = e.Graphics.MeasureString(text, e.Renderer.FontRegular);
									e.Graphics.DrawString(
										text, 
										e.Renderer.FontRegular, 
										new SolidBrush(markColor), 
										markTopX - textSize.Width, 
										markY - textSize.Height * 0.5f);
								}
							}
						}

						unitValue += rulerStep;
						lineIndex++;
					}
				}
			}
		}
	}
}
