using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AdamsLair.WinForms.NativeWinAPI
{
	public enum WindowMessage : int
	{
		GetText				= 0x000D,
		GetTextLength		= 0x000E,
		Paint				= 0x000F,
		EraseBackground		= 0x0014,
		HorizontalScroll	= 0x0114,
		VerticalScroll		= 0x0115
	}
	[Flags]
	public enum ExtendedWindowStyles
	{
		Composited	= 0x02000000,
	}
	public enum ScrollBarCommands : int
	{
		LineUp			= 0,
		LineLeft		= 0,
		LineDown		= 1,
		LineRight		= 1,
		PageUp			= 2,
		PageLeft		= 2,
		PageDown		= 3,
		PageRight		= 3,
		ThumbPosition	= 4,
		ThumbTrack		= 5,
		Top				= 6,
		Left			= 6,
		Bottom			= 7,
		Right			= 7,
		EndScroll		= 8,
	}
}
