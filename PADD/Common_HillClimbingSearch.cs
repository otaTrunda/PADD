using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    class HillClimbingSearch : HeuristicSearchEngine
    {
        private IState currentState;

        public override int Search(bool quiet = false)
        {
            List<int> bestOperators = new List<int>(); //list of operators that are equally good and all of them are best
            solution = new SolutionPlan();
            PrintMessage("search started. HillClimbingSearch on " + problem.GetProblemName() + ", " + heuristic.ToString(), quiet);
            DateTime start = DateTime.Now;

            int length = 0;
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
                    else if(val == bestVal)
                        bestOperators.Add(op.GetOrderIndex());
                }
                bestOp = ((SASProblem)problem).GetOperators()[bestOperators[Program.r.Next(bestOperators.Count)]];
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
}
