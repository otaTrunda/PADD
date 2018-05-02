using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace PADD.DomainDependentSolvers
{
	class VisitAllSolver : DomainDependentSolver
	{
		VisitAllDomain dom;
		VisitAllVisualizer vis;

		public override int Search(bool quiet = false)
		{
			throw new NotImplementedException();
		}

		protected override void init()
		{
			dom = new VisitAllDomain(this, sasProblem);
			vis = new VisitAllVisualizer(dom);
			vis.draw(new VisitAllState((SASState)sasProblem.GetInitialState(), dom));
		}
	}

	class VisitAllNode
	{
		private static int IDCounter = 0;

		public int ID;
		public string originalName;
		public int variableNumber;
		public int gridCoordX, gridCoordY;
		public List<VisitAllNode> successors;
		public VisitAllNode(string originalName)
		{
			this.ID = IDCounter++;
			this.originalName = originalName;
			var splitted = originalName.Split('-').Skip(1).ToList();
			gridCoordX = int.Parse(splitted[0].Substring(1));
			gridCoordY = int.Parse(splitted[1].Substring(1));
			this.successors = new List<VisitAllNode>();
		}
	}

	class VisitAllDomain
	{
		public List<VisitAllNode> nodes;
		public Dictionary<int, int> nodeIDByVariableNo;
		public Dictionary<string, int> nodeIDByOrigName;
		public bool[,] connected;
		public int[,] shortestDistances;
		public int positionVariable;
		public int startPosition;

		public VisitAllDomain(VisitAllSolver solver, SASProblem visitAllproblem)
		{
			nodes = new List<VisitAllNode>();
			nodeIDByVariableNo = new Dictionary<int, int>();
			nodeIDByOrigName = new Dictionary<string, int>();

			positionVariable = solver.allVariables.IndexOf(solver.allVariables.Where(v => v.valuesSymbolicMeaning.Any(s => s.Contains("at-robot"))).Single());
			startPosition = ((SASState)visitAllproblem.GetInitialState()).GetValue(positionVariable);
			for (int i = 0; i < visitAllproblem.GetVariablesRanges()[positionVariable]; i++)
			{
				string meaning = solver.getSymbolicMeaning(positionVariable, i);
				var splitted = meaning.Split('(').Skip(1).Single();
				var node = new VisitAllNode(splitted.Substring(0, splitted.Length - 1));
				nodes.Add(node);
				nodeIDByOrigName.Add(node.originalName, node.ID);
			}
			for (int i = 0; i < visitAllproblem.GetVariablesCount(); i++)
			{
				if (solver.allVariables[i].valuesSymbolicMeaning.Any(s => s.Contains("visited")))
				{
					string meaning = solver.allVariables[i].valuesSymbolicMeaning.Where(s => s.Contains("visited")).First().Split('(').Skip(1).Single();
					string cellName = meaning.Substring(0, meaning.Length - 1);
					int ID = nodeIDByOrigName[cellName];
					var node = nodes[ID];
					node.variableNumber = i;
					nodeIDByVariableNo.Add(i, node.ID);
				}
			}

			connected = new bool[nodes.Count, nodes.Count];
			shortestDistances = new int[nodes.Count, nodes.Count];

			for (int i = 0; i < connected.GetLength(0); i++)
				for (int j = 0; j < connected.GetLength(1); j++)
				{
					connected[i, j] = false;
					shortestDistances[i, j] = int.MaxValue;
					if (i == j)
					{
						connected[i, j] = true;
						shortestDistances[i, j] = 0;
					}
				}

			foreach (var op in visitAllproblem.GetOperators())
			{
				if (op.GetName().StartsWith("move"))
				{
					var splitted = op.GetName().Split(' ').Skip(1);
					string from = splitted.First(),
						to = splitted.Last();
					connected[nodeIDByOrigName[from], nodeIDByOrigName[to]] = true;
					nodes[nodeIDByOrigName[from]].successors.Add(nodes[nodeIDByOrigName[to]]);
				}
			}
			//computeShortestPaths();
		}

		protected void computeShortestPaths()
		{
			/*
			bool changed = true;
			while (changed)
			{
				changed = false;
			*/
				for (int k = 0; k < nodes.Count; k++)
				{
					for (int i = 0; i < nodes.Count; i++)
						for (int j = 0; j < nodes.Count; j++)
						{
							if (shortestDistances[i, k] + shortestDistances[k, j] < shortestDistances[i, j])
							{
								//changed = true;
								shortestDistances[i, j] = shortestDistances[i, k] + shortestDistances[k, j];
							}
						}
				}
			//}
		}
	}

	class VisitAllState
	{
		public bool[] visited;
		public int position;
		public VisitAllDomain domain;

		public VisitAllState(SASState state, VisitAllDomain domain)
		{
			var values = state.GetAllValues();
			position = values[domain.positionVariable];
			visited = new bool[domain.nodes.Count];
			for (int i = 0; i < values.Length; i++)
			{
				if (i == domain.positionVariable)
					continue;
				if (values[i] == 0)
					visited[domain.nodeIDByVariableNo[i]] = true;
			}
			visited[domain.startPosition] = true;
		}
	}

	class VisitAllVisualizer
	{
		public PictureBox screen;
		public VisitAllDomain domain;
		Graphics g;
		Color backColor = Color.Beige;
		Pen gridPen = Pens.Gray,
			obstaclePen = Pens.Black,
			connectedPen = Pens.Green,
			targetPen = new Pen(new SolidBrush(Color.Red), 5f);
		Brush visitedBrush = Brushes.Yellow;
		int maxGridWidth, maxGridHeigth;
		float tileSize;
		VisitAllVisForm form;
		float targetCrossMarginPercent = 20f;

		public VisitAllVisualizer(VisitAllDomain domain)
		{
			form = new VisitAllVisForm();
			this.screen = form.screen;
			this.domain = domain;
			screen.Image = new Bitmap(screen.Width, screen.Height);
			g = Graphics.FromImage(screen.Image);
			maxGridWidth = domain.nodes.Max(n => n.gridCoordX) + 1;
			maxGridHeigth = domain.nodes.Max(n => n.gridCoordY) + 1;
			tileSize = Math.Min(screen.Width / (maxGridWidth + 1), screen.Height / (maxGridHeigth + 1));
		}

		public void draw(VisitAllState state =  null)
		{
			g.Clear(backColor);
			for (int i = 0; i < maxGridWidth; i++)
				for (int j = 0; j < maxGridHeigth; j++)
				{
					var node = domain.nodes.Where(n => n.gridCoordX == i && n.gridCoordY == j).Single();
					g.DrawRectangle(gridPen, i * tileSize, j * tileSize, tileSize, tileSize);
					if (state?.visited[node.ID] == true)
						g.FillRectangle(visitedBrush, i * tileSize, j * tileSize, tileSize, tileSize);

					if (state?.position == node.ID)
					{
						g.FillEllipse(Brushes.BlueViolet, i * tileSize + tileSize * targetCrossMarginPercent / 100, j * tileSize + tileSize * targetCrossMarginPercent / 100,
						tileSize - 2 * tileSize * targetCrossMarginPercent / 100, tileSize - 2 * tileSize * targetCrossMarginPercent / 100);
					}
				}
			/*
			for (int i = 0; i < domain.nodes.Count(); i++)
				for (int j = i + 1; j < domain.nodes.Count(); j++)
				{
					if (domain.connected[i, j])
					{
						g.DrawLine(connectedPen, domain.nodes[i].gridCoordX * tileSize + tileSize / 2, domain.nodes[i].gridCoordY * tileSize + tileSize / 2,
							domain.nodes[j].gridCoordX * tileSize + tileSize / 2, domain.nodes[j].gridCoordY * tileSize + tileSize / 2);
					}
				}
			*/
			screen.Refresh();
			form.ShowDialog();
		}
	}
}
