using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms.TimelineControls
{
	public class TimelineGraphTrackModel : TimelineTrackModel, ITimelineGraphTrackModel
	{
		private List<ITimelineGraphModel> graphs = new List<ITimelineGraphModel>();
		
		public event EventHandler<TimelineGraphCollectionEventArgs> GraphsAdded;
		public event EventHandler<TimelineGraphCollectionEventArgs> GraphsRemoved;
		public event EventHandler<TimelineGraphRangeEventArgs> GraphChanged;

		public override float EndTime
		{
			get { return this.graphs.Max(g => g.EndTime); }
		}
		public override float BeginTime
		{
			get { return this.graphs.Min(g => g.BeginTime); }
		}
		public IEnumerable<ITimelineGraphModel> Graphs
		{
			get { return this.graphs; }
		}

		public void Add(ITimelineGraphModel graph)
		{
			this.AddRange(new[] { graph });
		}
		public void AddRange(IEnumerable<ITimelineGraphModel> graphs)
		{
			graphs = graphs.Where(t => !this.graphs.Contains(t)).Distinct().ToArray();
			if (!graphs.Any()) return;
			
			foreach (ITimelineGraphModel graph in graphs)
			{
				graph.GraphChanged += this.graph_GraphChanged;
			}
			this.graphs.AddRange(graphs);

			if (this.GraphsAdded != null)
				this.GraphsAdded(this, new TimelineGraphCollectionEventArgs(graphs));
		}
		public void Remove(ITimelineGraphModel graph)
		{
			this.RemoveRange(new[] { graph });
		}
		public void RemoveRange(IEnumerable<ITimelineGraphModel> graphs)
		{
			graphs = graphs.Where(t => this.graphs.Contains(t)).Distinct().ToArray();
			if (!graphs.Any()) return;

			foreach (ITimelineGraphModel graph in graphs)
			{
				graph.GraphChanged -= this.graph_GraphChanged;
				this.graphs.Remove(graph);
			}
			
			if (this.GraphsRemoved != null)
				this.GraphsRemoved(this, new TimelineGraphCollectionEventArgs(graphs));
		}
		public void Clear()
		{
			ITimelineGraphModel[] oldGraphs = this.graphs.ToArray();

			foreach (ITimelineGraphModel graph in this.graphs)
			{
				graph.GraphChanged -= this.graph_GraphChanged;
			}
			this.graphs.Clear();
			
			if (this.GraphsRemoved != null)
				this.GraphsRemoved(this, new TimelineGraphCollectionEventArgs(oldGraphs));
		}

		private void graph_GraphChanged(object sender, TimelineGraphRangeEventArgs e)
		{
			if (this.GraphChanged != null)
				this.GraphChanged(this, e);
		}
	}
}
