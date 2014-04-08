using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.ItemModels
{
	public class SimpleListModel<T> : IListModel, IList<T>
	{
		private List<T> items = new List<T>();

		public event EventHandler<EventArgs> CountChanged;
		public event EventHandler<ListModelItemsEventArgs> IndicesChanged;

		public int Count
		{
			get { return this.items.Count; }
		}
		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}
		public T this[int index]
		{
			get { return this.items[index]; }
			set
			{
				this.items[index] = value;
				this.OnIndicesChanged(index, 1);
			}
		}

		public SimpleListModel() : this(new T[0]) {}
		public SimpleListModel(IEnumerable<T> items)
		{
			this.items.AddRange(items);
		}

		public void Add(T item)
		{
			this.items.Add(item);
			this.OnCountChanged();
			this.OnIndicesChanged(this.items.Count - 1, 1);
		}
		public void AddRange(IEnumerable<T> items)
		{
			int oldCount = this.items.Count;
			this.items.AddRange(items);
			this.OnCountChanged();
			this.OnIndicesChanged(oldCount, this.items.Count - oldCount);
		}
		public void Insert(int index, T item)
		{
			this.items.Insert(index, item);
			this.OnCountChanged();
			this.OnIndicesChanged(index, this.items.Count - index);
		}
		public bool Remove(T item)
		{
			int index = this.items.IndexOf(item);
			if (index != -1)
			{
				this.RemoveAt(index);
				return true;
			}
			else
			{
				return false;
			}
		}
		public void RemoveRange(IEnumerable<T> items)
		{
			int oldCount = this.items.Count;
			if (oldCount == 0) return;

			int[] indices = items.Select(i => this.items.IndexOf(i)).Where(i => i != -1).ToArray();
			if (indices.Length > 0)
			{
				Array.Sort(indices);
				for (int i = indices.Length - 1; i >= 0; i--)
				{
					this.items.RemoveAt(indices[i]);
				}
				this.OnCountChanged();
				this.OnIndicesChanged(indices[0], oldCount - indices[0]);
			}
		}
		public void RemoveAt(int index)
		{
			this.items.RemoveAt(index);
			this.OnCountChanged();
			this.OnIndicesChanged(index, this.items.Count - index);
		}
		public void Clear()
		{
			int oldCount = this.items.Count;
			this.items.Clear();
			this.OnCountChanged();
			this.OnIndicesChanged(0, oldCount);
		}

		public bool Contains(T item)
		{
			return this.items.Contains(item);
		}
		public int IndexOf(T item)
		{
			return this.items.IndexOf(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			this.items.CopyTo(array, arrayIndex);
		}

		object IListModel.GetItemAt(int index)
		{
			return this.items[index];
		}
		int IListModel.GetIndexOf(object item)
		{
			return this.items.IndexOf((T)item);
		}
		private void OnCountChanged()
		{
			if (this.CountChanged != null)
				this.CountChanged(this, EventArgs.Empty);
		}
		private void OnIndicesChanged(int index, int count)
		{
			if (this.IndicesChanged != null)
				this.IndicesChanged(this, new ListModelItemsEventArgs(index, count));
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this.items.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}
