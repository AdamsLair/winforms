using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineGraphTrackModel : TimelineTrackModel, ITimelineGraphTrackModel
	{
		private List<ITimelineGraph> graphs = new List<ITimelineGraph>();

		public event EventHandler GraphCollectionChanged;
		public event EventHandler<TimelineGraphRangeEventArgs> GraphChanged;

		public override float EndTime
		{
			get { return this.graphs.Max(g => g.EndTime); }
		}
		public override float BeginTime
		{
			get { return this.graphs.Min(g => g.BeginTime); }
		}
		public IEnumerable<ITimelineGraph> Graphs
		{
			get { return this.graphs; }
		}

		public void Add(ITimelineGraph graph)
		{
			this.AddRange(new[] { graph });
		}
		public void AddRange(IEnumerable<ITimelineGraph> graphs)
		{
			graphs = graphs.Where(t => !this.graphs.Contains(t)).Distinct().ToArray();
			if (!graphs.Any()) return;
			
			foreach (ITimelineGraph graph in graphs)
			{
				graph.GraphChanged += this.graph_GraphChanged;
			}
			this.graphs.AddRange(graphs);

			if (this.GraphCollectionChanged != null)
				this.GraphCollectionChanged(this, EventArgs.Empty);
		}
		public void Remove(ITimelineGraph graph)
		{
			this.RemoveRange(new[] { graph });
		}
		public void RemoveRange(IEnumerable<ITimelineGraph> graphs)
		{
			graphs = graphs.Where(t => this.graphs.Contains(t)).Distinct().ToArray();
			if (!graphs.Any()) return;

			foreach (ITimelineGraph graph in graphs)
			{
				graph.GraphChanged -= this.graph_GraphChanged;
				this.graphs.Remove(graph);
			}

			if (this.GraphCollectionChanged != null)
				this.GraphCollectionChanged(this, EventArgs.Empty);
		}
		public void Clear()
		{
			foreach (ITimelineGraph graph in this.graphs)
			{
				graph.GraphChanged -= this.graph_GraphChanged;
			}

			this.graphs.Clear();

			if (this.GraphCollectionChanged != null)
				this.GraphCollectionChanged(this, EventArgs.Empty);
		}

		private void graph_GraphChanged(object sender, TimelineGraphRangeEventArgs e)
		{
			if (this.GraphChanged != null)
				this.GraphChanged(this, e);
		}
	}
}
