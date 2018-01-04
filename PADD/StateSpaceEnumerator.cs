using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using BrightWire;
using BrightWire.ExecutionGraph;
using ProtoBuf;
using System.IO;
using BrightWire.Models;

namespace PADD
{
	class StateDistanceResult
	{
		public IState state;
		public double realDistance;

		public StateDistanceResult(IState state, double currentGValue)
		{
			this.state = state;
			this.realDistance = currentGValue;
		}
	}

	class StateSpaceEnumerator
	{
		/// <summary>
		/// Enumerates all states in the state-space by backward search from goal states. Returns the states one by one together with their true goal-distances.
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static IEnumerable<StateDistanceResult> enumerateAllStatesWithDistances(IPlanningProblem p)
		{
			//if true, uses backward planning
			bool useRelativeStates = true;

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

				if (e.searchStatus == SearchStatus.MemoryLimitExceeded || e.searchStatus == SearchStatus.TimeLimitExceeded)
					foreach (var item in e.enumerateByRandomWalks(randomWalksCount, randomWalksMaxLength, true))
					{
						yield return item;
					}
			}
			else
			{
				foreach (var item in e.Enumerate())
				{
					yield return item;
				}


				if (e.searchStatus == SearchStatus.MemoryLimitExceeded || e.searchStatus == SearchStatus.TimeLimitExceeded)
					foreach (var item in e.enumerateByRandomWalks(randomWalksCount, randomWalksMaxLength, false))
					{
						yield return item;
					}
			}
		}

		public static IEnumerable<IState> getAllStatesMeetingConditions(Dictionary<int, int> conditions, SASProblem domain)
		{
			return StateSpaceEnumeratorInternal.getAllStatesMeetingConditions(conditions, domain);
		}

		private class StateSpaceEnumeratorInternal : HeuristicSearchEngine
		{
			public Stopwatch stopwatch = new Stopwatch();
			public SearchResults results;
			public IHeap<double, IState> openNodes;
			protected SASStatePiecewiseDictionary<StateInformation> gValues;
			protected Dictionary<IState, IState> predecessor;
			protected Dictionary<IState, IOperator> predOperator = new Dictionary<IState, IOperator>();

			protected double lengthShorteningCoeficient;

			public TimeSpan searchTime;
			public TimeSpan timeLimit = TimeSpan.FromHours(1);

			//protected const long memoryLimit = 5000;
            protected const long memoryLimit = 20000000;

            protected void addToClosedList(IState state)
			{
				StateInformation f = gValues[state];
				f.isClosed = true;
				gValues[state] = f;
			}

			protected virtual void addToOpenList(IState s, int gValue, IState pred, double hValue)
			{
				if (!gValues.ContainsKey((SASState)s))
				{
					gValues.Add((SASState)s, new StateInformation(gValue));
					//predecessor.Add(s, pred);
					openNodes.insert(gValue + hValue, s);
					return;
				}
				if (gValues[s].gValue > gValue)
				{
					throw new Exception();
					/*
					StateInformation f = gValues[s];
					f.gValue = gValue;
					gValues[s] = f;
					predecessor[s] = pred;
					openNodes.insert(gValue + hValue, s);
					return;
					*/
				}
			}

			protected virtual void addToOpenList(IState s, int gValue, IState pred, IOperator op)
			{
				if (!gValues.ContainsKey((SASState)s))
				{
					if (gValues.ContainsMatchingState((SASState)s))	//there is a matching state already added. Matching state will always have better gVal.
						return;

					//double hValue = heuristic.getValue(s);
					gValues.Add((SASState)s, new StateInformation(gValue));
#if DEBUG
					predecessor.Add(s, pred);
					predOperator.Add(s, op);
#endif
					openNodes.insert(0, s);
					return;
				}
				if (gValues[s].gValue > gValue)
				{
					//throw new Exception();	//this is still possible when there are action costs involved
					
					//double hValue = heuristic.getValue(s);
					StateInformation f = gValues[s];
					f.gValue = gValue;
					gValues[s] = f;
#if DEBUG
					predecessor[s] = pred;
#endif
					openNodes.insert(gValue, s);
					return;
					
				}
			}

			protected void setSearchResults()
			{
				this.results.expandedNodesCount = this.gValues.Count + this.openNodes.size();
				this.results.timeInSeconds = (int)stopwatch.Elapsed.TotalSeconds;
				this.results.solutionFound = false;
				results.planLength = -1;
			}

			/// <summary>
			/// Recursive function to enumerate all goal states. Goal states are created by fixing variables mentioned in goal conditions and enumerating all combination of values for variables not mentioned in goal conditions.
			/// </summary>
			/// <param name="variable"></param>
			/// <param name="value"></param>
			/// <param name="domain"></param>
			/// <returns></returns>
			protected static IEnumerable<IState> getGoalStatesRecur(int variable, int value, SASProblem domain, SASState result, Dictionary<int, int> goalConditions)
			{
				if (variable >= domain.GetVariablesCount())
				{
					yield return result.Clone();
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
						for (int val = 0; val < domain.GetVariableDomainRange(variable); val++)
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
				SASProblem f = (SASProblem)this.problem;
				Dictionary<int, int> goalConditions = new Dictionary<int, int>();
				foreach (var condition in f.GetGoalConditions())
					goalConditions.Add(condition.variable, condition.value);

				SASState initialState = (SASState)f.GetInitialState();
				for (int i = 0; i < f.GetVariablesCount(); i++)
				{
					if (f.isRigid(i))
					{
						goalConditions.Add(i, initialState.GetValue(i));
					}
				}

				List<int> addedConditions = new List<int>();

				foreach (var op in ((SASProblem)problem).GetOperatorsThatCanBeLast())
				{
					addedConditions.Clear();
					foreach (var opPrecond in op.GetPreconditions())
					{
						if (!goalConditions.ContainsKey(opPrecond.variable))
						{
							addedConditions.Add(opPrecond.variable);
							goalConditions.Add(opPrecond.variable, opPrecond.value);
						}
					}

					SASState res = (SASState)f.GetInitialState();
					//List<IState> goalStates = new List<IState>();
					foreach (var item in getGoalStatesRecur(0, 0, f, res, goalConditions))
					{
#if DEBUG
						if (!item.IsMeetingGoalConditions())
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
			/// Returns all states that meets specified conditions. It fixes variables mentioned in conditions to specified values and enumerates all combinations of values of other variables.
			/// </summary>
			/// <param name="conditions">Variable-value pair that all returned states must contain.</param>
			/// <returns></returns>
			public static IEnumerable<IState> getAllStatesMeetingConditions(Dictionary<int, int> conditions, SASProblem domain)
			{
				SASState res = (SASState)domain.GetInitialState();
				//List<IState> result = new List<IState>();
				foreach (var item in getGoalStatesRecur(0, 0, domain, res, conditions))
				{
					yield return item;
				}	 
			}

			private void estimateShorteningCoeficient(int numberOfSamples)
			{
				PrintMessage("Estimating shortening coeficient");
				//List<IState> initialStates = gValues.Where(kw => kw.Value.gValue == 0).Select(kw => kw.Key).ToList();
				List<SASState> initialStates = gValues.EnumerateKeys.Where(k => gValues[k].gValue == 0).ToList();
				Random r = new Random();
				List<IState> predecessors = new List<IState>();
				double coeffsSum = 0, coeffsCount = 0;

				for (int i = 0; i < numberOfSamples; i++)
				{
					IState currentState = initialStates[r.Next(initialStates.Count)];
					double walkLength = 0;
					while (true)
					{
						predecessors = problem.GetAllPredecessors(currentState).Select(p => p.GetPredecessorState()).ToList();
						if (predecessors.Count == 0)
							break;
						IState selectedPred = predecessors[r.Next(predecessors.Count)];
						if (!gValues.ContainsKey((SASState)selectedPred))
							break;
						currentState = selectedPred;
						walkLength++;
					}
					if (walkLength > 1)
					{
						int realDistance = gValues[currentState].gValue;
						double coeffSample = realDistance / walkLength;
						coeffsSum += coeffSample;
						coeffsCount++;
					}
				}
				lengthShorteningCoeficient = coeffsSum / coeffsCount;
				PrintMessage("DONE Estimating shortening coeficient");
			}

			/// <summary>
			/// To be called only AFTER the Enumerate method. (Gvals have to be computed!).
			/// </summary>
			/// <param name="randomWalksCount"></param>
			/// <returns></returns>
			public IEnumerable<StateDistanceResult> enumerateByRandomWalks(int randomWalksCount, int walkMaxLength, bool relativeStatesUsed = false)
			{
				PrintMessage("Enumerating by random walks");
				SASProblem sasProblem = (SASProblem)problem;
				SASState initialState = (SASState)sasProblem.GetInitialState();
				estimateShorteningCoeficient(randomWalksCount);
				//List<IState> initialStates = gValues.Where(kw => kw.Value.gValue == 0).Select(kw => kw.Key).ToList(),
				List<SASState> initialStates = gValues.EnumerateKeys.Where(k => gValues[k].gValue == 0).ToList();

				List<IState> predecessors = new List<IState>();
				//int maxVisitedGVal = gValues.Max(kw => kw.Value.gValue);
				int maxVisitedGVal = gValues.EnumerateKeys.Max(k => gValues[k].gValue);

				Random r = new Random();
				List<IState> visitedStates = new List<IState>();

				for (int i = 0; i < randomWalksCount; i++)
				{
					int walkLength = r.Next(walkMaxLength - maxVisitedGVal) + maxVisitedGVal;
					IState currentState = initialStates[r.Next(initialStates.Count)];
					visitedStates.Clear();
					int realWalkLength = 0;
					bool hasLeftVisitedArea = false;
					for (realWalkLength = 0; realWalkLength < walkLength; realWalkLength++)
					{
						visitedStates.Add(currentState);
						predecessors = problem.GetAllPredecessors(currentState).Select(p => p.GetPredecessorState()).Where(p => problem.GetAllPredecessors(p).Count > 0).ToList();
						IState selectedPred = null;
						bool visitedByThisWalk = false,
							visitedBefore = false;
						while (predecessors.Count > 0)
						{
							int selectedIndex = r.Next(predecessors.Count);
							selectedPred = predecessors[selectedIndex];
							visitedByThisWalk = visitedStates.Contains(selectedPred);
							visitedBefore = gValues.ContainsKey((SASState)selectedPred);
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
						if (!relativeStatesUsed)
							yield return new StateDistanceResult(currentState, realWalkLength * lengthShorteningCoeficient);
						yield return generateSample((RelativeState)currentState, realWalkLength * lengthShorteningCoeficient, r, sasProblem, initialState, true);
					}
				}
				PrintMessage("DONE Enumeratin by random walks");
			}

			/// <summary>
			/// Uses backward planning for enumeration. Does not iterate through all goal states. Search is preformed on meta-states that use wildcards.
			/// </summary>
			/// <param name="quiet"></param>
			/// <returns></returns>
			public IEnumerable<StateDistanceResult> EnumerateNEW(bool useApproximation, bool quiet = false)
			{
				PrintMessage("Computing g-Values");
				fillGvalues(quiet);
				PrintMessage("DONE Computing g-Values");
				/*
				var initialS = gValues.Keys.Where(state => ((SASState)state).GetAllValues().Zip(((SASState)(problem.GetInitialState())).GetAllValues(), (f, s) => f == s || f == -1 ? true : false).All(x => x));
				if (initialS.Any())
				{
					Console.WriteLine("plan found. Length = " + gValues[initialS.First()].gValue);
					Console.WriteLine("initial state:");
					var pred = predecessor[initialS.First()];
					var op = predOperator[initialS.First()];

					Console.WriteLine(string.Join(" ", ((SASState)(problem.GetInitialState())).GetAllValues().Select(v => v >= 0 ? " " + v : v.ToString())));
					Console.WriteLine(string.Join(" ", ((SASState)(initialS.First())).GetAllValues().Select(v => v >= 0 ? " " + v : v.ToString())));
					while (pred != null)
					{
						Console.WriteLine(string.Join(" ", ((SASState)(pred)).GetAllValues().Select(v => v >= 0 ? " " + v : v.ToString())) + "\t" + op.GetOrderIndex());
						if (pred != null && predOperator.ContainsKey(pred))
							op = predOperator[pred];
						pred = predecessor[pred];
					}

					Console.WriteLine();
					Console.WriteLine("operators:");
					int i = 1;
					foreach (var item in ((SASProblem)problem).GetOperators())
					{
						Console.WriteLine(i++ + " " + string.Join(", ", item.GetPreconditions().Select(t => "var" + t.variable + " = " + t.value)) + " => "
							+ string.Join(", ", item.GetEffects().Select(t => "var" + t.GetEff().variable + " = " + t.GetEff().value)));
					}
				}
				*/
				PrintMessage("Sampling from g-Values");
				double samplesPerNode = 1;
				for (int i = 0; i < ((SASProblem)problem).GetVariablesRanges().Length; i++)
				{
					samplesPerNode *= ((SASProblem)problem).GetVariablesRanges()[i] * 0.5;	//50% from every variable's range
				}
				samplesPerNode = Math.Max(samplesPerNode, 1);
				samplesPerNode = Math.Min(samplesPerNode, 100);
				PrintMessage("Samples per node: " + samplesPerNode);
				samplesPerNode = Math.Round(Math.Min(samplesPerNode * gValues.Count, memoryLimit / 10)); //ideally there will be "samplesPerNode * gValues.Count" samples, but no more than "memoryLimit / 10"
				PrintMessage("Number of samples: " + samplesPerNode);
				return sampleFromGValues((int)samplesPerNode, useApproximation, quiet);
			}

			/// <summary>
				/// Uses backward planning for enumeration. Does not iterate through all goal states. Search is preformed on meta-states that use wildcards.
				/// </summary>
				/// <param name="quiet"></param>
				/// <returns></returns>
			private void fillGvalues(bool quiet = false)
			{
				var sasProblem = (SASProblem)this.problem;
				gValues = new SASStatePiecewiseDictionary<StateInformation>();
				searchStatus = SearchStatus.InProgress;
				predecessor = new Dictionary<IState, IState>();
				PrintMessage("Enumeration started. Problem: " + problem.GetProblemName(), quiet);
				if (!stopwatch.IsRunning)
					stopwatch.Start();

				IState goalState = getRelativeGoalState();
				openNodes.insert(0, goalState);
				gValues.Add((SASState)goalState, new StateInformation());
				predecessor.Add(goalState, null);

				int steps = -1;
				while (openNodes.size() > 0)
				{
					steps++;
					if (steps % 100000 == 0)
						printSearchStats(quiet);

					if (stopwatch.Elapsed > timeLimit)
					{
						PrintMessage("Enumeration ended - time limit exceeded.", quiet);
						stopwatch.Stop();
						searchTime = stopwatch.Elapsed;
						PrintMessage("Enumeration ended in " + searchTime.TotalSeconds + " seconds", quiet);
						printSearchStats(quiet);
						searchStatus = SearchStatus.TimeLimitExceeded;
						setSearchResults();
						return;
					}

					if (gValues.Count > memoryLimit)
					{
						PrintMessage("Enumeration ended - memory limit exceeded.", quiet);
						stopwatch.Stop();
						searchTime = stopwatch.Elapsed;
						PrintMessage("Enumeration ended in " + searchTime.TotalSeconds + " seconds", quiet);
						printSearchStats(quiet);
						searchStatus = SearchStatus.MemoryLimitExceeded;
						setSearchResults();
						return;
					}

					IState currentState = openNodes.removeMin();
					if (gValues[currentState].isClosed)
						continue;
					addToClosedList(currentState);
					int currentGValue = gValues[currentState].gValue;

					var backwardSuccessors = sasProblem.GetPredecessorsRelative(currentState);
					foreach (var succ in backwardSuccessors)
					{
						if (stopwatch.Elapsed > timeLimit)
						{
							PrintMessage("Enumeration ended - time limit exceeded.", quiet);
							stopwatch.Stop();
							searchTime = stopwatch.Elapsed;
							PrintMessage("enumeration ended in " + searchTime.TotalSeconds + " seconds", quiet);
							printSearchStats(quiet);
							searchStatus = SearchStatus.TimeLimitExceeded;
							setSearchResults();
							return;
						}

						IState state = succ.GetPredecessorState();

						int gVal = currentGValue + succ.GetOperator().GetCost();
						try
						{
							addToOpenList(state, gVal, currentState, succ.GetOperator());
						}
						catch (OutOfMemoryException)
						{
							PrintMessage("Enumeration ended - memory limit exceeded.", quiet);
							stopwatch.Stop();
							searchTime = stopwatch.Elapsed;
							PrintMessage("enumeration ended in " + searchTime.TotalSeconds + " seconds", quiet);
							printSearchStats(quiet);
							searchStatus = SearchStatus.MemoryLimitExceeded;
							setSearchResults();
							return;
						}

					}
				}
				PrintMessage("All states has been enumerated.", quiet);
				printSearchStats(quiet);
				if (searchStatus == SearchStatus.InProgress)
					searchStatus = SearchStatus.NoSolutionExist;
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
				Random r = new Random();
				SASProblem sASProblem = (SASProblem)problem;
				SASState initialState = (SASState)problem.GetInitialState();
				int samplesGenerated = 0;
				double samplesPerRelativeState = (double)numberOfSamples / gValues.Count;
				double samplesToGenerate = 0;

				foreach (var relativeState in gValues.EnumerateKeys)
				{
					samplesToGenerate += samplesPerRelativeState;
					RelativeState s = (RelativeState)relativeState;
					int gVal = gValues[relativeState].gValue;

					yield return generateSample(s, gVal, r, sASProblem, initialState, useApproximation);
					samplesGenerated++;
					while(samplesToGenerate > samplesGenerated)
					{
						yield return generateSample(s, gVal, r, sASProblem, initialState, useApproximation);
						samplesGenerated++;
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

			private StateDistanceResult generateSample(RelativeState state, double gVal, Random r, SASProblem sasProblem, SASState initialState, bool useApproximation)
			{
				RelativeState fixedState = (RelativeState)state.Clone();
				for (int i = 0; i < sasProblem.GetVariablesCount(); i++)
				{
					if (fixedState.GetValue(i) != -1)
						continue;   //already fixed variables will not be modified.
					if (sasProblem.isRigid(i))
					{
						fixedState.SetValue(i, initialState.GetValue(i));    //rigid variables (that are never changed by any operator) has to take the same value as in the initial state.
						continue;
					}
					//this variable is not fixed, neither is it rigid, we assign it a random value from its range
					fixedState.SetValue(i, r.Next(sasProblem.GetVariableDomainRange(i)));
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

					var matchingStates = gValues.GetAllMatchingStates(fixedState);
					double realGVal = gVal;
					if (matchingStates.Any())	//sometimes this is called on a state generated by a randomWalk such that the result is not in the gVals.
						realGVal = matchingStates.Min(kw => kw.Value.gValue);

					return new StateDistanceResult(fixedState, realGVal);
				}
			}

			private IState getRelativeGoalState()
			{
				var sasProblem = (SASProblem)problem;
				int[] values = new int[sasProblem.GetVariablesCount()];
				for (int i = 0; i < values.Length; i++)
				{
					values[i] = -1;	//everything is wildcard unless told differently
				}
				foreach (var item in sasProblem.GetGoalConditions())
				{
					values[item.variable] = item.value;
				}

				RelativeState s = new RelativeState(sasProblem, values);
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
				int maxGoalStates = 1000000;

				//gValues = new Dictionary<IState, StateInformation>();
				searchStatus = SearchStatus.InProgress;
				predecessor = new Dictionary<IState, IState>();
				PrintMessage("Enumeration started. Problem: " + problem.GetProblemName(), quiet);
				if (!stopwatch.IsRunning)
					stopwatch.Start();

                PrintMessage("Generating goal states...", quiet);
				try
				{
					foreach (var item in getAllGoalStates())
					{
						openNodes.insert(0, item);
						gValues.Add((SASState)item, new StateInformation());
						if (openNodes.size() > maxGoalStates)
						{
							PrintMessage("Couldn't generate all goal states.", quiet);
							break;
						}
					}
				}
				catch (Exception)
				{
					PrintMessage("Couldn't generate all goal states.", quiet);
					yield break;
				}
			PrintMessage("Done.", quiet);
				predecessor.Add(problem.GetInitialState(), null);
				int steps = -1;
				while (openNodes.size() > 0)
				{
					steps++;

					if (stopwatch.Elapsed > timeLimit)
					{
						PrintMessage("Enumeration ended - time limit exceeded.", quiet);
						stopwatch.Stop();
						searchTime = stopwatch.Elapsed;
						PrintMessage("Enumeration ended in " + searchTime.TotalSeconds + " seconds", quiet);
						printSearchStats(quiet);
						searchStatus = SearchStatus.TimeLimitExceeded;
						setSearchResults();
						yield break;
					}

					if (gValues.Count > memoryLimit)
					{
						PrintMessage("Enumeration ended - memory limit exceeded.", quiet);
						stopwatch.Stop();
						searchTime = stopwatch.Elapsed;
						PrintMessage("Enumeration ended in " + searchTime.TotalSeconds + " seconds", quiet);
						printSearchStats(quiet);
						searchStatus = SearchStatus.MemoryLimitExceeded;
						setSearchResults();

						yield break;
					}

					IState currentState = openNodes.removeMin();
					if (gValues[currentState].isClosed)
						continue;
					addToClosedList(currentState);
					int currentGValue = gValues[currentState].gValue;
					yield return new StateDistanceResult(currentState, currentGValue);

					var backwardSuccessors = problem.GetAllPredecessors(currentState);
					foreach (var succ in backwardSuccessors)
					{
						if (stopwatch.Elapsed > timeLimit)
						{
							PrintMessage("Enumeration ended - time limit exceeded.", quiet);
							stopwatch.Stop();
							searchTime = stopwatch.Elapsed;
							PrintMessage("enumeration ended in " + searchTime.TotalSeconds + " seconds", quiet);
							printSearchStats(quiet);
							searchStatus = SearchStatus.TimeLimitExceeded;
							setSearchResults();
							yield break;
						}

						IState state = succ.GetPredecessorState();
						
						int gVal = currentGValue + succ.GetOperator().GetCost();
						try
						{
							addToOpenList(state, gVal, currentState, succ.GetOperator());
						}
						catch (OutOfMemoryException)
						{
							PrintMessage("Enumeration ended - memory limit exceeded.", quiet);
							stopwatch.Stop();
							searchTime = stopwatch.Elapsed;
							PrintMessage("enumeration ended in " + searchTime.TotalSeconds + " seconds", quiet);
							printSearchStats(quiet);
							searchStatus = SearchStatus.MemoryLimitExceeded;
							setSearchResults();
							yield break;
						}
					}
				}
				PrintMessage("All states has been enumerated.", quiet);
				printSearchStats(quiet);
				if (searchStatus == SearchStatus.InProgress)
					searchStatus = SearchStatus.NoSolutionExist;
				setSearchResults();
				yield break;
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

			public StateSpaceEnumeratorInternal(IPlanningProblem d, Heuristic h)
			{
				this.problem = d;
				this.heuristic = h;
				this.searchStatus = SearchStatus.NotStarted;
				//this.gValues = new Dictionary<IState, StateInformation>();
				this.gValues = new SASStatePiecewiseDictionary<StateInformation>();

				//this.openNodes = new Heaps.LeftistHeap<State>();
				this.openNodes = new SimpleQueue();
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

			protected void printSearchStats(bool quiet)
			{
				PrintMessage("Closed nodes: " + (gValues.Count) +
							"\tOpen nodes: " + openNodes.size()
							//"\tHeuristic calls: " + heuristic.heuristicCalls +
							//"\tMin heuristic: " + heuristic.statistics.bestHeuristicValue +
							//"\tAvg heuristic: " + heuristic.statistics.getAverageHeurValue().ToString("0.###")
							, quiet);
			}

			public override string getDescription()
			{
				return "A*";
			}

			public override int Search(bool quiet = false)
			{
				throw new NotImplementedException();
			}
		}

		private class SASStatePiecewiseDictionary<T>
		{
			TreeInnerNode<T> treeRoot;
			int size;
			List<KeyValuePair<SASState, T>> resultsPlaceholder = new List<KeyValuePair<SASState, T>>();

			public T this[IState key]
			{
				get
				{
					SASState key2 = (SASState)key;
					if (treeRoot.containsKey(key2, out T val))
						return val;
					throw new KeyNotFoundException();
				}
				set
				{
					SASState key2 = (SASState)key;
					if (!treeRoot.setValue(key2, value))
						throw new KeyNotFoundException();
				}
			}
			/// <summary>
			/// You CANNOT manipulate ICollection of keys on this dictionary implementaion since it would be totally inefficient. If you just want to iterate throught it, use "EnumerateKeys" instead which returns IEnumerable.
			/// </summary>
			public ICollection<SASState> Keys => throw new InvalidOperationException();

			/// <summary>
			/// You CANNOT manipulate ICollection of values on this dictionary implementaion since it would be totally inefficient. If you just want to iterate throught it, use "EnumerateValues" instead which returns IEnumerable. 
			/// Remember that dictionary may contain the same value multiple times. 
			/// The "EnumerateValues" will return all stored values, some of them may be the same (i.e. may return the same value several times). To avoid this use "EnumerateValues().Dictinct()".
			/// </summary>
			public ICollection<T> Values => throw new InvalidOperationException();

			public IEnumerable<SASState> EnumerateKeys
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

			public void Add(SASState key, T value)
			{
				Add(new KeyValuePair<SASState, T>(key, value));
			}

			public void Add(KeyValuePair<SASState, T> item)
			{
				if (treeRoot == null)
				{
					treeRoot = TreeNode<T>.createRoot(item.Key.GetVariablesRanges()[0]);
					TreeNode<T>.variablesCount = item.Key.GetAllValues().Length;
				}

				treeRoot.add(item);
				size++;
			}

			public void Clear()
			{
				this.treeRoot = null;
				size = 0;
				TreeNode<T>.variablesCount = 0;
			}

			public bool Contains(KeyValuePair<SASState, T> item)
			{
				if (treeRoot == null)
					return false;

				if (treeRoot.containsKey(item.Key, out T val))
					if (val.Equals(item.Value))
						return true;
				return false;
			}

			public bool ContainsKey(SASState key)
			{
				if (treeRoot == null)
					return false;
				return treeRoot.containsKey(key, out T val);
			}

			/// <summary>
			/// Returns true if the structure contains any key K1 such that "key" matches K1, e.i. K1 is more general (contains more wildcards).
			/// </summary>
			/// <param name="key"></param>
			/// <returns></returns>
			public bool ContainsMatchingState(SASState key)
			{
				if (treeRoot == null)
					return false;
				return treeRoot.containsMatchingState(key);
			}

			/// <summary>
			/// Returns a set of states such that 
			/// </summary>
			/// <param name="key"></param>
			/// <returns></returns>
			public List<KeyValuePair<SASState, T>> GetAllMatchingStates(SASState key)
			{
				resultsPlaceholder.Clear();
				if (treeRoot != null)
					treeRoot.addMatchingStates(key, resultsPlaceholder);
				return resultsPlaceholder;
			}

			public void CopyTo(KeyValuePair<SASState, T>[] array, int arrayIndex)
			{
				throw new NotImplementedException();
			}

			public IEnumerator<KeyValuePair<SASState, T>> GetEnumerator()
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// This implementation of dictionary allows only to ADD new (key, values) and modify values for already stored (key, values). You cannot remove values.
			/// </summary>
			/// <param name="key"></param>
			/// <returns></returns>
			public bool Remove(SASState key)
			{
				throw new InvalidOperationException();
			}

			/// <summary>
			/// This implementation of dictionary allows only to ADD new (key, values) and modify values for already stored (key, values). You cannot remove values.
			/// </summary>
			/// <param name="key"></param>
			/// <returns></returns>
			public bool Remove(KeyValuePair<SASState, T> item)
			{
				throw new InvalidOperationException();
			}

			public bool TryGetValue(SASState key, out T value)
			{
				if (treeRoot == null)
				{
					value = default(T);
					return false;
				}
				return treeRoot.containsKey(key, out value);
			}

			public SASStatePiecewiseDictionary()
			{
			}

			abstract class TreeNode<M>
			{
				public int decisionVariableIndex;
				public static int variablesCount = 0;
				public const int wildcard = -1;

				protected TreeNode<M> createLeaf(int variableIndex)
				{
					TreeNode<M> t = new TreeLeaf<M>();
					t.decisionVariableIndex = variableIndex;
					return t;
				}

				public static TreeInnerNode<M> createRoot(int firstVariableRange)
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
				protected TreeNode<M> createInnerNode(int variableIndex, KeyValuePair<SASState, M>? value)
				{
					TreeInnerNode<M> n = new TreeInnerNode<M>(variableIndex, value.Value.Key.GetVariablesRanges()[variableIndex]);
					n.decisionVariableIndex = variableIndex;
					n.add(value.Value);
					return n;
				}

				public abstract void add(KeyValuePair<SASState, M> value);

				public abstract bool containsKey(SASState key, out M value);

				public abstract bool containsMatchingState(SASState key);

				public abstract void addMatchingStates(SASState key, List<KeyValuePair<SASState, M>> result);

				/// <summary>
				/// TreeLeaf can hold only one value and will return true if it already has one. Inner nodes are never full.
				/// </summary>
				/// <returns></returns>
				public abstract bool isFull();

				/// <summary>
				/// Gets stored value in the object. Only leaves may store values.
				/// </summary>
				/// <returns></returns>
				public abstract KeyValuePair<SASState, M>? getStoredValue();

				public abstract bool remove(SASState key);

				/// <summary>
				/// Sets value for a key that is already present in the dictionary. Returns false if the key was not found.
				/// </summary>
				/// <param name="key"></param>
				/// <param name="value"></param>
				/// <returns></returns>
				public abstract bool setValue(SASState key, M value);

				public abstract IEnumerable<SASState> enumerateKeys();

				public abstract IEnumerable<M> enumerateValues();
			}

			class TreeInnerNode<M> : TreeNode<M>
			{
				Dictionary<int, TreeNode<M>> successors;

				public override void add(KeyValuePair<SASState, M> value)
				{
					int succIndex = value.Key.GetAllValues()[decisionVariableIndex];
					TreeNode<M> succ = successors[succIndex];
					if (succ.isFull())
						promoteSuccessor(succIndex);
					successors[succIndex].add(value);
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
				protected void promoteSuccessor(int index)
				{
					if (successors[index].decisionVariableIndex <= variablesCount - 1)
						successors[index] = createInnerNode(successors[index].decisionVariableIndex, successors[index].getStoredValue());
				}

				public override bool isFull()
				{
					//inner nodes are never full
					return false;
				}

				public override KeyValuePair<SASState, M>? getStoredValue()
				{
					//only leaves may hold values
					return null;
				}

				public override bool containsKey(SASState key, out M value)
				{
					return successors[key.GetAllValues()[decisionVariableIndex]].containsKey(key, out value);
				}

				public override bool remove(SASState key)
				{
					throw new NotImplementedException();
				}

				public override bool setValue(SASState key, M value)
				{
					return successors[key.GetAllValues()[decisionVariableIndex]].setValue(key, value);
				}

				public override IEnumerable<SASState> enumerateKeys()
				{
					foreach (var succ in successors.Values)
					{
						IEnumerable<SASState> succKeys = succ.enumerateKeys();
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

				public override bool containsMatchingState(SASState key)
				{
					return successors[wildcard].containsMatchingState(key) ||
						successors[key.GetAllValues()[decisionVariableIndex]].containsMatchingState(key);
				}

				public override void addMatchingStates(SASState key, List<KeyValuePair<SASState, M>> result)
				{
					successors[key.GetAllValues()[decisionVariableIndex]].addMatchingStates(key, result);
					successors[wildcard].addMatchingStates(key, result);
				}
			}

			class TreeLeaf<M> : TreeNode<M>
			{
				KeyValuePair<SASState, M>? storedState;

				public override void add(KeyValuePair<SASState, M> value)
				{
					//#if DEBUG
					if (storedState != null)
						throw new ArgumentException();  //item with the same key already present in the dictionary
														//#endif
					storedState = value;
				}

				public override void addMatchingStates(SASState key, List<KeyValuePair<SASState, M>> result)
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

				public override bool containsKey(SASState key, out M value)
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

				public override bool containsMatchingState(SASState key)
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

				public override IEnumerable<SASState> enumerateKeys()
				{
					if (storedState != null)
						yield return storedState.Value.Key;
				}

				public override IEnumerable<M> enumerateValues()
				{
					if (storedState != null)
						yield return storedState.Value.Value;
				}

				public override KeyValuePair<SASState, M>? getStoredValue()
				{
					return storedState;
				}

				public override bool isFull()
				{
					//is full if it already hold some value
					return storedState != null;
				}

				public override bool remove(SASState key)
				{
					throw new NotImplementedException();
				}

				public override bool setValue(SASState key, M value)
				{
					if (storedState == null)
						return false;

					for (int i = decisionVariableIndex; i < key.GetAllValues().Length; i++)
					{
						if (storedState.Value.Key.GetAllValues()[i] != key.GetAllValues()[i])
							return false;
					}
					storedState = new KeyValuePair<SASState, M>(this.storedState.Value.Key, value);
					return true;
				}
			}
		}
	}

	class StateSpaceHistogramCalculator
	{
		public static List<Dictionary<double, Dictionary<int,int>>> getHistogram(string problemFile, List<Heuristic> heurs)
		{
            //the data is organitzed such that the first level is real goal distance (first argument) and the second level is heuristic estimate. The value found on these coordinates represents the number of states that fall into such category.
            //the "list" corresponds to the list of hueristics. Every entry describes results for one heuristic.
            List<Dictionary<double, Dictionary<int, int>>> result = new List<Dictionary<double, Dictionary<int, int>>>();

			var d = SASProblem.CreateFromFile(problemFile);

            foreach (var item in heurs)
            {
                result.Add(new Dictionary<double, Dictionary<int, int>>());
            }

			foreach (var item in StateSpaceEnumerator.enumerateAllStatesWithDistances(d))
			{
                for (int i = 0; i < heurs.Count; i++)
                {
                    addEntry(result[i], item.realDistance, (int)(heurs[i].getValue(item.state)));
                }
			} 

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

	class HistogramTriplet
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

	class PredictionTuple
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

	class FeaturesCalculator
	{
		/// <summary>
		/// Currently it computes four features: number of variables, number of operators, average of variables' ranges, median of variables' ranges
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static List<double> generateFeaturesFromProblem(SASProblem p)
		{
			List<double> result = new List<double>();
			result.Add(p.GetVariablesCount());
			result.Add(p.GetOperators().Count);
			List<int> domainRanges = new List<int>();
			for (int i = 0; i < p.GetVariablesCount(); i++)
				domainRanges.Add(p.GetVariableDomainRange(i));

			result.Add(domainRanges.Average());
			domainRanges.Sort();
			result.Add(domainRanges[domainRanges.Count / 2]);

			return result;
		}

		public static List<PredictionTuple> generatePredictions(List<HistogramTriplet> data)
		{
			List<PredictionTuple> result = new List<PredictionTuple>();
			foreach (var heurVal in data.Select(t => t.heurDistance).Distinct())
			{
				int sumCount = data.Where(t => t.heurDistance == heurVal).Sum(t => t.count);
				double prediction = data.Where(t => t.heurDistance == heurVal).Select(t => (double)t.realDistance * t.count / sumCount).Sum();
				result.Add(new PredictionTuple(heurVal, prediction));
			}
			return result;
		}

		public static List<PredictionTuple> generatePredictions(string fileWithData)
		{
			return generatePredictions(loadHistogramData(fileWithData));
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
		public static void processHistogramsFolder(string folderPath)
		{
			string resultFileName = "dataToLearn.tsv";
			List<List<double>> resultFeatures = new List<List<double>>();
			foreach (var file in Directory.EnumerateFiles(folderPath))
			{
				//checks whether this file is a histogram of some SASProblem in a parrent directory.
				if (Path.GetExtension(file) != ".txt" || !Directory.GetParent(Directory.GetParent(file).FullName).EnumerateFiles().Any(f => Path.ChangeExtension(f.Name, "txt") == Path.GetFileName(file)))
					continue;

				var predictions = generatePredictions(file);
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

		/// <summary>
		/// writes a single number on a standard output that corresponds to total number of computed histograms for all domains in a given folder
		/// </summary>
		/// <param name="domainsFolder"></param>
		public static void countHistograms(string domainsFolder)
		{
			int totalCount = 0;
			foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolder))
			{
				foreach (var problemFile in Directory.EnumerateFiles(domainFolder))
				{
					if (Path.GetExtension(problemFile) != ".sas")
						continue;
					if (Program.IsHistogramComputed(domainFolder, problemFile))
						totalCount++;
				}
			}
			Console.WriteLine(totalCount);
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
		public static SASProblem readProblemFromFileName(string resultsFileName)
		{
			if (resultsFileName.Contains("histograms"))
			{
				//histograms are in this case in a separate folder just inside the domain folder
				string fileNameWithoutPathAndExtension = Path.GetFileNameWithoutExtension(resultsFileName);
				string domainDirectory = Directory.GetParent(Directory.GetParent(resultsFileName).FullName).FullName;
				string domainFile = Path.Combine(domainDirectory, fileNameWithoutPathAndExtension + ".sas");
				return SASProblem.CreateFromFile(domainFile);
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
			SASProblem p = SASProblem.CreateFromFile(@"./../tests/benchmarksSAS_ALL/" + domain + "/" + problem + ".sas");
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
				t[inputs.Count - 1]--;
			}
			return 0;
		}

		public double eval2(List<float> inputs)
		{
			var t = new MyList(inputs);
			if (mapping.ContainsKey(t))
				return mapping[t];

			return -1;
		}

	}

	class FileBasedHeuristic : NNHeuristic
	{
		public FileBasedHeuristic(SASProblem p, string trainedNetworkFile, bool useNetwork = false) : base(p, trainedNetworkFile, useNetwork)
		{
			this.network = new FileBasedModel(trainedNetworkFile);
		}
	}

}
