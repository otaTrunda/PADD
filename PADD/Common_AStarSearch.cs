using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Collections;

namespace PADD
{
    public class SearchResults
    {
        public int problemID;
        public int randomWalkLength;
        public string domainName;
        public string problemName;
        public string algorithm;
        public int expandedNodesCount;
        public string heuristicName;
        public int timeInSeconds;
        public bool solutionFound;
        public int planLength;
        public double bestHeuristicValue;
        public double avgHeuristicValue;

        public override string ToString()
        {
            string delimiter = ";";

            StringBuilder sb = new StringBuilder();
            sb.Append(problemID);
            sb.Append(delimiter);

            sb.Append(randomWalkLength);
            sb.Append(delimiter);

            sb.Append(domainName);
            sb.Append(delimiter);

            sb.Append(problemName);
            sb.Append(delimiter);

            sb.Append(algorithm);
            sb.Append(delimiter);

            sb.Append(heuristicName);
            sb.Append(delimiter);

            sb.Append(timeInSeconds);
            sb.Append(delimiter);

            sb.Append(expandedNodesCount);
            sb.Append(delimiter);

            sb.Append(solutionFound);
            sb.Append(delimiter);

            sb.Append(planLength);
            sb.Append(delimiter);

            sb.Append(bestHeuristicValue);
            sb.Append(delimiter);

            sb.Append(avgHeuristicValue);
            sb.Append(delimiter);

            return sb.ToString();
        }
    }

    public struct StateInformation
    {
        public int gValue;
        public bool isClosed;

        public StateInformation(int val = 0)
        {
            this.gValue = val;
            this.isClosed = false;
        }

		public override string ToString()
		{
			return "{" + gValue + ", " + isClosed + "}";
		}
	}

    public class AStarSearch : HeuristicSearchEngine
    {
        public Stopwatch stopwatch = new Stopwatch();
        public SearchResults results;
        public IHeap<double, IState> openNodes;
        protected Dictionary<IState, StateInformation> gValues;
        protected Dictionary<IState, IState> predecessor;

        public TimeSpan searchTime;
        public TimeSpan timeLimit = TimeSpan.FromMinutes(5);

        protected const long memoryLimit = 5000000;

        protected void addToClosedList(IState state)
        {
            StateInformation f = gValues[state];
            f.isClosed = true;
            gValues[state] = f;
        }

		/// <summary>
		/// Adds an entry to the open list. Returns true if the entry was added. (Some entries may already be present and therefore are not added.)
		/// </summary>
		/// <param name="s"></param>
		/// <param name="gValue"></param>
		/// <param name="pred"></param>
		/// <param name="hValue"></param>
		/// <returns></returns>
        protected virtual bool addToOpenList(IState s, int gValue, IState pred, double hValue)
        {
            if (!gValues.ContainsKey(s))
            {
                gValues.Add(s, new StateInformation(gValue));
                predecessor.Add(s, pred);
                openNodes.insert(gValue + hValue, s);
                return true;
            }
            if (gValues[s].gValue > gValue)
            {
                StateInformation f = gValues[s];
                f.gValue = gValue;
                gValues[s] = f;
                predecessor[s] = pred;
                openNodes.insert(gValue + hValue, s);
                return true;
            }
			return false;
        }

		/// <summary>
		/// Adds an entry to the open list. Returns true if the entry was added. (Some entries may already be present and therefore are not added.)
		/// </summary>
		/// <param name="s"></param>
		/// <param name="gValue"></param>
		/// <param name="pred"></param>
		/// <returns></returns>
		protected virtual bool addToOpenList(IState s, int gValue, IState pred)
        {
            if (!gValues.ContainsKey(s))
            {
                double hValue = heuristic.getValue(s);
                gValues.Add(s, new StateInformation(gValue));
                predecessor.Add(s, pred);
				if (!double.IsInfinity(hValue))	//infinity heuristic indicates dead-end
					openNodes.insert(gValue + hValue + hValue / 10000, s);	//breaking ties in favor of nodes that have lesser heuristic estimates. Heuristic value should always be less than 10000 (!!!)
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
						openNodes.insert(gValue + hValue + hValue / 10000, s);	//breaking ties in favor of nodes that have lesser heuristic estimates
					return true;
				}
            }
			return false;
		}

        protected void setSearchResults()
        {
            this.results.expandedNodesCount = this.gValues.Count + this.openNodes.size();
            this.results.timeInSeconds = (int)stopwatch.Elapsed.TotalSeconds;
            this.results.solutionFound = false;
            results.planLength = -1;
        }

		protected virtual IState popFromOpenList()
		{
			return openNodes.removeMin();
		}

		protected virtual bool isOpenListEmpty()
		{
			if (openNodes.size() % 10000 == 0)
				printSearchStats(false);
			return openNodes.size() <= 0;
		}

		protected virtual void insertInitialState(IState initialState)
		{
			openNodes.insert(0, initialState);
			gValues.Add(initialState, new StateInformation());
			predecessor.Add(initialState, null);
		}

        public override int Search(bool quiet = false)
        {
			Stopwatch loggingWatch = Stopwatch.StartNew();
			TimeSpan loggingInterval = TimeSpan.FromMinutes(5);

			bool printHeapContent = false;

            gValues = new Dictionary<IState, StateInformation>();
            searchStatus = SearchStatus.InProgress;
            predecessor = new Dictionary<IState, IState>();
            PrintMessage("search started. Algorithm: " + getDescription() + ", problem: " + problem.GetProblemName() + ", " + heuristic.ToString(), quiet);
            if (!stopwatch.IsRunning)
                stopwatch.Start();

			insertInitialState(problem.GetInitialState());

            int steps = -1;
            while (!isOpenListEmpty())
			{
                steps++;
				if (loggingWatch.Elapsed > loggingInterval)
				{
					printSearchStats(quiet);
					PrintMessage("Time elapsed: " + stopwatch.Elapsed.TotalMinutes + " minutes");
					loggingWatch.Restart();
				}

                if (stopwatch.Elapsed > timeLimit)
                {
                    PrintMessage("Search FAILED - time limit exceeded.", quiet);
                    stopwatch.Stop();
                    searchTime = stopwatch.Elapsed;
                    PrintMessage("search ended in " + searchTime.TotalSeconds + " seconds", quiet);
                    printSearchStats(quiet);
                    searchStatus = SearchStatus.TimeLimitExceeded;
                    setSearchResults();
                    return -1;
                }

                if (gValues.Count > memoryLimit)
                {
                    PrintMessage("Search FAILED - memory limit exceeded.", quiet);
                    stopwatch.Stop();
                    searchTime = stopwatch.Elapsed;
                    PrintMessage("search ended in " + searchTime.TotalSeconds + " seconds", quiet);
                    printSearchStats(quiet);
                    searchStatus = SearchStatus.MemoryLimitExceeded;
                    setSearchResults();
                    return -1;
                }

#if DEBUG
				double HeurValOfexpandedNode = openNodes.size() > 0 ? openNodes.getMinKey() : 0;
#endif

				IState currentState = popFromOpenList();
                if (gValues[currentState].isClosed)
                    continue;
#if DEBUG
				if (printHeapContent)
				{
					Console.WriteLine("Current heap content: (" + (openNodes.size() + 1) + ")");
					Console.WriteLine(HeurValOfexpandedNode + "\t" + currentState.ToString());
					foreach (var item in openNodes.getAllElements().OrderBy(item => item.k))
					{
						Console.WriteLine(item.k + "\t" + item.v.ToString());
					}
					Console.WriteLine("expanded node:\t" + currentState.ToString());
					Console.WriteLine("heuristic calls:\t" + heuristic.statistics.heuristicCalls + "\tsumValue:\t" + heuristic.statistics.sumOfHeuristicVals + "\tavgValue:\t" + heuristic.statistics.getAverageHeurValue());
				}		
#endif

				addToClosedList(currentState);
                if (problem.IsGoalState(currentState))
                {
                    stopwatch.Stop();
                    searchTime = stopwatch.Elapsed;
                    int GVAL = gValues[currentState].gValue;
                    PrintMessage("search ended in " + searchTime.TotalSeconds + " seconds, plan length: " + GVAL, quiet);
                    printSearchStats(quiet);
                    searchStatus = SearchStatus.SolutionFound;
                    this.solution = extractSolution(currentState);
                    setSearchResults();
                    results.solutionFound = true;
                    results.planLength = GVAL;
                    return GVAL;
                }
                int currentGValue = gValues[currentState].gValue;
                var successors = problem.GetAllSuccessors(currentState);
                foreach (var succ in successors)
                {
                    if (stopwatch.Elapsed > timeLimit)
                    {
                        PrintMessage("Search FAILED - time limit exceeded.", quiet);
                        stopwatch.Stop();
                        searchTime = stopwatch.Elapsed;
                        PrintMessage("search ended in " + searchTime.TotalSeconds + " seconds", quiet);
                        printSearchStats(quiet);
                        searchStatus = SearchStatus.TimeLimitExceeded;
                        setSearchResults();
                        return -1;
                    }

                    IState state = succ.GetSuccessorState();
                    int gVal = currentGValue + succ.GetOperator().GetCost();
                    try
                    {
                        addToOpenList(state, gVal, currentState);
                    }
                    catch (OutOfMemoryException)
                    {
                        PrintMessage("Search FAILED - memory limit exceeded.", quiet);
                        stopwatch.Stop();
                        searchTime = stopwatch.Elapsed;
                        PrintMessage("search ended in " + searchTime.TotalSeconds + " seconds", quiet);
                        printSearchStats(quiet);
                        searchStatus = SearchStatus.MemoryLimitExceeded;
                        setSearchResults();
                        return -1;
                    }
                }
            }
            PrintMessage("No solution exists.", quiet);
            printSearchStats(quiet);
            if (searchStatus == SearchStatus.InProgress)
                searchStatus = SearchStatus.NoSolutionExist;
            setSearchResults();
            return -1;
        }

        protected SolutionPlan extractSolution(IState state)
        {
            // SAS format: a list of operators used (their indices)
            // PDDL format: a list of operators IDs + substitued constant IDs

            problem.ResetTransitionsTriggers();

            List<IOperator> result = new List<IOperator>();
            IState current = state;
            while (current != null)
            {
                IState pred = predecessor[current];
                if (pred == null)
                    break;
                var successors = problem.GetAllSuccessors(pred);
                foreach (var succ in successors)
                {
                    if (succ.GetSuccessorState().Equals(current))
                    {
                        result.Add(succ.GetOperator());
                        break;
                    }
                }
                current = pred;
            }

            result.Reverse();
            return new SolutionPlan(result);
        }

        public AStarSearch(IPlanningProblem d, Heuristic h)
        {
            this.problem = d;
            this.heuristic = h;
            this.searchStatus = SearchStatus.NotStarted;
            this.gValues = new Dictionary<IState, StateInformation>();
			//this.openNodes = new Heaps.LeftistHeap<State>();
			//this.openNodes = new Heaps.RegularBinaryHeap<IState>();
			//this.openNodes = new Heaps.RedBlackTreeHeap<IState>();
			this.openNodes = new Heaps.FibonacciHeap1<IState>();
			//this.openNodes = new Heaps.BinomialHeap<State>();
			//this.openNodes = new Heaps.SingleBucket<State>(200000);
			//this.openNodes = new Heaps.SingleBucket<State>(200*h.getValue(d.initialState));
			//this.openNodes = new Heaps.OrderedBagHeap<State>();
			//this.openNodes = new Heaps.OrderedMutliDictionaryHeap<State>();
			results = new SearchResults();
            if (heuristic != null)
                results.heuristicName = heuristic.getDescription();
            results.bestHeuristicValue = int.MaxValue;
        }

        public void setHeapDatastructure(IHeap<double, IState> structure)
        {
            this.openNodes = structure;
        }

        protected virtual void printSearchStats(bool quiet)
        {
            PrintMessage("Closed nodes: " + (gValues.Where(item => item.Value.isClosed).Count()) +
                        "\tOpen nodes: " + openNodes.size() +
                        //"\tHeuristic calls: " + heuristic.heuristicCalls +
                        "\tMin heuristic: " + heuristic.statistics.bestHeuristicValue +
                        "\tAvg heuristic: " + heuristic.statistics.getAverageHeurValue().ToString("0.###"), quiet);
        }

        public override string getDescription()
        {
            return "A*";
        }
    }

	/// <summary>
	/// Uses more open lists, the closed list is shared. The number of open lists corresponds to number of different heuristics
	/// </summary>
	public class MultipleOpenListsAStar : AStarSearch
	{
		int currentOpenList, numberOfOpenLists;
		List<Heuristic> heurs;
		List<IHeap<double, IState>> openLists;
		long openListsSize = 0;

		public MultipleOpenListsAStar(IPlanningProblem d, Heuristic h) : base(d, h)
		{
			this.currentOpenList = 0;
			this.numberOfOpenLists = 1;
			this.heurs = new List<Heuristic>() { h };
			openLists = new List<IHeap<double, IState>>();
			foreach (var item in heurs)
			{
				//openLists.Add(new Heaps.RedBlackTreeHeap<IState>());
				openLists.Add(new Heaps.FibonacciHeap1<IState>());
			}
		}

		public MultipleOpenListsAStar(IPlanningProblem d, List<Heuristic> heurs) : base(d, heurs.First())
		{
			this.currentOpenList = 0;
			this.numberOfOpenLists = heurs.Count;
			this.heurs = heurs;
			openLists = new List<IHeap<double, IState>>();
			foreach (var item in heurs)
			{
				//openLists.Add(new Heaps.RedBlackTreeHeap<IState>());
				openLists.Add(new Heaps.FibonacciHeap1<IState>());
			}
		}

		protected override bool addToOpenList(IState s, int gValue, IState pred)
		{
			if (!gValues.ContainsKey(s))
			{
				gValues.Add(s, new StateInformation(gValue));
				predecessor.Add(s, pred);
				double hFFValue = heurs[1].getValue(s);
				for (int i = 0; i < numberOfOpenLists; i++)
				{
					heuristic = heurs[i];
					openNodes = openLists[i];
					heuristic.sethFFValueForNextState(hFFValue);
					double hValue = heuristic.getValue(s);
					if (!double.IsInfinity(hValue)) //infinity heuristic indicates dead-end
					{
						openNodes.insert(gValue + hValue + hValue / 10000, s);  //breaking ties
						openListsSize++;
					}
				}
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
					double hFFValue = heurs[1].getValue(s);
					for (int i = 0; i < numberOfOpenLists; i++)
					{
						heuristic = heurs[i];
						openNodes = openLists[i];
						heuristic.sethFFValueForNextState(hFFValue);
						double hValue = heuristic.getValue(s);
						if (!double.IsInfinity(hValue)) //infinity heuristic indicates dead-end
						{
							openNodes.insert(gValue + hValue + hValue / 10000, s); //breaking ties
							openListsSize++;
						}
					}
				}
				return true;
			}
			return false;
		}

		protected override IState popFromOpenList()
		{
			do
			{
				currentOpenList = (currentOpenList + 1) % numberOfOpenLists;
				openNodes = openLists[currentOpenList];
			} while (openNodes.size() <= 0);
			openListsSize--;
			return base.popFromOpenList();
		}

		protected override bool isOpenListEmpty()
		{
			if (openLists[0].size() % 10000 == 0)
				printSearchStats(false);
#if (DEBUG)
			if (openListsSize != openLists.Sum(ol => ol.size()))
			{
				throw new Exception();
			}
#endif

			return openListsSize <= 0;
		}

		protected override void insertInitialState(IState initialState)
		{
			foreach (var item in openLists)
			{
				item.insert(0, initialState);
				openListsSize++;
			}

			gValues.Add(initialState, new StateInformation());
			predecessor.Add(initialState, null);
		}

		public override string getDescription()
		{
			return "Multiple open lists A*";
		}

		protected override void printSearchStats(bool quiet)
		{
			PrintMessage("Closed nodes: " + (gValues.Count) +
			"\tOpen nodes: (" + string.Join(", ", openLists.Select(op => op.size())) + ")" +
			//"\tHeuristic calls: " + heuristic.heuristicCalls +
			"\tMin heuristic: (" + string.Join(", ", heurs.Select(h => h.statistics.bestHeuristicValue)) + ")" +
			"\tAvg heuristic: (" + string.Join(", ", heurs.Select(h => h.statistics.getAverageHeurValue().ToString("0.###"))) + ")"
			, quiet);
		}

	}

	class f_limitedAStarSearch : AStarSearch
    {
        protected double f_limit = 250;

        public void setLimit(double limit)
        {
            this.f_limit = limit;
        }

        protected override bool addToOpenList(IState s, int gValue, IState pred)
        {
            if (gValue > f_limit)
                return false;
            if (!gValues.ContainsKey(s))
            {
                double hValue = heuristic.getValue(s);
                if (hValue + gValue > f_limit)
                    return false;
                gValues.Add(s, new StateInformation(gValue));
                predecessor.Add(s, pred);
                openNodes.insert(gValue + hValue, s);
                return true;
            }
            if (gValues[s].gValue > gValue)
            {
                double hValue = heuristic.getValue(s);
                StateInformation f = gValues[s];
                f.gValue = gValue;
                gValues[s] = f;
                predecessor[s] = pred;
                openNodes.insert(gValue + hValue, s);
                return true;
            }
			return false;
        }

        public f_limitedAStarSearch(IPlanningProblem d, Heuristic h)
            : base(d, h)
        {
        }

        public override string getDescription()
        {
            return "f-limitedA*, limit = " + f_limit;
        }

    }

    public class IDAStarSearch : AStarSearch
    {
        double limit, nearestBehindTheLimit;

        public IDAStarSearch(IPlanningProblem d, Heuristic h)
            : base(d, h)
        {
            openNodes = new SimpleStack();
        }

        public override int Search(bool quiet = false)
        {
            results = new SearchResults();
            results.heuristicName = heuristic.getDescription();
            results.bestHeuristicValue = int.MaxValue;

            limit = heuristic.getValue(problem.GetInitialState());
            nearestBehindTheLimit = double.MaxValue;
            stopwatch.Start();
            while (true)
            {
                int result = -1;
                PrintMessage("searching with f-limit = " + limit, quiet);
                result = base.Search(quiet);
                switch (searchStatus)
                {
                    case SearchStatus.SolutionFound:
                    case SearchStatus.TimeLimitExceeded:
                    case SearchStatus.MemoryLimitExceeded:
                        results.planLength = result;
                        results.expandedNodesCount = openNodes.size() + gValues.Count();
                        results.solutionFound = searchStatus == SearchStatus.SolutionFound;
                        results.timeInSeconds = (int)searchTime.TotalSeconds;
                        return result;

                    case SearchStatus.NoSolutionExist:
                        PrintMessage("------------- Search failed. Increasing the limit.", quiet);
                        limit = nearestBehindTheLimit;
                        nearestBehindTheLimit = int.MaxValue;
                        continue;
                    default:
                        throw new Exception();
                }
            }
        }

        protected override bool addToOpenList(IState s, int gValue, IState pred)
        {
            if (gValue > limit)
                return false;

            if (!gValues.ContainsKey(s))
            {
                double hValue = heuristic.getValue(s);
                double fValue = gValue + hValue;
                if (fValue > limit)
                {
                    if (fValue < nearestBehindTheLimit)
                        nearestBehindTheLimit = fValue;
                    return false;
                }
                gValues.Add(s, new StateInformation(gValue));
                predecessor.Add(s, pred);
                openNodes.insert(gValue + hValue, s);
                return true;
            }
            if (gValues[s].gValue > gValue)
            {
                double hValue = heuristic.getValue(s);
                StateInformation f = gValues[s];
                f.gValue = gValue;
                gValues[s] = f;
                predecessor[s] = pred;
                openNodes.insert(gValue + hValue, s);
                return true;
            }
			return false;
        }

        public override string getDescription()
        {
            return "IDA*";
        }
    }

    class SimpleStack : IHeap<double, IState>
    {
        private List<IState> stack;

        public SimpleStack()
        {
            this.stack = new List<IState>();
        }

        void IHeap<double, IState>.insert(double k, IState v)
        {
            stack.Add(v);
        }

        IState IHeap<double, IState>.getMin()
        {
            return stack[stack.Count - 1];
        }

        double IHeap<double, IState>.getMinKey()
        {
            throw new NotImplementedException();
        }

        IState IHeap<double, IState>.removeMin()
        {
            IState item = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return item;
        }

        bool IHeap<double, IState>.remove(IState v)
        {
            throw new NotImplementedException();
        }

        bool IHeap<double, IState>.change(IState v, double newKey)
        {
            throw new NotImplementedException();
        }

        int IHeap<double, IState>.size()
        {
            return stack.Count;
        }

        void IHeap<double, IState>.clear()
        {
            stack.Clear();
        }

        string IHeap<double, IState>.getName()
        {
            return "Simple stack";
        }

		public IEnumerable<(double k, IState v)> getAllElements()
		{
			throw new NotImplementedException();
		}
	}

	class SimpleQueue : IHeap<double, IState>
	{
		private LinkedList<IState> queue;

		public SimpleQueue()
		{
			this.queue = new LinkedList<IState>();
		}

		void IHeap<double, IState>.insert(double k, IState v)
		{
			queue.AddLast(v);
		}

		IState IHeap<double, IState>.getMin()
		{
			return queue.First.Value;
		}

		double IHeap<double, IState>.getMinKey()
		{
			throw new NotImplementedException();
		}

		IState IHeap<double, IState>.removeMin()
		{
			IState item = queue.First.Value;
			queue.RemoveFirst();
			return item;
		}

		bool IHeap<double, IState>.remove(IState v)
		{
			throw new NotImplementedException();
		}

		bool IHeap<double, IState>.change(IState v, double newKey)
		{
			throw new NotImplementedException();
		}

		int IHeap<double, IState>.size()
		{
			return queue.Count;
		}

		void IHeap<double, IState>.clear()
		{
			queue.Clear();
		}

		string IHeap<double, IState>.getName()
		{
			return "Simple queue";
		}

		public IEnumerable<(double k, IState v)> getAllElements()
		{
			throw new NotImplementedException();
		}
	}


}
