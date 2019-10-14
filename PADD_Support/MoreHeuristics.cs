using PADD;
using PADD.DomainDependentSolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD_Support
{
	#region Noisy heuristics

	public class NoisyHeuristic : Heuristic
	{
		NoiseGenerator g;
		Heuristic h;

		public NoisyHeuristic(Heuristic h, NoiseGenerator generator)
		{
			this.h = h;
			this.g = generator;
		}

		protected override double evaluate(IState state)
		{
			double val = h.getValue(state);
			return val + g.generateNoise(val);
		}

		public override string getDescription()
		{
			return "Noisy heuristic(" + h.getDescription() + " + " + g.getDescription() + ")";
		}
	}

	public abstract class NoiseGenerator
	{
		protected Random r = new Random();
		public abstract double generateNoise(double argument);

		public abstract string getDescription();

		public static NoiseGenerator createInstance(NoiseGenerationType type, double parameter)
		{
			switch (type)
			{
				case NoiseGenerationType.constantUniform:
					return new ConstantUniformNoiseGenerator(parameter);
				case NoiseGenerationType.ProportionalUniform:
					return new ProportionalUniformNoiseGenerator(parameter);
				case NoiseGenerationType.ConstantNormal:
					return new ConstantNormalNoiseGenerator(parameter);
				case NoiseGenerationType.ProportionalNormal:
					return new ProportionalNormalNoiseGenerator(parameter);
				default:
					return null;
					break;
			}
		}
	}

	public enum NoiseGenerationType
	{
		constantUniform, ProportionalUniform, ConstantNormal, ProportionalNormal
	}

	class ConstantUniformNoiseGenerator : NoiseGenerator
	{
		double intervalRadius;

		public override double generateNoise(double argument)
		{
			double val = r.NextDouble() * intervalRadius * 2 - intervalRadius;
			return val;
		}

		public ConstantUniformNoiseGenerator(double intervalRadius)
		{
			this.intervalRadius = intervalRadius;
		}

		public override string getDescription()
		{
			return "U[-" + intervalRadius + ", " + intervalRadius + "]";
		}
	}

	class ProportionalUniformNoiseGenerator : NoiseGenerator
	{
		double proportion;

		public override double generateNoise(double argument)
		{
			double intervalRadius = argument * proportion;
			double val = r.NextDouble() * intervalRadius * 2 - intervalRadius;
			return val;
		}

		public ProportionalUniformNoiseGenerator(double proportion)
		{
			this.proportion = proportion;
		}
		public override string getDescription()
		{
			return "U[-" + (proportion * 100) + "%, " + (proportion * 100) + "%]";
		}
	}

	class ConstantNormalNoiseGenerator : NoiseGenerator
	{
		double stdDev;

		public override double generateNoise(double argument)
		{
			double val = MathNet.Numerics.Distributions.Normal.Sample(0, stdDev);
			return val;
		}

		public ConstantNormalNoiseGenerator(double stdDev)
		{
			this.stdDev = stdDev;
		}

		public override string getDescription()
		{
			return "N[0, " + stdDev + "]";
		}
	}

	class ProportionalNormalNoiseGenerator : NoiseGenerator
	{
		double proportion;

		public override double generateNoise(double argument)
		{
			double stdDev = argument * proportion;
			double val = MathNet.Numerics.Distributions.Normal.Sample(0, stdDev);
			return val;
		}

		public ProportionalNormalNoiseGenerator(double proportion)
		{
			this.proportion = proportion;
		}

		public override string getDescription()
		{
			return "N[0, " + (proportion * 100) + "%]";
		}
	}

	public class SpecificSolverHeuristic : Heuristic
	{
		DomainDependentSolver solver;

		public override string getDescription()
		{
			return "DomainDependentSolverHeuristic";
		}

		protected override double evaluate(IState state)
		{
			this.problem.SetInitialState(state);
			solver.SetProblem(this.problem);
			var solutionLength = solver.Search(quiet: true);
			return solutionLength;
		}

		public SpecificSolverHeuristic(IPlanningProblem problem, DomainType domain)
		{
			switch (domain)
			{
				case DomainType.Zeno:
					solver = new PADD.DomainDependentSolvers.Zenotravel.ZenotravelSolver(20, 5);
					break;
				case DomainType.VisitAll:
					solver = new PADD.DomainDependentSolvers.VisitAll.VisitAllSolver();
					break;
				case DomainType.Blocks:
					solver = new PADD.DomainDependentSolvers.BlocksWorld.BlocksWorldSolver();
					break;
				case DomainType.TSP:
					throw new Exception();
				default:
					throw new Exception();
			}
			this.problem = (SASProblem)problem;
			this.doMeasures = false;
		}
	}


	/// <summary>
	/// Should emulate the trained NN. Uses specific solver to get near-exact h-value and adds some gausian noise to it.
	/// The heuristic also keeps all state on which it was evaluated together with their real goal distances.
	/// </summary>
	public class NoisyPerfectHeuristic : SpecificSolverHeuristic
	{
		private double noisePercentage;
		public List<(IState, double)> perfectDistances;
		public double multiplier = 1;

		protected override double evaluate(IState state)
		{
			var val = base.evaluate(state);
			perfectDistances.Add((state, val));
			if (val <= 0)
				return 0;
			var res = MathNet.Numerics.Distributions.Normal.Sample(val, val * noisePercentage);
			var diff = res - val;

			val *= multiplier;
			val += diff;

			return val;
		}

		public NoisyPerfectHeuristic(IPlanningProblem problem, DomainType domain, double noisePercentage = 1d / 6)
			: base(problem, domain)
		{
			this.noisePercentage = noisePercentage;
			this.perfectDistances = new List<(IState, double)>();
		}

		public override string getDescription()
		{
			return base.getDescription() + ", noise: " + this.noisePercentage;
		}
	}

	#endregion Noisy heuristics 

}
