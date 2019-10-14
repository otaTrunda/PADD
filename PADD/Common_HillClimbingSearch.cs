using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    public class HillClimbingSearch : HeuristicSearchEngine
    {
        private IState currentState;
		private Action<SASState> onImprovement;
		Random r = new Random(456);

        public override int Search(bool quiet = false)
        {
            List<int> bestOperators = new List<int>(); //list of operators that are equally good and all of them are best
            solution = new SolutionPlan();
            PrintMessage("search started. HillClimbingSearch on " + problem.GetProblemName() + ", " + heuristic.ToString(), quiet);
            DateTime start = DateTime.Now;

            int length = 0;
			double previousBest = -1;
            currentState = problem.GetInitialState();
            while (!problem.IsGoalState(currentState))
            {
                var successors = problem.GetAllSuccessors(currentState);
                if (successors.Count == 0)
                {
                    PrintMessage("search FAILED - deadend reached", quiet);
                    break;
                }
                double bestVal = double.MaxValue;
                IOperator bestOp = null;

				foreach (var succ in successors)
				{
					IOperator op = succ.GetOperator();
					double val = op.GetCost() + heuristic.getValue(succ.GetSuccessorState());
					if (val < bestVal)
					{
						bestVal = val;
						bestOperators.Clear();
						bestOperators.Add(op.GetOrderIndex());
					}
					else if (val == bestVal)
						bestOperators.Add(op.GetOrderIndex());
				}
                bestOp = ((SASProblem)problem).GetOperators()[bestOperators[r.Next(bestOperators.Count)]];
				Console.WriteLine("bestVal: " + bestVal);
                solution.AppendOperator(bestOp);
                currentState = bestOp.Apply(currentState);// successors[bestOp];
                length += bestOp.GetCost();
            }
            DateTime end = DateTime.Now;
            PrintMessage("search ended in " + (end - start).TotalSeconds + " seconds", quiet);
            PrintMessage("plan length " + length, quiet);
            return length;
        }

        public HillClimbingSearch(IPlanningProblem d, Heuristic h)
        {
            this.problem = d;
            this.heuristic = h;
        }


    }

	/// <summary>
	/// Should give similar results as AStar with weighted heuristic with weight going to infinity.
	/// </summary>
	internal class GreedyBFS : AStarSearch
	{
		public GreedyBFS(IPlanningProblem d, Heuristic h) : base(d, h)
		{
		}

		protected override bool addToOpenList(IState s, int gValue, IState pred, IOperator op = null)
		{
			if (!gValues.ContainsKey(s))
			{
				double hValue = heuristic.getValue(s);
				gValues.Add(s, new StateInformation(gValue));
				predecessor.Add(s, pred);
				if (!double.IsInfinity(hValue)) //infinity heuristic indicates dead-end
					openNodes.insert(hValue, s);  //breaking ties in favor of nodes that have lesser heuristic estimates. Heuristic value should always be less than 10000 (!!!)
				return true;
			}
			if (gValues[s].gValue > gValue)
			{
				StateInformation f = gValues[s];
				f.gValue = gValue;
				gValues[s] = f;
				predecessor[s] = pred;
				if (!f.isClosed)
				{
					double hValue = heuristic.getValue(s);
					if (!double.IsInfinity(hValue)) //infinity heuristic indicates dead-end
						openNodes.insert(hValue, s);  //breaking ties in favor of nodes that have lesser heuristic estimates
					return true;
				}
			}
			return false;
		}

		public override string getDescription()
		{
			return "Greedy Best-first search";
		}
	}
}
