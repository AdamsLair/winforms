using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

using AdamsLair.WinForms.Drawing;
using AdamsLair.WinForms.ItemModels;

namespace AdamsLair.WinForms.ItemViews
{
	public class TiledView : Panel
	{
		public enum SelectMode
		{
			None,
			Single,
			Multi
		}
		public enum HorizontalAlignment
		{
			Left,
			Right,
			Center
		}

		private struct SelectedItem
		{
			public int ModelIndex;
			public object Item;

			public SelectedItem(int modelIndex, object obj)
			{
				this.ModelIndex = modelIndex;
				this.Item = obj;
			}
		}

		private	ControlRenderer			renderer			= new ControlRenderer();
		private	IListModel				model				= new EmptyListModel();
		private	Size					contentSize			= Size.Empty;
		private	Size					spacing				= new Size(2, 2);
		private	Size					tileSize			= new Size(50, 50);
		private	SelectMode				userSelectMode		= SelectMode.Multi;
		private	HorizontalAlignment		rowAlignment		= HorizontalAlignment.Center;
		private	bool					allowUserItemEdit	= false;
		private	bool					highlightHoverItems	= true;
		private	int						additionalSpace		= 0;
		private	int						tilesPerRow			= 0;
		private	int						rowCount			= 0;
		private	int						shiftIndex			= -1;
		private	int						hoverIndex			= -1;
		private	List<SelectedItem>		selection			= new List<SelectedItem>();
		private	StringFormat			itemStringFormat	= StringFormat.GenericDefault.Clone() as StringFormat;
		private	Point					mouseDownLoc		= Point.Empty;
		private	int						dragIndex			= -1;
		private	int						itemEditIndex		= -1;
		private	string					itemEditProperty	= "Name";
		private	SelectedItem			editedItem			= new SelectedItem(-1, null);
		private	ITiledViewItemEditor	itemEditor			= null;

		private TiledViewItemAppearanceEventArgs cachedEventItemAppearance = null;
		private TiledViewItemDrawEventArgs cachedEventItemUserPaint = null;


		public event EventHandler SelectionChanged = null;
		public event EventHandler<TiledViewItemDrawEventArgs> ItemUserPaint = null;
		public event EventHandler<TiledViewItemMouseEventArgs> ItemClicked = null;
		public event EventHandler<TiledViewItemMouseEventArgs> ItemDoubleClicked = null;
		public event EventHandler<TiledViewItemMouseEventArgs> ItemDrag = null;
		public event EventHandler<TiledViewItemEventArgs> ItemEdited = null;
		public event EventHandler<TiledViewItemAppearanceEventArgs> ItemAppearance = null;


		public IListModel Model
		{
			get { return this.model; }
			set
			{
				if (this.model != value)
				{
					int oldCount = this.model.Count;

					this.model.CountChanged -= this.model_CountChanged;
					this.model.IndicesChanged -= this.model_IndicesChanged;

					this.model = value ?? new EmptyListModel();
					
					this.OnModelIndicesChanged(0, oldCount);
					this.OnModelCountChanged();

					this.model.CountChanged += this.model_CountChanged;
					this.model.IndicesChanged += this.model_IndicesChanged;
				}
			}
		}
		public ControlRenderer ControlRenderer
		{
			get { return this.renderer; }
		}
		[DefaultValue(SelectMode.Multi)]
		public SelectMode UserSelectMode
		{
			get { return this.userSelectMode; }
			set { this.userSelectMode = value; }
		}
		[DefaultValue(true)]
		public bool HightlightHoverItems
		{
			get { return this.highlightHoverItems; }
			set { this.highlightHoverItems = value; }
		}
		[DefaultValue(false)]
		public bool AllowUserItemEditing
		{
			get { return this.allowUserItemEdit; }
			set { this.allowUserItemEdit = value; }
		}
		[DefaultValue(typeof(Size), "50, 50")]
		public Size TileSize
		{
			get { return this.tileSize; }
			set
			{
				if (value.Width < 16) value.Width = 16;
				if (value.Height < 16) value.Height = 16;
				this.tileSize = value;
				this.UpdateContentStats();
				this.Invalidate();
			}
		}
		[DefaultValue(typeof(Size), "2, 2")]
		public Size Spacing
		{
			get { return this.spacing; }
			set
			{
				this.spacing = value;
				this.UpdateContentStats();
				this.Invalidate();
			}
		}
		public int TilesPerRow
		{
			get { return this.tilesPerRow; }
		}
		public int RowCount
		{
			get { return this.rowCount; }
		}
		[DefaultValue(HorizontalAlignment.Left)]
		public HorizontalAlignment RowAlignment
		{
			get { return this.rowAlignment; }
			set
			{
				if (this.rowAlignment != value)
				{
					this.rowAlignment = value;
					this.Invalidate();
				}
			}
		}
		public bool IsEditing
		{
			get { return this.itemEditor != null; }
		}
		public object EditedModelItem
		{
			get { return this.editedItem.Item; }
		}
		public object HighlightModelItem
		{
			get { return this.hoverIndex != -1 ? this.model.GetItemAt(this.hoverIndex) : null; }
			set
			{
				int index = this.model.GetIndexOf(value);
				if (this.hoverIndex != index)
				{
					if (this.hoverIndex != -1) this.InvalidateModelIndices(this.hoverIndex, 1);
					this.hoverIndex = index;
					if (index != -1) this.InvalidateModelIndices(index, 1);
				}
			}
		}
		public IEnumerable SelectedModelItems
		{
			get { return this.selection.Select(i => i.Item); }
			set
			{
				this.OnSelectionChanging();
				this.selection.Clear();
				if (value != null)
				{
					this.selection.AddRange(value
						.OfType<object>()
						.Select(v => new SelectedItem(this.model.GetIndexOf(v), v))
						.Where(e => e.ModelIndex != -1));
				}
				this.OnSelectionChanged();
			}
		}
		public string ModelItemEditProperty
		{
			get { return this.itemEditProperty; }
			set { this.itemEditProperty = value; }
		}


		public TiledView()
		{
			this.SetStyle(ControlStyles.Selectable, true);
			this.TabStop = true;

			this.AutoScroll = true;

			this.SetStyle(ControlStyles.ResizeRedraw, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.Opaque, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			this.itemStringFormat.Alignment = StringAlignment.Center;
			this.itemStringFormat.LineAlignment = StringAlignment.Center;
			this.itemStringFormat.Trimming = StringTrimming.EllipsisCharacter;

			this.UpdateContentStats();
		}

		public void ClearSelection()
		{
			this.OnSelectionChanging();
			this.selection.Clear();
			this.OnSelectionChanged();
		}
		public void SelectItem(object item, bool clear = true)
		{
			this.OnSelectionChanging();
			if (clear) this.selection.Clear();
			int modelIndex = this.model.GetIndexOf(item);
			if (modelIndex >= 0 && modelIndex < this.model.Count)
			{
				SelectedItem selected = new SelectedItem(modelIndex, item);
				this.selection.Add(selected);
			}
			this.OnSelectionChanged();
		}
		public void SelectItem(int modelIndex, bool clear = true)
		{
			this.OnSelectionChanging();
			if (clear) this.selection.Clear();
			if (modelIndex >= 0 && modelIndex < this.model.Count)
			{
				SelectedItem selected = new SelectedItem(modelIndex, this.model.GetItemAt(modelIndex));
				this.selection.Add(selected);
			}
			this.OnSelectionChanged();
		}
		public void SelectItemRange(int modelIndex, int count, bool clear = true)
		{
			if (modelIndex < 0) throw new ArgumentException("modelIndex");
			if (modelIndex + count > this.model.Count) throw new ArgumentException("count");

			this.OnSelectionChanging();
			if (clear) this.selection.Clear();
			for (int i = modelIndex; i < modelIndex + count; i++)
			{
				SelectedItem selected = new SelectedItem(i, this.model.GetItemAt(i));
				this.selection.Add(selected);
			}
			this.OnSelectionChanged();
		}
		public void DeselectItem(object item)
		{
			this.OnSelectionChanging();
			this.selection.RemoveAll(s => s.Item == item);
			this.OnSelectionChanged();
		}
		public void DeselectItem(int modelIndex)
		{
			this.OnSelectionChanging();
			this.selection.RemoveAll(s => s.ModelIndex == modelIndex);
			this.OnSelectionChanged();
		}
		public bool IsItemSelected(object item)
		{
			return this.selection.Any(s => s.Item == item);
		}
		public bool IsItemSelected(int modelIndex)
		{
			return this.selection.Any(s => s.ModelIndex == modelIndex);
		}

		public bool BeginEdit(object item)
		{
			int modelIndex = this.model.GetIndexOf(item);
			return this.BeginEdit(modelIndex);
		}
		public virtual bool BeginEdit(int modelIndex)
		{
			if (modelIndex == -1) return false;
			if (this.itemEditor != null) return false;
			Point editorPos = this.GetModelIndexLocation(modelIndex);

			this.editedItem = new SelectedItem(modelIndex, this.model.GetItemAt(modelIndex));
			this.itemEditor = this.CreateItemEditor(this.editedItem.ModelIndex, this.editedItem.Item, editorPos);
			if (this.itemEditor != null && this.itemEditor.MainControl != null)
			{
				this.itemEditor.GetValueFromItem(this.editedItem.Item);
				this.itemEditor.StopEditing += this.itemEditor_StopEditing;

				this.Controls.Add(this.itemEditor.MainControl);
				this.itemEditor.MainControl.Focus();
			}
			else
			{
				this.editedItem = new SelectedItem(-1, null);
				this.itemEditor = null;
				return false;
			}

			this.itemEditIndex = -1;
			this.InvalidateModelIndices(modelIndex, 1);
			return true;
		}
		public virtual bool EndEdit(bool apply)
		{
			if (this.itemEditor == null) return true;

			if (apply)
			{
				if (!this.itemEditor.ApplyValueToItem(this.editedItem.Item))
					return false;
				else
					this.OnItemEdited(this.editedItem.ModelIndex, this.editedItem.Item);
			}

			this.itemEditor.StopEditing -= this.itemEditor_StopEditing;
			this.DestroyItemEditor(this.itemEditor);

			this.Controls.Remove(this.itemEditor.MainControl);
			this.itemEditor.MainControl.Dispose();
			this.itemEditor = null;

			this.InvalidateModelIndices(this.editedItem.ModelIndex, 1);
			this.editedItem = new SelectedItem(-1, null);
			this.Focus();

			return true;
		}

		public void InvalidateModelIndices(int index, int count)
		{
			if (index < 0)
			{
				count += index;
				index = 0;
			}
			if (count <= 0) return;
			Rectangle itemRect = this.GetEnclosingRect(index, count);
			this.Invalidate(itemRect);
		}
		public int PickModelIndexAt(int x, int y, bool scrolled = true, bool allowNearest = false)
		{
			if (scrolled)
			{
				x -= this.AutoScrollPosition.X;
				y -= this.AutoScrollPosition.Y;
			}

			x -= this.ClientRectangle.X + this.Padding.Left - this.spacing.Width / 2;
			y -= this.ClientRectangle.Y + this.Padding.Top - this.spacing.Height / 2;

			switch (this.rowAlignment)
			{
				default:
				case HorizontalAlignment.Left:
					break;
				case HorizontalAlignment.Right:
					x -= this.additionalSpace;
					break;
				case HorizontalAlignment.Center:
					x -= this.additionalSpace / 2;
					break;
			}

			if (allowNearest)
			{
				if (x < 0) x = 0;
				if (y < 0) y = 0;
				if (x >= this.contentSize.Width) x = this.contentSize.Width - 1;
				if (y >= this.contentSize.Height) y = this.contentSize.Height - 1;
			}
			else
			{
				if (x < 0) return -1;
				if (y < 0) return -1;
				if (x >= this.contentSize.Width) return -1;
				if (y >= this.contentSize.Height) return -1;
			}

			int rowIndex = y / (this.tileSize.Height + this.spacing.Height);
			int colIndex = x / (this.tileSize.Width + this.spacing.Width);
			int modelIndex = rowIndex * this.tilesPerRow + colIndex;

			if (allowNearest)
			{
				if (modelIndex < 0) modelIndex = 0;
				if (modelIndex >= this.model.Count) modelIndex = this.model.Count - 1;
			}
			else
			{
				if (modelIndex < 0) modelIndex = -1;
				if (modelIndex >= this.model.Count) modelIndex = -1;
			}

			return modelIndex;
		}
		public Point GetModelIndexLocation(int modelIndex, bool scrolled = true)
		{
			Point result = this.ClientRectangle.Location;
			result.X += this.Padding.Left;
			result.Y += this.Padding.Top;

			switch (this.rowAlignment)
			{
				default:
				case HorizontalAlignment.Left:
					break;
				case HorizontalAlignment.Right:
					result.X += this.additionalSpace;
					break;
				case HorizontalAlignment.Center:
					result.X += this.additionalSpace / 2;
					break;
			}

			int rowIndex = modelIndex / this.tilesPerRow;
			int colIndex = modelIndex % this.tilesPerRow;
			result.X += colIndex * (this.tileSize.Width + this.spacing.Width);
			result.Y += rowIndex * (this.tileSize.Height + this.spacing.Height);

			if (scrolled)
			{
				result.X += this.AutoScrollPosition.X;
				result.Y += this.AutoScrollPosition.Y;
			}
			return result;
		}
		public Rectangle GetEnclosingRect(int modelIndex, int itemCount, bool scrolled = true)
		{
			Point first = this.GetModelIndexLocation(modelIndex, scrolled);
			if (itemCount == 1)
			{
				return new Rectangle(
					first.X,
					first.Y,
					this.tileSize.Width + 1,
					this.tileSize.Height + 1);
			}

			Point last = this.GetModelIndexLocation(modelIndex + itemCount - 1, scrolled);
			if (last.Y != first.Y)
			{
				return new Rectangle(
					0,
					first.Y,
					this.ClientSize.Width + 1,
					last.Y - first.Y + this.tileSize.Height + this.spacing.Width + 1);
			}
			else
			{
				return new Rectangle(
					first.X,
					first.Y,
					last.X - first.X + this.tileSize.Width + this.spacing.Width + 1,
					this.tileSize.Height + this.spacing.Height + 1);
			}
		}
		public void ScrollToModelIndex(int modelIndex)
		{
			Point scrolledPos = this.GetModelIndexLocation(modelIndex);
			if (scrolledPos.Y >= this.Padding.Top && scrolledPos.Y + this.tileSize.Height <= this.ClientSize.Height - this.Padding.Bottom)
				return;
			
			if (scrolledPos.Y < this.Padding.Top)
			{
				this.AutoScrollPosition = new Point(-this.AutoScrollPosition.X, -this.AutoScrollPosition.Y - (this.Padding.Top - scrolledPos.Y));
			}
			else if (scrolledPos.Y + this.tileSize.Height > this.ClientSize.Height - this.Padding.Bottom)
			{
				this.AutoScrollPosition = new Point(-this.AutoScrollPosition.X, -this.AutoScrollPosition.Y + (scrolledPos.Y + this.tileSize.Height) - (this.ClientSize.Height - this.Padding.Bottom));
			}
		}

		/// <summary>
		/// Override this method to allow editing items in the <see cref="TiledView"/> using the provided editor.
		/// </summary>
		/// <param name="modelIndex"></param>
		/// <param name="item"></param>
		/// <param name="editorPos"></param>
		/// <returns></returns>
		protected virtual ITiledViewItemEditor CreateItemEditor(int modelIndex, object item, Point editorPos)
		{
			string text;
			Image icon;
			this.GetItemAppearance(modelIndex, item, out text, out icon);
			
			TiledViewTextItemEditor editor = new TiledViewTextItemEditor();
			editor.EditedPropertyName = this.itemEditProperty;
			editor.BorderStyle = System.Windows.Forms.BorderStyle.None;

			bool styledEditor = this.BackColor != SystemColors.Control;
			int iconOffset = this.tileSize.Height - editor.PreferredHeight;
			if (icon == null) iconOffset /= 2;

			editor.Location = new Point(editorPos.X, editorPos.Y + iconOffset);
			editor.Width = this.tileSize.Width;
			editor.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			if (styledEditor)
			{
				editor.BackColor = this.BackColor;
				editor.ForeColor = this.ForeColor;
			}

			return editor;
		}
		/// <summary>
		/// Override this method to clean up after editing an item in the <see cref="TiledView"/>.
		/// </summary>
		/// <param name="editor"></param>
		protected virtual void DestroyItemEditor(ITiledViewItemEditor editor) {}
		private void itemEditor_StopEditing(object sender, EventArgs e)
		{
			this.EndEdit(this.itemEditor.IsAcceptingValue);
		}

		protected void UpdateContentStats()
		{
			Rectangle contentArea = new Rectangle(
				this.ClientRectangle.X + this.Padding.Left,
				this.ClientRectangle.Y + this.Padding.Top,
				this.ClientRectangle.Width - this.Padding.Horizontal,
				this.ClientRectangle.Height - this.Padding.Vertical);

			this.tilesPerRow = Math.Max(1, (contentArea.Width + this.spacing.Width) / (this.tileSize.Width + this.spacing.Width));
			this.rowCount = (int)Math.Ceiling((float)this.model.Count / (float)this.tilesPerRow);
			{
				int hTiles = Math.Min(this.model.Count, this.tilesPerRow);
				this.contentSize = new Size(
					hTiles * this.tileSize.Width + (hTiles - 1) * this.spacing.Width, 
					this.rowCount * this.tileSize.Height + (this.rowCount - 1) * this.spacing.Height);
			}
			{
				int lastAdditionalSpace = this.additionalSpace;
				this.additionalSpace = contentArea.Width - this.contentSize.Width;
				if (this.additionalSpace != lastAdditionalSpace) this.Invalidate();
			}

			Size autoScrollSize;
			if (contentArea.Width - 1 > this.tileSize.Width)
				autoScrollSize = new Size(0, this.contentSize.Height);
			else
				autoScrollSize = this.contentSize;
			autoScrollSize.Width += this.Padding.Horizontal;
			autoScrollSize.Height += this.Padding.Vertical;

			if (this.AutoScrollMinSize != autoScrollSize)
				this.AutoScrollMinSize = autoScrollSize;
		}
		protected void UpdateSelectionIndices(int index = 0, int count = -1)
		{
			if (count < 0) count = this.model.Count;

			// Check currently edited item
			if (this.editedItem.ModelIndex != -1 && !IsSelectedItemIndexValid(this.editedItem))
			{
				this.EndEdit(false);
			}

			// Iterate over all selected items and check whether their indices are still correct
			List<int> removeIndices = null;
			for (int i = 0; i < this.selection.Count; i++)
			{
				SelectedItem selected = this.selection[i];

				// Skip unaffected items
				if (selected.ModelIndex < index) continue;
				if (selected.ModelIndex >= index + count) continue;

				// Check whether the model index can be valid at all, and if it actually still matches its element
				if (!IsSelectedItemIndexValid(selected))
				{
					// If it doesn't match, retrieve the new index for the given element
					selected.ModelIndex = this.model.GetIndexOf(selected.Item);
					if (selected.ModelIndex == -1)
					{
						// If the new index is invalid, remove the element from the selection completely
						if (removeIndices == null) removeIndices = new List<int>();
						removeIndices.Add(i);
					}
				}
			}

			// Remove previously scheduled elements from the selection
			if (removeIndices != null)
			{
				this.OnSelectionChanging();
				for (int i = removeIndices.Count - 1; i >= 0; i--)
				{
					this.selection.RemoveAt(removeIndices[i]);
				}
				this.OnSelectionChanged();
			}
		}
		protected void ProcessUserItemClick(int modelIndex, bool nonDestructive = false)
		{
			if (this.userSelectMode == SelectMode.None) return;
			if (nonDestructive && this.IsItemSelected(modelIndex)) return;

			if (modelIndex == -1)
			{
				if (!nonDestructive) this.ClearSelection();
				return;
			}

			if (this.userSelectMode == SelectMode.Multi && ModifierKeys.HasFlag(Keys.Shift))
			{
				if (this.shiftIndex == -1)
				{
					this.SelectItem(modelIndex, false);
				}
				else
				{
					int first = Math.Min(this.shiftIndex, modelIndex);
					int last = Math.Max(this.shiftIndex, modelIndex);
					this.SelectItemRange(first, 1 + last - first);
				}
			}
			else if (this.userSelectMode == SelectMode.Multi && ModifierKeys.HasFlag(Keys.Control))
			{
				this.shiftIndex = modelIndex;
				if (this.IsItemSelected(modelIndex))
					this.DeselectItem(modelIndex);
				else
					this.SelectItem(modelIndex, false);
			}
			else
			{
				this.shiftIndex = modelIndex;
				this.SelectItem(modelIndex);
			}

			this.ScrollToModelIndex(modelIndex);
		}
		private bool IsSelectedItemIndexValid(SelectedItem selected)
		{
			bool isValid = selected.ModelIndex < this.model.Count;
			bool isEqual = isValid && object.Equals(this.model.GetItemAt(selected.ModelIndex), selected.Item);
			return isEqual;
		}
		
		protected virtual void OnSelectionChanging()
		{
			if (this.selection.Count > 0)
			{
				int selectionFirst = this.selection.Min(i => i.ModelIndex);
				int selectionLast = this.selection.Max(i => i.ModelIndex);
				this.InvalidateModelIndices(selectionFirst, 1 + selectionLast - selectionFirst);
			}
		}
		protected virtual void OnSelectionChanged()
		{
			if (this.SelectionChanged != null)
				this.SelectionChanged(this, EventArgs.Empty);
			
			if (this.selection.Count > 0)
			{
				int selectionFirst = this.selection.Min(i => i.ModelIndex);
				int selectionLast = this.selection.Max(i => i.ModelIndex);
				this.InvalidateModelIndices(selectionFirst, 1 + selectionLast - selectionFirst);
			}
		}
		protected virtual void OnModelCountChanged()
		{
			this.UpdateContentStats();
		}
		protected virtual void OnModelIndicesChanged(int index, int count)
		{
			this.UpdateSelectionIndices(index, count);
			this.InvalidateModelIndices(index, count);
		}
		protected virtual void OnItemClicked(int index, object item, Point location, MouseButtons buttons)
		{
			if (this.ItemClicked != null)
				this.ItemClicked(this, new TiledViewItemMouseEventArgs(this, index, item, location, buttons));

			if (this.itemEditIndex == index && this.allowUserItemEdit)
			{
				this.BeginEdit(index);
			}
		}
		protected virtual void OnItemDoubleClicked(int index, object item, Point location, MouseButtons buttons)
		{
			if (this.ItemDoubleClicked != null)
				this.ItemDoubleClicked(this, new TiledViewItemMouseEventArgs(this, index, item, location, buttons));
		}
		protected virtual void OnItemDrag(int index, object item, Point location, MouseButtons buttons)
		{
			if (this.ItemDrag != null)
				this.ItemDrag(this, new TiledViewItemMouseEventArgs(this, index, item, location, buttons));
		}
		protected virtual void OnItemEdited(int index, object item)
		{
			if (this.ItemEdited != null)
				this.ItemEdited(this, new TiledViewItemEventArgs(this, index, item));
		}
		
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			this.UpdateContentStats();
			this.Invalidate();
		}
		protected override void OnPaddingChanged(EventArgs e)
		{
			base.OnPaddingChanged(e);
			this.UpdateContentStats();
			this.Invalidate();
		}
		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			this.Invalidate();
		}

		protected void GetItemAppearance(int index, object item, out string text, out Image image)
		{
			text = (item != null) ? item.ToString() : "null";
			image = null;
			if (this.ItemAppearance != null)
			{
				if (this.cachedEventItemAppearance == null)
					this.cachedEventItemAppearance = new TiledViewItemAppearanceEventArgs(this);

				this.cachedEventItemAppearance.ModelIndex = index;
				this.cachedEventItemAppearance.Item = item;
				this.cachedEventItemAppearance.DisplayedText = text;
				this.cachedEventItemAppearance.DisplayedImage = image;

				this.ItemAppearance(this, this.cachedEventItemAppearance);

				text = this.cachedEventItemAppearance.DisplayedText;
				image = this.cachedEventItemAppearance.DisplayedImage;
			}
		}
		protected virtual void OnPaintItem(Graphics g, int modelIndex, object item, Rectangle itemRect, bool hovered, bool selected)
		{
			if (this.ItemUserPaint != null)
			{
				if (this.cachedEventItemUserPaint == null)
					this.cachedEventItemUserPaint = new TiledViewItemDrawEventArgs(this);

				this.cachedEventItemUserPaint.Handled = false;
				this.cachedEventItemUserPaint.Graphics = g;
				this.cachedEventItemUserPaint.ModelIndex = modelIndex;
				this.cachedEventItemUserPaint.Item = item;
				this.cachedEventItemUserPaint.ItemRect = itemRect;
				this.cachedEventItemUserPaint.IsHovered = hovered;
				this.cachedEventItemUserPaint.IsSelected = selected;

				this.ItemUserPaint(this, this.cachedEventItemUserPaint);

				if (this.cachedEventItemUserPaint.Handled) return;
			}
			
			string text;
			Image icon;
			this.GetItemAppearance(modelIndex, item, out text, out icon);

			bool isItemEditing = this.IsEditing && modelIndex == this.editedItem.ModelIndex;
			bool hasText = !string.IsNullOrEmpty(text);
			SizeF textSize = SizeF.Empty;
			if (hasText)
			{
				textSize = g.MeasureString(
					text, 
					this.Font, 
					itemRect.Width, 
					this.itemStringFormat);
			}

			if (icon != null)
			{
				int iconAreaHeight = itemRect.Height - (int)Math.Ceiling(textSize.Height);
				if (icon.Width > itemRect.Width || icon.Height > itemRect.Height)
				{
					SizeF iconSize = icon.Size;
					float factor = 1.0f;
					if ((float)itemRect.Width / iconSize.Width < (float)itemRect.Height / iconSize.Height)
						factor = (float)itemRect.Width / iconSize.Width;
					else
						factor = (float)itemRect.Height / iconSize.Height;
					iconSize.Height = iconSize.Height * factor;
					iconSize.Width = iconSize.Width * factor;
					g.DrawImage(icon, new RectangleF(
						itemRect.X + (itemRect.Width - iconSize.Width) * 0.5f, 
						itemRect.Y + Math.Max(0, iconAreaHeight - iconSize.Height) * 0.5f, 
						iconSize.Width, 
						iconSize.Height));
				}
				else
				{
					g.DrawImageUnscaled(icon, new Rectangle(
						itemRect.X + (itemRect.Width - icon.Width) / 2, 
						itemRect.Y + Math.Max(0, iconAreaHeight - icon.Height) / 2, 
						icon.Width, 
						icon.Height));
				}
			}

			if (this.Enabled && !isItemEditing)
			{
				if (selected || (Control.MouseButtons != MouseButtons.None && this.highlightHoverItems && hovered))
					this.renderer.DrawSelection(g, itemRect, true);
				else if (this.highlightHoverItems && hovered)
					this.renderer.DrawSelection(g, itemRect, false);
			}

			if (hasText && !isItemEditing)
			{
				if (icon != null)
					this.itemStringFormat.LineAlignment = StringAlignment.Far;
				else
					this.itemStringFormat.LineAlignment = StringAlignment.Center;

				g.DrawString(
					text, 
					this.Font, 
					new SolidBrush(this.ForeColor), 
					itemRect, 
					this.itemStringFormat);
			}
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.FillRectangle(new SolidBrush(this.BackColor), this.ClientRectangle);
			e.Graphics.SetClip(new Rectangle(
				this.ClientRectangle.X + this.Padding.Left,
				this.ClientRectangle.Y + this.Padding.Top,
				this.ClientRectangle.Width - this.Padding.Horizontal,
				this.ClientRectangle.Height - this.Padding.Vertical), 
				System.Drawing.Drawing2D.CombineMode.Intersect);

			if (this.model.Count > 0)
			{
				int firstIndex = this.PickModelIndexAt(e.ClipRectangle.Left, e.ClipRectangle.Top, true, true);
				int lastIndex = this.PickModelIndexAt(e.ClipRectangle.Right - 1, e.ClipRectangle.Bottom - 1, true, true);
				Point firstItemPos = this.GetModelIndexLocation(firstIndex);

				int firstSelected = this.selection.Count > 0 ? this.selection.Min(s => s.ModelIndex) : -1;
				int lastSelected = this.selection.Count > 0 ? this.selection.Max(s => s.ModelIndex) : -1;

				Point basePos = firstItemPos;
				Point curPos = basePos;
				int itemsInRow = 0;
				for (int i = firstIndex; i <= lastIndex; i++)
				{
					this.OnPaintItem(
						e.Graphics,
						i, 
						this.model.GetItemAt(i), 
						new Rectangle(curPos.X, curPos.Y, this.tileSize.Width, this.tileSize.Height),
						i == this.hoverIndex,
						i >= firstSelected && i <= lastSelected && this.selection.Any(s => s.ModelIndex == i));

					itemsInRow++;
					if (itemsInRow == this.tilesPerRow)
					{
						curPos.X = basePos.X;
						curPos.Y += this.tileSize.Height + this.spacing.Height;
						itemsInRow = 0;
					}
					else
					{
						curPos.X += this.tileSize.Width + this.spacing.Width;
					}
				}
			}

			if (!this.Enabled)
			{
				e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, this.BackColor)), this.ClientRectangle);
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData.HasFlag(Keys.Up) || keyData.HasFlag(Keys.Down) || keyData.HasFlag(Keys.Left) || keyData.HasFlag(Keys.Right))
			{
				KeyEventArgs args = new KeyEventArgs(keyData);
				args.Handled = false;
				this.OnKeyDown(args);
				if (args.Handled) return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			int focusIndex = (this.hoverIndex == -1 || this.selection.Any()) ? this.selection.Last().ModelIndex : this.hoverIndex;
			if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Left || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
			{
				e.Handled = true;
				if (!this.EndEdit(true)) return;
				if (focusIndex == -1)
				{
					focusIndex = this.PickModelIndexAt(0, 0, true, true);
					if (focusIndex == -1) return;
				}

				if (e.KeyCode == Keys.Right)
					focusIndex = (focusIndex + 1) % this.model.Count;
				if (e.KeyCode == Keys.Left)
					focusIndex = (focusIndex - 1 + this.model.Count) % this.model.Count;
				if (e.KeyCode == Keys.Down)
					focusIndex = (focusIndex + this.tilesPerRow) % this.model.Count;
				if (e.KeyCode == Keys.Up)
					focusIndex = (focusIndex - this.tilesPerRow + this.model.Count) % this.model.Count;

				this.hoverIndex = focusIndex;
				this.ProcessUserItemClick(focusIndex);
			}
			else if (e.KeyCode == Keys.F2 && this.allowUserItemEdit)
			{
				e.Handled = true;
				if (this.IsEditing)
					this.EndEdit(true);
				else
					this.BeginEdit(focusIndex);
			}
		}
		protected override void OnDragOver(DragEventArgs drgevent)
		{
			base.OnDragOver(drgevent);
			this.dragIndex = -1;
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			int lastHoverIndex = this.hoverIndex;
			this.hoverIndex = this.PickModelIndexAt(e.X, e.Y, true);
			if (this.hoverIndex != lastHoverIndex)
			{
				this.InvalidateModelIndices(lastHoverIndex, 1);
				this.InvalidateModelIndices(this.hoverIndex, 1);
			}
			
			if (this.dragIndex != -1 && this.IsItemSelected(this.dragIndex))
			{
				Point diff = new Point(e.X - this.mouseDownLoc.X, e.Y - this.mouseDownLoc.Y);
				bool dragSizeReached = 
					Math.Abs(diff.X) > SystemInformation.DragSize.Width / 2 || 
					Math.Abs(diff.Y) > SystemInformation.DragSize.Height / 2;
				if (dragSizeReached)
				{
					Point itemPos = this.GetModelIndexLocation(this.dragIndex);
					this.OnItemDrag(this.dragIndex, this.model.GetItemAt(this.dragIndex), new Point(e.X - itemPos.X, e.Y - itemPos.Y), e.Button);
					this.dragIndex = -1;
				}
			}
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			this.Focus();
			base.OnMouseDown(e);
			this.mouseDownLoc = e.Location;
			this.dragIndex = this.hoverIndex;
			if (this.hoverIndex != -1)
			{
				this.InvalidateModelIndices(this.hoverIndex, 1);
			}
			this.ProcessUserItemClick(this.hoverIndex, true);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			this.dragIndex = -1;
			if (this.hoverIndex != -1)
			{
				this.InvalidateModelIndices(this.hoverIndex, 1);
			}
		}
		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (this.hoverIndex != -1)
			{
				Point itemPos = this.GetModelIndexLocation(this.hoverIndex);
				this.OnItemClicked(this.hoverIndex, this.model.GetItemAt(this.hoverIndex), new Point(e.X - itemPos.X, e.Y - itemPos.Y), e.Button);
			}
			this.ProcessUserItemClick(this.hoverIndex);
			this.itemEditIndex = this.hoverIndex;
		}
		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			base.OnMouseDoubleClick(e);
			if (this.hoverIndex != -1)
			{
				Point itemPos = this.GetModelIndexLocation(this.hoverIndex);
				this.OnItemDoubleClicked(this.hoverIndex, this.model.GetItemAt(this.hoverIndex), new Point(e.X - itemPos.X, e.Y - itemPos.Y), e.Button);
			}
		}
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			this.InvalidateModelIndices(this.hoverIndex, 1);
			this.hoverIndex = -1;
		}
		protected override Point ScrollToControl(Control activeControl)
		{
			// Prevent AutoScroll on focus or content resize - will always scroll to top.
			// Solution: Just don't scroll. Won't be needed here anyway.
			return this.AutoScrollPosition;
			//return base.ScrollToControl(activeControl);
		}

		private void model_IndicesChanged(object sender, ListModelItemsEventArgs e)
		{
			this.OnModelIndicesChanged(e.Index, e.Count);
		}
		private void model_CountChanged(object sender, EventArgs e)
		{
			this.OnModelCountChanged();
		}
		private bool ShouldSerializeItemAppearance()
		{
			// Prevent the Designer from going crazy.
			return false;
		}
	}
}
