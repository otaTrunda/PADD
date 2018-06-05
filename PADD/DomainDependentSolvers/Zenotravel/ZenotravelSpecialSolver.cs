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
	class ZenotravelSpecialSolver
	{
		ZenotravelSingleSolver singleSolver;

		public ZenotravelSpecialSolver()
		{
			this.singleSolver = new ZenotravelSingleSolver();
		}

		/// <summary>
		/// Returns length of a plan that solves the given problem.
		/// </summary>
		/// <param name=""></param>
		/// <returns></returns>
		public int solve(ZenoTravelProblem problem)
		{
			singleSolver.solveSingle(problem, problem.planesByIDs[1], problem.personsByIDs.Values.ToList());
			return 0;
		}

	}

	/// <summary>
	/// Solver a single-plane problem with given subset of persons that the plane must transport to their destinations.
	/// </summary>
	class ZenotravelSingleSolver
	{
		protected Dictionary<int, int> inputDegreesOfCities,
			outputDegreesOfCities;

		protected int[,] desireMatrix;
		protected Plane thisPlane;

		protected Dictionary<int, int> cityToIndex;
		protected List<int> indexToCity;

		public ZenotravelSingleSolver()
		{
			this.cityToIndex = new Dictionary<int, int>();
		}

		/// <summary>
		/// Returns the number of plane-moves + number of person-bordings + number of person-unbordings.
		/// Some persons from <paramref name="persons"/> list may be already borded in the plane.
		/// </summary>
		/// <param name="problem"></param>
		/// <param name="planeID"></param>
		/// <param name="persons"></param>
		/// <returns></returns>
		public virtual int solveSingle(ZenoTravelProblem problem, Plane plane, List<Person> persons)
		{
			init(plane);
			showTravelGraph(persons, plane);
			return 0;
		}

		protected virtual void init(Plane plane)
		{
			this.cityToIndex = new Dictionary<int, int>();
			indexToCity = new List<int>();
			this.thisPlane = plane;
		}

		protected void createDesireMatrix(List<Person> persons, Plane plane)
		{
			HashSet<int> addedCities = new HashSet<int>();
			foreach (var item in persons)
			{
				if (!item.isBoarded)
					if (!addedCities.Contains(item.location))
					{
						cityToIndex.Add(item.location, addedCities.Count);
						indexToCity.Add(item.location);
						addedCities.Add(item.location);
					}
				if (!addedCities.Contains(item.destination))
				{
					cityToIndex.Add(item.destination, addedCities.Count);
					indexToCity.Add(item.destination);
					addedCities.Add(item.destination);
				}
			}
			/*
			if (!addedCities.Contains(plane.location))
			{
				cityToIndex.Add(plane.location, addedCities.Count);
				indexToCity.Add(plane.location);
				addedCities.Add(plane.location);
			}
			*/

			if (plane.isDestinationSet)
				if (!addedCities.Contains(plane.destination))
				{
					cityToIndex.Add(plane.destination, addedCities.Count);
					indexToCity.Add(plane.destination);
					addedCities.Add(plane.destination);
				}

			desireMatrix = new int[cityToIndex.Count, cityToIndex.Count];
			foreach (var item in persons)
			{
				if (!item.isBoarded)	//if the person is already borded, then their original location doesn't matter anymore
					if (item.destination != plane.destination)	//if it is the same, we don't need to care about them - the plane will always go to its destination anyways.
						addEdge(item.location, item.destination);
			}

			/*
			foreach (var item in addedCities)
			{
				if (plane.location != item)
					addEdge(plane.location, item);
			}
			

			if (plane.isDestinationSet)
				foreach (var item in addedCities)
				{
					if (plane.destination != item)
						addEdge(item, plane.destination);
				}
			*/
		}

		protected Graph createMSAGLGraph(List<Person> persons, Plane plane)
		{
			Graph g = new Graph();
			createDesireMatrix(persons, plane);
			for (int i = 0; i < desireMatrix.GetLength(0); i++)
			{
				for (int j = 0; j < desireMatrix.GetLength(1); j++)
				{
					if (desireMatrix[i, j] > 0)
						g.AddEdge(indexToCity[i].ToString(), desireMatrix[i, j].ToString(), indexToCity[j].ToString());
				}
			}
			if (plane.isDestinationSet && cityToIndex.ContainsKey(plane.location))
			{
				var e = g.AddEdge(plane.location.ToString(), plane.destination.ToString());
				e.Attr.Color = Color.Blue;
			}
			return g;
		}

		protected void showTravelGraph(List<Person> persons, Plane plane)
		{
			Form f = new Form();
			var graphDrawer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
			graphDrawer.Dock = DockStyle.Fill;
			f.Controls.Add(graphDrawer);
			graphDrawer.Graph = createMSAGLGraph(persons, plane);
			var t = findAllLeaves();
			f.ShowDialog();
		}

		/// <summary>
		/// Edge represents a desire of some person to travel from <paramref name="from"/> to <paramref name="to"/>
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		protected void addEdge(int from , int to)
		{
			desireMatrix[cityToIndex[from], cityToIndex[to]]++;
		}

		/// <summary>
		/// Should only be called after the desire matrix is computed.
		/// Finds all start- and end- leaves. Returns them in a layered fashion: startLeaves in the first layer should be visited first, then startLeaves in the second layer can be visited
		/// and so on. The same with the end-leaves.
		/// </summary>
		/// <returns></returns>
		protected (List<HashSet<int>> startLeaves, List<HashSet<int>> endLeaves) findAllLeaves()
		{
			HashSet<int> isolated = findIsolatedVertices();

			List<HashSet<int>> startLeaves = new List<HashSet<int>>();
			HashSet<int> allStartLeaves = new HashSet<int>();
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
			for (int i = 0; i < desireMatrix.GetLength(0); i++)
			{
				if (ignoredNodes.Contains(i) || leaves.Contains(i))
					continue;
				bool isLeave = true;
				for (int j = 0; j < desireMatrix.GetLength(0); j++)
				{
					if (i == j)
						continue;
					if ((isForStartingLeaves && desireMatrix[j, i] > 0 && !leaves.Contains(j)) ||
					   (!isForStartingLeaves && desireMatrix[i, j] > 0 && !leaves.Contains(j)))
					{
						isLeave = false;
						break;
					}
				}
				if (isLeave)
				{
					result.Add(i);
				}
			}
			return result;
		}

		protected HashSet<int> findIsolatedVertices()
		{
			HashSet<int> result = new HashSet<int>();
			for (int i = 0; i < desireMatrix.GetLength(0); i++)
			{
				bool isIsolated = true;
				for (int j = 0; j < desireMatrix.GetLength(0); j++)
				{
					if (desireMatrix[i,j] > 0 || desireMatrix[j,i] > 0)
					{
						isIsolated = false;
						break;
					}
				}
				if (isIsolated)
					result.Add(i);
			}
			return result;
		}

		/// <summary>
		/// Takes the original input and preprocesses it by making obviously correct choices. This may solve part of the problem. 
		/// The solved part is removed and the rest of the problem is returned to be solved by some other method.
		/// After that core-problem has been solved in a form of a partially-ordered plan, this core-solution can be extended to a solution of the original problem by calling
		/// POPlanExtender. (It again returns the solution in a form of a P-O plan).
		/// This technique can be used recursivelly several times, but it is not guaranteed that it will have any effect (i.e. it may just return the original input if it cannot be preprocessed)
		/// </summary>
		protected (bool didPreprocess, ZenoTravelProblem problem, Plane plane, List<Person> persons, Func<List<HashSet<int>>, List<HashSet<int>>> POPlanExtender) 
			createPreprocessedInput(ZenoTravelProblem problem, Plane plane, List<Person> persons)
		{
			createDesireMatrix(persons, plane);
			var enforcedActions = findAllLeaves();

			if (enforcedActions.startLeaves.Count == 0 && enforcedActions.endLeaves.Count == 0)	//couldn't preprocess the problem :(
			{
				return (false, problem, plane, persons, new Func<List<HashSet<int>>, List<HashSet<int>>>(a => a));
			}

			List<Person> remainingPersons = persons.Where(p => !enforcedActions.startLeaves.Any(POLayer => POLayer.Contains(p.location) || POLayer.Contains(p.destination)) &&
															   !enforcedActions.endLeaves.Any(POLayer => POLayer.Contains(p.location) || POLayer.Contains(p.destination))).ToList();

			var POPlanExtender = new Func<List<HashSet<int>>, List<HashSet<int>>>(POPlan =>
			{
				var result = new List<HashSet<int>>();
				result.AddRange(enforcedActions.startLeaves);
				result.AddRange(POPlan);
				result.AddRange(enforcedActions.endLeaves);
				return result;
			});

			return (true, problem, plane, remainingPersons, POPlanExtender);
		}

	}

	class ZenotravelSingleOptimalSolver : ZenotravelSingleSolver
	{
		protected Dictionary<int, List<int>> positionsOfCitiesInSolution;

		public override int solveSingle(ZenoTravelProblem problem, Plane plane, List<Person> persons)
		{
			createDesireMatrix(persons, plane);

			return base.solveSingle(problem, plane, persons);
		}

	}
}
