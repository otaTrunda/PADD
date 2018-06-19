using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSP;

namespace PADD.DomainDependentSolvers.VisitAll
{
	class VisitAllDomain
	{
		public List<VisitAllNode> nodes;
		public Dictionary<int, int> nodeIDByVariableNo;
		public Dictionary<int, int> variableNoByNodeID;
		public Dictionary<string, int> nodeIDByOrigName;
		public Dictionary<int, Dictionary<int, int>> nodeIDByCoordinates;
		public bool[,] connected;
		public int[,] shortestDistances;
		public int positionVariable;
		public int startPosition;
		public SASProblem visitAllproblem;

		public VisitAllDomain(VisitAllSolver solver, SASProblem visitAllproblem)
		{
			nodes = new List<VisitAllNode>();
			nodeIDByVariableNo = new Dictionary<int, int>();
			nodeIDByOrigName = new Dictionary<string, int>();
			variableNoByNodeID = new Dictionary<int, int>();
			nodeIDByCoordinates = new Dictionary<int, Dictionary<int, int>>();
			this.visitAllproblem = visitAllproblem;

			positionVariable = solver.allVariables.IndexOf(solver.allVariables.Where(v => v.valuesSymbolicMeaning.Any(s => s.Contains("at-robot"))).Single());
			startPosition = ((SASState)visitAllproblem.GetInitialState()).GetValue(positionVariable);
			for (int i = 0; i < visitAllproblem.GetVariablesRanges()[positionVariable]; i++)
			{
				string meaning = solver.getSymbolicMeaning(positionVariable, i);
				var splitted = meaning.Split('(').Skip(1).Single();
				var node = new VisitAllNode(splitted.Substring(0, splitted.Length - 1));
				nodes.Add(node);
				nodeIDByOrigName.Add(node.originalName, node.ID);
				if (!nodeIDByCoordinates.ContainsKey(node.gridCoordX))
					nodeIDByCoordinates.Add(node.gridCoordX, new Dictionary<int, int>());
				nodeIDByCoordinates[node.gridCoordX].Add(node.gridCoordY, node.ID);
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
					variableNoByNodeID.Add(node.ID, i);
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

		protected int getNodeIDByTSPNodeID(int TSPNodeID, TSPSolutionPath solution)
		{
			var TSPNode = solution.inp.getPoint(TSPNodeID);
			return nodeIDByCoordinates[(int)TSPNode.x][(int)TSPNode.y];
		}

		public List<SASState> transformToPlan(TSPSolutionPath solution)
		{
			List<SASState> result = new List<SASState>();
			SASState initialState = (SASState)visitAllproblem.GetInitialState();
			int[] currentValues = new int[initialState.GetAllValues().Length];
			for (int i = 0; i < currentValues.Length; i++)
				currentValues[i] = 1;
			int currentPosition = solution.startNode;
			int previous = solution.endNode;
			int successor = 0;

			for (int i = 0; i < solution.inp.nodesCount - 1; i++)
			{
				if (currentValues.Skip(1).All(v => v == 0)) //everything waas visited
					return result;
				int currentNodeID = getNodeIDByTSPNodeID(currentPosition, solution);
				currentValues[0] = currentNodeID;
				if (variableNoByNodeID.ContainsKey(currentNodeID))
					currentValues[variableNoByNodeID[currentNodeID]] = 0;
				successor = solution.getSuccessor(currentPosition, previous);
				int successorNodeID = getNodeIDByTSPNodeID(successor, solution);
				if (currentNodeID == successorNodeID)
					continue;
				if (connected[currentNodeID, successorNodeID])
				{
					previous = currentPosition;
					SASState currentState = new SASState(visitAllproblem, currentValues.ToList().ToArray());
					result.Add(currentState);
				}
				else
				{
					addPath(result, currentPosition, successor, out previous, currentValues, solution);
				}
				currentPosition = successor;
			}
			currentValues[0] = getNodeIDByTSPNodeID(currentPosition, solution);
			currentValues[variableNoByNodeID[getNodeIDByTSPNodeID(currentPosition, solution)]] = 0;
			result.Add(new SASState(visitAllproblem, currentValues.ToList().ToArray()));
			return result;
		}

		private void addPath(List<SASState> result, int currentPositionTSP, int targetPositionTSP, out int previousTSP, int[] currentValues, TSPSolutionPath solution)
		{
			//VisitAllVisualizer vis = new VisitAllVisualizer(this);
			//vis.draw(new VisitAllState(new SASState(visitAllproblem, currentValues.ToList().ToArray()), this));
			int pos = currentPositionTSP,
				succ = 0,
				prev = 0;
			var targetNode = this.nodes[getNodeIDByTSPNodeID(targetPositionTSP, solution)];
			var node = this.nodes[getNodeIDByTSPNodeID(currentPositionTSP, solution)];
			SASState currentState = new SASState(visitAllproblem, currentValues.ToList().ToArray());
			result.Add(currentState);
			do
			{
				if (node.gridCoordX < targetNode.gridCoordX)
					succ = node.successors.Where(s => s.gridCoordX > node.gridCoordX).Single().ID;
				if (node.gridCoordX > targetNode.gridCoordX)
					succ = node.successors.Where(s => s.gridCoordX < node.gridCoordX).Single().ID;
				if (node.gridCoordY < targetNode.gridCoordY)
					succ = node.successors.Where(s => s.gridCoordY > node.gridCoordY).Single().ID;
				if (node.gridCoordY > targetNode.gridCoordY)
					succ = node.successors.Where(s => s.gridCoordY < node.gridCoordY).Single().ID;
				currentValues[0] = succ;
				currentValues[variableNoByNodeID[succ]] = 0;
				currentState = new SASState(visitAllproblem, currentValues.ToList().ToArray());
				result.Add(currentState);
				prev = pos;
				pos = succ;
				node = nodes[pos];
			} while (node.ID != targetNode.ID);
			previousTSP = Enumerable.Range(0, solution.inp.nodesCount).Where(r => getNodeIDByTSPNodeID(r, solution) == prev).Single();
		}
	}
}
