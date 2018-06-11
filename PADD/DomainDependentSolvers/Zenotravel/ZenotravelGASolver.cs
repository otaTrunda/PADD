using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GAF;
using GAF.Operators;

namespace PADD.DomainDependentSolvers.Zenotravel
{
	class ZenotravelGASolver : ZenoLocalSearchSolver
	{
		GeneticAlgorithm alg;
		int populationSize;
		System.Diagnostics.Stopwatch totalTimeWatch;
		int currentGenerationsWithoutImprovement;
		int bestFitness;
		int maxGenerationsWithoutImprovement;
		Chromosome bestIndividual;

		ZenoHillClimbingSolver hillClimber;

		protected void init()
		{
			this.bestFitness = int.MaxValue;
			this.bestIndividual = null;
			this.currentGenerationsWithoutImprovement = 0;
			this.hillClimber = new ZenoHillClimbingSolver(HillClimbingMode.FirstChoice, maxSteps: 10, random: r, restartsCount: 1);
			hillClimber.setProblem(problem);
		}

		public ZenotravelGASolver(Random r, int populationSize = 50, int maxGenerationsWithoutImprovement = 20) : base(r)
		{
			this.populationSize = populationSize;
			this.maxGenerationsWithoutImprovement = maxGenerationsWithoutImprovement;
		}

		protected double CalculateFitness(Chromosome chromosome)
		{
			var length = eval(chromosome.Genes, g => (int)g.ObjectValue);
			if (length < bestFitness)
			{
				bestFitness = length;
				bestIndividual = chromosome.DeepClone();
				currentGenerationsWithoutImprovement = 0;
			}
			return 1d / length;
		}

		protected bool TerminationCriterion(Population population, int currentGeneration, long currentEvaluation)
		{
			logMsg(() => "Generation: " + currentGeneration + ",\twithout improvement: " + currentGenerationsWithoutImprovement +
				"\ttotal run time: " + totalTimeWatch.Elapsed.ToString() + "\tbest fitness: " + bestFitness + "\tdiversity: " + computeDiversity(population));
			if (currentGenerationsWithoutImprovement >= maxGenerationsWithoutImprovement)
				return true;
			currentGenerationsWithoutImprovement++;
			return false;
		}

		/// <summary>
		/// Diversity is 1 if there are no identical individuals in the population, it is 0 if all individuals are identical, it is 0.5 if half of the population is unique and so on.
		/// </summary>
		/// <param name="population"></param>
		/// <returns></returns>
		protected double computeDiversity(Population population)
		{
			int uniqueOnes = numberOfDistinctIndividuals(population);
			var currentDiversity = (double)uniqueOnes / population.PopulationSize;
			return currentDiversity;
		}

		/// <summary>
		/// Returns the number of int[]-individuals in the population that are unique (distinct). I.e. a group of identical individuals counts as one.
		/// </summary>
		/// <param name="population"></param>
		/// <returns></returns>
		protected int numberOfDistinctIndividuals(Population population)
		{
			HashSet<int[]> allDistinct = new HashSet<int[]>(new ArrayInt_EqualityComparer());
			foreach (var item in population.Solutions)
			{
				int[] genes = item.Genes.Select(g => (int)g.ObjectValue).ToArray();
				allDistinct.Add(genes);
			}
			return allDistinct.Count;
		}

		protected override int[] doLocalSearch()
		{
			init();
			alg = new GeneticAlgorithm(createInitialPopulation(populationSize), CalculateFitness);
			alg.Operators.AddRange(createOperators());
			totalTimeWatch = System.Diagnostics.Stopwatch.StartNew();
			alg.Run(TerminationCriterion);
			totalTimeWatch.Stop();
			return bestIndividual.Genes.Select(g => (int)g.ObjectValue).ToArray();
		}

		protected Population createInitialPopulation(int populationSize)
		{
			var population = new Population();
			population.ParentSelectionMethod = ParentSelectionMethod.TournamentSelection;

			//create the chromosomes
			for (var p = 0; p < populationSize; p++)
			{
				var chromosome = new Chromosome(generateRandom());
				population.Solutions.Add(chromosome);
			}
			return population;
		}

		protected virtual IEnumerable<IGeneticOperator> createOperators()
		{
			yield return new Crossover(0.7, allowDuplicates: true, crossoverType: CrossoverType.SinglePoint, replacementMethod: ReplacementMethod.GenerationalReplacement);
			yield return new SinglePointMutation(r, (geneMinVal, geneMaxVal), 0.5);
			yield return new ReplaceByRandom_Mutation(r, (geneMinVal, geneMaxVal), this);
			yield return new PostprocessingMutation(r, greedyPostprocess, 0.01);
			yield return new PostprocessingMutation(r, x => hillClimber.doOneIteration(x).sol, 0.01);
			yield return new Elite(5);
		}

		protected class SinglePointMutation : MutateBase
		{
			protected Random r;
			protected (int lowerBound, int upperBound) geneValueBounds;

			public SinglePointMutation(Random r, (int lowerBound, int upperBound) geneValueBounds, double mutationProbability)
				: base(mutationProbability)
			{
				this.r = r;
				this.geneValueBounds = geneValueBounds;
				this.RequiresEvaluatedPopulation = false;
			}

			protected override void Mutate(Chromosome child)
			{
				//logMsg(() => "exucuting SinglePointMutation");
				//cannot mutate elites or else we will ruin them
				if (child.IsElite)
					return;

				double randomNumber = r.NextDouble();
				if (randomNumber > MutationProbability) //not mutating this time
					return;

				int randomIndex = r.Next(child.Genes.Count);
				child.Genes[randomIndex].ObjectValue = r.Next(geneValueBounds.lowerBound, geneValueBounds.upperBound);
			}

			protected override void MutateGene(Gene gene)
			{
				//this should never be called.
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Replaces all genes by random numbers from their respective ranges.
		/// </summary>
		protected class ReplaceByRandom_Mutation : MutateBase
		{
			protected Random r;
			protected (int lowerBound, int upperBound) geneValueBounds;
			protected ZenotravelGASolver parrentObject;

			public ReplaceByRandom_Mutation(Random r, (int lowerBound, int upperBound) geneValueBounds, ZenotravelGASolver parrentObject)
				: base(1d)
			{
				this.r = r;
				this.geneValueBounds = geneValueBounds;
				this.parrentObject = parrentObject;
				this.RequiresEvaluatedPopulation = false;
			}

			protected override void Mutate(Chromosome child)
			{
				//logMsg(() => "exucuting ReplaceByRandom_Mutation");

				//cannot mutate elites or else we will ruin them
				if (child.IsElite)
					return;

				this.MutationProbability = parrentObject.currentGenerationsWithoutImprovement / (10d * parrentObject.maxGenerationsWithoutImprovement);

				double randomNumber = r.NextDouble();
				if (randomNumber > MutationProbability) //not mutating this time
					return;

				for (int i = 0; i < child.Count; i++)
				{
					child.Genes[i].ObjectValue = r.Next(geneValueBounds.lowerBound, geneValueBounds.upperBound);
				}
			}

			protected override void MutateGene(Gene gene)
			{
				//this should never be called.
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Performes given operation on the individual. The operation might be some kind of post-processing, like Hill-Climbing, greedy step, etc.
		/// </summary>
		protected class PostprocessingMutation : MutateBase
		{
			Func<int[], int[]> modifier;
			Random r;

			public PostprocessingMutation(Random r, Func<int[], int[]> modifier, double mutationProbability) : base(mutationProbability)
			{
				this.modifier = modifier;
				this.r = r;
				this.RequiresEvaluatedPopulation = false;
			}

			protected override void Mutate(Chromosome child)
			{
				//logMsg(() => "exucuting SinglePointMutation");
				//cannot mutate elites or else we will ruin them
				if (child.IsElite)
					return;

				double randomNumber = r.NextDouble();
				if (randomNumber > MutationProbability) //not mutating this time
					return;

				int[] genes = child.Genes.Select(g => (int)g.ObjectValue).ToArray();
				int[] newGenes = modifier(genes);
				for (int i = 0; i < child.Genes.Count; i++)
				{
					child.Genes[i].ObjectValue = newGenes[i];
				}
			}

			protected override void MutateGene(Gene gene)
			{
				throw new NotImplementedException();
			}
		}

		protected class ArrayInt_EqualityComparer : IEqualityComparer<int[]>
		{
			public bool Equals(int[] first, int[] second)
			{
				if (first.Length != second.Length)
					return false;

				for (int i = 0; i < first.Length; i++)
				{
					if (first[i] != second[i])
						return false;
				}
				return true;
			}

			public int GetHashCode(int[] array)
			{
				int multiplier = 31;
				int res = 7;
				unchecked
				{
					foreach (var item in array)
					{
						res += item * multiplier;
						multiplier *= 3;
					}
					return res;
				}
			}
		}
		
	}
}
