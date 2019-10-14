using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using BrightWire;
using BrightWire.ExecutionGraph;
using System.IO;
using BrightWire.Models;
using ProtoBuf;
using PAD.Planner.SAS;
using PAD.Planner.Heaps;
using PAD.Planner.Heuristics;
using PAD.Planner.Search;

namespace PADD_Support
{
	public class StateDistanceResult
	{
		public IState state;
		public double realDistance;
		public bool isSurelyCorrect;

		public StateDistanceResult(IState state, double currentGValue, bool isSurelyCorrect = true)
		{
			this.state = state;
			this.realDistance = currentGValue;
			this.isSurelyCorrect = isSurelyCorrect;
		}
	}

	public class StateInformation : PAD.Planner.Search.NodeInfo
	{
		public StateInformation(int gValue = 0) : base(gValue, null)
		{
		}
	}

	public class StateSpaceEnumerator
	{
		/// <summary>
		/// Enumerates all states in the state-space by backward search from goal states. Returns the states one by one together with their true goal-distances.
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static IEnumerable<StateDistanceResult> enumerateAllStatesWithDistances(PAD.Planner.IProblem p, bool useRelativeStates = true)
		{
			//if true, uses backward planning

			StateSpaceEnumeratorInternal e = new StateSpaceEnumeratorInternal(p, new BlindHeuristic());
			int randomWalksCount = 1000,
				randomWalksMaxLength = 200;
			bool useApproximation = false;

			if (useRelativeStates)
			{
				foreach (var item in e.EnumerateNEW(useApproximation, false))
				{
					yield return item;
				}
				yield break;

				if (e.searchStatus == ResultStatus.MemoryLimitExceeded || e.searchStatus == ResultStatus.TimeLimitExceeded)
				{
					foreach (var item in e.enumerateByRandomWalks(randomWalksCount, randomWalksMaxLength, true))
					{
						yield return item;
					}
					foreach (var item in e.enumerateByRandomWalksFromInitial(randomWalksCount, randomWalksMaxLength))
					{
						yield return item;
					}
				}
			}
			else
			{
				foreach (var item in e.Enumerate())
				{
					yield return item;
				}

				/*
				if (e.searchStatus == SearchStatus.MemoryLimitExceeded || e.searchStatus == SearchStatus.TimeLimitExceeded)
					foreach (var item in e.enumerateByRandomWalks(randomWalksCount, randomWalksMaxLength, false))
					{
						yield return item;
					}
				*/
			}
		}

		public static IEnumerable<IState> getAllStatesMeetingConditions(Dictionary<int, int> conditions, Problem domain)
		{
			return StateSpaceEnumeratorInternal.getAllStatesMeetingConditions(conditions, domain);
		}

		private class StateSpaceEnumeratorInternal : HeuristicSearch
		{
			public ResultStatus searchStatus;

			protected List<IState> allGoalRelativeStates;

			protected List<IState> getAllGoalRelativeStates()
			{
				if (allGoalRelativeStates == null)
					allGoalRelativeStates = gValues.EnumerateKeys.Where(k => gValues[k].GValue == 0).ToList();
				return allGoalRelativeStates;
			}

			public Stopwatch stopwatch = new Stopwatch(),
				statsIntervalWatch = new Stopwatch();
			public SearchResults results;
			public IHeap<double, IState> openNodes;
			protected SASStatePiecewiseDictionary<StateInformation> gValues;
			protected Dictionary<IState, IState> predecessor;
			protected Dictionary<IState, IOperator> predOperator = new Dictionary<IState, IOperator>();
			protected double branchingSum = 1,
				branchingCount = 1;

			private List<IOperator> operatorsPlaceholder = new List<IOperator>();

			protected double lengthShorteningCoeficient;

			public TimeSpan searchTime;
			public TimeSpan timeLimit = TimeSpan.FromHours(2);
			public TimeSpan statsPrintingInterval = TimeSpan.FromMinutes(5);

			//protected const long memoryLimit = 5000;
            protected long memoryLimit = 1000000000;

            protected void addToClosedList(IState state)
			{
				StateInformation f = gValues[state];
				f.IsClosed = true;
				gValues[state] = f;
			}

			protected virtual void addToOpenList(IState s, int gValue, IState pred, double hValue)
			{
				if (!gValues.ContainsKey((IState)s))
				{
					gValues.Add((IState)s, new StateInformation(gValue));
					//predecessor.Add(s, pred);
					openNodes.Add(gValue + hValue, s);
					return;
				}
				if (gValues[s].GValue > gValue)
				{
					throw new Exception();
					/*
					StateInformation f = gValues[s];
					f.GValue = gValue;
					gValues[s] = f;
					predecessor[s] = pred;
					openNodes.Add(gValue + hValue, s);
					return;
					*/
				}
			}

			protected virtual void addToOpenList(IState s, int gValue, IState pred, IOperator op)
			{
				if (!gValues.ContainsKey((IState)s))
				{
					if (gValues.ContainsMatchingState((IState)s))	//there is a matching state already added. Matching state will always have better gVal.
						return;

					//double hValue = heuristic.getValue(s);
					gValues.Add((IState)s, new StateInformation(gValue));
#if DEBUG
					predecessor.Add(s, pred);
					predOperator.Add(s, op);
#endif
					openNodes.Add(0, s);
					return;
				}
				if (gValues[s].GValue > gValue)
				{
					//throw new Exception();	//this is still possible when there are action costs involved
					
					//double hValue = heuristic.getValue(s);
					StateInformation f = gValues[s];
					f.GValue = gValue;
					gValues[s] = f;
#if DEBUG
					predecessor[s] = pred;
#endif
					openNodes.Add(gValue, s);
					return;
					
				}
			}

			protected void setSearchResults()
			{
				results.ClosedNodes = this.gValues.Count;
				results.OpenNodes = this.openNodes.GetSize();
				results.SearchTime = stopwatch.Elapsed;
				results.ResultStatus = ResultStatus.NoSolutionFound;
				results.SolutionCost = -1;
			}

			/// <summary>
			/// Recursive function to enumerate all goal states. Goal states are created by fixing variables mentioned in goal conditions and enumerating all combination of values for variables not mentioned in goal conditions.
			/// </summary>
			/// <param name="variable"></param>
			/// <param name="value"></param>
			/// <param name="domain"></param>
			/// <returns></returns>
			protected static IEnumerable<IState> getGoalStatesRecur(int variable, int value, Problem domain, IState result, Dictionary<int, int> goalConditions)
			{
				if (variable >= domain.Variables.Count)
				{
					yield return (IState)result.Clone();
				}

				else
				{
					if (goalConditions.ContainsKey(variable))
					{
						result.SetValue(variable, goalConditions[variable]);
						foreach (var item in getGoalStatesRecur(variable + 1, 0, domain, result, goalConditions))
						{
							yield return item;
						}
					}
					else
					{
						for (int val = 0; val < domain.Variables[variable].GetDomainRange(); val++)
						{
							result.SetValue(variable, val);
							foreach (var item in getGoalStatesRecur(variable + 1, 0, domain, result, goalConditions))
							{
								yield return item;
							}
						}
					}
				}
			}

			protected IEnumerable<IState> getAllGoalStates()
			{
				Problem f = (Problem)this.Problem;
				Dictionary<int, int> goalConditions = new Dictionary<int, int>();
				foreach (var condition in f.GoalConditions)
					goalConditions.Add(condition.GetVariable(), condition.GetValue());

				IState initialState = f.InitialState;
				for (int i = 0; i < f.Variables.Count; i++)
				{
					if (f.Variables[i].IsRigid())
					{
						goalConditions.Add(i, initialState.GetValue(i));
					}
				}

				List<int> addedConditions = new List<int>();

				foreach (var op in GetOperatorsThatCanBeLast())
				{
					addedConditions.Clear();
					foreach (var opPrecond in op.GetPreconditions())
					{
						if (!goalConditions.ContainsKey(opPrecond.GetVariable()))
						{
							addedConditions.Add(opPrecond.GetVariable());
							goalConditions.Add(opPrecond.GetVariable(), opPrecond.GetValue());
						}
					}

					IState res = (IState)f.GetInitialState();
					//List<IState> goalStates = new List<IState>();
					foreach (var item in getGoalStatesRecur(0, 0, f, res, goalConditions))
					{
#if DEBUG
						if (!Problem.IsGoalState(item))
							throw new Exception();
#endif

						//if (problem.GetAllPredecessors(item).Count > 0)
						yield return item;
					}

					foreach (var item in addedConditions)
					{
						goalConditions.Remove(item);
					}
				}
			}

			/// <summary>
			/// Returns a set of operators that can occur on the last place of a plan. I.e. operators, that add some goal conditions and do not destroy any goal conditions.
			/// </summary>
			/// <returns></returns>
			public List<IOperator> GetOperatorsThatCanBeLast()
			{
				Problem problem = (Problem)Problem;

				operatorsPlaceholder.Clear();
				foreach (var item in problem.Operators)
				{
					bool isRelevant = false,
					destroysGoal = false;
					foreach (var eff in item.GetEffects())
					{
						if (problem.GoalConditions.Any(cond => cond.GetVariable() == eff.GetAssignment().GetVariable()))
						{
							if (problem.GoalConditions.Where(cond => cond.GetVariable() == eff.GetAssignment().GetVariable()).Single().GetValue() == eff.GetAssignment().GetValue())
								isRelevant = true;
							else
							{
								destroysGoal = true;
								break;
							}
						}
					}
					if (isRelevant && !destroysGoal)
						operatorsPlaceholder.Add(item);
				}
				return operatorsPlaceholder;
			}

			/// <summary>
			/// Returns all states that meets specified conditions. It fixes variables mentioned in conditions to specified values and enumerates all combinations of values of other variables.
			/// </summary>
			/// <param name="conditions">Variable-value pair that all returned states must contain.</param>
			/// <returns></returns>
			public static IEnumerable<IState> getAllStatesMeetingConditions(Dictionary<int, int> conditions, Problem domain)
			{
				IState res = (IState)domain.GetInitialState();
				//List<IState> result = new List<IState>();
				foreach (var item in getGoalStatesRecur(0, 0, domain, res, conditions))
				{
					yield return item;
				}	 
			}

			private void estimateShorteningCoeficient(int numberOfSamples)
			{
				LogMessage("Estimating shortening coeficient");
				//List<IState> initialStates = gValues.Where(kw => kw.Value.GValue == 0).Select(kw => kw.Key).ToList();
				List<IState> initialStates = gValues.EnumerateKeys.Where(k => gValues[k].GValue == 0).ToList();
				Random r = new Random();
				List<PAD.Planner.IState> predecessors = new List<PAD.Planner.IState>();
				double coeffsSum = 0, coeffsCount = 0;

				for (int i = 0; i < numberOfSamples; i++)
				{
					IState currentState = initialStates[r.Next(initialStates.Count)];
					double walkLength = 0;

					if (statsIntervalWatch.Elapsed >= statsPrintingInterval)
					{
						Console.WriteLine("Elapsed minutes: " + statsIntervalWatch.Elapsed.TotalMinutes + " " + i * 100 / numberOfSamples + " %");
					}
					Stopwatch w = Stopwatch.StartNew();
					while (true)
					{
						if (w.Elapsed > TimeSpan.FromMinutes(2))
						{
							Console.WriteLine("Inside while cycle for more than 2 minutes. Breaking.."); ;
							break;
						}

						predecessors = Problem.GetPredecessorStates(currentState).ToList();
						if (predecessors.Count == 0)
							break;
						IState selectedPred = (IState)predecessors[r.Next(predecessors.Count)];
						if (!gValues.ContainsKey(selectedPred))
							break;
						currentState = selectedPred;
						walkLength++;
					}
					if (walkLength > 1)
					{
						int realDistance = gValues[currentState].GValue;
						double coeffSample = realDistance / walkLength;
						coeffsSum += coeffSample;
						coeffsCount++;
					}
					if (stopwatch.Elapsed > timeLimit)
						break;
				}
				lengthShorteningCoeficient = coeffsSum / coeffsCount;
				LogMessage("DONE Estimating shortening coeficient");
			}

			/// <summary>
			/// To be called only AFTER the Enumerate method. (Gvals have to be computed!).
			/// </summary>
			/// <param name="randomWalksCount"></param>
			/// <returns></returns>
			public IEnumerable<StateDistanceResult> enumerateByRandomWalks(int randomWalksCount, int walkMaxLength, bool relativeStatesUsed = false)
			{
				double walksLengthsSum = 1;
				int walksCount = 1,
					walksDiscarted = 0;
				Heuristic newHeur = new FFHeuristic((Problem)Problem);

				stopwatch.Restart();
				statsIntervalWatch.Restart();
				LogMessage("Enumerating by random walks from the goal state. Total count = " + randomWalksCount);
				Problem sasProblem = (Problem)Problem;
				IState initialState = (IState)sasProblem.GetInitialState();
				estimateShorteningCoeficient(randomWalksCount);
				stopwatch.Restart();
				//List<IState> initialStates = gValues.Where(kw => kw.Value.GValue == 0).Select(kw => kw.Key).ToList(),
				List<IState> initialStates = gValues.EnumerateKeys.Where(k => gValues[k].GValue == 0).ToList();

				List<PAD.Planner.IState> predecessors = new List<PAD.Planner.IState>();
				//int maxVisitedGVal = gValues.Max(kw => kw.Value.GValue);
				int maxVisitedGVal = gValues.EnumerateKeys.Max(k => gValues[k].GValue);

				Random r = new Random();
				List<IState> visitedStates = new List<IState>();

				for (int i = 0; i < randomWalksCount; i++)
				{
					if (statsIntervalWatch.Elapsed > statsPrintingInterval)
					{
						LogMessage("Walks generated: " + walksCount + " (" + ((double)walksCount * 100 / randomWalksCount).ToString("0.##") + 
							" %,\tAverage length: " + (walksLengthsSum / walksCount).ToString("0.##"));
						statsIntervalWatch.Restart();
					}
					int walkLength = 0;
					if (walkMaxLength > maxVisitedGVal)
						walkLength = r.Next(walkMaxLength - maxVisitedGVal) + maxVisitedGVal;
					else
						walkLength = walkLength = r.Next(walkMaxLength) + maxVisitedGVal;
					IState currentState = initialStates[r.Next(initialStates.Count)];
					visitedStates.Clear();
					int realWalkLength = 0;
					bool hasLeftVisitedArea = false;
					for (realWalkLength = 0; realWalkLength < walkLength; realWalkLength++)
					{
						visitedStates.Add(currentState);
						predecessors = Problem.GetPredecessorStates(currentState).Where(p => Problem.GetPredecessorStates(p).Count() > 0).Distinct().ToList();
						IState selectedPred = null;
						bool visitedByThisWalk = false,
							visitedBefore = false;
						Stopwatch ww = Stopwatch.StartNew();
						while (predecessors.Count > 0)
						{
							if (ww.Elapsed > TimeSpan.FromMinutes(2))
							{
								Console.WriteLine("Inside while cycle for more than 2 minutes. Breaking..");
								break;
							}
							int selectedIndex = r.Next(predecessors.Count);
							selectedPred = (IState)predecessors[selectedIndex];
							visitedByThisWalk = visitedStates.Contains(selectedPred);
							visitedBefore = gValues.ContainsKey((IState)selectedPred);
							if (visitedByThisWalk || (hasLeftVisitedArea && visitedBefore))
							{
								predecessors.RemoveAt(selectedIndex);
								selectedPred = null;
								continue;
							}
							break;
						}
						if (selectedPred == null)
							break;
						if (!hasLeftVisitedArea && !visitedBefore)
							hasLeftVisitedArea = true;
						currentState = selectedPred;
					}
					if (realWalkLength >= maxVisitedGVal)
					{
						walksCount++;

						IState endState = (IState)generateSample((RelativeState)currentState, 0, r, sasProblem, initialState, true).state;
						double realDistance = trySearchForShortestPathIntoVisitedTeritory(endState, newHeur, sasProblem);
						if (realDistance == -1)	//could not find the real distance, using aproximation
						{
							walksLengthsSum += realWalkLength;
							if (!relativeStatesUsed)
								yield return new StateDistanceResult(currentState, realWalkLength * lengthShorteningCoeficient);
							var sample = generateSample((RelativeState)currentState, realWalkLength * lengthShorteningCoeficient, r, sasProblem, initialState, false);
							sample.isSurelyCorrect = false;
							yield return sample;
						}
						else
						{
							walksLengthsSum += realDistance;
							StateDistanceResult sample = new StateDistanceResult(endState, realDistance, true);
							yield return sample;
						}
					}
					else
						walksDiscarted++;
					if (stopwatch.Elapsed > timeLimit)
					{
						LogMessage("canceling.. time limit reached");
						yield break;
					}
				}
				LogMessage("DONE Enumerating by random walks. Total walks generated: " + walksCount +  
					", walks discarted: " + walksDiscarted + ", average length: " + (walksLengthsSum / walksCount).ToString("0.##"));
			}

			/// <summary>
			/// To be called only AFTER the Enumerate method. (Gvals have to be computed!).
			/// </summary>
			/// <param name="randomWalksCount"></param>
			/// <returns></returns>
			public IEnumerable<StateDistanceResult> enumerateByRandomWalksFromInitial(int randomWalksCount, int walkMaxLength)
			{
				double walksLengthsToGoalSum = 1;
				int walksCount = 1,
					walksDiscarted = 0;
				Heuristic newHeur = new FFHeuristic((Problem)Problem);

				stopwatch.Restart();
				statsIntervalWatch.Restart();
				LogMessage("Enumerating by random walks from the initial state. Total count = " + randomWalksCount);
				Problem sasProblem = (Problem)Problem;
				IState initialState = (IState)sasProblem.GetInitialState();

				stopwatch.Restart();
				List<PAD.Planner.IState> successors = new List<PAD.Planner.IState>();
				Random r = new Random();

				List<IState> visitedStates = new List<IState>();

				for (int i = 0; i < randomWalksCount; i++)
				{
					if (statsIntervalWatch.Elapsed > statsPrintingInterval)
					{
						LogMessage("Walks generated: " + walksCount + " (" + ((double)walksCount * 100 / randomWalksCount).ToString("0.##") +
							" %,\tAverage length: " + (walksLengthsToGoalSum / walksCount).ToString("0.##"));
						statsIntervalWatch.Restart();
					}
					int walkLength = 0;

					IState currentState = initialState;
					visitedStates.Clear();
					int currentWalkLength = 0;

					for (currentWalkLength = 0; currentWalkLength < walkLength; currentWalkLength++)
					{
						visitedStates.Add(currentState);
						successors = Problem.GetSuccessors(currentState).Select(p => p.GetSuccessorState()).Where(p => Problem.GetSuccessors(p).Count() > 0).Distinct().ToList();
						if (successors.Count == 0)
							break;
						IState selectedSucc = (IState)successors[r.Next(successors.Count)];
						currentState = selectedSucc;
					}

					walksCount++;
					IState endState = (IState)currentState;
					double realDistanceToGoal = trySearchForShortestPathIntoVisitedTeritory(endState, newHeur, sasProblem);
					if (realDistanceToGoal == -1) //could not find the real distance, discarting
					{
						walksDiscarted++;
					}
					else
					{
						walksLengthsToGoalSum += realDistanceToGoal;
						StateDistanceResult sample = new StateDistanceResult(endState, realDistanceToGoal, true);
						yield return sample;
					}

					if (stopwatch.Elapsed > timeLimit)
					{
						LogMessage("canceling.. time limit reached");
						yield break;
					}
				}
				LogMessage("DONE Enumerating by random walks from the initial state. Total walks generated: " + walksCount +
					", walks discarted: " + walksDiscarted + ", average length to goal: " + (walksLengthsToGoalSum / (walksCount - walksDiscarted)).ToString("0.##"));
			}

			/// <summary>
			/// Tries to find a path from given state to some states that was visited by the backwards planning. It is used to compute a distance-estimate of the last state of a random walk.
			/// It returns -1 if it was not able to find any path to previously visited state, or returns the total length of the path (from given node to goal).
			/// </summary>
			/// <param name="state"></param>
			protected double trySearchForShortestPathIntoVisitedTeritory(IState state, Heuristic h, Problem prob)
			{
				Stopwatch newWatch = Stopwatch.StartNew();
				TimeSpan newLimit = TimeSpan.FromMinutes(1);

				Dictionary<IState, StateInformation> newClosedList = new Dictionary<IState, StateInformation>();
				IHeap<double, IState> newOpenList = new FibonacciHeap<IState>();
				newOpenList.Add(0, state);
				newClosedList.Add(state, new StateInformation(0));

				while(newOpenList.GetSize() > 0)
				{
					if (newWatch.Elapsed > newLimit)
						return -1;

					IState newCurrentState = (IState)newOpenList.RemoveMin();
					StateInformation currentStateInfo = newClosedList[newCurrentState];
					if (currentStateInfo.IsClosed)
						continue;
					currentStateInfo.IsClosed = true;
					newClosedList[newCurrentState] = currentStateInfo;

					var matchingStates = gValues.GetAllMatchingStates(newCurrentState, 10);
					if (matchingStates.Any())
					{
						int goalDistance = matchingStates.Min(s => s.Value.GValue);
						if (newClosedList[newCurrentState].GValue > 0)
						{

						}
						return newClosedList[newCurrentState].GValue + goalDistance;
					}
					int newCurrentGVal = currentStateInfo.GValue;
					var successors = prob.GetSuccessors(newCurrentState);
					foreach (var item in successors)
					{
						if (newWatch.Elapsed > newLimit)
							return -1;
						int successorsGVal = newCurrentGVal + item.GetAppliedOperator().GetCost();
                        var successorState = (IState)item.GetSuccessorState();
						if (newClosedList.ContainsKey(successorState))
						{
							var info = newClosedList[successorState];
							if (info.GValue > successorsGVal)
							{
								info.GValue = successorsGVal;
								info.IsClosed = false;
								newClosedList[successorState] = info;
							}
						}
						newClosedList.Add(successorState, new StateInformation(successorsGVal));
						double hVal = h.GetValue(item.GetSuccessorState());
						newOpenList.Add(successorsGVal + hVal + hVal / 1000, successorState);
					}
				}
				return -1;
			}

			/// <summary>
			/// Uses backward planning for enumeration. Does not iterate through all goal states. Search is preformed on meta-states that use wildcards.
			/// </summary>
			/// <param name="quiet"></param>
			/// <returns></returns>
			public IEnumerable<StateDistanceResult> EnumerateNEW(bool useApproximation, bool quiet = false)
			{
				stopwatch.Restart();
				LogMessage("Computing g-Values");
				fillGvalues(quiet);
				this.openNodes.Clear();	//clearing memory. Open list will no longer be required now.
				LogMessage("DONE Computing g-Values");
				stopwatch.Restart();
				/*
				var initialS = gValues.Keys.Where(state => ((IState)state).GetAllValues().Zip(((IState)(Problem.GetInitialState())).GetAllValues(), (f, s) => f == s || f == -1 ? true : false).All(x => x));
				if (initialS.Any())
				{
					Console.WriteLine("plan found. Length = " + gValues[initialS.First()].GValue);
					Console.WriteLine("initial state:");
					var pred = predecessor[initialS.First()];
					var op = predOperator[initialS.First()];

					Console.WriteLine(string.Join(" ", ((IState)(Problem.GetInitialState())).GetAllValues().Select(v => v >= 0 ? " " + v : v.ToString())));
					Console.WriteLine(string.Join(" ", ((IState)(initialS.First())).GetAllValues().Select(v => v >= 0 ? " " + v : v.ToString())));
					while (pred != null)
					{
						Console.WriteLine(string.Join(" ", ((IState)(pred)).GetAllValues().Select(v => v >= 0 ? " " + v : v.ToString())) + "\t" + op.GetOrderIndex());
						if (pred != null && predOperator.ContainsKey(pred))
							op = predOperator[pred];
						pred = predecessor[pred];
					}

					Console.WriteLine();
					Console.WriteLine("operators:");
					int i = 1;
					foreach (var item in ((Problem)Problem).GetOperators())
					{
						Console.WriteLine(i++ + " " + string.Join(", ", item.GetPreconditions().Select(t => "var" + t.variable + " = " + t.value)) + " => "
							+ string.Join(", ", item.GetEffects().Select(t => "var" + t.GetEff().variable + " = " + t.GetEff().value)));
					}
				}
				*/
				LogMessage("Sampling from g-Values");
				double samplesPerNode = 1;
				for (int i = 0; i < ((Problem)Problem).Variables.Count; i++)
				{
					samplesPerNode *= ((Problem)Problem).Variables[i].GetDomainRange() * 0.5;	//50% from every variable's range
				}
				samplesPerNode = Math.Max(samplesPerNode, 1);
				samplesPerNode = Math.Min(samplesPerNode, 100);
				LogMessage("Samples per node: " + samplesPerNode);
				samplesPerNode = Math.Round(Math.Min(samplesPerNode * gValues.Count, memoryLimit / 10)); //ideally there will be "samplesPerNode * gValues.Count" samples, but no more than "memoryLimit / 10"
				LogMessage("Number of samples: " + samplesPerNode);
				foreach (var item in sampleFromGValues((int)samplesPerNode, useApproximation, quiet))
				{
					yield return item;
				}
				//return sampleFromGValues((int)samplesPerNode, useApproximation, quiet);
			}

			/// <summary>
				/// Uses backward planning for enumeration. Does not iterate through all goal states. Search is preformed on meta-states that use wildcards.
				/// </summary>
				/// <param name="quiet"></param>
				/// <returns></returns>
			private void fillGvalues(bool quiet = false)
			{
                LoggingEnabled = !quiet;

				statsIntervalWatch.Start();
				var sasProblem = (Problem)this.Problem;
				this.memoryLimit = memoryLimit / sasProblem.Variables.Count;


				gValues = new SASStatePiecewiseDictionary<StateInformation>(sasProblem);
				predecessor = new Dictionary<IState, IState>();
				LogMessage("Enumeration started. Problem: " + Problem.GetProblemName());
				if (!stopwatch.IsRunning)
					stopwatch.Start();

				IState goalState = getRelativeGoalState();
				openNodes.Add(0, goalState);
				gValues.Add((IState)goalState, new StateInformation());
				predecessor.Add(goalState, null);

				int steps = -1;
				while (openNodes.GetSize() > 0)
				{
					steps++;
					if (statsIntervalWatch.Elapsed > statsPrintingInterval)
					{
						printSearchStats(quiet);
						statsIntervalWatch.Restart();
					}

					if (stopwatch.Elapsed > timeLimit)
					{
						LogMessage("Enumeration ended - time limit exceeded.");
						stopwatch.Stop();
						searchTime = stopwatch.Elapsed;
						LogMessage("Enumeration ended in " + searchTime.TotalSeconds + " seconds");
						printSearchStats(quiet);
						searchStatus = ResultStatus.TimeLimitExceeded;
						setSearchResults();
						return;
					}

					if (gValues.Count + openNodes.GetSize() > memoryLimit)
					{
						LogMessage("Enumeration ended - memory limit exceeded.");
						stopwatch.Stop();
						searchTime = stopwatch.Elapsed;
						LogMessage("Enumeration ended in " + searchTime.TotalSeconds + " seconds");
						printSearchStats(quiet);
						searchStatus = ResultStatus.MemoryLimitExceeded;
						setSearchResults();
						return;
					}

					IState currentState = openNodes.RemoveMin();
					if (gValues[currentState].IsClosed)
						continue;
					addToClosedList(currentState);
					int currentGValue = gValues[currentState].GValue;

					var backwardSuccessors = sasProblem.GetPredecessors(currentState);
					branchingCount++;
					branchingSum += backwardSuccessors.Count();

					foreach (var succ in backwardSuccessors)
					{
						if (stopwatch.Elapsed > timeLimit)
						{
							LogMessage("Enumeration ended - time limit exceeded.");
							stopwatch.Stop();
							searchTime = stopwatch.Elapsed;
							LogMessage("enumeration ended in " + searchTime.TotalSeconds + " seconds");
							printSearchStats(quiet);
							searchStatus = ResultStatus.TimeLimitExceeded;
							setSearchResults();
							return;
						}

						foreach (var state in succ.GetPredecessorStates(Problem))
						{
							int gVal = currentGValue + succ.GetAppliedOperator().GetCost();
							try
							{
								addToOpenList((IState)state, gVal, currentState, (IOperator)succ.GetAppliedOperator());
							}
							catch (OutOfMemoryException)
							{
								this.openNodes.Clear();
								LogMessage("Enumeration ended - memory limit exceeded.");
								stopwatch.Stop();
								searchTime = stopwatch.Elapsed;
								LogMessage("enumeration ended in " + searchTime.TotalSeconds + " seconds");
								printSearchStats(quiet);
								searchStatus = ResultStatus.MemoryLimitExceeded;
								setSearchResults();
								return;
							}
						}

					}
				}
				LogMessage("All states has been enumerated.");
				printSearchStats(quiet);
				searchStatus = ResultStatus.NoSolutionFound;
				setSearchResults();
				return;
			}

			/// <summary>
			/// Now that the GValues for relative states have been computed, we sample some of stored relative states. Relative states will most likely describe several "ground" states (i.e. will contain wildcards).
			/// During the sampling, a ground state is created by substituting wildcards by random ground values. Furthermore, the ground state's real G-Val might not be the same as its "parent's" g-val, because the same
			/// ground state might match several relative states. Therefore it is necessary to find all relative states that this ground state matches and take minimum from their g-vals.
			/// To do that a specialized data structure is used, and gVals from the dictionary are transformed into this structure.
			/// The special structure is decission tree that allows to quickly find all relative states that match given ground state.
			/// 
			/// We create one sample for every relative state stored. And then some more, randomly, until required number of samples is reached.
			/// 
			/// Currently, only the g-val from "parrent" is used, that might over-estimate the real g-value. To be improved later.. TODO
			/// </summary>
			/// <param name="quiet"></param>
			/// <returns></returns>
			private IEnumerable<StateDistanceResult> sampleFromGValues(long numberOfSamples, bool useApproximation, bool quiet = false)
			{
				if (!statsIntervalWatch.IsRunning)
					statsIntervalWatch.Start();

				LogMessage("Total samples to be generated: " + numberOfSamples);
				Random r = new Random();
				Problem sASProblem = (Problem)Problem;
				IState initialState = (IState)Problem.GetInitialState();
				int samplesGenerated = 0;
				double samplesPerRelativeState = (double)numberOfSamples / gValues.Count;
				double samplesToGenerate = 0;

				foreach (var relativeState in gValues.EnumerateKeys)
				{
					if (stopwatch.Elapsed > this.timeLimit)
					{
						LogMessage("Sampling canceled - time limit reached. Total samples generated " + samplesGenerated + " out of " + numberOfSamples);
							yield break;
					}
					/*
					if (samplesGenerated > numberOfSamples)
					{
						LogMessage("DONE. Required number of samples have been generated.");
						yield break;
					}*/

					RelativeState s = (RelativeState)relativeState;
					int gVal = gValues[relativeState].GValue;

					var samplesForThisState = samplesPerRelativeState * Math.Ceiling(5000 / Math.Exp(gVal *2));
					samplesToGenerate += samplesForThisState;

					yield return generateSample(s, gVal, r, sASProblem, initialState, useApproximation);
					samplesGenerated++;
					if (statsIntervalWatch.Elapsed > statsPrintingInterval)
					{
						LogMessage("generated samples: " + samplesGenerated + ", " + (100 * samplesGenerated / (double)numberOfSamples).ToString("0.##") + " %" + "\ttime elapsed: " + stopwatch.Elapsed.TotalMinutes + " min");
						statsIntervalWatch.Restart();
					}
					while(samplesToGenerate > samplesGenerated)
					{
						yield return generateSample(s, gVal, r, sASProblem, initialState, useApproximation);
						samplesGenerated++;
						if (samplesGenerated % 1000000 == 0)
						{
							LogMessage("generated samples: " + samplesGenerated + ", " + (100 * samplesGenerated / (double)numberOfSamples).ToString("0.##") + " %" + "\ttotal time elapsed: " + stopwatch.Elapsed.TotalMinutes + " min");
						}
					}
				}
			}

			/// <summary>
			/// Returns true if given fixed state matches given relative state, i.e. for all variables it must hold that either their values are identical in both states, or relative state has a wildcard on that position.
			/// </summary>
			/// <param name="relative"></param>
			/// <param name="fixedState"></param>
			/// <returns></returns>
			private bool canMatch(RelativeState relative, RelativeState fixedState)
			{
				for (int i = 0; i < relative.GetAllValues().Length; i++)
				{
					if (relative.GetAllValues()[i] == -1)
						continue;
					if (relative.GetAllValues()[i] != fixedState.GetAllValues()[i])
						return false;
				}
				return true;
			}

			private StateDistanceResult generateSample(RelativeState state, double gVal, Random r, Problem sasProblem, IState initialState, bool useApproximation)
			{
				RelativeState fixedState = (RelativeState)state.Clone();
				for (int i = 0; i < sasProblem.Variables.Count; i++)
				{
					if (fixedState.GetValue(i) != -1)
						continue;   //already fixed variables will not be modified.
					if (sasProblem.Variables[i].IsRigid())
					{
						fixedState.SetValue(i, initialState.GetValue(i));    //rigid variables (that are never changed by any operator) has to take the same value as in the initial state.
						continue;
					}
					//this variable is not fixed, neither is it rigid, we assign it a random value from its range
					fixedState.SetValue(i, r.Next(sasProblem.Variables[i].GetDomainRange()));
				}
				//now all variables are set (there are no more wildcards)

				if (useApproximation)
				{
					//This is just an approximation. It takes just the gVal of the parrent. This can over-estimate the result.
					return new StateDistanceResult(fixedState, gVal);
				}

				else
				{
					//this finds all matching relative states and takes minimum of their gvalues. This will return the REAL GoalDistance, but is quite costly.
					//var matchingStates = gValues.Keys.Where(key => canMatch((RelativeState)key, fixedState));

					var matchingStates = gValues.GetAllMatchingStates(fixedState, 10000);	//returns at most 10000 relative
					double realGVal = gVal;
					if (matchingStates.Any())	//sometimes this is called on a state generated by a randomWalk such that the result is not in the gVals.
						realGVal = matchingStates.Min(kw => kw.Value.GValue);

					return new StateDistanceResult(fixedState, realGVal);
				}
			}

			private IState getRelativeGoalState()
			{
				var sasProblem = (Problem)Problem;
				int[] values = new int[sasProblem.Variables.Count];
				for (int i = 0; i < values.Length; i++)
				{
					values[i] = -1;	//everything is wildcard unless told differently
				}
				foreach (var item in sasProblem.GoalConditions)
				{
					values[item.GetVariable()] = item.GetValue();
				}

				RelativeState s = new RelativeState(values);
				return s;
			}

			/// <summary>
			/// Enumerates all goal states and then runs a standard BFS with reversed operators. Migh fail when there are too many goal states. Method EnumerateNEW should be used instead.
			/// </summary>
			/// <param name="quiet"></param>
			/// <returns></returns>
			[Obsolete]
			public IEnumerable<StateDistanceResult> Enumerate(bool quiet = false)
			{
				int maxGoalStates = 100000000;
				bool initialStatesGenerated = true;

				//gValues = new Dictionary<IState, StateInformation>();
				predecessor = new Dictionary<IState, IState>();
				LogMessage("Enumeration started. Problem: " + Problem.GetProblemName());
				if (!stopwatch.IsRunning)
					stopwatch.Start();

				LogMessage("Generating goal states...");
				//try
				{
					foreach (var item in getAllGoalStates())
					{
						openNodes.Add(0, item);
						gValues.Add((IState)item, new StateInformation());
						if (openNodes.GetSize() > maxGoalStates)
						{
							LogMessage("Couldn't generate all goal states.");
							//yield break;
							initialStatesGenerated = false;
							break;
						}
					}
				}/*
				catch (Exception e)
				{
					LogMessage("Couldn't generate all goal states.", quiet);
					yield break;
				}*/
				LogMessage("Done.");
				predecessor.Add(((Problem)Problem).InitialState, null);
				int steps = -1;
				while (openNodes.GetSize() > 0)
				{
					steps++;

					if (stopwatch.Elapsed > timeLimit)
					{
						LogMessage("Enumeration ended - time limit exceeded.");
						stopwatch.Stop();
						searchTime = stopwatch.Elapsed;
						LogMessage("Enumeration ended in " + searchTime.TotalSeconds + " seconds");
						printSearchStats(quiet);
						searchStatus = ResultStatus.TimeLimitExceeded;
						setSearchResults();
						yield break;
					}

					if (gValues.Count + openNodes.GetSize()> memoryLimit)
					{
						LogMessage("Enumeration ended - memory limit exceeded.");
						stopwatch.Stop();
						searchTime = stopwatch.Elapsed;
						LogMessage("Enumeration ended in " + searchTime.TotalSeconds + " seconds");
						printSearchStats(quiet);
						searchStatus = ResultStatus.MemoryLimitExceeded;
						setSearchResults();

						yield break;
					}

					IState currentState = openNodes.RemoveMin();
					var stateInfo = gValues[currentState];
					if (stateInfo.IsClosed)
						continue;
					addToClosedList(currentState);
					int currentGValue = stateInfo.GValue;
					yield return new StateDistanceResult(currentState, currentGValue);

					var backwardSuccessors = Problem.GetPredecessors(currentState);
					foreach (var succ in backwardSuccessors)
					{
						if (stopwatch.Elapsed > timeLimit)
						{
							LogMessage("Enumeration ended - time limit exceeded.");
							stopwatch.Stop();
							searchTime = stopwatch.Elapsed;
							LogMessage("enumeration ended in " + searchTime.TotalSeconds + " seconds");
							printSearchStats(quiet);
							searchStatus = ResultStatus.TimeLimitExceeded;
							setSearchResults();
							yield break;
						}

						foreach (var state in succ.GetPredecessorStates(Problem))
						{
							int gVal = currentGValue + succ.GetAppliedOperator().GetCost();
							try
							{
								if (initialStatesGenerated)
									addToOpenList((IState)state, gVal, currentState, (IOperator)succ.GetAppliedOperator());
							}
							catch (OutOfMemoryException)
							{
								LogMessage("Enumeration ended - memory limit exceeded.");
								stopwatch.Stop();
								searchTime = stopwatch.Elapsed;
								LogMessage("enumeration ended in " + searchTime.TotalSeconds + " seconds");
								printSearchStats(quiet);
								searchStatus = ResultStatus.MemoryLimitExceeded;
								setSearchResults();
								yield break;
							}
						}
					}
				}
				LogMessage("All states has been enumerated.");
				printSearchStats(quiet);
				searchStatus = ResultStatus.NoSolutionFound;
				setSearchResults();
				yield break;
			}

			protected SolutionPlan extractSolution(IState state)
			{
				SolutionPlan result = new SolutionPlan(null);
				IState current = state;
				while (current != null)
				{
					IState pred = predecessor[current];
					if (pred == null)
						break;
					var successors = Problem.GetSuccessors(pred);
					foreach (var succ in successors)
					{
						if (succ.GetSuccessorState().Equals(current))
						{
							result.Add(succ.GetAppliedOperator());
							break;
						}
					}
					current = pred;
				}

				result.InitialState = current;
				result.Reverse();
				return result;
			}

			public StateSpaceEnumeratorInternal(PAD.Planner.IProblem d, Heuristic h) : base(d, h)
			{
				//this.gValues = new Dictionary<IState, StateInformation>();
				this.gValues = new SASStatePiecewiseDictionary<StateInformation>((Problem)d);

                //this.openNodes = new Heaps.LeftistHeap<IState>();
                this.openNodes = new SimpleQueue<IState>();
                //this.openNodes = new Heaps.BinomialHeap<IState>();
                //this.openNodes = new Heaps.SingleBucket<IState>(200000);
                //this.openNodes = new Heaps.SingleBucket<IState>(200*h.getValue(d.initialState));
                //this.openNodes = new Heaps.OrderedBagHeap<IState>();
                //this.openNodes = new Heaps.OrderedMutliDictionaryHeap<IState>();
                results = new SearchResults();
				if (Heuristic != null)
					results.Heuristic = Heuristic.GetDescription();
				results.BestHeuristicValue = int.MaxValue;
			}

			public void setHeapDatastructure(IHeap<double, IState> structure)
			{
				this.openNodes = structure;
			}

			protected void printSearchStats(bool quiet)
			{
				LogMessage("Closed nodes: " + (gValues.Count) +
							"\tOpen nodes: " + openNodes.GetSize() +
							"\tTime elapsed: " + stopwatch.Elapsed.TotalMinutes + " minutes" +
							"\tAverage branching: " + (branchingSum / branchingCount).ToString("0.##") +
							"\tExpanded nodes per sec: " + (gValues.Count / stopwatch.Elapsed.TotalSeconds).ToString("0.##")
							//"\tHeuristic calls: " + heuristic.heuristicCalls +
							//"\tMin heuristic: " + heuristic.statistics.bestHeuristicValue +
							//"\tAvg heuristic: " + heuristic.statistics.getAverageHeurValue().ToString("0.###")
							);
			}

			public override string GetDescription()
			{
				return "A*";
			}

			public override ResultStatus Start(SearchType type = SearchType.Forward)
			{
				throw new NotImplementedException();
			}
		}

		private class SASStatePiecewiseDictionary<T>
		{
			TreeInnerNode<T> treeRoot;
			int size;
			List<KeyValuePair<IState, T>> resultsPlaceholder = new List<KeyValuePair<IState, T>>();
            Problem problem;

			public T this[IState key]
			{
				get
				{
					System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
					IState key2 = (IState)key;
					if (treeRoot.containsKey(key2, watch, out T val))
						return val;
					throw new KeyNotFoundException();
				}
				set
				{
					IState key2 = (IState)key;
					if (!treeRoot.setValue(key2, value))
						throw new KeyNotFoundException();
				}
			}
			/// <summary>
			/// You CANNOT manipulate ICollection of keys on this dictionary implementaion since it would be totally inefficient. If you just want to iterate throught it, use "EnumerateKeys" instead which returns IEnumerable.
			/// </summary>
			public ICollection<IState> Keys => throw new InvalidOperationException();

			/// <summary>
			/// You CANNOT manipulate ICollection of values on this dictionary implementaion since it would be totally inefficient. If you just want to iterate throught it, use "EnumerateValues" instead which returns IEnumerable. 
			/// Remember that dictionary may contain the same value multiple times. 
			/// The "EnumerateValues" will return all stored values, some of them may be the same (i.e. may return the same value several times). To avoid this use "EnumerateValues().Dictinct()".
			/// </summary>
			public ICollection<T> Values => throw new InvalidOperationException();

			public IEnumerable<IState> EnumerateKeys
			{
				get
				{
					if (treeRoot == null)
						yield break;
					foreach (var item in treeRoot.enumerateKeys())
					{
						yield return item;
					}
				}
			}

			public int Count => size;

			public bool IsReadOnly => false;

			public void Add(IState key, T value)
			{
				Add(new KeyValuePair<IState, T>(key, value));
			}

			public void Add(KeyValuePair<IState, T> item)
			{
				if (treeRoot == null)
				{
					treeRoot = TreeNode<T>.createRoot(problem.Variables[0].GetDomainRange(), problem);
					TreeNode<T>.variablesCount = item.Key.GetSize();
				}

				treeRoot.add(item, problem);
				size++;
			}

			public void Clear()
			{
				this.treeRoot = null;
				size = 0;
				TreeNode<T>.variablesCount = 0;
			}

			public bool Contains(KeyValuePair<IState, T> item)
			{
				if (treeRoot == null)
					return false;
				System.Diagnostics.Stopwatch w = Stopwatch.StartNew();
				if (treeRoot.containsKey(item.Key, w, out T val))
					if (val.Equals(item.Value))
						return true;
				return false;
			}

			public bool ContainsKey(IState key)
			{
				Stopwatch watch = Stopwatch.StartNew();
				if (treeRoot == null)
					return false;
				return treeRoot.containsKey(key, watch, out T val);
			}

			/// <summary>
			/// Returns true if the structure contains any key K1 such that "key" matches K1, e.i. K1 is more general (contains more wildcards).
			/// </summary>
			/// <param name="key"></param>
			/// <returns></returns>
			public bool ContainsMatchingState(IState key)
			{
				//this method can be VERY time demanding! There is therefore a stopwatch added that keeps the time in a reasonable limit.
				Stopwatch watch = Stopwatch.StartNew();
				if (treeRoot == null)
					return false;
				return treeRoot.containsMatchingState(key, watch);
			}

			/// <summary>
			/// Returns a set of states such that 
			/// </summary>
			/// <param name="key"></param>
			/// <returns></returns>
			public List<KeyValuePair<IState, T>> GetAllMatchingStates(IState key, int maxItems)
			{
				//this method can be VERY time demanding! There is therefore a stopwatch added that keeps the time in a reasonable limit.
				Stopwatch watch = Stopwatch.StartNew();
				resultsPlaceholder.Clear();
				if (treeRoot != null)
					treeRoot.addMatchingStates(key, resultsPlaceholder, maxItems, watch);
				return resultsPlaceholder;
			}

			public void CopyTo(KeyValuePair<IState, T>[] array, int arrayIndex)
			{
				throw new NotImplementedException();
			}

			public IEnumerator<KeyValuePair<IState, T>> GetEnumerator()
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// This implementation of dictionary allows only to ADD new (key, values) and modify values for already stored (key, values). You cannot remove values.
			/// </summary>
			/// <param name="key"></param>
			/// <returns></returns>
			public bool Remove(IState key)
			{
				throw new InvalidOperationException();
			}

			/// <summary>
			/// This implementation of dictionary allows only to ADD new (key, values) and modify values for already stored (key, values). You cannot remove values.
			/// </summary>
			/// <param name="key"></param>
			/// <returns></returns>
			public bool Remove(KeyValuePair<IState, T> item)
			{
				throw new InvalidOperationException();
			}

			public bool TryGetValue(IState key, out T value)
			{
				if (treeRoot == null)
				{
					value = default(T);
					return false;
				}
				Stopwatch w = Stopwatch.StartNew();
				return treeRoot.containsKey(key, w, out value);
			}

			public SASStatePiecewiseDictionary(Problem d)
			{
				problem = d;
			}

			abstract class TreeNode<M>
			{
				public int decisionVariableIndex;
				public static int variablesCount = 0;
				public const int wildcard = -1;
				protected TimeSpan timeLimit = TimeSpan.FromSeconds(10);
				protected static Random r = new Random();

				protected TreeNode<M> createLeaf(int variableIndex)
				{
					TreeNode<M> t = new TreeLeaf<M>();
					t.decisionVariableIndex = variableIndex;
					return t;
				}

				public static TreeInnerNode<M> createRoot(int firstVariableRange, Problem problem)
				{
					TreeInnerNode<M> n = new TreeInnerNode<M>(0, firstVariableRange);
					n.decisionVariableIndex = 0;
					return n;
				}

				/// <summary>
				/// Creates a new innerNode and inserts given value into it.
				/// </summary>
				/// <param name="variableIndex"></param>
				/// <param name="value"></param>
				/// <returns></returns>
				protected TreeNode<M> createInnerNode(int variableIndex, KeyValuePair<IState, M>? value, Problem problem)
				{
					TreeInnerNode<M> n = new TreeInnerNode<M>(variableIndex, problem.Variables[variableIndex].GetDomainRange());
					n.decisionVariableIndex = variableIndex;
					n.add(value.Value, problem);
					return n;
				}

				public abstract void add(KeyValuePair<IState, M> value, Problem problem);

				public abstract bool containsKey(IState key, Stopwatch w, out M value);

				public abstract bool containsMatchingState(IState key, Stopwatch w);

				public abstract void addMatchingStates(IState key, List<KeyValuePair<IState, M>> result, int maxItems, Stopwatch w);

				/// <summary>
				/// TreeLeaf can hold only one value and will return true if it already has one. Inner nodes are never full.
				/// </summary>
				/// <returns></returns>
				public abstract bool isFull();

				/// <summary>
				/// Gets stored value in the object. Only leaves may store values.
				/// </summary>
				/// <returns></returns>
				public abstract KeyValuePair<IState, M>? getStoredValue();

				public abstract bool remove(IState key);

				/// <summary>
				/// Sets value for a key that is already present in the dictionary. Returns false if the key was not found.
				/// </summary>
				/// <param name="key"></param>
				/// <param name="value"></param>
				/// <returns></returns>
				public abstract bool setValue(IState key, M value);

				public abstract IEnumerable<IState> enumerateKeys();

				public abstract IEnumerable<M> enumerateValues();
			}

			class TreeInnerNode<M> : TreeNode<M>
			{
				Dictionary<int, TreeNode<M>> successors;

				public override void add(KeyValuePair<IState, M> value, Problem problem)
				{
					int succIndex = value.Key.GetAllValues()[decisionVariableIndex];
					TreeNode<M> succ = successors[succIndex];
					if (succ.isFull())
						promoteSuccessor(succIndex, problem);
					successors[succIndex].add(value, problem);
				}

				public TreeInnerNode(int variableIndex, int variableRange)
				{
					this.successors = new Dictionary<int, TreeNode<M>>();
					//starts at -1 because it is a wildcard symbol.
					for (int i = -1; i < variableRange; i++)
					{
						successors.Add(i, createLeaf(variableIndex + 1));
					}
				}

				/// <summary>
				/// Replaces a treeLeaf by TreeInnerNode so that it can hold more than one value. Keeps the currently stored value in the new structure.
				/// </summary>
				/// <param name="index"></param>
				protected void promoteSuccessor(int index, Problem problem)
				{
					if (successors[index].decisionVariableIndex <= variablesCount - 1)
						successors[index] = createInnerNode(successors[index].decisionVariableIndex, successors[index].getStoredValue(), problem);
				}

				public override bool isFull()
				{
					//inner nodes are never full
					return false;
				}

				public override KeyValuePair<IState, M>? getStoredValue()
				{
					//only leaves may hold values
					return null;
				}

				public override bool containsKey(IState key, Stopwatch w, out M value)
				{
					if (w.Elapsed > this.timeLimit)
					{
						new Logger().Log("containsKey: time limit elapsed!!");
						value = default(M);
						return false;
					}
					return successors[key.GetAllValues()[decisionVariableIndex]].containsKey(key, w, out value);
				}

				public override bool remove(IState key)
				{
					throw new NotImplementedException();
				}

				public override bool setValue(IState key, M value)
				{
					return successors[key.GetAllValues()[decisionVariableIndex]].setValue(key, value);
				}

				public override IEnumerable<IState> enumerateKeys()
				{
					foreach (var succ in successors.Values)
					{
						IEnumerable<IState> succKeys = succ.enumerateKeys();
						foreach (var key in succKeys)
							yield return key;
					}
				}

				public override IEnumerable<M> enumerateValues()
				{
					foreach (var succ in successors.Values)
					{
						var succVales = succ.enumerateValues();
						foreach (var val in succVales)
							yield return val;
					}
				}

				public override bool containsMatchingState(IState key, Stopwatch w)
				{
					if (w.Elapsed > timeLimit)
						return false;

					if (key.GetAllValues()[decisionVariableIndex] == wildcard)
						return successors[wildcard].containsMatchingState(key, w);

					if (r.Next() < 0.5)
						return successors[wildcard].containsMatchingState(key, w) ||
							successors[key.GetAllValues()[decisionVariableIndex]].containsMatchingState(key, w);

					return successors[key.GetAllValues()[decisionVariableIndex]].containsMatchingState(key, w) ||
						successors[wildcard].containsMatchingState(key, w);
				}

				public override void addMatchingStates(IState key, List<KeyValuePair<IState, M>> result, int maxItems, Stopwatch w)
				{
					if (w.Elapsed > timeLimit)
					{
						new Logger().Log("addMatchingStates: time limit elapsed!!");
						return;
					}

					if (result.Count >= maxItems)
						return;

					if (r.NextDouble() < 0.5)
					{
						successors[key.GetAllValues()[decisionVariableIndex]].addMatchingStates(key, result, maxItems, w);
						successors[wildcard].addMatchingStates(key, result, maxItems, w);
					}
					else
					{
						successors[wildcard].addMatchingStates(key, result, maxItems, w);
						successors[key.GetAllValues()[decisionVariableIndex]].addMatchingStates(key, result, maxItems, w);
					}
				}
			}

			class TreeLeaf<M> : TreeNode<M>
			{
				KeyValuePair<IState, M>? storedState;

				public override void add(KeyValuePair<IState, M> value, Problem problem)
				{
					//#if DEBUG
					if (storedState != null && storedState.HasValue && !storedState.Value.Value.Equals(value.Value))
						throw new ArgumentException();  //item with the same key already present in the dictionary
														//#endif
					storedState = value;
				}

				public override void addMatchingStates(IState key, List<KeyValuePair<IState, M>> result, int maxItems, Stopwatch w)
				{
					if (storedState == null)
						return;

					for (int i = decisionVariableIndex; i < key.GetAllValues().Length; i++)
					{
						if (storedState.Value.Key.GetAllValues()[i] != wildcard && storedState.Value.Key.GetAllValues()[i] != key.GetAllValues()[i])
							return;
					}
					result.Add(storedState.Value);
				}

				public override bool containsKey(IState key, Stopwatch w, out M value)
				{
					if (storedState == null)
					{
						value = default(M);
						return false;
					}
					for (int i = decisionVariableIndex; i < key.GetAllValues().Length; i++)
					{
						if (storedState.Value.Key.GetAllValues()[i] != key.GetAllValues()[i])
						{
							value = default(M);
							return false;
						}
					}
					value = storedState.Value.Value;
					return true;
				}

				public override bool containsMatchingState(IState key, Stopwatch w)
				{
					if (storedState == null)
						return false;

					for (int i = decisionVariableIndex; i < key.GetAllValues().Length; i++)
					{
						if (storedState.Value.Key.GetAllValues()[i] != wildcard && storedState.Value.Key.GetAllValues()[i] != key.GetAllValues()[i])
							return false;
					}
					return true;
				}

				public override IEnumerable<IState> enumerateKeys()
				{
					if (storedState != null)
						yield return storedState.Value.Key;
				}

				public override IEnumerable<M> enumerateValues()
				{
					if (storedState != null)
						yield return storedState.Value.Value;
				}

				public override KeyValuePair<IState, M>? getStoredValue()
				{
					return storedState;
				}

				public override bool isFull()
				{
					//is full if it already hold some value
					return storedState != null;
				}

				public override bool remove(IState key)
				{
					throw new NotImplementedException();
				}

				public override bool setValue(IState key, M value)
				{
					if (storedState == null)
						return false;

					for (int i = decisionVariableIndex; i < key.GetAllValues().Length; i++)
					{
						if (storedState.Value.Key.GetAllValues()[i] != key.GetAllValues()[i])
							return false;
					}
					storedState = new KeyValuePair<IState, M>(this.storedState.Value.Key, value);
					return true;
				}
			}
		}
	}

	/// <summary>
	/// Holds database of state from the state-space of some problem together with their goal-distances. Distances are either exact or approximative.
	/// </summary>
	class StatesDistanceDatabase
	{
		protected string problemFilePath;
		protected string savedResultsFilePath;

		protected string resultsFolderName = "_problemInfo";
		protected string resultsFileNamePrefix = "statesDB_";

		protected List<StateDistanceResult> newItems;

		public StatesDistanceDatabase(string problemFilePath)
		{
			this.problemFilePath = problemFilePath;
			this.savedResultsFilePath = Path.Combine(Directory.GetParent(problemFilePath).FullName, resultsFolderName, resultsFileNamePrefix + Path.GetFileNameWithoutExtension(problemFilePath) + ".tsv");
			this.newItems = new List<StateDistanceResult>();

			if (!Directory.Exists(Directory.GetParent(savedResultsFilePath).FullName))
				Directory.CreateDirectory(Directory.GetParent(savedResultsFilePath).FullName);
			if (!File.Exists(savedResultsFilePath))
				File.Create(savedResultsFilePath);
		}

		public void addEntry(StateDistanceResult r)
		{
			newItems.Add(r);
		}

		public void save()
		{
			File.AppendAllLines(savedResultsFilePath, newItems.Select(item => itemToString(item)));
			newItems.Clear();
		}

		protected string itemToString(StateDistanceResult item)
		{
			return item.state.ToString() + "\t" + item.realDistance + "\t" + item.isSurelyCorrect;
		}

	}

	public class StateSpaceHistogramCalculator
	{
		public static List<Dictionary<double, Dictionary<int,int>>> getHistogram(string problemFile, List<Heuristic> heurs)
		{
            //the data is organitzed such that the first level is real goal distance (first argument) and the second level is heuristic estimate. The value found on these coordinates represents the number of states that fall into such category.
            //the "list" corresponds to the list of hueristics. Every entry describes results for one heuristic.
            List<Dictionary<double, Dictionary<int, int>>> result = new List<Dictionary<double, Dictionary<int, int>>>();

			var d = new Problem(problemFile, false);
			var statesDB = new StatesDistanceDatabase(problemFile);

            foreach (var item in heurs)
            {
                result.Add(new Dictionary<double, Dictionary<int, int>>());
            }

			foreach (var item in StateSpaceEnumerator.enumerateAllStatesWithDistances(d))
			{
				statesDB.addEntry(item);
                for (int i = 0; i < heurs.Count; i++)
                {
                    addEntry(result[i], item.realDistance, (int)(heurs[i].GetValue(item.state)));
                }
			}
			statesDB.save();
			return result;
		}

		/// <summary>
		/// Adds a new entry into the database.
		/// </summary>
		/// <param name="result">Representation of the database that will be updated by this call</param>
		/// <param name="realGoalDistance">Real goal-distance of the state</param>
		/// <param name="heuristicValue">Heuristic estimate of the state</param>
		private static void addEntry(Dictionary<double, Dictionary<int, int>> result, double realGoalDistance, int heuristicValue)
		{
			if (!result.ContainsKey(realGoalDistance))
				result.Add(realGoalDistance, new Dictionary<int, int>());
			if (!result[realGoalDistance].ContainsKey(heuristicValue))
				result[realGoalDistance].Add(heuristicValue, 0);
			result[realGoalDistance][heuristicValue]++;
		}

		public static void writeHistograms(string outputFile, List<Dictionary<double, Dictionary<int, int>>> histograms)
		{
            if (histograms == null || histograms.Count == 0 || histograms[0] == null || histograms[0].Count == 0)
                return;
			using (System.IO.StreamWriter writer = new System.IO.StreamWriter(outputFile))
			{
                foreach (var item in histograms)
                {
                    writer.WriteLine("realDistance\theuristic\tcount");
                    foreach (var realDistance in item.Keys)
                    {
                        foreach (var heurDistance in item[realDistance].Keys)
                        {
                            writer.WriteLine(realDistance + "\t" + heurDistance + "\t" + item[realDistance][heurDistance]);
                        }
                    }
                    writer.WriteLine("--- end of histogram ---");
                }
			}
		}

	}

	public class HistogramTriplet
	{
		public double realDistance;
		public int heurDistance, count;

		/// <summary>
		/// String contains three numbers separated by "\t" in order (realDistance, heurDistance, count)
		/// </summary>
		/// <param name="values"></param>
		public HistogramTriplet(string values)
		{
			var splitted = values.Split('\t');
			realDistance = double.Parse(splitted[0]);
			heurDistance = int.Parse(splitted[1]);
			count = int.Parse(splitted[2]);
		}

		public override string ToString()
		{
			return "(" + realDistance + ", " + heurDistance + ", " + count + ")";
		}
	}

	public class PredictionTuple
	{
		public int heurDistance;
		public double predictedRealDistance;

		public PredictionTuple(int heurVal, double prediction)
		{
			this.heurDistance = heurVal;
			this.predictedRealDistance = prediction;
		}

		public override string ToString()
		{
			return "(" + heurDistance + ", " + predictedRealDistance.ToString("0.00") + ")";
		}
	}

	public class FeaturesCalculator
	{
		/// <summary>
		/// Currently it computes these features: number of variables, number of operators, average of variables' ranges, median of variables' ranges,
		/// Value of FFheuristic for the initial state.
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static List<double> generateFeaturesFromProblem(Problem p)
		{
			List<double> result = new List<double>();
			result.Add(p.Variables.Count);
			result.Add(p.Operators.Count);
			List<int> domainRanges = new List<int>();
			for (int i = 0; i < p.Variables.Count; i++)
				domainRanges.Add(p.Variables[i].GetDomainRange());

			result.Add(domainRanges.Average());
			domainRanges.Sort();
			result.Add(domainRanges[domainRanges.Count / 2]);
			result.Add(getFFHeuristicEstimateOfInitialState(p));

			return result;
		}

		/// <summary>
		/// Returns a hFF_val of the initial state of this problem. When this information is first requested, it is computed and stored in a file in the same directory as the .sas file. Next time it is requested, the stored value is returned.
		/// </summary>
		/// <returns></returns>
		public static double getFFHeuristicEstimateOfInitialState(Problem problem)
		{
			string inputFilePath = problem.GetInputFilePath();
			string infoName = "hFF_ofInitialState";
			string infoFolderName = "_problemInfo";
			string infoFolderPath = Path.Combine(Directory.GetParent(inputFilePath).FullName, infoFolderName);
			if (!Directory.Exists(infoFolderPath))
				Directory.CreateDirectory(infoFolderPath);

			string infoFilePath = Path.Combine(infoFolderPath, Path.GetFileName(Path.ChangeExtension(inputFilePath, "txt")));
			if (File.Exists(infoFilePath))
			{
				var lines = File.ReadAllLines(infoFilePath);
				var splittedLines = lines.Select(l => l.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries));
				var infoINeed = splittedLines.Where(s => s[0] == infoName);
				if (infoINeed.Any())
					return double.Parse(infoINeed.First()[1]);
				else
				{
					double heurVal = new FFHeuristic(problem).GetValue(problem.InitialState);
					string lineToAppend = infoName + "\t" + heurVal.ToString();
					File.AppendAllLines(infoFilePath, new List<string>() { lineToAppend });
					return heurVal;
				}
			}
			else
			{
				double heurVal = new FFHeuristic(problem).GetValue(problem.InitialState);
				string lineToAppend = infoName + "\t" + heurVal.ToString();
				File.WriteAllLines(infoFilePath, new List<string>() { lineToAppend });
				return heurVal;
			}
		}

		/// <summary>
		/// Looks at all records with the same heuristicVal and averages their realVal.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="useMean">If set to true, it will compute weighted average of the results, otherwise it produces median.</param>
		/// <returns></returns>
		public static List<PredictionTuple> generatePredictions(List<HistogramTriplet> data, StatesHistogramType type)
		{
			List<PredictionTuple> result = new List<PredictionTuple>();
			if (type == StatesHistogramType.heurFF_Mean)
			{
				foreach (var heurVal in data.Select(t => t.heurDistance).Distinct())
				{
					int sumCount = data.Where(t => t.heurDistance == heurVal).Sum(t => t.count);
					double prediction = data.Where(t => t.heurDistance == heurVal).Select(t => (double)t.realDistance * t.count / sumCount).Sum();
					result.Add(new PredictionTuple(heurVal, prediction));
				}
				return result;
			}
			if (type == StatesHistogramType.heurFF_Median)
			{
				foreach (var heurVal in data.Select(t => t.heurDistance).Distinct())
				{
					int sumCount = data.Where(t => t.heurDistance == heurVal).Sum(t => t.count);
					int desiredIndex = (sumCount + 1) / 2;	//index of the median
					var sortedSamples = data.Where(t => t.heurDistance == heurVal).OrderBy(t => t.realDistance).ToList();
					if (sortedSamples.Count == 1)
					{
						result.Add(new PredictionTuple(heurVal, sortedSamples.First().realDistance));
						continue;
					}
					int currentCount = 0;
					int currentIndex = 0;
					double currentRes = sortedSamples[0].realDistance;
					while (currentCount < desiredIndex)
					{
						currentCount += sortedSamples[currentIndex].count;
						currentRes = sortedSamples[currentIndex].realDistance;
						currentIndex++;
					}
					result.Add(new PredictionTuple(heurVal, currentRes));
				}
				return result;
			}
			throw new Exception();  //it should never get here (it would mean that new item was added to StatesHistogramType but can't be processed here.)
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileWithData"></param>
		/// <param name="useMean">If set to true, results will be averaged, if false, it will take a median of results</param>
		/// <returns></returns>
		public static List<PredictionTuple> generatePredictions(string fileWithData, StatesHistogramType type)
		{
			return generatePredictions(loadHistogramData(fileWithData), type);
		}

		/// <summary>
			/// The file should contain a one-line header nad then each line should contain a triple (realDistance, HeurDistance, Count) separated by tab ("\t").
			/// The file may contain several histograms, only the first one will be loaded!!!
			/// </summary>
			/// <param name="filePath"></param>
			/// <returns></returns>
		public static List<HistogramTriplet> loadHistogramData(string filePath)
		{
			List<HistogramTriplet> result = new List<HistogramTriplet>();
			foreach (var line in System.IO.File.ReadAllLines(filePath))
			{
				if (String.IsNullOrWhiteSpace(line) || line.Length <= 0)
					continue;
				if (line.Contains('-'))
					break;
				if (!char.IsDigit(line[0]))
					continue;
				result.Add(new HistogramTriplet(line));
			}
			return result;
		}

		/// <summary>
		/// The folder should contain multiple text files with histograms (histogram triplets). This method reads them all and produces a set of features and targets for a ML model to learn.
		/// (In a .tsv format).
		/// These features will be written to file "dataToLearn.tsv" in the same directory. Values are tab-separated, last column is the target.
		/// Features include problem description and heuristic function, desired targets are the best real-distance predictions based on heurVals. 
		/// (I.e. weighted sum of real values on which the heuristic returns the given heurVal, weights are the number of accurences where it happens.)
		/// </summary>
		/// <param name="folderPath"></param>
		public static void processHistogramsFolder(string folderPath, DateTime reWriteIfOlderThan, StatesHistogramType type)
		{
			string resultFileName = "dataToLearn_" + type.ToString() + ".tsv";
			Console.WriteLine("Processing folder: " + folderPath + " will create file " + resultFileName);

			if (File.Exists(resultFileName) && File.GetCreationTime(resultFileName) >= reWriteIfOlderThan)
			{
				Console.WriteLine("Skipping. File already exists and is up to date (" + File.GetCreationTime(resultFileName).ToShortDateString() + ")");
				return;
			}

			List<List<double>> resultFeatures = new List<List<double>>();
			foreach (var file in Directory.EnumerateFiles(folderPath))
			{
				//checks whether this file is a histogram of some Problem in a parrent directory.
				if (Path.GetExtension(file) != ".txt" || !Directory.GetParent(Directory.GetParent(file).FullName).EnumerateFiles().Any(f => Path.ChangeExtension(f.Name, "txt") == Path.GetFileName(file)))
					continue;

				Console.WriteLine("\t\tfile: " + file);

				var predictions = generatePredictions(file, type);
				var problemFeatures = generateFeaturesFromProblem(readProblemFromFileName(file));
				foreach (var pred in predictions)
				{
					List<double> vector = new List<double>(problemFeatures);
					vector.Add(pred.heurDistance);
					vector.Add(pred.predictedRealDistance);
					resultFeatures.Add(vector);
				}
			}
			System.IO.File.WriteAllLines(Path.Combine(folderPath, resultFileName), resultFeatures.Select(t => ListToString(t, "\t")));
		}


		private static string ListToString<T>(List<T> list, string separator)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var item in list)
			{
				sb.Append(item.ToString() + separator);
			}
			sb.Remove(sb.Length - separator.Length, separator.Length);
			return sb.ToString();
		}

		/// <summary>
		/// Gets the name of a file with results (e.g. "pegsol-opt11-strips_p10.txt") and determines the original planning problem (e.g. domain: "pegsol-opt11-strips", problem: "p10"). 
		/// Loads the problem description from an appropriate file and returns the loaded problem.
		/// </summary>
		/// <param name="resultsFileName"></param>
		/// <returns></returns>
		public static Problem readProblemFromFileName(string resultsFileName)
		{
			if (resultsFileName.Contains("histograms"))
			{
				//histograms are in this case in a separate folder just inside the domain folder
				string fileNameWithoutPathAndExtension = Path.GetFileNameWithoutExtension(resultsFileName);
				string domainDirectory = Directory.GetParent(Directory.GetParent(resultsFileName).FullName).FullName;
				string domainFile = Path.Combine(domainDirectory, fileNameWithoutPathAndExtension + ".sas");
				return new Problem(domainFile, false);
			}


			string name = resultsFileName;
			if (resultsFileName.Contains('/') || resultsFileName.Contains('\\'))
			{
				string[] parts = name.Split('/', '\\');
				name = parts[parts.Length - 1];
			}
			if (name.Contains('.'))
			{
				string[] parts = name.Split('.');
				name = parts[0];
			}
			var splitted = name.Split('_');
			string domain = splitted[0], problem = splitted[1];
			Problem p = new Problem(@"./../tests/benchmarksSAS_ALL/" + domain + "/" + problem + ".sas", false);
			return p;
		}

	}

	public abstract class MLModel
	{
		public abstract double eval(List<float> inputs);
		public abstract void train(string dataFilePath);

	}

	public class BrightWireNN : MLModel
	{
		IGraphTrainingEngine trainingEngine;
		IGraphEngine model;
		private GraphModel bestNetwork;


		public override double eval(List<float> inputs)
		{
			var g = (model ?? trainingEngine).Execute(inputs.ToArray());
			return g.Output.First().Data.First();
		}

		public BrightWireNN()
		{
		}

		/*
		[Obsolete]
		public (IDataTable Training, IDataTable Test) splitData(IDataTable data, int? randomSeed, double trainPercentage = 0.8)
		{
			HashSet<int> selectedRowIndices = new HashSet<int>();
			List<int> allRowIndices = new List<int>();
			for (int i = 0; i < data.RowCount; i++)
			{
				allRowIndices.Add(i);
			}
			int trainingCount = (int)(data.RowCount * trainPercentage);
			Random r = randomSeed != null ? new Random(randomSeed.Value) : new Random();
			for (int i = 0; i < trainingCount; i++)
			{
				int selectedIndex = r.Next(allRowIndices.Count);
				selectedRowIndices.Add(allRowIndices[selectedIndex]);
				allRowIndices.RemoveAt(selectedIndex);
			}

			string trainingSetPath = Path.GetTempFileName(),
				testSetPath = Path.GetTempFileName();

			using (var writer = new System.IO.StreamWriter(trainingSetPath))
			{
				foreach (var item in selectedRowIndices)
				{
					foreach (var rowData in data.GetRow(item).Data)
					{
						writer.Write(rowData.ToString());
						writer.Write("\t");						
					}
					writer.WriteLine();
				}
			}

			using (var writer = new System.IO.StreamWriter(testSetPath))
			{
				foreach (var item in allRowIndices)
				{
					foreach (var rowData in data.GetRow(item).Data)
					{
						writer.Write(rowData.ToString());
						writer.Write("\t");
					}
					writer.WriteLine();
				}
			}

			var result = (new System.IO.StreamReader(trainingSetPath).ParseCSV('\t'), new System.IO.StreamReader(testSetPath).ParseCSV('\t'));
			//File.Delete(trainingSetPath);
			//File.Delete(testSetPath);

			return result;
		}
		*/

		public override void train(string dataFilePath)
		{
			int randomSeed = 123;

			var dataTable = new System.IO.StreamReader(dataFilePath).ParseCSV('\t');
			//IDataTable t = 

			dataTable.TargetColumnIndex = 5;

			//var splittedData = splitData(dataTable, randomSeed);// dataTable.Split(randomSeed);
			var splittedData = dataTable.Split(randomSeed, shuffle:true);
			int BATCH_SIZE = 64;
			float TRAINING_RATE = 0.1f;

			//var t = new MathNet.Numerics.Providers.LinearAlgebra.Mkl.MklLinearAlgebraProvider();

			using (var lap = BrightWireProvider.CreateLinearAlgebra())
			//using (var lap = BrightWireGpuProvider.CreateLinearAlgebra())
			{
				var graph = new GraphFactory(lap);
				var errorMetric = graph.ErrorMetric.Quadratic;
				var trainingData = graph.CreateDataSource(splittedData.Training);
				var testData = trainingData.CloneWith(splittedData.Test);

				graph.CurrentPropertySet
					.Use(graph.GradientDescent.RmsProp)
					.Use(graph.WeightInitialisation.Xavier);

				trainingEngine = graph.CreateTrainingEngine(trainingData, TRAINING_RATE, BATCH_SIZE);
				trainingEngine.LearningContext.ScheduleLearningRate(15, TRAINING_RATE / 3);

				graph.Connect(trainingEngine)
					.AddFeedForward(128)
					.Add(graph.ReluActivation())
					.AddDropOut(0.5f)
					.AddFeedForward(128)
					.Add(graph.ReluActivation())
					.AddDropOut(0.2f)
					.AddFeedForward(trainingEngine.DataSource.OutputSize)
					.Add(graph.ReluActivation())
					//.Add(graph.SigmoidActivation())
					.AddBackpropagation(errorMetric);

				trainingEngine.Train(5000, testData, errorMetric, bn => bestNetwork = bn, 50);

				model = graph.CreateEngine(bestNetwork.Graph);
			}
		}

		public List<double> evalOnFile(string dataFilePath)
		{
			List<double> result = new List<double>();
			List<string> lines = System.IO.File.ReadAllLines(dataFilePath).ToList();
			foreach (var line in lines)
			{
				var splitted = line.Split('\t').Select(t => float.Parse(t)).ToList();
				float[] inputs = splitted.Take(splitted.Count() - 1).ToArray();

				float output = (float)eval(inputs.ToList());
				result.Add(output);				
			}
			return result;
		}

		/// <summary>
		/// Saves the trained model that can be loaded and used in the future.
		/// </summary>
		/// <param name="filePath"></param>
		public static void save(BrightWireNN instance, string filePath)
		{
			using (var fs = File.Create(filePath))
			{
				Serializer.Serialize(fs, (instance.model ?? instance.trainingEngine).Graph);
			}
		}

		/// <summary>
		/// Loads previously saved model
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static BrightWireNN load(string filePath)
		{
			BrightWireNN result = new BrightWireNN();

			var graph = new GraphFactory(BrightWireProvider.CreateLinearAlgebra(false));
			ExecutionGraph executionGraphReloaded = null;

			using (var file = File.OpenRead(filePath))
			{
				executionGraphReloaded = Serializer.Deserialize<ExecutionGraph>(file);
			}
			result.model = graph.CreateEngine(executionGraphReloaded);
			return result;
		}
	}

	public class FileBasedModel : BrightWireNN
	{
		class MyList : List<float>
		{
			public override int GetHashCode()
			{
				unchecked
				{
					int p = 17177117, m = 7;
					foreach (var item in this)
					{
						p += (int)(m * item);
						m *= 71;
					}
					return p;
				}
			}

			public override bool Equals(object obj)
			{
				if (obj is MyList)
				{
					var second = (MyList)obj;
					if (second.Count != this.Count)
						return false;
					for (int i = 0; i < second.Count; i++)
					{
						if (this[i] != second[i])
							return false;
					}
					return true;
				}
				return false;
			}

			public MyList(List<float> list)
			{
				foreach (var item in list)
				{
					Add(item);
				}
			}
		}

		Dictionary<MyList, double> mapping;
		string file;

		public FileBasedModel()
		{
			this.mapping = new Dictionary<MyList, double>();
		}

		public FileBasedModel(string dataFile) : this()
		{
			this.file = dataFile;
			train(dataFile);
		}

		public override void train(string dataFilePath)
		{
			var lines = System.IO.File.ReadAllLines(dataFilePath);
			foreach (var line in lines)
			{
				var splitted = line.Split('\t').Select(t => float.Parse(t)).ToList();
				List<float> inputs = splitted.Take(splitted.Count() - 1).ToList();
				var toAdd = new MyList(inputs);
				if (!mapping.ContainsKey(toAdd))
					mapping.Add(new MyList(inputs), splitted[splitted.Count - 1]);
			}
		}

		public override double eval(List<float> inputs)
		{
			var t = new MyList(inputs);
			for (int i = 0; i < 100000; i++)
			{
				if (mapping.ContainsKey(t))
					return mapping[t];
				t[inputs.Count - 1]++;
			}
			return inputs[inputs.Count - 1];
		}

		public double eval2(List<float> inputs)
		{
			var t = new MyList(inputs);
			if (mapping.ContainsKey(t))
				return mapping[t];

			return -1;
		}

	}



}
