using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.PropertyEditing
{
	public abstract class GroupedPropertyEditor : PropertyEditor
	{
		public enum GroupHeaderStyle
		{
			Flat,
			Simple,
			Emboss,
			SmoothSunken
		}

		public const int	DefaultIndent		= 15;
		public const int	DefaultHeaderHeight	= 18;

		private	int						headerHeight	= DefaultHeaderHeight;
		private	int						indent			= DefaultIndent;
		private	bool					expanded		= false;
		private	bool					active			= false;
		private	bool					contentInit		= false;
		private	List<PropertyEditor>	propertyEditors	= new List<PropertyEditor>();
		private	PropertyEditor			hoverEditor		= null;
		private	bool					hoverEditorLock	= false;
		private	string					headerValueText	= null;
		private	IconImage				headerIcon		= null;
		private	Color?					headerColor		= null;
		private	GroupHeaderStyle		headerStyle		= GroupHeaderStyle.Flat;
		private	Rectangle				headerRect		= Rectangle.Empty;
		private	Rectangle				expandCheckRect		= Rectangle.Empty;
		private	bool					expandCheckHovered	= false;
		private	bool					expandCheckPressed	= false;
		private	Rectangle				activeCheckRect		= Rectangle.Empty;
		private	bool					activeCheckHovered	= false;
		private	bool					activeCheckPressed	= false;
		private	Size					sizeBeforeUpdate	= Size.Empty;

		public event EventHandler<PropertyEditorEventArgs>	EditorAdded;
		public event EventHandler<PropertyEditorEventArgs>	EditorRemoving;
		public event EventHandler							ActiveChanged;
		

		public bool Expanded
		{
			get { return this.expanded; }
			set 
			{ 
				if (this.expanded != value)
				{
					this.expanded = value;
					if (this.ParentGrid != null)
					{
						this.Invalidate();
						if (this.expanded && !this.contentInit)
							this.InitContent();
						else
							this.UpdateChildGeometry();
					}
				}
			}
		}
		public bool Active
		{
			get { return this.active; }
			set 
			{ 
				if (this.active != value)
				{
					this.active = value;
					this.Invalidate();
					this.OnActiveChanged();
				}
			}
		}
		public int Indent
		{
			get { return this.indent; }
			set 
			{
				if (this.indent != value)
				{
					this.indent = value;
					this.UpdateChildGeometry();
					this.Invalidate();
				}
			}
		}
		public int HeaderHeight
		{
			get { return this.headerHeight; }
			set
			{
				if (this.headerHeight != value)
				{
					this.headerHeight = value;
					this.UpdateChildGeometry();
				}
			}
		}
		public Image HeaderIcon
		{
			get { return this.headerIcon.SourceImage; }
			set
			{
				if (this.headerIcon == null || this.headerIcon.SourceImage != value)
				{
					this.headerIcon = value != null ? new IconImage(value) : null;
					this.Invalidate(this.headerRect);
				}
			}
		}
		public Color HeaderColor
		{
			get { return this.headerColor.Value; }
			set
			{
				this.headerColor = value;
				this.Invalidate(this.headerRect);
			}
		}
		public GroupHeaderStyle HeaderStyle
		{
			get { return this.headerStyle; }
			set
			{
				if (this.headerStyle != value)
				{
					this.headerStyle = value;
					this.Invalidate(this.headerRect);
				}
			}
		}
		public string HeaderValueText
		{
			get { return this.headerValueText; }
			set
			{
				if (this.headerValueText != value)
				{
					this.headerValueText = value;
					this.Invalidate(this.headerRect);
				}
			}
		}
		public bool ContentInitialized
		{
			get { return this.contentInit; }
		}
		public bool CanExpand
		{
			get { return !this.contentInit || this.propertyEditors.Count > 0; }
		}
		public override IEnumerable<PropertyEditor> Children
		{
			get { return this.expanded ? this.propertyEditors : base.Children; }
		}
		public override bool FocusOnClick
		{
			get { return false; }
		}
		protected bool UseIndentChildExpand
		{
			get { return this.indent > ControlRenderer.ExpandNodeSize.Width + 1; }
		}
		protected bool ParentUseIndentChildExpand
		{
			get { return (this.ParentEditor as GroupedPropertyEditor) != null && (this.ParentEditor as GroupedPropertyEditor).UseIndentChildExpand; }
		}
		protected PropertyEditor HoverEditor
		{
			get { return this.hoverEditor; }
		}


		public GroupedPropertyEditor()
		{
			this.Hints &= (~HintFlags.HasPropertyName);
			this.Hints |= HintFlags.HasExpandCheck | HintFlags.ExpandEnabled;

			this.ClearContent();
		}
		protected override void OnDisposing(bool manually)
		{
			base.OnDisposing(manually);
			foreach (PropertyEditor child in this.propertyEditors)
				child.Dispose();
		}

		public virtual void InitContent()
		{
			this.contentInit = true;
		}
		public virtual void ClearContent()
		{
			this.contentInit = false;
			this.ClearPropertyEditors();
		}
		public override bool BeginUpdate()
		{
			if (base.BeginUpdate())
			{
				this.sizeBeforeUpdate = this.Size;
				return true;
			}
			return false;
		}
		public override bool EndUpdate()
		{
			if (base.EndUpdate())
			{
				if (this.Size != this.sizeBeforeUpdate)
					this.OnSizeChanged();
			}
			return false;
		}

		protected override void OnSetValue() {}

		public PropertyEditor PickEditorAt(int x, int y, bool ownChildrenOnly = false)
		{
			Rectangle indentClientRect = this.ClientRectangle;
			indentClientRect.X += this.indent;
			indentClientRect.Width -= this.indent;
			if (this.expanded && indentClientRect.Contains(new Point(x, y)))
			{
				foreach (PropertyEditor child in this.propertyEditors)
				{
					if (child.EditorRectangle.Contains(x, y))
					{
						GroupedPropertyEditor groupedEditor = child as GroupedPropertyEditor;
						if (groupedEditor != null && !ownChildrenOnly)
							return groupedEditor.PickEditorAt(x, y);
						else
							return child;
					}
				}
			}

			return this;
		}

		protected bool HasPropertyEditor(PropertyEditor editor)
		{
			return this.propertyEditors.Contains(editor);
		}
		protected void AddPropertyEditor(PropertyEditor editor, PropertyEditor before)
		{
			int atIndex = -1;
			if (before != null && before.ParentEditor == this)
			{
				int index = 0;
				foreach (PropertyEditor child in this.Children)
				{
					if (child == before)
					{
						atIndex = index;
						break;
					}
					++index;
				}
			}
			this.AddPropertyEditor(editor, atIndex);
		}
		protected void AddPropertyEditor(PropertyEditor editor, int atIndex = -1)
		{
			if (this.propertyEditors.Contains(editor)) this.propertyEditors.Remove(editor);

			editor.ParentEditor = this;

			if (atIndex == -1)
				this.propertyEditors.Add(editor);
			else
				this.propertyEditors.Insert(atIndex, editor);
			
			GroupedPropertyEditor groupedEditor = editor as GroupedPropertyEditor;
			if (groupedEditor != null && groupedEditor.Expanded && !groupedEditor.ContentInitialized)
				groupedEditor.InitContent();

			this.OnEditorAdded(editor);
			this.UpdateChildGeometry();

			editor.ValueChanged += this.OnValueChanged;
			editor.EditingFinished += this.OnEditingFinished;
			editor.SizeChanged += this.child_SizeChanged;
		}
		protected void RemovePropertyEditor(PropertyEditor editor)
		{
			editor.ParentEditor = null;
			editor.ValueChanged -= this.OnValueChanged;
			editor.EditingFinished -= this.OnEditingFinished;
			editor.SizeChanged -= this.child_SizeChanged;

			this.OnEditorRemoving(editor);
			this.propertyEditors.Remove(editor);
			this.UpdateChildGeometry();
		}
		protected void ClearPropertyEditors()
		{
			foreach (PropertyEditor e in this.propertyEditors)
			{
				e.ParentEditor = null;
				e.ValueChanged -= this.OnValueChanged;
				e.EditingFinished -= this.OnEditingFinished;
				e.SizeChanged -= this.child_SizeChanged;
				this.OnEditorRemoving(e);
			}
			this.propertyEditors.Clear();
			this.UpdateChildGeometry();
		}
		protected void UpdateChildGeometry()
		{
			int height = this.headerHeight;
			int y = this.Location.Y + height;
			foreach (PropertyEditor e in this.propertyEditors)
			{
				e.Location = new Point(this.ClientRectangle.X + this.indent, y);
				e.Width = this.ClientRectangle.Width - this.indent;
				y += e.Height;
				if (this.expanded)
					height += e.Height;
			}
			this.Height = height;
		}
		protected override void UpdateGeometry()
		{
			base.UpdateGeometry();

			Rectangle clientRect = this.ClientRectangle;
			Rectangle buttonRect = this.ButtonRectangle;

			clientRect.Width += buttonRect.Width;
			buttonRect.Height = Math.Min(buttonRect.Height, this.headerHeight);
			buttonRect.Width = Math.Min(buttonRect.Width, this.headerHeight);
			buttonRect.X = this.EditorRectangle.Right - buttonRect.Width - 1;
			buttonRect.Y = this.Location.Y + this.headerHeight / 2 - buttonRect.Height / 2;

			this.ClientRectangle = clientRect;
			this.ButtonRectangle = buttonRect;

			this.headerRect = new Rectangle(this.ClientRectangle.X, this.ClientRectangle.Y, this.ClientRectangle.Width, this.headerHeight);
			bool parentExpand = (this.ParentEditor as GroupedPropertyEditor) != null && (this.ParentEditor as GroupedPropertyEditor).UseIndentChildExpand;
			if (!parentExpand && (this.Hints & HintFlags.HasExpandCheck) != HintFlags.None)
			{
				this.expandCheckRect = new Rectangle(
					this.headerRect.X + 2,
					this.headerRect.Y + this.headerRect.Height / 2 - ControlRenderer.CheckBoxSize.Height / 2 - 1,
					ControlRenderer.CheckBoxSize.Width,
					ControlRenderer.CheckBoxSize.Height);
			}
			else
			{
				this.expandCheckRect = new Rectangle(this.headerRect.X, this.headerRect.Y, 0, 0);
			}

			if ((this.Hints & HintFlags.HasActiveCheck) != HintFlags.None)
			{
				this.activeCheckRect = new Rectangle(
					this.expandCheckRect.Right + 2,
					this.headerRect.Y + this.headerRect.Height / 2 - ControlRenderer.CheckBoxSize.Height / 2 - 1,
					ControlRenderer.CheckBoxSize.Width,
					ControlRenderer.CheckBoxSize.Height);
			}
			else
			{
				this.activeCheckRect = new Rectangle(this.expandCheckRect.Right, this.expandCheckRect.Y, 0, 0);
			}

			this.UpdateChildGeometry();
		}
		
		protected virtual void OnEditorAdded(PropertyEditor e)
		{
			if (this.EditorAdded != null)
				this.EditorAdded(this, new PropertyEditorEventArgs(e));
		}
		protected virtual void OnEditorRemoving(PropertyEditor e)
		{
			if (this.EditorRemoving != null)
				this.EditorRemoving(this, new PropertyEditorEventArgs(e));
		}

		internal protected override void OnReadOnlyChanged()
		{
			base.OnReadOnlyChanged();

			foreach (PropertyEditor e in this.propertyEditors)
				e.OnReadOnlyChanged();
		}

		protected void PaintHeader(Graphics g)
		{
			if (this.headerHeight == 0) return;
			Rectangle buttonRect = this.ButtonRectangle;

			CheckBoxState activeState = CheckBoxState.UncheckedDisabled;
			ExpandBoxState expandState = ExpandBoxState.ExpandDisabled;
			bool parentExpand = this.ParentUseIndentChildExpand;
			if (!parentExpand && (this.Hints & HintFlags.HasExpandCheck) != HintFlags.None)
			{
				if (this.Enabled && this.CanExpand && (this.Hints & HintFlags.ExpandEnabled) != HintFlags.None)
				{
					if (!this.Expanded)
					{
						if (this.expandCheckPressed)		expandState = ExpandBoxState.ExpandPressed;
						else if (this.expandCheckHovered)	expandState = ExpandBoxState.ExpandHot;
						else								expandState = ExpandBoxState.ExpandNormal;
					}
					else
					{
						if (this.expandCheckPressed)		expandState = ExpandBoxState.CollapsePressed;
						else if (this.expandCheckHovered)	expandState = ExpandBoxState.CollapseHot;
						else								expandState = ExpandBoxState.CollapseNormal;
					}
				}
				else
				{
					if (this.Expanded)	expandState = ExpandBoxState.ExpandDisabled;
					else				expandState = ExpandBoxState.CollapseDisabled;
				}
			}
			if ((this.Hints & HintFlags.HasActiveCheck) != HintFlags.None)
			{
				if (this.Enabled && !this.ReadOnly && (this.Hints & HintFlags.ActiveEnabled) != HintFlags.None)
				{
					if (this.Active)
					{
						if (this.activeCheckPressed)		activeState = CheckBoxState.CheckedPressed;
						else if (this.activeCheckHovered)	activeState = CheckBoxState.CheckedHot;
						else								activeState = CheckBoxState.CheckedNormal;
					}
					else
					{
						if (this.activeCheckPressed)		activeState = CheckBoxState.UncheckedPressed;
						else if (this.activeCheckHovered)	activeState = CheckBoxState.UncheckedHot;
						else								activeState = CheckBoxState.UncheckedNormal;
					}
				}
				else
				{
					if (this.Active)	activeState = CheckBoxState.CheckedDisabled;
					else				activeState = CheckBoxState.UncheckedDisabled;
				}
			}

			Rectangle iconRect;
			if (this.headerIcon != null)
			{
				iconRect = new Rectangle(
					this.activeCheckRect.Right + 2,
					this.headerRect.Y + this.headerRect.Height / 2 - this.headerIcon.Height / 2, 
					this.headerIcon.Width,
					this.headerIcon.Height);
			}
			else
			{
				iconRect = new Rectangle(this.activeCheckRect.Right, this.headerRect.Y, 0, 0);
			}
			Rectangle textRect = new Rectangle(
				iconRect.Right, 
				this.headerRect.Y, 
				this.headerRect.Width - buttonRect.Width - iconRect.Width - this.expandCheckRect.Width - this.activeCheckRect.Width - 2, 
				this.headerRect.Height);
			Rectangle nameTextRect;
			Rectangle valueTextRect;
			if (!string.IsNullOrEmpty(this.PropertyName) && !string.IsNullOrEmpty(this.headerValueText))
			{
				int nameWidth;
				nameWidth = this.NameLabelWidth - (textRect.X - this.headerRect.X);
				nameTextRect = new Rectangle(textRect.X, textRect.Y, nameWidth, textRect.Height);
				valueTextRect = new Rectangle(textRect.X + nameWidth, textRect.Y, textRect.Width - nameWidth, textRect.Height);
			}
			else if (!string.IsNullOrEmpty(this.headerValueText))
			{
				nameTextRect = new Rectangle(textRect.X, textRect.Y, 0, 0);
				valueTextRect = textRect;
			}
			else
			{
				nameTextRect = textRect;
				valueTextRect = new Rectangle(textRect.X, textRect.Y, 0, 0);
			}


			bool focusBg = this.Focused || (this is IPopupControlHost && (this as IPopupControlHost).IsDropDownOpened);
			bool focusBgColor = this.headerStyle == GroupHeaderStyle.Flat || this.headerStyle == GroupHeaderStyle.Simple;
			Color headerBgColor = this.headerColor.Value;
			if (focusBg && focusBgColor) headerBgColor = headerBgColor.ScaleBrightness(this.ControlRenderer.FocusBrightnessScale);
			GroupedPropertyEditor.DrawGroupHeaderBackground(g, this.headerRect, headerBgColor, this.headerStyle);
			if (focusBg && !focusBgColor)
			{
				this.ControlRenderer.DrawBorder(g, this.headerRect, Drawing.BorderStyle.Simple, BorderState.Normal);
			}
			
			if (!parentExpand && (this.Hints & HintFlags.HasExpandCheck) != HintFlags.None)
				this.ControlRenderer.DrawExpandBox(g, this.expandCheckRect.Location, expandState);
			if ((this.Hints & HintFlags.HasActiveCheck) != HintFlags.None)
				this.ControlRenderer.DrawCheckBox(g, this.activeCheckRect.Location, activeState);

			if (this.headerIcon != null)
				g.DrawImage(this.Enabled ? this.headerIcon.Normal : this.headerIcon.Disabled, iconRect);

			this.ControlRenderer.DrawStringLine(g, 
				this.PropertyName, 
				this.ValueModified ? this.ControlRenderer.FontBold : this.ControlRenderer.FontRegular, 
				nameTextRect, 
				this.Enabled && !this.NonPublic ? this.ControlRenderer.ColorText : this.ControlRenderer.ColorGrayText);
			this.ControlRenderer.DrawStringLine(g, 
				this.headerValueText, 
				this.ValueModified ? this.ControlRenderer.FontBold : this.ControlRenderer.FontRegular, 
				valueTextRect, 
				this.Enabled ? this.ControlRenderer.ColorText : this.ControlRenderer.ColorGrayText);
		}
		protected void PaintIndentExpandButton(Graphics g, GroupedPropertyEditor childGroup, int curY)
		{
			if (childGroup.headerHeight == 0) return;
			if ((childGroup.Hints & HintFlags.HasExpandCheck) == HintFlags.None) return;

			Rectangle indentExpandRect = new Rectangle(childGroup.Location.X - this.indent, childGroup.Location.Y, this.indent, childGroup.headerHeight);
			Rectangle expandButtonRect = new Rectangle(
				indentExpandRect.X + indentExpandRect.Width / 2 - ControlRenderer.ExpandNodeSize.Width / 2,
				indentExpandRect.Y + indentExpandRect.Height / 2 - ControlRenderer.ExpandNodeSize.Height / 2 - 1,
				ControlRenderer.ExpandNodeSize.Width,
				ControlRenderer.ExpandNodeSize.Height);
			ExpandNodeState expandState = ExpandNodeState.OpenedDisabled;
			if (childGroup.Enabled && childGroup.CanExpand && (childGroup.Hints & HintFlags.ExpandEnabled) != HintFlags.None)
			{
				if (!childGroup.Expanded)
				{
					if (childGroup.expandCheckPressed)		expandState = ExpandNodeState.ClosedPressed;
					else if (childGroup.expandCheckHovered)	expandState = ExpandNodeState.ClosedHot;
					else if (childGroup.Focused)			expandState = ExpandNodeState.ClosedHot;
					else									expandState = ExpandNodeState.ClosedNormal;
				}
				else
				{
					if (childGroup.expandCheckPressed)		expandState = ExpandNodeState.OpenedPressed;
					else if (childGroup.expandCheckHovered)	expandState = ExpandNodeState.OpenedHot;
					else if (childGroup.Focused)			expandState = ExpandNodeState.OpenedHot;
					else									expandState = ExpandNodeState.OpenedNormal;
				}
			}
			else
			{
				if (childGroup.Expanded)	expandState = ExpandNodeState.OpenedDisabled;
				else						expandState = ExpandNodeState.ClosedDisabled;
			}

			ControlRenderer.DrawExpandNode(g, expandButtonRect.Location, expandState);
		}
		protected internal override void OnPaint(PaintEventArgs e)
		{
			// Paint background and name label
			this.PaintBackground(e.Graphics);
			this.PaintNameLabel(e.Graphics);

			// Paint header
			this.PaintHeader(e.Graphics);

			// Paint right button
			this.PaintButton(e.Graphics);
			
			// Paint children
			if (this.expanded)
			{
				Rectangle clipRectBase = new Rectangle(
					(int)e.Graphics.ClipBounds.X,
					(int)e.Graphics.ClipBounds.Y,
					(int)e.Graphics.ClipBounds.Width,
					(int)e.Graphics.ClipBounds.Height);
				foreach (PropertyEditor child in this.propertyEditors)
				{
					if (clipRectBase.IntersectsWith(new Rectangle(
						child.Location.X - this.indent, 
						child.Location.Y,
						child.Width, 
						child.Height)))
					{
						// Paint child editor
						GraphicsState oldState = e.Graphics.Save();
						{
							Rectangle clipRect = child.EditorRectangle;
							clipRect.Intersect(this.ClientRectangle);
							clipRect.Intersect(clipRectBase);
							e.Graphics.SetClip(clipRect);
							child.OnPaint(e);
						}
						e.Graphics.Restore(oldState);

						// Paint child groups expand button
						if (child is GroupedPropertyEditor && this.UseIndentChildExpand)
							this.PaintIndentExpandButton(e.Graphics, child as GroupedPropertyEditor, child.Location.Y);
					}
				}
			}
		}

		protected void IndentChildExpandOnMouseMove(MouseEventArgs e, GroupedPropertyEditor childGroup, int curY)
		{
			if (childGroup == null) return;
			Rectangle expandRect = new Rectangle(childGroup.Location.X - this.indent, childGroup.Location.Y, this.indent, childGroup.headerHeight);
			bool lastExpandHovered = childGroup.expandCheckHovered;

			childGroup.expandCheckHovered = 
				childGroup.CanExpand && 
				(childGroup.Hints & HintFlags.ExpandEnabled) != HintFlags.None && 
				expandRect.Contains(e.Location);

			if (lastExpandHovered != childGroup.expandCheckHovered) this.Invalidate(expandRect);
		}
		protected void IndentChildExpandOnMouseLeave(EventArgs e, GroupedPropertyEditor childGroup, int curY)
		{
			if (childGroup == null) return;
			Rectangle expandRect = new Rectangle(childGroup.Location.X - this.indent, childGroup.Location.Y, this.indent, childGroup.headerHeight);

			if (childGroup.expandCheckHovered) this.Invalidate(expandRect);
			childGroup.expandCheckHovered = false;
			childGroup.expandCheckPressed = false;
		}
		protected bool IndentChildExpandOnMouseDown(MouseEventArgs e, GroupedPropertyEditor childGroup, int curY)
		{
			if (childGroup == null) return false;

			if (childGroup.expandCheckHovered && (e.Button & MouseButtons.Left) != MouseButtons.None)
			{
				Rectangle expandRect = new Rectangle(childGroup.Location.X - this.indent, childGroup.Location.Y, this.indent, childGroup.headerHeight);
				childGroup.expandCheckPressed = true;
				this.Invalidate(expandRect);
				childGroup.OnExpandCheckPressed();
				return true;
			}

			return false;
		}
		protected void IndentChildExpandOnMouseUp(MouseEventArgs e, GroupedPropertyEditor childGroup, int curY)
		{
			if (childGroup == null) return;
			
			if (childGroup.expandCheckPressed && (e.Button & MouseButtons.Left) != MouseButtons.None)
			{
				Rectangle expandRect = new Rectangle(childGroup.Location.X - this.indent, childGroup.Location.Y, this.indent, childGroup.headerHeight);
				childGroup.expandCheckPressed = false;
				this.Invalidate(expandRect);
			}
		}

		protected void UpdateHoverEditor(MouseEventArgs e)
		{
			PropertyEditor lastHoverEditor = this.hoverEditor;
			this.hoverEditor = this.PickEditorAt(e.X, e.Y, true);
			if (this.hoverEditor == this) this.hoverEditor = null;

			if (lastHoverEditor != this.hoverEditor && lastHoverEditor != null)
				lastHoverEditor.OnMouseLeave(EventArgs.Empty);
			if (lastHoverEditor != this.hoverEditor && this.hoverEditor != null)
			{
				// Indent expand button
				if (this.UseIndentChildExpand)
				{
					int curY = this.headerHeight;
					foreach (PropertyEditor child in this.propertyEditors)
					{
						this.IndentChildExpandOnMouseLeave(EventArgs.Empty, child as GroupedPropertyEditor, curY);
						curY += child.Height;
					}
				}

				this.hoverEditor.OnMouseEnter(EventArgs.Empty);
			}
		}
		protected internal override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			PropertyEditor lastHoverEditor = this.hoverEditor;
			
			if (!this.hoverEditorLock) this.UpdateHoverEditor(e);

			if (this.hoverEditor != null)
			{
				this.hoverEditor.OnMouseMove(e);
			}
			else
			{
				bool lastExpandHovered = this.expandCheckHovered;
				bool lastActiveHovered = this.activeCheckHovered;
				Rectangle expandHotSpot = new Rectangle(this.expandCheckRect.X, this.headerRect.Y, this.expandCheckRect.Width, this.headerRect.Height);
				Rectangle activeHotSpot = new Rectangle(this.activeCheckRect.X, this.headerRect.Y, this.activeCheckRect.Width, this.headerRect.Height);
				this.expandCheckHovered = this.CanExpand && (this.Hints & HintFlags.ExpandEnabled) != HintFlags.None && expandHotSpot.Contains(e.Location);
				this.activeCheckHovered = !this.ReadOnly && (this.Hints & HintFlags.ActiveEnabled) != HintFlags.None && activeHotSpot.Contains(e.Location);
				if (lastExpandHovered != this.expandCheckHovered) this.Invalidate();
				if (lastActiveHovered != this.activeCheckHovered) this.Invalidate();

				// Indent expand button
				if (this.UseIndentChildExpand)
				{
					int curY = this.headerHeight;
					foreach (PropertyEditor child in this.propertyEditors)
					{
						this.IndentChildExpandOnMouseMove(e, child as GroupedPropertyEditor, curY);
						curY += child.Height;
					}
				}
			}
		}
		protected internal override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			if (this.hoverEditor != null)
			{
				PropertyEditor lastHoverEditor = this.hoverEditor;
				this.hoverEditor = null;

				if (lastHoverEditor != this.hoverEditor && lastHoverEditor != null)
					lastHoverEditor.OnMouseLeave(EventArgs.Empty);
			}

			if (this.expandCheckHovered) this.Invalidate();
			if (this.activeCheckHovered) this.Invalidate();
			this.expandCheckHovered = false;
			this.expandCheckPressed = false;
			this.activeCheckHovered = false;
			this.activeCheckPressed = false;
			
			// Indent expand button
			if (this.UseIndentChildExpand)
			{
				int curY = this.headerHeight;
				foreach (PropertyEditor child in this.propertyEditors)
				{
					this.IndentChildExpandOnMouseLeave(e, child as GroupedPropertyEditor, curY);
					curY += child.Height;
				}
			}
		}
		protected internal override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			this.hoverEditorLock = Control.MouseButtons != MouseButtons.None;
			if (this.hoverEditor != null)
			{
				this.hoverEditor.OnMouseDown(e);
			}
			else
			{
				// Indent expand button
				bool handled = false;
				if (this.UseIndentChildExpand)
				{
					int curY = this.headerHeight;
					foreach (PropertyEditor child in this.propertyEditors)
					{
						handled = handled || this.IndentChildExpandOnMouseDown(e, child as GroupedPropertyEditor, curY);
						curY += child.Height;
					}
				}

				if (!handled)
				{
					if (this.EditorRectangle.Contains(e.Location))
						this.Focus();

					if (this.expandCheckHovered && (e.Button & MouseButtons.Left) != MouseButtons.None)
					{
						this.expandCheckPressed = true;
						this.Invalidate();
						this.OnExpandCheckPressed();
					}
					else if (this.activeCheckHovered && (e.Button & MouseButtons.Left) != MouseButtons.None)
					{
						this.activeCheckPressed = true;
						this.Invalidate();
						this.OnActiveCheckPressed();
					}
				}
			}
		}
		protected internal override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			this.hoverEditorLock = Control.MouseButtons != MouseButtons.None;
			if (this.hoverEditor != null)
			{
				this.hoverEditor.OnMouseUp(e);
			}
			else
			{
				if (this.expandCheckPressed && (e.Button & MouseButtons.Left) != MouseButtons.None)
				{
					this.expandCheckPressed = false;
					this.Invalidate();
				}
				else if (this.activeCheckPressed && (e.Button & MouseButtons.Left) != MouseButtons.None)
				{
					this.activeCheckPressed = false;
					this.Invalidate();
				}

				// Indent expand button
				if (this.UseIndentChildExpand)
				{
					int curY = this.headerHeight;
					foreach (PropertyEditor child in this.propertyEditors)
					{
						this.IndentChildExpandOnMouseUp(e, child as GroupedPropertyEditor, curY);
						curY += child.Height;
					}
				}
			}
		}
		protected internal override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (this.hoverEditor != null)
			{
				this.hoverEditor.OnMouseClick(e);
			}
		}
		protected internal override void OnMouseDoubleClick(MouseEventArgs e)
		{
			base.OnMouseDoubleClick(e);
			if (this.hoverEditor != null)
			{
				this.hoverEditor.OnMouseDoubleClick(e);
			}
			else if ( // Double-Click expand, if we're certain it's not handled elsewhere
				this.CanExpand && 
				(this.Hints & HintFlags.ExpandEnabled) != HintFlags.None && 
				(this.Hints & HintFlags.HasExpandCheck) != HintFlags.None && 
				!this.expandCheckHovered && 
				!this.activeCheckHovered && 
				this.headerRect.Contains(e.Location) && 
				!this.ButtonRectangle.Contains(e.Location))
			{
				this.Invalidate();
				this.OnExpandCheckPressed();
			}
		}

		protected internal override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (this.Focused && e.KeyCode == Keys.Return)
			{
				if (this.CanExpand && 
					(this.Hints & HintFlags.ExpandEnabled) != HintFlags.None &&
					(this.Hints & HintFlags.HasExpandCheck) != HintFlags.None)
				{
					this.OnExpandCheckPressed();

					// Indent expand button
					if (this.ParentEditor != null && 
						this.ParentEditor is GroupedPropertyEditor && 
						(this.ParentEditor as GroupedPropertyEditor).UseIndentChildExpand)
						this.ParentEditor.Invalidate();
				}
				e.Handled = true;
			}
		}

		protected internal override void OnDragEnter(DragEventArgs e)
		{
			base.OnDragEnter(e);
			if (this.hoverEditorLock)
			{
				this.hoverEditorLock = false;
				this.hoverEditor = null;
			}
			e.Effect = e.AllowedEffect; // Accept anything to pass it on to children
		}
		protected internal override void OnDragLeave(EventArgs e)
		{
			base.OnDragLeave(e);
			if (this.hoverEditor != null)
			{
				this.hoverEditor.OnDragLeave(e);
				this.hoverEditor = null;
			}
		}
		protected internal override void OnDragOver(DragEventArgs e)
		{
			base.OnDragOver(e);
			PropertyEditor lastHoverEditor = this.hoverEditor;
			
			this.hoverEditor = this.PickEditorAt(e.X, e.Y, true);
			if (this.hoverEditor == this) this.hoverEditor = null;

			if (lastHoverEditor != this.hoverEditor && lastHoverEditor != null)
				lastHoverEditor.OnDragLeave(EventArgs.Empty);
			if (lastHoverEditor != this.hoverEditor && this.hoverEditor != null)
			{
				e.Effect = DragDropEffects.None;
				this.hoverEditor.OnDragEnter(e);
			}

			if (this.hoverEditor != null)
			{
				this.hoverEditor.OnDragOver(e);
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}
		protected internal override void OnDragDrop(DragEventArgs e)
		{
			base.OnDragDrop(e);
			if (this.hoverEditor != null)
			{
				this.hoverEditor.OnDragDrop(e);
			}
		}

		protected internal override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			if (this.ParentUseIndentChildExpand) this.ParentEditor.Invalidate();
		}
		protected internal override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			if (this.ParentUseIndentChildExpand) this.ParentEditor.Invalidate();
		}

		protected override void OnParentEditorChanged()
		{
			base.OnParentEditorChanged();
			if (!this.headerColor.HasValue) this.headerColor = ControlRenderer.ColorBackground;
			if (this.HeaderHeight == DefaultHeaderHeight)
				this.HeaderHeight = 5 + (int)Math.Round((float)this.ControlRenderer.FontRegular.Height);
			if (this.expanded && !this.contentInit)
				this.InitContent();
		}
		protected override void OnSizeChanged()
		{
			if (this.IsUpdating) return;
			if (this.Disposed) return;
			base.OnSizeChanged();
			this.UpdateChildGeometry();
		}
		protected void OnExpandCheckPressed()
		{
			this.Expanded = !this.Expanded;
		}
		protected void OnActiveCheckPressed()
		{
			if (this.ReadOnly) return;
			this.Active = !this.Active;
		}
		protected virtual void OnActiveChanged()
		{
			if (this.ActiveChanged != null)
				this.ActiveChanged(this, EventArgs.Empty);
		}

		private void child_SizeChanged(object sender, EventArgs e)
		{
			this.UpdateChildGeometry();
			this.Invalidate();
		}
		
		public static void DrawGroupHeaderBackground(Graphics g, Rectangle rect, Color baseColor, GroupHeaderStyle style)
		{
			if (rect.Height == 0 || rect.Width == 0) return;
			Color lightColor = baseColor.ScaleBrightness(style == GroupHeaderStyle.SmoothSunken ? 0.85f : 1.1f);
			Color darkColor = baseColor.ScaleBrightness(style == GroupHeaderStyle.SmoothSunken ? 0.95f : 0.85f);
			LinearGradientBrush gradientBrush = new LinearGradientBrush(rect, lightColor, darkColor, 90.0f);

			if (style != GroupHeaderStyle.Simple && style != GroupHeaderStyle.Flat)
				g.FillRectangle(gradientBrush, rect);
			else
				g.FillRectangle(new SolidBrush(baseColor), rect);

			if (style == GroupHeaderStyle.Flat) return;

			g.DrawLine(new Pen(Color.FromArgb(128, Color.White)), rect.Left, rect.Top, rect.Right, rect.Top);
			g.DrawLine(new Pen(Color.FromArgb(64, Color.Black)), rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);

			g.DrawLine(new Pen(Color.FromArgb(64, Color.White)), rect.Left, rect.Top, rect.Left, rect.Bottom - 1);
			g.DrawLine(new Pen(Color.FromArgb(32, Color.Black)), rect.Right, rect.Top, rect.Right, rect.Bottom - 1);
		}
	}
}
