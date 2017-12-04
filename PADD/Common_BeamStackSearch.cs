using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    class BeamStackSearch : AStarSearch
    {
        private int maxWidth;
        private LinkedList<IState> bestSuccessors;
        private LinkedList<double> best_hValues;
        private LinkedList<int> bestOperatorsCosts;

        private void selectBestSuccessors(Successors successors)
        {
            bestSuccessors.Clear();
            best_hValues.Clear();
            bestOperatorsCosts.Clear();
            foreach (var succ in successors)
            {
                IState state = succ.GetSuccessorState();
                int opCost = succ.GetOperator().GetCost();

                if (bestSuccessors.Count >= maxWidth && bestOperatorsCosts.Last.Value + best_hValues.Last.Value < opCost)
                    continue;

                double hVal = heuristic.getValue(state);
                if (bestSuccessors.Count < maxWidth || bestOperatorsCosts.Last.Value + best_hValues.Last.Value > opCost + hVal)
                    addSuccessorCandidate(state, opCost, hVal);
            }
        }

        private void addSuccessorCandidate(IState state, int cost, double hVal)
        {
            if (bestSuccessors.Count == 0)
            {
                bestSuccessors.AddFirst(state);
                best_hValues.AddFirst(hVal);
                bestOperatorsCosts.AddFirst(cost);
                return;
            }
            if (best_hValues.Last.Value + bestOperatorsCosts.Last.Value < cost + hVal)
            {
                bestSuccessors.AddLast(state);
                best_hValues.AddLast(hVal);
                bestOperatorsCosts.AddLast(cost);
                return;
            }

            LinkedListNode<double> hValIterator = best_hValues.Last;
            LinkedListNode<int> opIterator = bestOperatorsCosts.Last;
            LinkedListNode<IState> stateIterator = bestSuccessors.Last;
            while (hValIterator.Previous != null && opIterator.Previous.Value + hValIterator.Previous.Value > cost + hVal)
            {
                hValIterator = hValIterator.Previous;
                opIterator = opIterator.Previous;
                stateIterator = stateIterator.Previous;
            }
            bestSuccessors.AddBefore(stateIterator, state);
            best_hValues.AddBefore(hValIterator, hVal);
            bestOperatorsCosts.AddBefore(opIterator, cost);
            if (bestSuccessors.Count > maxWidth)
            {
                bestSuccessors.RemoveLast();
                best_hValues.RemoveLast();
                bestOperatorsCosts.RemoveLast();
            }
        }

        private void addBestSuccessorsToOpenList(int parrentGValue, IState predecessor)
        {
            LinkedListNode<double> hValIterator = best_hValues.First;
            LinkedListNode<int> opIterator = bestOperatorsCosts.First;
            LinkedListNode<IState> stateIterator = bestSuccessors.First;
            while (hValIterator != null)
            {
                addToOpenList(stateIterator.Value, opIterator.Value + parrentGValue, predecessor, hValIterator.Value);
                hValIterator = hValIterator.Next;
                opIterator = opIterator.Next;
                stateIterator = stateIterator.Next;
            }
        }

        public override int Search(bool quiet = false)
        {
            predecessor = new Dictionary<IState, IState>();
            PrintMessage("Search started. Algorithm: Beam search, width: " + maxWidth + " problem: " + problem.GetProblemName() + ", " + heuristic.ToString(), quiet);
            DateTime start = DateTime.Now;
            openNodes.insert(0, problem.GetInitialState());
            gValues.Add(problem.GetInitialState(), new StateInformation());
            predecessor.Add(problem.GetInitialState(), null);
            int steps = -1;
            while (openNodes.size() > 0)
            {
                steps++;
                if (steps % 100000 == 0)
                {
                    PrintMessage("Expanded nodes: " + (gValues.Count - openNodes.size()) +
                        "\tOpen nodes: " + openNodes.size() + "\tVisited nodes: " + gValues.Count +
                        "\tHeuristic calls: " + heuristic.statistics.heuristicCalls, quiet);
                    if (gValues.Count > memoryLimit)
                    {
                        PrintMessage("Search FAILED - memory limit exceeded.", quiet);
                        DateTime end = DateTime.Now;
                        PrintMessage("search ended in " + (end - start).TotalSeconds + " seconds", quiet);
                        break;
                    }
                }
                IState currentState = openNodes.removeMin();
                if (gValues[currentState].isClosed)
                    continue;
                addToClosedList(currentState);
                if (problem.IsGoalState(currentState))
                {
                    DateTime end = DateTime.Now;
                    int GVAL = gValues[currentState].gValue;
                    PrintMessage("search ended in " + (end - start).TotalSeconds + " seconds", quiet);
                    PrintMessage("Expanded nodes: " + (gValues.Count - openNodes.size()) + ", plan length " + GVAL, quiet);
                    this.solution = extractSolution(currentState);
                    return GVAL;
                }
                int currentGValue = gValues[currentState].gValue;
                selectBestSuccessors(problem.GetAllSuccessors(currentState));
                addBestSuccessorsToOpenList(currentGValue, currentState);
            }
            PrintMessage("No solution found.", quiet);
            return -1;
        }

        public BeamStackSearch(IPlanningProblem d, Heuristic h) : base(d, h)
        {
            this.problem = d;
            this.heuristic = h;
            this.gValues = new Dictionary<IState, StateInformation>();
            //this.openNodes = new LeftistHeap<int[]>();
            this.openNodes = new Heaps.RegularBinaryHeap<IState>();
            //this.openNodes = new BinomialHeap<int[]>();
            //this.openNodes = new SingleBucket<State>();
            this.maxWidth = 2;
            this.best_hValues = new LinkedList<double>();
            this.bestOperatorsCosts = new LinkedList<int>();
            this.bestSuccessors = new LinkedList<IState>();
        }
    }
}
