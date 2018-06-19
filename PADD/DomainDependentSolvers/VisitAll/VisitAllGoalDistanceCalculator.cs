using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSP;

namespace PADD.DomainDependentSolvers.VisitAll
{

	class VisitAllGoalDistanceCalculator
	{
		Dictionary<int, Tile> allTilesbyIDs;
		public int blackTiles,
			whiteTiles,
			visited,
			blackLeaves,
			whiteLeaves,
			visitedTouchingNonVisited;
		TSPSolver solver = new GreedyImprovedSolver();
		//TSPSolver solver = new GreedySolver();

		private double computeDistnaceTSP(VisitAllState s)
		{
			var tspinp = s.toTSP();
			return solver.solveStartPoint(tspinp.input, tspinp.position).totalDistance;
		}

		public double computeDistance(VisitAllState s)
		{
			//return computeDistnaceTSP(s);

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

			double result = 20 * nonVisitedTiles + penalty + (double)visitedTouchingNonVisited / (allTilesbyIDs.Count() * 4);
			return result;// + computeDistnaceTSP(s) / 10;
		}

		private int distanceToNearestNonVisited(VisitAllState s)
		{
			HashSet<int> processedItems = new HashSet<int>();
			Queue<int> queue = new Queue<int>();
			processedItems.Add(s.position);
			queue.Enqueue(s.position);
			while (queue.Count > 0)
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
				while (!g.isSelfGoverned)
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

}
