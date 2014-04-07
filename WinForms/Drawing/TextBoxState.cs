using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using AdamsLair.WinForms.Properties;

namespace AdamsLair.WinForms.Drawing
{
	[Flags]
	public enum TextBoxState
	{
		Disabled		= 0x1,
		Normal			= 0x2,
		Hot				= 0x4,
		Focus			= 0x8,

		ReadOnlyFlag	= 0x100
	}
}
