using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using TSP;
using PAD.Planner.SAS;

namespace PADD.DomainDependentSolvers.VisitAll
{
	public class VisitAllSolver : DomainDependentSolver
	{
		protected VisitAllDomain dom;
		protected VisitAllVisualizer vis;
		double previousBest = double.MaxValue;
		int withoutImprovement = 0;
		bool drawNonimproving = false;
		bool drawTSPPlan = false;
		protected List<IState> plan;

		public override double Search(bool quiet = false)
		{
			TSPSolver solver = new GreedyImprovedSolver();
			var tspinp = new VisitAllState((IState)this.sasProblem.GetInitialState(), dom).toTSP();
			var solution = solver.solveStartPoint(tspinp.input, tspinp.position);
			plan = dom.transformToPlan((TSPSolutionPath)solution);
			if (drawTSPPlan)
			{
				vis.draw(plan);
			}

			VisitAllGoalDistanceCalculator c = new VisitAllGoalDistanceCalculator();
			var state = new VisitAllState((IState)this.sasProblem.GetInitialState(), dom);
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
			}
			return plan.Count() - 1;
			return c.computeDistance(state);
		}

		protected override void init()
		{
			previousBest = int.MaxValue;
			withoutImprovement = 0;
			VisitAllNode.resetIDCounter();

			dom = new VisitAllDomain(this, sasProblem);
			vis = new VisitAllVisualizer(dom);
			//vis.draw(new VisitAllState((IState)sasProblem.GetInitialState(), dom));
		}

		public void drawPlan(List<IState> states)
		{
			vis.draw(states);
		}

		public override List<string> getPDDLPlan()
		{
			List<string> result = new List<string>();
			int previousPos = new VisitAllState(plan[0], dom).position;
			for (int i = 1; i < plan.Count; i++)
			{
				var currentState = new VisitAllState(plan[i], dom);
				var pos = currentState.position;
				result.Add("(move " + dom.nodes[previousPos].originalName + " " + dom.nodes[pos].originalName + ")");
				previousPos = pos;
			}
			return result;
		}
	}
}
