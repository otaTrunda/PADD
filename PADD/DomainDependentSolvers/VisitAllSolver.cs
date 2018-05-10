using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using TSP;

namespace PADD.DomainDependentSolvers
{
	class VisitAllSolver : DomainDependentSolver
	{
		VisitAllDomain dom;
		VisitAllVisualizer vis;
		double previousBest = double.MaxValue;
		int withoutImprovement = 0;
		bool drawNonimproving = true;

		public override double Search(bool quiet = false)
		{
			VisitAllGoalDistanceCalculator c = new VisitAllGoalDistanceCalculator();
			var state = new VisitAllState((SASState)this.sasProblem.GetInitialState(), dom);
			double dist = c.computeDistance(state);

			if (dist < previousBest)
			{
				previousBest = dist;
				withoutImprovement = 0;
			}
			else withoutImprovement++;

			if (drawNonimproving && withoutImprovement >= 8)
			{
				vis.draw(state);
				c.computeDistance(state);
			}
			return c.computeDistance(state);
		}

		protected override void init()
		{
			previousBest = int.MaxValue;
			withoutImprovement = 0;
			VisitAllNode.resetIDCounter();

			dom = new VisitAllDomain(this, sasProblem);
			vis = new VisitAllVisualizer(dom);
			//vis.draw(new VisitAllState((SASState)sasProblem.GetInitialState(), dom));
		}

		public void drawPlan(List<SASState> states)
		{
			vis.draw(states);
		}
	}

	class VisitAllNode
	{
		private static int IDCounter = 0;
		public static void resetIDCounter()
		{
			IDCounter = 0;
		}

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

	class VisitAllGoalDistanceCalculator
	{
		Dictionary<int, Tile> allTilesbyIDs;
		public int blackTiles,
			whiteTiles,
			visited,
			blackLeaves,
			whiteLeaves,
			visitedTouchingNonVisited;
		//TSPSolver solver = new GreedyGrowingSolver();
		TSPSolver solver = new GreedySolver();

		private double computeDistnaceTSP(VisitAllState s)
		{
			var tspinp = s.toTSP();
			return solver.solveStartPoint(tspinp.input, tspinp.position).totalDistance;
		}

		public double computeDistance(VisitAllState s)
		{
			init();
			foreach (var item in s.domain.nodes)
			{
				Tile t = new Tile(item, s);
				if (!t.isVisited && t.isBlack)
					blackTiles++;
				if (!t.isVisited && !t.isBlack)
					whiteTiles++;
				if (t.isLeaf)
				{
					if (t.isBlack) blackLeaves++;
					else whiteLeaves++;
				}
				if (t.isVisited)
				{
					visited++;
					visitedTouchingNonVisited += t.visitedConectedToNonVisited;
				}

				allTilesbyIDs.Add(item.ID, t);
			}
			foreach (var item in allTilesbyIDs.Values)
			{
				item.computeGovernance(allTilesbyIDs);
			}

			int nonVisitedTiles = allTilesbyIDs.Count() - visited;
			if (nonVisitedTiles == 0)
				return 0;
			int penalty = 0;
			if (blackTiles > whiteTiles + 1)
			{
				penalty += blackTiles - (whiteTiles + 1);
				penalty += whiteLeaves;
				if (blackLeaves > blackTiles - (whiteTiles + 1))
					penalty += blackLeaves - (blackTiles - (whiteTiles + 1));
			}

			else
			{
				if (whiteTiles > blackTiles + 1)
				{
					penalty += whiteTiles - (blackTiles + 1);
					penalty += blackLeaves;
					if (whiteLeaves > whiteTiles - (blackTiles + 1))
						penalty += whiteLeaves - (whiteTiles - (blackTiles + 1));
				}
				else
				{
					penalty += Math.Max(0, whiteLeaves + blackLeaves - 2);
				}
			}

			penalty += ComponentGovernor.highestGovernors.Count() - 1;  //number of components
			penalty += distanceToNearestNonVisited(s) - 1;

			double result = nonVisitedTiles + penalty + (double)visitedTouchingNonVisited / (allTilesbyIDs.Count() * 4);
			return result + computeDistnaceTSP(s);
		}

		private int distanceToNearestNonVisited(VisitAllState s)
		{
			HashSet<int> processedItems = new HashSet<int>();
			Queue<int> queue = new Queue<int>();
			processedItems.Add(s.position);
			queue.Enqueue(s.position);
			while(queue.Count > 0)
			{
				var item = queue.Dequeue();
				if (!s.visited[item])
				{
					return manhatonDistance(allTilesbyIDs[item], allTilesbyIDs[s.position]);
				}
				Tile t = allTilesbyIDs[item];
				foreach (var succ in t.node.successors)
				{
					if (!processedItems.Contains(succ.ID))
					{
						processedItems.Add(succ.ID);
						queue.Enqueue(succ.ID);
					}
				}
			}
			return int.MaxValue;
		}

		private int manhatonDistance(Tile t1, Tile t2)
		{
			return Math.Abs(t1.node.gridCoordX - t2.node.gridCoordX) + Math.Abs(t1.node.gridCoordY - t2.node.gridCoordY);
		}

		public void init()
		{
			allTilesbyIDs = new Dictionary<int, Tile>();
			blackTiles = 0;
			whiteTiles = 0;
			visited = 0;
			blackLeaves = 0;
			whiteLeaves = 0;
			ComponentGovernor.highestGovernors.Clear();
			visitedTouchingNonVisited = 0;
		}

		private class Tile
		{
			public int ID => node.ID;
			public VisitAllNode node;
			public bool isBlack;
			ComponentGovernor governor;
			public bool isVisited;
			public int visitedConectedToNonVisited = 0;

			/// <summary>
			/// This is true iff it is not visited and has only one non-visited neighour.
			/// </summary>
			public bool isLeaf;

			public Tile(VisitAllNode node, VisitAllState state)
			{
				this.node = node;
				/*
				if (node.ID == 119)
				{

				}
				*/
				this.isLeaf = false;
				this.isBlack = (node.gridCoordX + node.gridCoordY) % 2 == 0;
				this.isVisited = state.visited[this.ID];
				int nonVisitedNeighours = 0;
				foreach (var item in this.node.successors)
				{
					if (!state.visited[item.ID])
						nonVisitedNeighours++;
				}
				if (!this.isVisited && nonVisitedNeighours == 1)
					this.isLeaf = true;
				if (this.isVisited && nonVisitedNeighours > 0)
					this.visitedConectedToNonVisited = nonVisitedNeighours;
				if (!isVisited)
					this.governor = new ComponentGovernor();
			}

			public void computeGovernance(Dictionary<int, Tile> allTilesbyIDs)
			{
				if (this.isVisited)
					return;

				foreach (var item in this.node.successors)
				{
					if (!allTilesbyIDs[item.ID].isVisited)
						allTilesbyIDs[item.ID].governor.getHighestGovernor().setGovernor(this.governor.getHighestGovernor());
				}
			}
		}

		private class ComponentGovernor
		{
			private static int IDCounter = 0;
			public static HashSet<int> highestGovernors = new HashSet<int>();

			public int ID;
			public ComponentGovernor governor;
			public bool isSelfGoverned = false;
			public ComponentGovernor getHighestGovernor()
			{
				ComponentGovernor g = this;
				while(!g.isSelfGoverned)
				{
					g = g.governor;
				}
				return g;
			}
			public void setGovernor(ComponentGovernor g)
			{
				if (g.ID == this.ID)
					return;

				this.isSelfGoverned = false;
				this.governor = g;
				if (highestGovernors.Contains(this.ID))
					highestGovernors.Remove(this.ID);
			}

			public ComponentGovernor()
			{
				this.isSelfGoverned = true;
				this.ID = IDCounter++;
				this.governor = this;
				highestGovernors.Add(this.ID);
			}
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
			this.domain = domain;
		}

		public (TSPInput input, int position) toTSP()
		{
			TSPInput i = TSPInput.create((point1, point2) => Math.Abs(point1.x - point2.x) + Math.Abs(point1.y - point2.y));
			int realPosition = 0;
			foreach (var item in this.domain.nodes)
			{
				if (!visited[item.ID] || position == item.ID)
				{
					var point = TSPPoint.create(item.gridCoordX, item.gridCoordY);
					i.addPoint(point);
					if (position == item.ID)
					{
						realPosition = point.ID;
					}
				}
			}
			return (i, realPosition);
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
		Brush visitedBrush = Brushes.Yellow,
			idStringBrush = Brushes.DarkCyan;
		int maxGridWidth, maxGridHeigth;
		float tileSize;
		VisitAllVisForm form;
		float targetCrossMarginPercent = 20f;
		Font IDStringFont = new Font("Arial", 10);

		protected List<SASState> statesToDraw;
		protected int alreadyDrawnStates = 0;

		private bool drawAnotherState()
		{
			if (statesToDraw == null || alreadyDrawnStates >= statesToDraw.Count)
				return false;
			draw(new VisitAllState(statesToDraw[alreadyDrawnStates], domain));
			alreadyDrawnStates++;
			return true;
		}

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

		public void draw(List<SASState> states)
		{
			this.statesToDraw = states;
			alreadyDrawnStates = 0;
			new System.Threading.Thread(() =>
			{
				form.startTimer(drawAnotherState, () => true);
				Application.Run(form);
				//form.Show();
				
			}).Start();

			System.Threading.Thread.CurrentThread.Join();
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
						g.FillRectangle(visitedBrush, i * tileSize + 1, j * tileSize + 1, tileSize - 2, tileSize - 2);

					if (state?.position == node.ID)
					{
						g.FillEllipse(Brushes.BlueViolet, i * tileSize + tileSize * targetCrossMarginPercent / 100, j * tileSize + tileSize * targetCrossMarginPercent / 100,
						tileSize - 2 * tileSize * targetCrossMarginPercent / 100, tileSize - 2 * tileSize * targetCrossMarginPercent / 100);
					}
					g.DrawString(node.ID.ToString(), IDStringFont, idStringBrush, i * tileSize + 1, j * tileSize + 1);
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
			if (!form.Visible) form.ShowDialog();
		}
	}
}
