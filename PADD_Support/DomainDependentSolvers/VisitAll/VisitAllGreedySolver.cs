using PADD_Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PAD.Planner.SAS;

namespace PADD.DomainDependentSolvers.VisitAll
{
	/// <summary>
	/// Only works on the initial state!!
	/// </summary>
	class VisitAllGreedySolver : VisitAllSolver
	{
		protected VisitAllNode startNode;
		protected HashSet<int> visited;

		protected override void init()
		{
			base.init();
			this.plan = new List<IState>();
		}

		public override double Search(bool quiet = false)
		{
			startNode = dom.nodes[dom.startPosition];
			visited = new HashSet<int>();
			visited.Add(startNode.ID);

			VisitAllNode currentNode = startNode;
			IState currentState = (IState)dom.visitAllproblem.GetInitialState();
			plan.Clear();
			plan.Add(currentState);

			var operators = dom.visitAllproblem.Operators.GroupBy(op => op.GetPreconditions().Where(p => p.GetVariable() == 0).Single().GetValue()).ToDictionary(q => q.Key, 
				q => q.GroupBy(g => g.GetEffects().Where(eff => eff.GetAssignment().GetVariable() == 0).Single().GetAssignment().GetValue()).ToDictionary(r => r.Key, r => r.Single()));

			while (visited.Count < dom.nodes.Count)
			{
				var bestSucc = getSuccessors(currentNode).MaxElement(s => evaluateSuccessor(s));
				var op = getTransitionOperator(currentNode, bestSucc, operators);
				var newState = (IState)op.Apply(currentState);
				plan.Add(newState);
				currentNode = bestSucc;
				currentState = newState;
				visited.Add(bestSucc.ID);
			}
			return plan.Count() - 1;
		}

		private IOperator getTransitionOperator(VisitAllNode currentNode, VisitAllNode bestSucc, Dictionary<int, Dictionary<int, IOperator>> operators)
		{
			if (!dom.variableNoByNodeID.ContainsKey(currentNode.ID))
				return operators[((IState)dom.visitAllproblem.GetInitialState()).GetValue(0)][bestSucc.ID];

			return operators[currentNode.ID][bestSucc.ID];
		}

		/// <summary>
		/// Returns how good it is to visit the given successor
		/// </summary>
		/// <param name="successorNodeID"></param>
		/// <returns></returns>
		protected double evaluateSuccessor(VisitAllNode node)
		{
			if (getSuccessors(node).Count() <= 1)
				return double.MaxValue;
			return 1d / (getDistanceFromStart(node) + 1);
		}

		protected double getDistanceFromStart(VisitAllNode n)
		{
			return (n.gridCoordX - startNode.gridCoordX) * (n.gridCoordX - startNode.gridCoordX) + (n.gridCoordY - startNode.gridCoordY) * (n.gridCoordY - startNode.gridCoordY);
		}

		/// <summary>
		/// Returns all non-visited successors of given node.
		/// </summary>
		/// <param name="nodeID"></param>
		/// <returns></returns>
		protected IEnumerable<VisitAllNode> getSuccessors(VisitAllNode node)
		{
			var succ = node.successors.Where(s => !visited.Contains(s.ID)).ToList();
			if (succ.Count == 0)
			{
				//drawPlan(plan);
			}
			return node.successors.Where(s => !visited.Contains(s.ID));
		}
	}
}
