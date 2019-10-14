using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;

namespace PADD_Support.KnowledgeExtraction
{
	public abstract class GraphVisualizable
	{
		public abstract Graph toMSAGLGraph();

		public void visualize(System.Windows.Forms.Panel panel)
		{
			Graph g = toMSAGLGraph();

			GViewer viewer = new GViewer();
			viewer.Graph = g;
			viewer.CurrentLayoutMethod = LayoutMethod.MDS;
			viewer.Dock = System.Windows.Forms.DockStyle.Fill;
			panel.Controls.Clear();
			panel.Controls.Add(viewer);
			panel.Refresh();
		}
	}

}
