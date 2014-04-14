using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using AdamsLair.WinForms.NativeWinAPI;

namespace AdamsLair.WinForms
{
	internal static class ExtMethodsMessage
	{
		public static WindowMessage GetWindowMessage(this Message msg)
		{
			return (WindowMessage)msg.Msg;
		}
	}
}
