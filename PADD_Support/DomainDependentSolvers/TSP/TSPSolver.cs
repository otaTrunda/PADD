using PADD_Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils.ExtensionMethods;

namespace TSP
{
    abstract class TSPSolver
    {
        public abstract TSPSolution solve(TSPInput input);
		public abstract TSPSolution solvePath(TSPInput input, int startNode, int endNode);
		public virtual TSPSolution solveStartPoint(TSPInput input, int startNode)
		{
			if (input.nodesCount <= 1)
				return new TSPSolution(input);

			int endnode = Enumerable.Range(0, input.nodesCount).Select(x => input.getDistance(startNode, x)).MaxWithIndex().index;
			return solvePath(input, startNode, endnode);

			/*
			double bestDist = double.MaxValue;
			TSPSolution bestSolution = null;
			for (int i = 0; i < input.nodesCount; i++)
			{
				if (i == startNode)
					continue;
				var sol = solvePath(input, startNode, i);
				double dist = sol.totalDistance;
				if (dist < bestDist)
				{
					bestSolution = sol;
					bestDist = dist;
					if (dist <= input.minimumDistance * (input.nodesCount - 1))
						break;
					//break;
				}
			}
			return bestSolution;
			*/
		}
	}

    interface TSPSolverFactory<S>
        where S : TSPSolver
    {
        S create();
    }
}
