using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers.Zenotravel
{
	abstract class ZenoLocalSearchSolver : ZenotravelSpecialSolver
	{

		protected int genomeLength,
			geneMinVal,	//inclusive
			geneMaxVal; //exclusive

		protected Random r;

		protected IEnumerable<int[]> generateNeighbourhood(int[] item)
		{
			for (int i = 0; i < item.Length; i++)
			{
				for (int j = geneMinVal; j < geneMaxVal; j++)
				{
					if (item[i] == j)
						continue;
					int[] clone = (int[])item.Clone();
					clone[i] = j;
					yield return clone;
				}
			}
		}

		/// <summary>
		/// Just repeatedly modifies the original item in place.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		protected IEnumerable<int[]> generateNeighbourhoodInPlace(int[] item)
		{
			for (int i = 0; i < item.Length; i++)
			{
				int originalValue = item[i];
				for (int j = geneMinVal; j < geneMaxVal; j++)
				{
					if (originalValue == j)
						continue;
					item[i] = j;
					yield return item;
				}
				item[i] = originalValue;
			}
		}

		/// <summary>
		/// Just repeatedly modifies the original item in place. Returns the modified item and changes that were done to the original.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		protected IEnumerable<(int[] item, int changedIndex, int newValue)> generateNeighbourhoodInPlace(int[] item, bool withChangeDescription)
		{
			for (int i = 0; i < item.Length; i++)
			{
				int originalValue = item[i];
				for (int j = geneMinVal; j < geneMaxVal; j++)
				{
					if (originalValue == j)
						continue;
					item[i] = j;
					yield return (item, i, j);
				}
				item[i] = originalValue;
			}
		}

		protected int[] singlePointMutation(int[] item)
		{
			int[] clone = (int[])item.Clone();
			int index = r.Next(clone.Length);
			clone[index] = r.Next(geneMinVal, geneMaxVal);
			return clone;
		}

		/// <summary>
		/// Performs a single point mutation in place. Returns the item as well as description of changes that were done to the item by the mutation.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		protected (int[]item, int indexOfChange, int newVal, int previousVal) singlePointMutationInPlace(int[] item)
		{
			int index = r.Next(item.Length);
			int previousVal = item[index];
			int newVal = r.Next(geneMinVal, geneMaxVal);
			item[index] = newVal;
			return (item, index, newVal, previousVal);
		}

		protected int[] generateRandom()
		{
			int[] result = new int[genomeLength];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = r.Next(geneMinVal, geneMaxVal);
			}
			return result;
		}

		/// <summary>
		/// Tries to modify the assingment by some greedy rules-of-thumb. Works in-place.
		/// </summary>
		/// <returns></returns>
		protected int[] greedyPostprocess(int[] item)
		{
			//persons whose destination is the same as target destination of some plane are assigned to that plane
			foreach (var person in problem.personsByIDs.Values)
			{
				if (problem.planesByTheirTargetDestination.ContainsKey(person.destination))
				{
					var suitablePlanes = problem.planesByTheirTargetDestination[person.destination];
					var planeID = suitablePlanes[r.Next(suitablePlanes.Count)];
					var personPosition = getPersonsIndexInSolution(person.ID);
					item[personPosition] = planeID;
				}
			}

			//persons whose original location is the same as location of some plane are assigned to that plane
			foreach (var person in problem.personsByIDs.Values)
			{
				if (problem.planesByTheirOriginalLocation.ContainsKey(person.location))
				{
					var suitablePlanes = problem.planesByTheirOriginalLocation[person.location];
					var planeID = suitablePlanes[r.Next(suitablePlanes.Count)];
					var personPosition = getPersonsIndexInSolution(person.ID);
					item[personPosition] = planeID;
				}
			}
			return item;
		}

		protected int getPersonsIndexInSolution(int personID)
		{
			return problem.allPersonsIDs.IndexOf(personID);
		}

		public override int solve(ZenoTravelProblem problem)
		{
			this.genomeLength = problem.personsByIDs.Keys.Count;
			this.geneMinVal = problem.planesByIDs.Keys.Min();
			this.geneMaxVal = problem.planesByIDs.Keys.Max();
			this.problem = problem;

			var result = new int[0];
			if (problem.personsByIDs.Count > 0)
				result = doLocalSearch();
			return eval(result, writeSolution: !quiet);
		}

		protected abstract int[] doLocalSearch();

		public ZenoLocalSearchSolver(Random r)
		{
			this.r = r;
		}

	}
}
