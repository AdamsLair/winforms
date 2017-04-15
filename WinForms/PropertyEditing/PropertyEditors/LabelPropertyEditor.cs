using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

using AdamsLair.WinForms.Drawing;
using AdamsLair.WinForms.PropertyEditing.Templates;

namespace AdamsLair.WinForms.PropertyEditing.Editors
{
	public class LabelPropertyEditor : PropertyEditor
	{
		private	StringEditorTemplate	selectableLabel	= null;
		private string	val				= null;
		private	bool	valMultiple		= false;

		public override object DisplayedValue
		{
			get { return Convert.ChangeType(this.val, this.EditedType); }
		}
		

		public LabelPropertyEditor()
		{
			this.selectableLabel = new StringEditorTemplate(this);
			this.selectableLabel.ReadOnly = true;
			this.selectableLabel.Invalidate += this.stringEditor_Invalidate;
		}
		protected override void OnParentEditorChanged()
		{
			base.OnParentEditorChanged();
			this.Height = 5 + (int)Math.Round((float)this.ControlRenderer.FontRegular.Height);
		}

		protected override void OnGetValue()
		{
			base.OnGetValue();
			this.BeginUpdate();
			object[] values = this.GetValue().ToArray();

			// Apply values to editors
			if (!values.Any())
			{
				this.val = null;
			}
			else
			{
				object rawValue = values.First();
				this.valMultiple = false;
				this.val = this.GetLabelFor(rawValue, ref this.valMultiple);
				this.valMultiple = this.valMultiple || values.Any(o => o == null || this.GetLabelFor(o, ref this.valMultiple) != this.val);
			}

			this.selectableLabel.Text = this.val;
			this.EndUpdate();
		}

		private string GetLabelFor(object rawValue, ref bool highlight)
		{
			if (rawValue is string)
			{
				return rawValue as string;
			}
			else if (rawValue != null)
			{
				try
				{
					return rawValue.ToString();
				}
				catch (Exception)
				{
					highlight = true;
					return string.Empty;
				}
			}
			else
			{
				highlight = true;
				return string.Empty;
			}
		}

		protected internal override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			this.selectableLabel.OnPaint(e, this.Enabled, this.valMultiple);
		}
		protected internal override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			this.selectableLabel.OnGotFocus(e);
			this.selectableLabel.Select();
		}
		protected internal override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			this.selectableLabel.OnLostFocus(e);
		}
		protected internal override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);
			this.selectableLabel.OnKeyPress(e);
		}
		protected internal override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			this.selectableLabel.OnKeyDown(e);
		}
		protected internal override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			this.selectableLabel.OnMouseMove(e);
		}
		protected internal override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			this.selectableLabel.OnMouseLeave(e);
		}
		protected internal override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			this.selectableLabel.OnMouseDown(e);
		}
		protected internal override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			this.selectableLabel.OnMouseUp(e);
		}
		protected internal override void OnMouseDoubleClick(MouseEventArgs e)
		{
			base.OnMouseDoubleClick(e);
			this.selectableLabel.OnMouseDoubleClick(e);
		}

		protected override void UpdateGeometry()
		{
			base.UpdateGeometry();
			this.selectableLabel.Rect = new Rectangle(
				this.ClientRectangle.X + 1,
				this.ClientRectangle.Y + 1,
				this.ClientRectangle.Width - 2,
				this.ClientRectangle.Height - 1);
		}

		private void stringEditor_Invalidate(object sender, EventArgs e)
		{
			this.Invalidate();
		}
	}
}
