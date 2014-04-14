using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;

using AdamsLair.WinForms.Drawing;
using AdamsLair.WinForms.ItemModels;

namespace AdamsLair.WinForms.ItemViews
{
	public class TiledViewTextItemEditor : TextBox, ITiledViewItemEditor
	{
		private string editedPropertyName = null;
		private bool accepted = false;

		public event EventHandler StopEditing = null;

		public string EditedPropertyName
		{
			get { return this.editedPropertyName; }
			set { this.editedPropertyName = value; }
		}
		Control ITiledViewItemEditor.MainControl
		{
			get { return this; }
		}
		bool ITiledViewItemEditor.IsAcceptingValue
		{
			get { return this.accepted; }
		}

		void ITiledViewItemEditor.GetValueFromItem(object item)
		{
			if (string.IsNullOrEmpty(this.editedPropertyName)) return;
			if (item == null) return;

			Type itemType = item.GetType();
			PropertyInfo property = itemType.GetProperty(this.editedPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property == null) return;
			if (property.PropertyType != typeof(string)) return;

			string val = property.GetValue(item, null) as string;
			this.Text = val;
		}
		bool ITiledViewItemEditor.ApplyValueToItem(object item)
		{
			if (string.IsNullOrEmpty(this.editedPropertyName)) return true;
			if (item == null) return true;

			Type itemType = item.GetType();
			PropertyInfo property = itemType.GetProperty(this.editedPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property == null) return true;
			if (property.PropertyType != typeof(string)) return true;

			property.SetValue(item, this.Text, null);
			return true;
		}

		private void Accept()
		{
			this.accepted = true;
			if (this.StopEditing != null) this.StopEditing(this, EventArgs.Empty);
		}
		private void Reject()
		{
			this.accepted = false;
			if (this.StopEditing != null) this.StopEditing(this, EventArgs.Empty);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.Escape)
				this.Reject();
			else if (e.KeyCode == Keys.Return)
				this.Accept();
		}
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			this.Accept();
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			this.Reject();
		}
	}
}
