using System;
using System.Collections.Generic;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;

namespace PADD_Support.KnowledgeExtraction
{
	public class CausualGraph
	{
		public List<int> vertices;
		public bool[,] isEdge;
		private bool hasSomeEdge = false;
		public bool[] isMentionedInGoal;

		public void setEdge(int from, int to)
		{
			isEdge[from, to] = true;
			hasSomeEdge = true;
		}

		public void visualize(System.Windows.Forms.Panel panel = null, HashSet<int> invertibleVariables = null)
		{
			Graph g = new Graph("Causual Graph");
			foreach (var item in vertices)
			{
				var node = g.AddNode(item.ToString());
				if (invertibleVariables != null && !invertibleVariables.Contains(item))
				{
					node.Attr.Color = Microsoft.Msagl.Drawing.Color.Red;
				}
				if (isMentionedInGoal[item])
					node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Yellow;
			}
			for (int i = 0; i < isEdge.GetLength(0); i++)
				for (int j = 0; j < isEdge.GetLength(1); j++)
					if (isEdge[i, j]) g.AddEdge(i.ToString(), j.ToString());

			GViewer viewer = new GViewer();
			viewer.Graph = g;
			viewer.CurrentLayoutMethod = LayoutMethod.MDS;
			if (panel == null)
			{
				KnowledgeVisualizerForm form = new KnowledgeVisualizerForm();
				form.SuspendLayout();
				viewer.Dock = System.Windows.Forms.DockStyle.Fill;
				form.Controls.Add(viewer);
				form.ResumeLayout();
				System.Windows.Forms.Application.Run(form);
			}
			else
			{
				viewer.Dock = System.Windows.Forms.DockStyle.Fill;
				panel.Controls.Clear();
				panel.Controls.Add(viewer);
			}
		}

		public CausualGraph projection(HashSet<int> variables)
		{
			CausualGraph result = new CausualGraph();
			result.vertices = new List<int>();
			int max = 0;
			foreach (var item in this.vertices)
			{
				if (variables.Contains(item))
				{
					result.vertices.Add(item);
					if (max < item)
						max = item;
				}
			}
			result.isEdge = new bool[max, max];
			for (int i = 0; i < max; i++)
			{
				for (int j = 0; j < max; j++)
				{
					if (this.isEdge[i, j])
						result.setEdge(i, j);
				}
			}
			return result;
		}

		private class acyclicityChecker
		{
			// 0 = not visited, 1 = pending, 2 = closed
			Dictionary<int, int> visited = new Dictionary<int, int>(),
				enterTime = new Dictionary<int, int>(),
				exitTime = new Dictionary<int, int>();

			int time = 0;
			CausualGraph gr;
			bool hasCycle = false;

			private void doDFS(CausualGraph g)
			{
				hasCycle = false;
				visited.Clear();
				enterTime.Clear();
				exitTime.Clear();
				gr = g;
				foreach (var item in g.vertices)
					visited.Add(item, 0);
				time = 0;
				foreach (var item in g.vertices)
				{
					if (visited[item] == 0)
						visit(item);
				}
			}

			public bool isAcyclic(CausualGraph g)
			{
				doDFS(g);
				return !hasCycle;
			}

			private void visit(int vertex)
			{
				visited[vertex] = 1;
				time++;
				enterTime[vertex] = time;
				foreach (var successor in gr.vertices)
				{
					if (gr.isEdge[vertex, successor])
					{
						if (visited[successor] == 0)
						{
							visit(successor);
						}
						else
						{
							if (visited[successor] == 1)
							{
								hasCycle = true;
							}
						}

					}
				}
				visited[vertex] = 3;
				time++;
				exitTime[vertex] = time;
			}
		}

		//    for i:=1 to n do barva[i]:=bílá;
		//čas:=0;
		//for i:=1 to n do if barva[i]=bílá then NAVŠTIV(i)

		//    NAVŠTIV(i) 
		//begin	barva[i]:=šedá; čas:=čas+1; d[i]:=čas;
		//for each j je soused i do 
		//if barva[j]=bílá 
		//    then 	begin	NAVŠTIV(j);
		//            označ (i,j) jako stromovou
		//        end
		//    else if barva[j]=šedá 
		//        then 	begin 	ohlas nalezení cyklu;
		//                označ (i,j) jako zpětnou
		//            end
		//        else if d[i] < d[j] 
		//            then označ (i,j) jako dopřednou
		//            else označ (i,j) jako příčnou
		//barva[i]:=černá; čas:=čas+1; f[i]:=čas
		//end;

		public bool isAcyclic()
		{
			acyclicityChecker checker = new acyclicityChecker();
			return checker.isAcyclic(this);
		}

		public List<int> topologicalOrder()
		{
			if (isEmpty())
			{
				return vertices;
			}
			else throw new Exception("Causual graph is not empty");
		}

		public bool isEmpty()
		{
			return !hasSomeEdge;
		}
	}

}
