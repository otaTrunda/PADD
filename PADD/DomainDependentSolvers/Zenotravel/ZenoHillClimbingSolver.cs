using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers.Zenotravel
{
	internal class ZenoHillClimbingSolver : ZenoLocalSearchSolver
	{
		HillClimbingMode mode;
		int maxSteps;
		int restarts = 1;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mode">Various modes of the search</param>
		/// <param name="maxSteps">Max steps during ONE iteration (i.e. between restarts). If local optimum is reached before this limit, the current iteration will end.</param>
		/// <param name="r"></param>
		/// <param name="restartsCount"></param>
		public ZenoHillClimbingSolver(HillClimbingMode mode, int maxSteps, Random r, int restartsCount = 1) : base(r)
		{
			this.mode = mode;
			this.maxSteps = maxSteps;
			this.restarts = restartsCount;
		}

		protected override int[] doLocalSearch()
		{
			int[] bestSolution = null;
			int bestFitness = int.MaxValue;
			
			for (int i = 0; i < restarts; i++)
			{
				Console.WriteLine("\t--restarting--");
				var res = doOneIteration();
				if (res.fitness < bestFitness)
				{
					bestSolution = res.sol;
					bestFitness = res.fitness;
				}
			}
			return bestSolution;
		}

		protected (int[] sol, int fitness) doOneIteration()
		{
			int steps = 0;
			int[] solution = generateRandom();
			int fitness = eval(solution);
			while (steps < maxSteps)
			{
				steps++;
				var res = doOneStep(solution, fitness);
				if (res.localOptimumReached)
					break;
				fitness = res.newFitness;
				Console.WriteLine("New fitness: " + fitness);
			}
			return (solution, fitness);
		}

		/// <summary>
		/// Modifies the solution in place, returns its new fitness and true if local optimum was reached, of false otherwise.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		protected (bool localOptimumReached, int newFitness) doOneStep(int[] item, int currentFitness)
		{
			switch (mode)
			{
				case HillClimbingMode.RMHC:
					var res = singlePointMutationInPlace(item);
					var newFitness = eval(item);
					if (newFitness < currentFitness)
						return (false, newFitness);
					item[res.indexOfChange] = res.previousVal;
					return (false, currentFitness);
				case HillClimbingMode.FirstChoice:
					foreach (var modified in generateNeighbourhoodInPlace(item))
					{
						int fitness = eval(modified);
						if (fitness < currentFitness)
							return (false, fitness);
					}
					return (true, currentFitness);
				case HillClimbingMode.Standard:
					int bestFitness = int.MaxValue;
					int indexOfChange = -1, newValue = -1;
					foreach (var modified in generateNeighbourhoodInPlace(item, true))
					{
						int fitness = eval(modified.item);
						if (fitness < bestFitness)
						{
							bestFitness = fitness;
							indexOfChange = modified.changedIndex;
							newValue = modified.newValue;
						}
					}
					if (bestFitness < currentFitness)
					{
						item[indexOfChange] = newValue;
						return (false, bestFitness);
					}
					return (true, currentFitness);
				default:
					throw new Exception();
			}
		}
	}

	enum HillClimbingMode
	{
		RMHC,
		FirstChoice,
		Standard,
	}
}
