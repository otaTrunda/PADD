using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    class GreedySolver : TSPSolver
    {
        protected HashSet<int> available;

        public GreedySolver()
        {
            available = new HashSet<int>();
        }

        #region TSPSolver Members

        protected virtual int findBest(int from, TSPInput input) 
        {
            int best = -1;
            double bestDistance = double.MaxValue;
            foreach (var item in available)
            {
                double distance = input.getDistance(from, item);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = item;
                }
            }
            return best;
        }

        public override TSPSolution solve(TSPInput input)
        {
            available.Clear();
            for (int i = 1; i < input.nodesCount; i++)
                available.Add(i);
            TSPSolution result = new TSPSolution(input);
            int j = 0;
            for (int i = 0; i < input.nodesCount - 1; i++)
            {
                int best = findBest(j, input);
                result.setSuccessor(j, best);
                j = best;
                available.Remove(best);
            }
            result.setSuccessor(j, 0);
            return result;
        }

		public override TSPSolution solvePath(TSPInput input, int startNode, int endNode)
		{
			available.Clear();
			for (int i = 0; i < input.nodesCount; i++)
				available.Add(i);
			TSPSolution result = new TSPSolutionPath(input, startNode, endNode);

			int j = startNode;
			available.Remove(startNode);
			available.Remove(endNode);
			result.setSuccessor(endNode, startNode);
			for (int i = 0; i < input.nodesCount - 2; i++)
			{
				int best = findBest(j, input);
				result.setSuccessor(j, best);
				j = best;
				available.Remove(best);
			}
			result.setSuccessor(j, endNode);
			return result;
		}

		#endregion
	}

    class GreedySolverFactory : TSPSolverFactory<GreedySolver>
    {
        #region TSPSolverFactory<RandomSolver> Members

        public GreedySolver create()
        {
            return new GreedySolver();
        }

        #endregion
    }

    class GreedyImprovedSolver : GreedySolver
    {
		int pathStart, pathEnd;
		bool solvingPath = false;

		/// <summary>
		/// Computes average of distance of all non-visited nodes from given node.
		/// </summary>
		/// <param name="point"></param>
		protected double averageDistanceOfNonVisitedFrom(int point, TSPInput input)
		{
			return available.Average(r => input.getDistance(point, r));
		}

        protected override int findBest(int from, TSPInput input)
        {
			int best = -1;
			double bestEvaluation = double.MaxValue;
			foreach (var item in available)
			{
				double evaluation = input.getDistance(from, item);

				if (solvingPath)
				{
					evaluation += airDistance(this.pathStart, item, input) / input.maximumDistance;
					evaluation -= airDistance(this.pathEnd, item, input) / (input.maximumDistance);
				}
				if (isLeaf(item, input))
					evaluation -= input.minimumDistance;

				if (evaluation < bestEvaluation)
				{
					bestEvaluation = evaluation;
					best = item;
				}
			}
			return best;
		}

		/// <summary>
		/// Node is leaf if there is only one other node such that their distance is the minimalDistance (among all nodes)
		/// </summary>
		/// <param name="node"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		protected bool isLeaf(int node, TSPInput input)
		{
			int neighbours = available.Select(r => input.getDistance(r, node)).Where(d => d == input.minimumDistance).Count();
			if (input.getDistance(this.pathEnd, node) == input.minimumDistance)
				neighbours++;
			return neighbours == 1;
		}

		protected double airDistance(int point1, int point2, TSPInput input)
		{
			var p1 = input.getPoint(point1);
			var p2 = input.getPoint(point2);
			return Math.Sqrt(Math.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y)));
		}

		public override TSPSolution solvePath(TSPInput input, int startNode, int endNode)
		{
			this.solvingPath = true;
			this.pathStart = startNode;
			this.pathEnd = endNode;
			var result = base.solvePath(input, startNode, endNode);
			solvingPath = false;
			return result;
		}
	}

    class GreedyImprovedSolverFactory : TSPSolverFactory<GreedyImprovedSolver>
    {
        #region TSPSolverFactory<RandomSolver> Members

        public GreedyImprovedSolver create()
        {
            return new GreedyImprovedSolver();
        }

        #endregion
    }

    class GreedyGrowingSolver : TSPSolver
    {
        struct edge
        {
            public int node1, node2;
            public double distance;

            public edge(int node1, int node2, double distance)
            {
                this.node1 = node1;
                this.node2 = node2;
                this.distance = distance;
				if (distance == double.NaN || node1 == node2)
				{

				}
            }
        }

        private int edgesUsed = 0;
        List<List<int>> succ;

        public GreedyGrowingSolver()
        {
            succ = new List<List<int>>();
        }

        public override TSPSolution solve(TSPInput input)
        {
			return solve(input, false, 0, 0);
        }

        private void addEdgesToResult(TSPSolution result)
        {
            int node = 0, succ = 0, pred = -1;
            for (int i = 0; i < result.inp.nodesCount; i++)
            {
                succ = getSuccessor(node, pred);
                result.setSuccessor(node, succ);
                pred = node;
                node = succ;
            }
        }

        private void addToSolution(edge e)
        {
            succ[e.node1].Add(e.node2);
            succ[e.node2].Add(e.node1);
            edgesUsed++;
        }

        private bool createsCycle(edge e)
        {
            if (succ[e.node1].Count < 1 && succ[e.node2].Count < 1)
                return false;

            int node = e.node1, pred = e.node2;
            while(node != e.node2)
            {
                if (node == -1)
                    return false;
                int pom = node;
                node = getSuccessor(node, pred);
                pred = pom;
            }
            return true;
        }

        private int getSuccessor(int node, int predecessor)
        {
            if (succ[node].Count == 0)
                return -1;
            if (succ[node][0] == predecessor)
            {
                if (succ[node].Count == 1)
                    return -1;
                return succ[node][1];
            }
            return succ[node][0];
        }

		private TSPSolution solve(TSPInput input, bool hasEdge = false, int startNode = 0, int endNode = 0)
		{
			TSPSolution result = null;
			if (hasEdge)
				result = new TSPSolutionPath(input, startNode, endNode);
			else result = new TSPSolution(input);
			succ = new List<List<int>>();
			edge predefinedEdge = default;

			edgesUsed = 0;
			List<edge> edges = new List<edge>();
			for (int i = 0; i < input.nodesCount; i++)
			{
				succ.Add(new List<int>());
				for (int j = 0; j < i; j++)
				{
					edge ed = new edge(i, j, input.getDistance(i, j));
					if (hasEdge && (startNode == i && endNode == j) || (startNode == j && endNode == i))
						predefinedEdge = ed;
					else edges.Add(ed);
				}
			}
			if (hasEdge)
				addToSolution(predefinedEdge);

			edges.Sort((a, b) => (int)(a.distance - b.distance));
			int index = -1;
			while (edgesUsed < input.nodesCount)
			{
				index++;
				edge e = edges[index];
				if (succ[e.node1].Count >= 2 || succ[e.node2].Count >= 2)
					continue;
				if (createsCycle(e) && edgesUsed != input.nodesCount - 1)
					continue;
				addToSolution(e);
			}
			addEdgesToResult(result);
			return result;
		}

		public override TSPSolution solvePath(TSPInput input, int startNode, int endNode)
		{
			return solve(input, true, startNode, endNode);
		}
	}
    class GreedyGrowingSolverSolverFactory : TSPSolverFactory<GreedyGrowingSolver>
    {
        #region TSPSolverFactory<GreedyGrowingSolver> Members

        public GreedyGrowingSolver create()
        {
            return new GreedyGrowingSolver();
        }

        #endregion
    }


}
