using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using AdamsLair.WinForms.ColorControls;
using AdamsLair.WinForms.PropertyEditing;
using AdamsLair.WinForms.ItemModels;
using AdamsLair.WinForms.ItemViews;
using AdamsLair.WinForms.TimelineControls;

namespace AdamsLair.WinForms.TestApp
{
	public partial class DemoForm : Form
	{
		#region Some Test / Demo classes
		[Flags]
		private enum FlaggedEnumTest : uint
		{
			One	= 0x1,
			Two	= 0x2,
			Three = 0x4,

			OneAndThree = One | Three,
			None = 0x0,
			All = One | Two | Three
		}
		private enum EnumTest
		{
			One,
			Two,
			Three
		}
		private interface ISomeInterface
		{
			int InterfaceInt { get; }
		}
		private class Test
		{
			private int i;
			private int i2;
			private float f;
			private byte b;
			private int[] i3 = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
			private string t;
			private Test2 substruct;
			private	object any = "Hello";
			public List<string> stringListField;
			public FlaggedEnumTest enumField1;
			public EnumTest enumField2;
			private System.Diagnostics.Stopwatch w = System.Diagnostics.Stopwatch.StartNew();

			public int IPropWithAVeryLongName
			{
				get { return this.i; }
				set { this.i = value; }
			}
			public int SomeInt
			{
				get { return this.i2; }
				set { this.i2 = value * 2; }
			}
			public float SomeFloat
			{
				get { return this.f; }
				set { this.f = value; }
			}
			public byte SomeByte
			{
				get { return this.b; }
				set { this.b = value; }
			}
			public int[] SomeIntArray
			{
				get { return this.i3; }
			}
			public string SomeString
			{
				get { return this.t; }
				set { this.t = value; }
			}
			public string SomeString2
			{
				get { return this.t; }
				set { this.t = value; }
			}
			public Test2 Substruct
			{
				get { return this.substruct; }
				set { this.substruct = value; }
			}
			public object[] ReflectedTypeTestA { get; set; }
			public Dictionary<string,object> ReflectedTypeTestB { get; set; }
			public object ReflectedTypeTestC
			{
				get { return this.any; }
				set { this.any = 17; }
			}
			public bool BoolOne { get; set; }
			public bool BoolTwo { get; set; }
			public List<Test2> StructList { get; set; }
			public Dictionary<string,int> SomeDict { get; set; }
			public Dictionary<string,List<int>> SomeDict2 { get; set; }
			public TimeSpan ElapsedTime
			{
				get { return this.w.Elapsed; }
			}
			public string ElapsedTimeString
			{
				get { return this.w.Elapsed.ToString(); }
			}
			public long ElapsedTicks
			{
				get { return this.w.ElapsedTicks; }
			}
			public long ElapsedMs
			{
				get { return this.w.ElapsedMilliseconds; }
			}
			public double ElapsedMsHighPrecision
			{
				get { return this.w.Elapsed.TotalMilliseconds; }
			}
		}
		private struct Test2
		{
			private int yoink;
			public bool testBool;

			public int Yoink
			{
				get { return this.yoink; }
				set { this.yoink = value; }
			}

			public Test2(int val)
			{
				this.yoink = val;
				this.testBool = true;
			}

			public override string ToString()
			{
				return "Yoink: " + this.yoink;
			}
		}
		private class Test3 : ISomeInterface
		{
			public int InterfaceInt { get; set; }
			public string HiddenString { get; set; }
		}
		private class TiledModelItem
		{
			public string Name { get; set; }

			public override string ToString()
			{
				return this.Name;
			}
		}
		#endregion

		private static Bitmap bmpItemSmall	= null;
		private static Bitmap bmpItemBig	= null;
		private static Bitmap bmpItemHigh	= null;
		private static Bitmap bmpItemWide	= null;

		static DemoForm()
		{
			bmpItemSmall	= Properties.Resources.ItemSmall;
			bmpItemBig		= Properties.Resources.ItemBig;
			bmpItemHigh		= Properties.Resources.ItemHigh;
			bmpItemWide		= Properties.Resources.ItemWide;
		}

		private Test objA;
		private Test objB;
		private SimpleListModel<TiledModelItem> tiledViewModel;

		public DemoForm()
		{
			this.InitializeComponent();

			// Generate some test / demo objects
			this.objA = new Test();
			this.objA.IPropWithAVeryLongName = 42;
			this.objA.SomeString = "Blubdiwupp";
			this.objA.SomeFloat = (float)Math.PI;
			this.objA.SomeByte = 128;
			this.objA.Substruct = new Test2(42);
			this.objA.ReflectedTypeTestA = new Test3[] { new Test3() };
			this.objA.ReflectedTypeTestB = new Dictionary<string,object> { { "First", new Test3() } };
			this.objA.stringListField = new List<string>() { "hallo", "welt" };

			this.objB = new Test();
			this.objB.IPropWithAVeryLongName = 17;
			this.objB.SomeString = "Kratatazong";
			this.objB.SomeFloat = 3.0f;
			this.objB.SomeByte = 0;
			this.objB.Substruct = new Test2(100);
			this.objB.stringListField = new List<string>() { "hallo", "welt" };

			this.propertyGrid1.SelectObject(this.objA);

			this.tiledViewModel = new SimpleListModel<TiledModelItem>();
			this.tiledViewModel.Add(new TiledModelItem { Name = "Frederick" });
			this.tiledViewModel.Add(new TiledModelItem { Name = "Herbert" });
			this.tiledViewModel.Add(new TiledModelItem { Name = "Mary" });
			this.tiledViewModel.Add(new TiledModelItem { Name = "John" });
			this.tiledViewModel.Add(new TiledModelItem { Name = "Sally" });
			this.tiledView.Model = this.tiledViewModel;
			this.tiledView.ItemAppearance += this.tiledView_ItemAppearance;

			TimelineViewGraphTrack trackA = new TimelineViewGraphTrack();
			trackA.FillHeight = 10;
			TimelineViewGraphTrack trackB = new TimelineViewGraphTrack();
			trackB.BaseHeight = 50;
			TimelineViewGraphTrack trackC = new TimelineViewGraphTrack();
			trackC.BaseHeight = 200;
			trackC.FillHeight = 100;
			this.timelineView1.AddTrack(trackA);
			this.timelineView1.AddTrack(trackB);
			this.timelineView1.AddTrack(trackC);
		}

		private void radioEnabled_CheckedChanged(object sender, EventArgs e)
		{
			if (this.radioEnabled.Checked)
			{
				this.propertyGrid1.Enabled = true;
				this.propertyGrid1.ReadOnly = false;
			}
		}
		private void radioReadOnly_CheckedChanged(object sender, EventArgs e)
		{
			if (this.radioReadOnly.Checked)
			{
				this.propertyGrid1.Enabled = true;
				this.propertyGrid1.ReadOnly = true;
			}
		}
		private void radioDisabled_CheckedChanged(object sender, EventArgs e)
		{
			if (this.radioDisabled.Checked)
			{
				this.propertyGrid1.Enabled = false;
				this.propertyGrid1.ReadOnly = false;
			}
		}
		private void buttonRefresh_Click(object sender, EventArgs e)
		{
			this.propertyGrid1.UpdateFromObjects();
		}
		private void checkBoxNonPublic_CheckedChanged(object sender, EventArgs e)
		{
			this.propertyGrid1.ShowNonPublic = this.checkBoxNonPublic.Checked;
		}

		private void buttonObjMulti_Click(object sender, EventArgs e)
		{
			this.propertyGrid1.SelectObjects(new object[] { this.objA, this.objB });
		}
		private void buttonObjB_Click(object sender, EventArgs e)
		{
			this.propertyGrid1.SelectObject(this.objB);
		}
		private void buttonObjA_Click(object sender, EventArgs e)
		{
			this.propertyGrid1.SelectObject(this.objA);
		}
		private void buttonColorPicker_Click(object sender, EventArgs e)
		{
			ColorPickerDialog dialog = new ColorPickerDialog();
			dialog.ShowDialog();
		}
		private void buttonAddTenTileItems_Click(object sender, EventArgs e)
		{
			Random rnd = new Random();
			IEnumerable<TiledModelItem> newItemQuery = Enumerable.Range(0, 100).Select(i => new TiledModelItem { Name = rnd.Next().ToString() });
			this.tiledViewModel.AddRange(newItemQuery);
		}
		private void buttonAddThousandTileItems_Click(object sender, EventArgs e)
		{
			Random rnd = new Random();
			IEnumerable<TiledModelItem> newItemQuery = Enumerable.Range(0, 100000).Select(i => new TiledModelItem { Name = rnd.Next().ToString() });
			this.tiledViewModel.AddRange(newItemQuery);
		}
		private void buttonRemoveTileItem_Click(object sender, EventArgs e)
		{
			this.tiledViewModel.RemoveRange(this.tiledView.SelectedModelItems.OfType<TiledModelItem>());
		}
		private void buttonClearTileItems_Click(object sender, EventArgs e)
		{
			this.tiledViewModel.Clear();
		}
		private void radioTiledDisabled_CheckedChanged(object sender, EventArgs e)
		{
			this.tiledView.Enabled = this.radioTiledEnabled.Checked;
		}
		private void radioTiledEnabled_CheckedChanged(object sender, EventArgs e)
		{
			this.tiledView.Enabled = this.radioTiledEnabled.Checked;
		}
		private void tiledView_ItemClicked(object sender, TiledViewItemMouseEventArgs e)
		{
			Console.WriteLine("ItemClicked {0} at {1} with {2}", e.Item, e.Location, e.Buttons);
		}
		private void tiledView_ItemDoubleClicked(object sender, TiledViewItemMouseEventArgs e)
		{
			Console.WriteLine("ItemDoubleClicked {0} at {1} with {2}", e.Item, e.Location, e.Buttons);
		}
		private void tiledView_ItemDrag(object sender, TiledViewItemMouseEventArgs e)
		{
			Console.WriteLine("ItemDrag {0} at {1} with {2}", e.Item, e.Location, e.Buttons);
			DragDropEffects result = this.tiledView.DoDragDrop(e.Item, DragDropEffects.All);
			Console.WriteLine("  Result: {0}", result);
		}
		private void tiledView_ItemAppearance(object sender, TiledViewItemAppearanceEventArgs e)
		{
			e.DisplayedText = e.Item.ToString();
			switch (e.Item.GetHashCode() % 5)
			{
				default:
				case 0: e.DisplayedImage = bmpItemSmall; break;
				case 1: e.DisplayedImage = bmpItemBig; break;
				case 2: e.DisplayedImage = bmpItemHigh; break;
				case 3: e.DisplayedImage = bmpItemWide; break;
				case 4: e.DisplayedImage = null; break;
			}
		}
		private void checkBoxTileViewHighlightHover_CheckedChanged(object sender, EventArgs e)
		{
			this.tiledView.HightlightHoverItems = this.checkBoxTileViewHighlightHover.Checked;
		}
		private void checkBoxTileViewUserSelect_CheckedChanged(object sender, EventArgs e)
		{
			this.tiledView.UserSelectMode = this.checkBoxTileViewUserSelect.Checked ? TiledView.SelectMode.Multi : TiledView.SelectMode.None;
		}
		private void trackBarTileViewWidth_ValueChanged(object sender, EventArgs e)
		{
			this.tiledView.TileSize = new Size(this.trackBarTileViewWidth.Value, this.tiledView.TileSize.Height);
		}
		private void trackBarTileViewHeight_ValueChanged(object sender, EventArgs e)
		{
			this.tiledView.TileSize = new Size(this.tiledView.TileSize.Width, this.trackBarTileViewHeight.Value);
		}
		private void checkBoxTiledViewStyle_CheckedChanged(object sender, EventArgs e)
		{
			if (this.checkBoxTiledViewStyle.Checked)
			{
				this.tiledView.BackColor = Color.Black;
				this.tiledView.ForeColor = Color.FromArgb(192, Color.White);
			}
			else
			{
				this.tiledView.BackColor = SystemColors.Control;
				this.tiledView.ForeColor = SystemColors.ControlText;
			}
		}
		private void trackBarTimelineUnitZoom_ValueChanged(object sender, EventArgs e)
		{
			this.timelineView1.UnitZoom = (float)Math.Pow(2.0f, (float)this.trackBarTimelineUnitZoom.Value / 100.0f);
		}
	}
}
