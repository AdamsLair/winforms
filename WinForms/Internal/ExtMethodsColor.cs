using System;
using System.Drawing;

namespace AdamsLair.WinForms
{
	internal static class ExtMethodsColor
	{
		public static Color ScaleBrightness(this Color c, float ratio)
		{
			return Color.FromArgb(c.A,
				(byte)Math.Min(Math.Max((float)c.R * ratio, 0.0f), 255.0f),
				(byte)Math.Min(Math.Max((float)c.G * ratio, 0.0f), 255.0f),
				(byte)Math.Min(Math.Max((float)c.B * ratio, 0.0f), 255.0f));
		}
		public static Color ScaleAlpha(this Color c, float ratio)
		{
			return Color.FromArgb((byte)((float)c.A * ratio), c);
		}
		public static Color MixWith(this Color c, Color other, float ratio, bool lockBrightness = false)
		{
			float myRatio = 1.0f - ratio;
			if (lockBrightness)
			{
				int oldBrightness = Math.Max(c.R, Math.Max(c.G, c.B));
				int newBrightness = Math.Max(other.R, Math.Max(other.G, other.B));
				other = other.ScaleBrightness((float)oldBrightness / (float)newBrightness);
			}
			return Color.FromArgb(c.A,
				(byte)Math.Min(Math.Max((float)c.R * myRatio + (float)other.R * ratio, 0.0f), 255.0f),
				(byte)Math.Min(Math.Max((float)c.G * myRatio + (float)other.G * ratio, 0.0f), 255.0f),
				(byte)Math.Min(Math.Max((float)c.B * myRatio + (float)other.B * ratio, 0.0f), 255.0f));
		}

		public static float GetLuminance(this Color color)
		{
			return (0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B) / 255.0f;
		}
		public static float GetHSVHue(this Color color)
		{
			return color.GetHue() / 360.0f;
		}
		public static float GetHSVBrightness(this Color color)
		{
			return Math.Max(Math.Max(color.R, color.G), color.B) / 255.0f;
		}
		public static float GetHSVSaturation(this Color color)
		{
			int max = Math.Max(color.R, Math.Max(color.G, color.B));
			int min = Math.Min(color.R, Math.Min(color.G, color.B));

			return (max == 0) ? 0.0f : 1.0f - (1.0f * (float)min / (float)max);
		}

		public static Color ColorFromHSV(float hue, float saturation, float value)
		{
			hue *= 360.0f;
			hue = (hue + 360.0f) % 360.0f;
			saturation = Math.Min(Math.Max(saturation, 0.0f), 1.0f);
			value = Math.Min(Math.Max(value, 0.0f), 1.0f);

			int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
			double f = hue / 60 - Math.Floor(hue / 60);

			value = value * 255;
			int v = Convert.ToInt32(value);
			int p = Convert.ToInt32(value * (1 - saturation));
			int q = Convert.ToInt32(value * (1 - f * saturation));
			int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

			if (hi == 0)
				return Color.FromArgb(255, v, t, p);
			else if (hi == 1)
				return Color.FromArgb(255, q, v, p);
			else if (hi == 2)
				return Color.FromArgb(255, p, v, t);
			else if (hi == 3)
				return Color.FromArgb(255, p, q, v);
			else if (hi == 4)
				return Color.FromArgb(255, t, p, v);
			else
				return Color.FromArgb(255, v, p, q);
		}
	}
}
