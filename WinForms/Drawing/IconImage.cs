using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using AdamsLair.WinForms.Properties;

namespace AdamsLair.WinForms.Drawing
{
	public class IconImage
	{
		private	Image		sourceImage	= null;
		private	Bitmap[]	images		= new Bitmap[4];
		
		public Image SourceImage
		{
			get { return this.sourceImage; }
		}
		public Image Passive
		{
			get { return this.images[0]; }
		}
		public Image Normal
		{
			get { return this.images[1]; }
		}
		public Image Active
		{
			get { return this.images[2]; }
		}
		public Image Disabled
		{
			get { return this.images[3]; }
		}

		public int Width
		{
			get { return this.sourceImage.Width; }
		}
		public int Height
		{
			get { return this.sourceImage.Height; }
		}
		public Size Size
		{
			get { return this.sourceImage.Size; }
		}

		public IconImage(Image source)
		{
			this.sourceImage = source;

			// Generate specific images
			var imgAttribs = new System.Drawing.Imaging.ImageAttributes();
			System.Drawing.Imaging.ColorMatrix colorMatrix = null;
			{
				colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][] {
					new float[] {1.0f, 0.0f, 0.0f, 0.0f, 0.0f},
					new float[] {0.0f, 1.0f, 0.0f, 0.0f, 0.0f},
					new float[] {0.0f, 0.0f, 1.0f, 0.0f, 0.0f},
					new float[] {0.0f, 0.0f, 0.0f, 0.65f, 0.0f},
					new float[] {0.0f, 0.0f, 0.0f, 0.0f, 1.0f}});
				imgAttribs.SetColorMatrix(colorMatrix);
				this.images[0] = new Bitmap(source.Width, source.Height);
				using (Graphics g = Graphics.FromImage(this.images[0]))
				{
					g.DrawImage(source, 
						new Rectangle(Point.Empty, source.Size), 
						0, 0, source.Width, source.Height, GraphicsUnit.Pixel, 
						imgAttribs);
				}
			}
			{
				colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][] {
					new float[] {1.0f, 0.0f, 0.0f, 0.0f, 0.0f},
					new float[] {0.0f, 1.0f, 0.0f, 0.0f, 0.0f},
					new float[] {0.0f, 0.0f, 1.0f, 0.0f, 0.0f},
					new float[] {0.0f, 0.0f, 0.0f, 1.0f, 0.0f},
					new float[] {0.0f, 0.0f, 0.0f, 0.0f, 1.0f}});
				imgAttribs.SetColorMatrix(colorMatrix);
				this.images[1] = new Bitmap(source.Width, source.Height);
				using (Graphics g = Graphics.FromImage(this.images[1]))
				{
					g.DrawImage(source, 
						new Rectangle(Point.Empty, source.Size), 
						0, 0, source.Width, source.Height, GraphicsUnit.Pixel, 
						imgAttribs);
				}
			}
			{
				colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][] {
					new float[] {1.3f, 0.0f, 0.0f, 0.0f, 0.0f},
					new float[] {0.0f, 1.3f, 0.0f, 0.0f, 0.0f},
					new float[] {0.0f, 0.0f, 1.3f, 0.0f, 0.0f},
					new float[] {0.0f, 0.0f, 0.0f, 1.0f, 0.0f},
					new float[] {0.0f, 0.0f, 0.0f, 0.0f, 1.0f}});
				imgAttribs.SetColorMatrix(colorMatrix);
				this.images[2] = new Bitmap(source.Width, source.Height);
				using (Graphics g = Graphics.FromImage(this.images[2]))
				{
					g.DrawImage(source, 
						new Rectangle(Point.Empty, source.Size), 
						0, 0, source.Width, source.Height, GraphicsUnit.Pixel, 
						imgAttribs);
				}
			}
			{
				colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][] {
					new float[] {0.34f, 0.34f, 0.34f, 0.0f, 0.0f},
					new float[] {0.34f, 0.34f, 0.34f, 0.0f, 0.0f},
					new float[] {0.34f, 0.34f, 0.34f, 0.0f, 0.0f},
					new float[] {0.0f, 0.0f, 0.0f, 0.5f, 0.0f},
					new float[] {0.0f, 0.0f, 0.0f, 0.0f, 1.0f}});
				imgAttribs.SetColorMatrix(colorMatrix);
				this.images[3] = new Bitmap(source.Width, source.Height);
				using (Graphics g = Graphics.FromImage(this.images[3]))
				{
					g.DrawImage(source, 
						new Rectangle(Point.Empty, source.Size), 
						0, 0, source.Width, source.Height, GraphicsUnit.Pixel, 
						imgAttribs);
				}
			}
		}
	}
}
