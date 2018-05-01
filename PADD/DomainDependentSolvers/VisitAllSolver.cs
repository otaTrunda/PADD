using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers
{
	class VisitAllSolver : DomainDependentSolver
	{
		VisitAllDomain dom;

		public override int Search(bool quiet = false)
		{
			throw new NotImplementedException();
		}

		protected override void init()
		{
			dom = new VisitAllDomain(this, sasProblem);
		}
	}

	class VisitAllNode
	{
		private static int IDCounter = 0;

		public int ID;
		public string originalName;
		public int variableNumber;
		public int gridCoordX, gridCoordY;

		public VisitAllNode(string originalName)
		{
			this.ID = IDCounter++;
			this.originalName = originalName;
			var splitted = originalName.Split('-').Skip(1).ToList();
			gridCoordX = int.Parse(splitted[0].Substring(1));
			gridCoordY = int.Parse(splitted[1].Substring(1));
		}
	}

	class VisitAllDomain
	{
		List<VisitAllNode> nodes;
		Dictionary<int, int> nodeIDByVariableNo;
		Dictionary<string, int> nodeIDByOrigName;
		bool[,] connected;
		int[,] shortestDistances;

		public VisitAllDomain(VisitAllSolver solver, SASProblem visitAllproblem)
		{
			nodes = new List<VisitAllNode>();
			nodeIDByVariableNo = new Dictionary<int, int>();
			nodeIDByOrigName = new Dictionary<string, int>();

			int positionVariable = solver.allVariables.IndexOf(solver.allVariables.Where(v => v.valuesSymbolicMeaning.Any(s => s.Contains("at-robot"))).Single());
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
			for (int i = 0; i < connected.GetLength(0); i++)
				for (int j = 0; j < connected.GetLength(1); j++)
				{
					connected[i, j] = false;
					if (i == j)
						connected[i, j] = true;
				}

			foreach (var op in visitAllproblem.GetOperators())
			{
				if (op.GetName().StartsWith("move"))
				{
					var splitted = op.GetName().Split(' ').Skip(1);
					string from = splitted.First(),
						to = splitted.Last();
					connected[nodeIDByOrigName[from], nodeIDByOrigName[to]] = true;
				}
			}
			//TODO jeste neco...?
		}
	}

	class VisitAllState
	{
		public bool[] visited;
		public int position;
	}
}
