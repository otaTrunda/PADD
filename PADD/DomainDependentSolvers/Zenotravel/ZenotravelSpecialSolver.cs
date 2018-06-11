using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Drawing;
using System.Windows.Forms;
using PADD.ExtensionMethods;


namespace PADD.DomainDependentSolvers.Zenotravel
{
	abstract class ZenotravelSpecialSolver
	{
		ZenotravelSingleSolver singleSolver;
		protected ZenoTravelProblem problem;
		public ZenotravelSpecialSolver()
		{
			this.singleSolver = new ZenotravelSingleGreedySolver();
		}

		public void setProblem(ZenoTravelProblem problem)
		{
			this.problem = problem;
		}

		public void showTravelGraph(ZenoTravelProblem problem, List<Person> onlyThesePersons = null)
		{
			singleSolver.showTravelGraph((onlyThesePersons == null ? problem.personsByIDs.Values.ToList() : onlyThesePersons), problem.planesByIDs.Values.First());
		}

		/// <summary>
		/// Returns length of a plan that solves the given problem.
		/// </summary>
		/// <param name=""></param>
		/// <returns></returns>
		public abstract int solve(ZenoTravelProblem problem);

		protected int eval(int[] assignment, bool writeSolution = false)
		{
			var assignmentAsDictionary = translateAssignment(assignment, problem);
			var length = 0;
			foreach (var item in assignmentAsDictionary)
			{
				var result = singleSolver.solveSingle(problem, problem.planesByIDs[item.Key], item.Value.Select(id => problem.personsByIDs[id]).ToList());
				if (writeSolution)
				{
					Console.WriteLine(string.Join(" ", result.plan) + ",\t" + result.length);
				}
				length += result.length;
			}
			var actionsMovingUnusedPlanesToTheirDestinations = 0;
			foreach (var item in problem.planesByIDs.Values)
			{
				if (!item.isDestinationSet)
					continue;
				if (assignmentAsDictionary.ContainsKey(item.ID))
					continue;
				actionsMovingUnusedPlanesToTheirDestinations++;
				if (item.fuelReserve == 0)
					actionsMovingUnusedPlanesToTheirDestinations++;
			}

			return length + actionsMovingUnusedPlanesToTheirDestinations;
		}

		protected int eval<T>(List<T> assignment, Func<T, int> selector, bool writeSolution = false)
		{
			var assignmentAsDictionary = translateAssignment(assignment, selector, problem);
			var length = 0;
			foreach (var item in assignmentAsDictionary)
			{
				var result = singleSolver.solveSingle(problem, problem.planesByIDs[item.Key], item.Value.Select(id => problem.personsByIDs[id]).ToList());
				if (writeSolution)
				{
					Console.WriteLine(string.Join(" ", result.plan) + ",\t" + result.length);
				}
				length += result.length;
			}

			return length;
		}

		protected Dictionary<int, HashSet<int>> translateAssignment(int[] assignment, ZenoTravelProblem problem)
		{
			Dictionary<int, HashSet<int>> result = new Dictionary<int, HashSet<int>>();
			for (int i = 0; i < assignment.Length; i++)
			{
				if (!result.ContainsKey(assignment[i]))
					result.Add(assignment[i], new HashSet<int>());
				result[assignment[i]].Add(problem.allPersonsIDs[i]);
			}
			return result;
		}

		protected Dictionary<int, HashSet<int>> translateAssignment<T>(List<T> assignment, Func<T, int> selector, ZenoTravelProblem problem)
		{
			Dictionary<int, HashSet<int>> result = new Dictionary<int, HashSet<int>>();
			for (int i = 0; i < assignment.Count; i++)
			{
				if (!result.ContainsKey(selector(assignment[i])))
					result.Add(selector(assignment[i]), new HashSet<int>());
				result[selector(assignment[i])].Add(problem.allPersonsIDs[i]);
			}
			return result;
		}


	}

	class ZenotravelTestSolver : ZenotravelSpecialSolver
	{
		public override int solve(ZenoTravelProblem problem)
		{
			//showTravelGraph(problem, (new List<int>() { 6, 7, 9 }).Select(id => problem.personsByIDs[id]).ToList());

			//var selectedPersons = new HashSet<int>() { 6, 7, 9 };
			//return new ZenotravelSingleGreedySolver().solveSingle(problem, problem.planesByIDs[4], selectedPersons.Select(id => problem.personsByIDs[id]).ToList()).length;

			var plan = new ZenotravelSingleGreedySolver().solveSingle(problem, problem.planesByIDs[1], problem.personsByIDs.Values.ToList());

			Console.WriteLine(string.Join(" ", plan.plan));
			Console.WriteLine(plan.length);
			return plan.length;


		}
	}

	/// <summary>
	/// Solver a single-plane problem with given subset of persons that the plane must transport to their destinations.
	/// </summary>
	abstract class ZenotravelSingleSolver
	{
		/// <summary>
		/// Returns the number of actions required to perform the given path by the plane
		/// </summary>
		/// <param name="planePath"></param>
		/// <param name="plane"></param>
		/// <param name="persons"></param>
		/// <returns></returns>
		protected int evaluatePlan(List<int> planePath, Plane plane, List<Person> persons)
		{
			if (!checkPlan(planePath, plane, persons))
				return -1;
			int flyActionsCount = planePath.Count - 1;
			int fuel = plane.fuelReserve;
			int refuelActionCount = fuel >= flyActionsCount ? 0 : flyActionsCount - fuel;
			int embarkAndDisembrakActionCount = persons.Sum(p => p.isBoarded ? p.weight : 2 * p.weight);
			return flyActionsCount + refuelActionCount + embarkAndDisembrakActionCount;
		}

		protected bool checkPlan(List<int> planePath, Plane plane, List<Person> persons)
		{
			foreach (var item in persons)
			{
				if (!item.isBoarded && !planePath.Contains(item.location))
					return false;
				if (item.isDestinationSet && !planePath.Contains(item.destination))
					return false;
				if (!item.isBoarded && item.isDestinationSet)
				{
					if (planePath.IndexOf(item.location) > planePath.LastIndexOf(item.destination))
						return false;
				}
			}
			if (plane.isDestinationSet && planePath.Last() != plane.destination)
				return false;
			if (planePath.First() != plane.location)
				return false;
			return true;
		}

		protected Dictionary<int, Dictionary<int, int>> inEdges, outEdges;
		protected HashSet<int> involvedCities;
		protected Plane plane;

		public ZenotravelSingleSolver()
		{
		}

		/// <summary>
		/// Returns the number of plane-moves + number of person-bordings + number of person-unbordings.
		/// Some persons from <paramref name="persons"/> list may be already borded in the plane.
		/// </summary>
		/// <param name="problem"></param>
		/// <param name="planeID"></param>
		/// <param name="persons"></param>
		/// <returns></returns>
		public abstract (List<int> plan, int length) solveSingle(ZenoTravelProblem problem, Plane plane, List<Person> persons);

		protected virtual void init(Plane plane)
		{
			this.involvedCities = new HashSet<int>();
			this.plane = plane;
			this.inEdges = new Dictionary<int, Dictionary<int, int>>();
			this.outEdges = new Dictionary<int, Dictionary<int, int>>();
		}

		protected void createTravelGraph(List<Person> persons, Plane plane)
		{
			init(plane);
			foreach (var item in persons)
			{
				if (!item.isBoarded)
					involvedCities.Add(item.location);
				if (item.isDestinationSet)
					involvedCities.Add(item.destination);
			}

			if (plane.isDestinationSet)
				involvedCities.Add(plane.destination);

			foreach (var item in persons)
			{
				if (!item.isBoarded)	//if the person is already boarded, then their original location doesn't matter anymore
					if (item.destination != plane.destination)	//if person's destination is the same as plane's, we don't need to care about them - the plane will always go to its destination anyways.
						addEdge(item.location, item.destination, item.weight);
			}
		}

		protected Graph createMSAGLGraph(List<Person> persons, Plane plane)
		{
			Graph g = new Graph();
			createTravelGraph(persons, plane);
			
			foreach (var from in outEdges.Keys)
				foreach (var to in outEdges[from].Keys)
					g.AddEdge(from.ToString(), outEdges[from][to].ToString(), to.ToString());
			
			if (plane.isDestinationSet)
			{
				var e = g.AddEdge(plane.location.ToString(), plane.destination.ToString());
				e.Attr.Color = Color.Blue;
			}
			return g;
		}

		public void showTravelGraph(List<Person> persons, Plane plane)
		{
			Form f = new Form();
			var graphDrawer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
			graphDrawer.Dock = DockStyle.Fill;
			f.Controls.Add(graphDrawer);
			graphDrawer.Graph = createMSAGLGraph(persons, plane);
			f.ShowDialog();
		}

		/// <summary>
		/// Edge represents a desire of some person to travel from <paramref name="from"/> to <paramref name="to"/>
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		protected void addEdge(int from , int to, int weight)
		{
			if (!outEdges.ContainsKey(from))
				outEdges.Add(from, new Dictionary<int, int>());
			if (!outEdges[from].ContainsKey(to))
				outEdges[from].Add(to, 0);
			outEdges[from][to] += weight;

			if (!inEdges.ContainsKey(to))
				inEdges.Add(to, new Dictionary<int, int>());
			if (!inEdges[to].ContainsKey(from))
				inEdges[to].Add(from, 0);
			inEdges[to][from] += weight;
		}

		/// <summary>
		/// Should only be called after the travel graph is computed.
		/// Finds all start- and end- leaves. Returns them in a layered fashion: startLeaves in the first layer should be visited first, then startLeaves in the second layer can be visited
		/// and so on. The same with the end-leaves.
		/// </summary>
		/// <returns></returns>
		protected (List<HashSet<int>> startLeaves, List<HashSet<int>> endLeaves) findAllLeaves(List<int> visitedNodes)
		{
			HashSet<int> isolated = findIsolatedVertices();

			List<HashSet<int>> startLeaves = new List<HashSet<int>>();
			HashSet<int> allStartLeaves = new HashSet<int>(visitedNodes);
			HashSet<int> ignoredNodes = isolated;
			HashSet<int> newLeaves;
			do
			{
				newLeaves = findLeaves(allStartLeaves, isForStartingLeaves: true, ignoredNodes);
				if (newLeaves.Count > 0)
				{
					startLeaves.Add(newLeaves);
					allStartLeaves.AddRange(newLeaves);
				}
			} while (newLeaves.Count > 0);


			List<HashSet<int>> endLeaves = new List<HashSet<int>>();
			HashSet<int> allEndLeaves = new HashSet<int>();
			ignoredNodes = allStartLeaves;
			ignoredNodes.AddRange(isolated);
			
			do
			{
				newLeaves = findLeaves(allEndLeaves, isForStartingLeaves: false, ignoredNodes);
				if (newLeaves.Count > 0)
				{
					endLeaves.Add(newLeaves);
					allEndLeaves.AddRange(newLeaves);
				}
			} while (newLeaves.Count > 0);

			return (startLeaves, endLeaves);
		}

		/// <summary>
		/// Scans the desire matrix and tries to find leaves - i.e. nodes that have either only out-edges, or only input-edges.
		/// Nodes with input-edges only are called endingLeaves and should be visited last (in any order).
		/// Nodes with out-edges only are called startingLeaves and should be visited first (in any order).
		/// </summary>
		/// <param name="startingLeaves"></param>
		/// <returns>New leaves that were found. All new leaves will also be added to <paramref name="leaves"/>!!!</returns>
		protected HashSet<int> findLeaves(HashSet<int> leaves, bool isForStartingLeaves, HashSet<int> ignoredNodes)
		{
			HashSet<int> result = new HashSet<int>();

			if(isForStartingLeaves)
			{
				foreach (var key in outEdges.Keys)
				{
					if (leaves.Contains(key) || ignoredNodes.Contains(key))
						continue;

					if (inEdges.ContainsKey(key))
					{
						if (inEdges[key].Keys.All(pred => leaves.Contains(pred)))
							result.Add(key);
					}
					else
						result.Add(key);
				}
			}

			if (!isForStartingLeaves)
			{
				foreach (var key in inEdges.Keys)
				{
					if (leaves.Contains(key) || ignoredNodes.Contains(key))
						continue;

					if (outEdges.ContainsKey(key))
					{
						if (outEdges[key].Keys.All(pred => leaves.Contains(pred)))
							result.Add(key);
					}
					else
						result.Add(key);
				}
			}
			return result;
		}

		protected HashSet<int> findIsolatedVertices()
		{
			HashSet<int> result = new HashSet<int>();
			foreach (var item in involvedCities)
			{
				if (!outEdges.ContainsKey(item) && !inEdges.ContainsKey(item))
					result.Add(item);
			}
			return result;
		}

		/// <summary>
		/// Takes the original input and preprocesses it by making obviously correct choices. This may solve part of the problem. 
		/// The solved part is removed and the rest of the problem is returned to be solved by some other method.
		/// After that core-problem has been solved in a form of a partially-ordered plan, this core-solution can be extended to a solution of the original problem by calling
		/// POPlanExtender. (It again returns the solution in a form of a P-O plan).
		/// Using this technique twice will not do anything new.
		/// It is not guaranteed that it will have any effect (i.e. it may just return the original input if it cannot be preprocessed).
		/// </summary>
		protected (bool didPreprocess, ZenoTravelProblem problem, List<Person> persons, Func<List<HashSet<int>>, List<HashSet<int>>> POPlanExtender) 
			createPreprocessedInput(ZenoTravelProblem problem, List<Person> persons, List<int> visitedNodes)
		{
			createTravelGraph(persons, plane);
			/*
			var cycles = CycleFounder.getElementaryCycles((this.outEdges.Keys.ToDictionary(k => k, k => outEdges[k].Keys.ToList()), null, involvedCities));
			foreach (var cycle in cycles)
			{
				Console.WriteLine(string.Join(" ", cycle));
			}
			*/

			var enforcedActions = findAllLeaves(visitedNodes);

			if (enforcedActions.startLeaves.Count == 0 && enforcedActions.endLeaves.Count == 0)	//couldn't preprocess the problem :(
			{
				return (false, problem, persons, new Func<List<HashSet<int>>, List<HashSet<int>>>(a => a));
			}

			List<Person> remainingPersons = persons.Where(p => !enforcedActions.startLeaves.Any(POLayer => POLayer.Contains(p.location) || POLayer.Contains(p.destination)) &&
															   !enforcedActions.endLeaves.Any(POLayer => POLayer.Contains(p.location) || POLayer.Contains(p.destination)) 
															   //&&  p.destination != plane.destination
															   ).ToList();

			var POPlanExtender = new Func<List<HashSet<int>>, List<HashSet<int>>>(POPlan =>
			{
				var result = new List<HashSet<int>>();
				result.AddRange(enforcedActions.startLeaves);
				result.AddRange(POPlan);
				result.AddRange(enforcedActions.endLeaves, reverse: true);
				return result;
			});

			return (true, problem, remainingPersons, POPlanExtender);
		}

		/// <summary>
		/// When there are no obvious good choices (that means all remaining nodes are part of cycles), this will find all cycles in the remaining graph and return node that lies in the largest
		/// number of cycles. If there are more such nodes, those with larger degree are prefered (sum of in- and out- degree).
		/// Visiting such node should break cycles and the "preprocess" migh solve further parts of the remaining problem.
		/// </summary>
		/// <param name="problem"></param>
		/// <param name="plane"></param>
		/// <param name="persons"></param>
		/// <param name="visitedNodes"></param>
		/// <returns></returns>
		protected int findBestNodeToVisit(ZenoTravelProblem problem, Plane plane, List<Person> persons, List<int> visitedNodes)
		{
			var remainingNodes = this.involvedCities.Where(c => persons.Any(p => (!p.isBoarded && p.location == c) || p.destination == c)).ToHashSet();
			var remainingEdges = new Dictionary<int, List<int>>();
			foreach (var item in remainingNodes)
			{
				if (outEdges.ContainsKey(item) && outEdges[item].Keys.Any(r => remainingNodes.Contains(r)))
					remainingEdges.Add(item, this.outEdges[item].Keys.Where(r => remainingNodes.Contains(r)).ToList());
			}

			var cycles = CycleDetection.findElementaryCycles((remainingEdges, null, remainingNodes));
			Dictionary<int, int> occurences = remainingNodes.ToDictionary(k => k, k => 0);
			foreach (var item in cycles.SelectMany(n => n))
			{
				occurences[item]++;
			}
			var mostOccurences = occurences.Keys.Max(k => occurences[k]);
			var bestNodes = occurences.Keys.Where(k => occurences[k] == mostOccurences);

			if (bestNodes.Count() == 1)
				return bestNodes.Single();
			var bestNodesDegrees = bestNodes.Select(node => (node, remainingEdges[node].Count + this.inEdges[node].Keys.Count(c => remainingNodes.Contains(c))));
			var maxDegree = bestNodesDegrees.Max(r => r.Item2);
			var bestNode = bestNodesDegrees.Where(r => r.Item2 == maxDegree);
			return bestNode.First().node;

		}

	}

	class ZenotravelSingleGreedySolver : ZenotravelSingleSolver
	{
		SolutionCreator creator = new SolutionCreator();

		/// <summary>
		/// Person is called "easy" if it travels to the final destination of the plane.
		/// </summary>
		List<Person> easyPersons;

		public override (List<int> plan, int length) solveSingle(ZenoTravelProblem problem, Plane plane, List<Person> persons)
		{
			init(plane, persons);

			var POplan = solveRecur(problem, persons, new List<int>(), isCycleSearchEpoch: false);
			var plan = creator.createLinearPlan(problem, plane, persons, POplan);
			//Console.WriteLine(string.Join(" ", plan));
			persons.AddRange(easyPersons);
			var length = evaluatePlan(plan, plane, persons);
			return (plan, length);
		}

		protected List<HashSet<int>> solveRecur(ZenoTravelProblem problem, List<Person> persons, List<int> visitedNodes, bool isCycleSearchEpoch)
		{
			if (persons.Count == 0)
				return solveEasyPersons(problem, this.easyPersons);
			if (persons.Count == 1)
				return solveSinglePerson(problem, persons);
			if (isCycleSearchEpoch == false)
			{
				var preprocessed = createPreprocessedInput(problem, persons, visitedNodes);
				return preprocessed.POPlanExtender(solveRecur(preprocessed.problem, preprocessed.persons, visitedNodes, !isCycleSearchEpoch));
			}
			else
			{
				var nodeToVisit = findBestNodeToVisit(problem, plane, persons, visitedNodes);
				visitedNodes.Add(nodeToVisit);
				//persons.ForEach(p => { if (p.location == nodeToVisit) p.isBoarded = true; });
				var newPersons = persons.Where(p => p.location != nodeToVisit).ToList();
				var subProblemSolution = solveRecur(problem, newPersons, visitedNodes, !isCycleSearchEpoch);
				var visitAction = new HashSet<int>() { nodeToVisit };
				subProblemSolution.Insert(0, visitAction);
				subProblemSolution.Add(visitAction);
				return subProblemSolution;
			}
		}

		/// <summary>
		/// Solves the problem with just one person.
		/// </summary>
		/// <param name="problem"></param>
		/// <param name="persons"></param>
		/// <returns></returns>
		protected List<HashSet<int>> solveSinglePerson(ZenoTravelProblem problem, List<Person> persons)
		{
			List<HashSet<int>> result = new List<HashSet<int>>();
			var departureLoc = new HashSet<int>();
			departureLoc.Add(persons.Single().location);

			var arivalLoc = new HashSet<int>();
			arivalLoc.Add(persons.Single().destination);

			result.Add(departureLoc);
			result.Add(arivalLoc);

			result.AddRange(solveEasyPersons(problem, this.easyPersons));
			return result;
		}

		/// <summary>
		/// Solves the problem where all persons want to travel the the final destination of the plane.
		/// </summary>
		/// <param name="problem"></param>
		/// <param name="easyPersons"></param>
		/// <returns></returns>
		protected List<HashSet<int>> solveEasyPersons(ZenoTravelProblem problem, List<Person> easyPersons)
		{
			List<HashSet<int>> result = new List<HashSet<int>>();

			foreach (var item in easyPersons)
			{
				var departureLoc = new HashSet<int>();
				departureLoc.Add(item.location);
				result.Add(departureLoc);
			}
			return result;
		}

		protected void init(Plane plane, List<Person> persons)
		{
			this.easyPersons = persons.Where(p => p.destination == plane.destination).ToList();
			persons.RemoveAll(p => p.destination == plane.destination);
			base.init(plane);
		}

	}

	class SolutionCreator
	{
		protected static List<int> emptyList = new List<int>();

		protected Dictionary<int, List<int>> positionsOfCitiesInSolution;
		protected List<int> result;

		protected Dictionary<int, List<int>> requiredPredecessors;

		public List<int> createLinearPlan(ZenoTravelProblem problem, Plane plane, List<Person> persons, List<HashSet<int>> POPlan)
		{
			result = new List<int>();
			positionsOfCitiesInSolution.Clear();
			computePredecessors(persons);

			addToResult(plane.location);
			for (int i = 0; i < POPlan.Count - 1; i++)
			{
				var POLayer = POPlan[i];
				foreach (var city in POLayer)
				{
					addToResultIfNecessary(city);
				}
			}
			//last layer:

			foreach (var city in POPlan[POPlan.Count - 1])
			{
				if (city == plane.destination)
					continue;
				addToResultIfNecessary(city);
			}
			if (plane.isDestinationSet)
				addToResult(plane.destination);

			return result;
		}

		protected void computePredecessors(List<Person> persons)
		{
			requiredPredecessors.Clear();
			foreach (var item in persons)
			{
				if (item.destination == -1 || item.location == item.destination)
					continue;
				addPredecessor(item.destination, item.location);
			}
		}

		protected void addPredecessor(int item, int predecessor)
		{
			if (!requiredPredecessors.ContainsKey(item))
				requiredPredecessors.Add(item, new List<int>());
			requiredPredecessors[item].Add(predecessor);
		}

		protected List<int> getPreviousVisits(int city)
		{
			if (positionsOfCitiesInSolution.ContainsKey(city))
				return positionsOfCitiesInSolution[city];
			return emptyList;
		}

		protected List<int> getPredecessors(int city)
		{
			if (requiredPredecessors.ContainsKey(city))
				return requiredPredecessors[city];
			return emptyList;
		}

		protected void addToResultIfNecessary(int city)
		{
			int positionToAdd = result.Count;
			var positions = getPreviousVisits(city);
			if (positions.Count == 0)
			{
				addToResult(city);
				return;
			}
			var forcingPredecessors = getPredecessors(city).Where(pred => getPreviousVisits(pred).Count > 0);
			var predecessorsFirstOccurences = forcingPredecessors.Select(pred => getPreviousVisits(pred).First());
			var lastOccurenceOfThis = positions.Last();
			if (predecessorsFirstOccurences.Any(oc => lastOccurenceOfThis < oc))
				addToResult(city);
		}

		protected void addToResult(int city)
		{
			if (!positionsOfCitiesInSolution.ContainsKey(city))
				positionsOfCitiesInSolution.Add(city, new List<int>());
			positionsOfCitiesInSolution[city].Add(result.Count);
			result.Add(city);
		}

		public SolutionCreator()
		{
			this.positionsOfCitiesInSolution = new Dictionary<int, List<int>>();
			requiredPredecessors = new Dictionary<int, List<int>>();
		}

	}
}
