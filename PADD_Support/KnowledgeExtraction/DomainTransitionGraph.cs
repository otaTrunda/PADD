using PAD.Planner.SAS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD_Support.KnowledgeExtraction
{
	public class DomainTransitionGraph
	{
		public int variable;
		public List<int> vertices;
		public List<GraphEdge> edges;
		public int goalValue = -1;

		private List<GraphEdge>[] edgesByVertices;
		private bool isTransformed = false;

		private void transformToSuccesorsLists()
		{
			this.edgesByVertices = new List<GraphEdge>[vertices.Count];
			foreach (var item in edges)
			{
				if (edgesByVertices[item.from] == null)
					edgesByVertices[item.from] = new List<GraphEdge>();
				edgesByVertices[item.from].Add(item);
			}
			isTransformed = true;
		}

		public bool isRSE_Invertible = false;

		public void computeRSE_Invertibility()
		{
			isRSE_Invertible = true;
			foreach (var item in edges)
			{
				if (!isEdgeRSE_Invertible(item))
				{
					isRSE_Invertible = false;
					return;
				}
			}
		}

		private bool isEdgeRSE_Invertible(GraphEdge e)
		{
			if (e.isInvertibilityComputed)
				return e.isRSE_Invertible;
			e.isInvertibilityComputed = true;
			bool isJ_thConditionMet = false;
			foreach (var item in edges)
			{
				if (item.from != e.to || item.to != e.from)
					continue;
				if (item.outsideCondition.GetConditions() == null)
				{
					e.isRSE_Invertible = true;
					return true;
				}

				foreach (var jItem in item.outsideCondition.GetConditions())
				{
					isJ_thConditionMet = false;

					foreach (var iItem in e.outsideCondition.GetConditions())
					{
						if (iItem.Equals(jItem))
						{
							isJ_thConditionMet = true;
							break;
						}
					}
					if (isJ_thConditionMet)
						continue;

					foreach (var iItem in e.outsideEffect.GetConditions())
					{
						if (iItem.Equals(jItem))
						{
							isJ_thConditionMet = true;
							break;
						}
					}
					if (!isJ_thConditionMet)
						break;
				}
				if (isJ_thConditionMet)
				{
					e.isRSE_Invertible = true;
					return true;
				}
			}
			e.isRSE_Invertible = false;
			return false;
		}

		public void visualize(bool isLabeled = true, System.Windows.Forms.Panel panel = null)
		{
			Microsoft.Msagl.Drawing.Graph g = new Microsoft.Msagl.Drawing.Graph("PlanningProblem Transition Graph of variable " + variable);
			foreach (var item in vertices)
			{
				var node = g.AddNode(item.ToString());
				if (item == goalValue)
					node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Yellow;
			}
			if (isLabeled)
				foreach (var item in edges)
				{
					var e = g.AddEdge(item.from.ToString(), "Cond:" + item.outsideCondition.GetConditions().ToString() + "Eff:" + item.outsideEffect.GetConditions().ToString(), item.to.ToString());
					if (!isEdgeRSE_Invertible(item))
					{
						e.Attr.Color = Microsoft.Msagl.Drawing.Color.Red;
					}
				}
			else
			{
				bool[,] isEdge = new bool[vertices.Count, vertices.Count];
				foreach (var item in edges)
				{
					if (isEdge[item.from, item.to])
						continue;
					var e = g.AddEdge(item.from.ToString(), item.to.ToString());
					if (!isEdgeRSE_Invertible(item))
					{
						e.Attr.Color = Microsoft.Msagl.Drawing.Color.Red;
					}
					isEdge[item.from, item.to] = true;
				}
			}

			Microsoft.Msagl.GraphViewerGdi.GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
			viewer.Graph = g;
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

		/// <summary>
		/// Finds a path in DTG from given value to another value. Should be called only for black variables, and no other black variables should occur
		/// in the outside conditions. The red variables may occur and an edge is accessible only if the outisde condition is met by given RedValues.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="redValues"></param>
		/// <returns></returns>
		public List<PAD.Planner.SAS.IOperator> findPath(int from, int to, Dictionary<int, HashSet<int>> redValues)
		{
			if (!isTransformed)
				transformToSuccesorsLists();

			HashSet<int> visited = new HashSet<int>();
			PAD.Planner.Heaps.IHeap<double, int> nodes = new PAD.Planner.Heaps.BinomialHeap<int>();
			int[] lengths = new int[vertices.Count], previous = new int[vertices.Count];
			PAD.Planner.SAS.IOperator[] previousOperator = new PAD.Planner.SAS.IOperator[vertices.Count];
			for (int i = 0; i < vertices.Count; i++)
			{
				lengths[i] = int.MaxValue;
			}

			lengths[from] = 0;
			nodes.Add(0, from);
			visited.Add(from);
			while (nodes.GetSize() > 0)
			{
				int current = nodes.RemoveMin();
				if (current == to)
					break;
				foreach (var item in edgesByVertices[current])
				{
					if (isOutsideConditionMet(item.outsideCondition, redValues))
					{
						int succesor = item.to;
						int newLength = lengths[current] + (int)item.op.GetCost();
						if (newLength < lengths[succesor])
						{
							lengths[succesor] = newLength;
							previous[succesor] = current;
							previousOperator[succesor] = item.op;
							if (visited.Contains(succesor))
							{
								nodes.Add(lengths[succesor], succesor);
							}
						}
						if (!visited.Contains(succesor))
						{
							visited.Add(succesor);
							nodes.Add(lengths[succesor], succesor);
						}
					}
				}
			}

			List<PAD.Planner.SAS.IOperator> result = new List<PAD.Planner.SAS.IOperator>();

			int currentVal = to;
			while (currentVal != from)
			{
				result.Insert(0, previousOperator[currentVal]);
				currentVal = previous[currentVal];
			}

			return result;
		}

		private bool isOutsideConditionMet(PAD.Planner.SAS.IEffect condition, Dictionary<int, HashSet<int>> redValues)
		{
			ConditionalEffect condEff = condition as ConditionalEffect;
			if (condEff != null)
			{
				foreach (var cond in condEff.Conditions)
				{
					if (!redValues.ContainsKey(cond.GetVariable()))
					{
						throw new Exception("Outside condition contains a black variable.");
					}
					if (!redValues[cond.GetVariable()].Contains(cond.GetValue()))
						return false;
				}
			}
			return true;
		}

	}

}
