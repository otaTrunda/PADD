using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;

namespace Utils.GraphVisualization
{
	public partial class GraphVis : Form
	{
		Microsoft.Msagl.GraphViewerGdi.GViewer viewer;
		public GraphVis()
		{
			InitializeComponent();
			viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
			panel1.Controls.Add(viewer);
			viewer.Dock = DockStyle.Fill;
		}

		public void draw(Graph g)
		{
			viewer.Graph = g;
			panel1.Refresh();
		}

		public static void showGraph(Graph g)
		{
			GraphVis vis = new GraphVis();
			vis.draw(g);
			vis.ShowDialog();
		}

	}
}
