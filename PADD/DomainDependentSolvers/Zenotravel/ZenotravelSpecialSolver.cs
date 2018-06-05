using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Drawing;
using System.Windows.Forms;


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
			singleSolver.solveSingle(problem, 0, problem.personsByIDs.Values.ToList());
			return 0;
		}

	}

	/// <summary>
	/// Solver a single-plane problem with given subset of persons that the plane must transport to their destinations.
	/// </summary>
	class ZenotravelSingleSolver
	{
		Dictionary<int, int> inputDegreesOfCities,
			outputDegreesOfCities;

		int[,] desireMatrix;

		Dictionary<int, int> cityToIndex;

		public int solveSingle(ZenoTravelProblem problem, int planeID, List<Person> persons)
		{
			showTravelGraph(persons);
			return 0;
		}

		protected void createDesireMatrix(List<Person> persons)
		{
			HashSet<int> addedCities = new HashSet<int>();
			foreach (var item in persons)
			{
				if (!addedCities.Contains(item.location))
				{
					cityToIndex.Add(item.location, addedCities.Count);
					addedCities.Add(item.location);
				}
				if (!addedCities.Contains(item.destination))
				{
					cityToIndex.Add(item.destination, addedCities.Count);
					addedCities.Add(item.destination);
				}
			}
			desireMatrix = new int[cityToIndex.Count, cityToIndex.Count];
			foreach (var item in persons)
			{
				addEdge(item.location, item.destination);
			}
		}

		protected Graph createMSAGLGraph(List<Person> persons)
		{
			Graph g = new Graph();
			createDesireMatrix(persons);
			for (int i = 0; i < desireMatrix.GetLength(0); i++)
			{
				for (int j = 0; j < desireMatrix.GetLength(1); j++)
				{
					g.AddEdge(i.ToString(), desireMatrix[i, j].ToString(), j.ToString());
				}
			}
			return g;
		}

		protected void showTravelGraph(List<Person> persons)
		{
			Form f = new System.Windows.Forms.Form();
			var graphDrawer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
			graphDrawer.Dock = DockStyle.Fill;
			f.Controls.Add(graphDrawer);
			graphDrawer.Graph = createMSAGLGraph(persons);
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
	}
}
