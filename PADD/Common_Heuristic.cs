using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics;
using NeuralNetTrainer;
using NeuralNetTrainer.TrainingSamples;
using SolutionSpecificUtils;
using SolutionSpecificUtils.DataTransformations;
using SolutionSpecificUtils.Graphs;

namespace PADD
{
	public abstract class Heuristic
	{
		protected SASProblem problem;
		protected abstract double evaluate(IState state);
		public abstract string getDescription();

		/// <summary>
		/// If true, the instance will store information about how many times the heuristic has been called and what is the minimal and average value of all heuristic calls. These results may be accessed through "statistics". If set to false, evaluation should be slightly faster.
		/// </summary>
		public bool doMeasures = true;

		public override string ToString()
		{
			return getDescription();
		}

		public HeuristicStatistics statistics = new HeuristicStatistics();

		public IOperator getBestStateIndex(Dictionary<IOperator, IState> states)
		{
			IOperator best = null;
			double bestValue = int.MaxValue;
			foreach (var item in states.Keys)
			{
				double val = evaluate(states[item]);
				if (val < bestValue)
				{
					best = item;
					bestValue = val;
				}
			}
			return best;
		}

		public double getValue(IState state)
		{
			if (doMeasures)
			{
				double val = evaluate(state);
				if (!double.IsInfinity(val))    //infinity heuristic indicates dead-end. we don't want those included in computation of average heuristic value
				{
					statistics.heuristicCalls++;
					statistics.sumOfHeuristicVals += val;
					if (val < statistics.bestHeuristicValue)
						statistics.bestHeuristicValue = val;
				}
				return val;
			}
			else return evaluate(state);
		}

		/// <summary>
		/// Sets the value of FF heuristic for the state on which the next "getValue" will be called.
		/// </summary>
		public virtual void sethFFValueForNextState(double heurVal)
		{
		}
	}

	public class HeuristicStatistics
	{
		public double bestHeuristicValue;
		public double sumOfHeuristicVals;
		public long heuristicCalls;

		public double getAverageHeurValue()
		{
			return sumOfHeuristicVals / (heuristicCalls + 1);
		}

		public HeuristicStatistics()
		{
			heuristicCalls = 0;
			bestHeuristicValue = double.MaxValue;
			sumOfHeuristicVals = 0d;
		}
	}

	class BlindHeuristic : Heuristic
	{
		protected override double evaluate(IState state)
		{
			return 0;
		}

		public override string getDescription()
		{
			return "Blind heuristic";
		}
	}

	class NotAccomplishedGoalCount : Heuristic
	{
		protected override double evaluate(IState state)
		{
			return state.GetNotAccomplishedGoalsCount();
		}

		public override string getDescription()
		{
			return "Not Accomplished Goals Count heuristic";
		}

		public NotAccomplishedGoalCount()
		{
		}
	}

	class AbstractStateSizeHeuristic : Heuristic
	{
		protected override double evaluate(IState state)
		{
			if (state is SASState)
			{
				return state.GetNotAccomplishedGoalsCount();
			}
			else
			{
				SASStateRedBlack s = (SASStateRedBlack)state;
				return 10000 - 10 * s.Size();
			}
		}

		public override string getDescription()
		{
			return "Abstract state size heuristic";
		}

		public AbstractStateSizeHeuristic(SASProblem d)
		{
			this.problem = d;
		}
	}

	class DeleteRelaxationHeuristic_Perfect : Heuristic
	{
		private SASProblemRedBlack rbProblem;
		private AStarSearch ast;

		public override string getDescription()
		{
			return "Perfect delete relaxation heuristic";
		}

		protected override double evaluate(IState state)
		{
			rbProblem.SetInitialState(new SASStateRedBlack((SASState)state, rbProblem));
			this.ast = new AStarSearch(rbProblem, new AbstractStateSizeHeuristic(rbProblem));
			return ast.Search(true);
		}

		public DeleteRelaxationHeuristic_Perfect(SASProblem d)
		{
			this.problem = d;
			this.rbProblem = SASProblemRedBlack.CreateFromFile(d.GetInputFilePath());
			rbProblem.MakeAllAbstracted();
		}
	}

	class PlannigGraphLayersHeuristic : Heuristic
	{
		private SASProblemRedBlack rbProblem;

		protected override double evaluate(IState state)
		{
			int result = 0;
			IState s = new SASStateRedBlack((SASState)state, rbProblem);
			while (!problem.IsGoalState(s))
			{
				var transitions = rbProblem.GetApplicableRelevantTransitions(s);
				if (transitions == null || transitions.Count == 0)
					return int.MaxValue / 2;

				foreach (var item in transitions)
				{
					s = item.GetOperator().Apply(s);
				}
				//s = Operator.apply(op, s);
				result++;
			}
			/*
            PlanningGraphComputation pgc = new PlanningGraphComputation(this.problem);
            pgc.computePlanningGraph(state);
            if (pgc.OpsLayers.Count != result)
            {
                Console.WriteLine("chyba");
            }
             */
			return result;
		}

		public int getValue1Overestimating(IState state)
		{
			int result = 0;
			IState s = new SASStateRedBlack((SASState)state, rbProblem);
			while (!problem.IsGoalState(s))
			{
				var succ = problem.GetApplicableRelevantTransition(s);
				if (succ == null)
					return int.MaxValue / 2;
				/*
                foreach (var item in operators)
                {
                    s = Operator.apply(item, s);
                }*/
				s = succ.GetOperator().Apply(s);
				result++;
			}
			return result;
		}

		public override string getDescription()
		{
			return "PlannigGraph Layers Count heuristic";
		}

		public PlannigGraphLayersHeuristic(SASProblem d)
		{
			this.problem = d;
			this.rbProblem = SASProblemRedBlack.CreateFromFile(d.GetInputFilePath());
			rbProblem.MakeAllAbstracted();
		}
	}

	class PlanningGraphComputation
	{
		private SASProblemRedBlack rbProblem;
		public List<IState> stateLayers;
		public List<List<IOperator>> OpsLayers;
		public Dictionary<int, Dictionary<int, Dictionary<int, List<int>>>> supportOp;
		public bool isCutOff = false;
		private const int cutOffLimit = 100;

		/// <summary>
		/// Returns list of indices of all operators that can accomplish the given fact in the given fact-layer. 
		/// Operators are described by their indices in the previous op-layer.
		/// If the fact is already present in the previous fact-layer, this method returns null.
		/// </summary>
		/// <param name="layer"></param>
		/// <param name="variable"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public List<int> getSupport(int layer, int variable, int value)
		{
			if (!supportOp.ContainsKey(layer) ||
				!supportOp[layer].ContainsKey(variable) ||
				!supportOp[layer][variable].ContainsKey(value))
				return null;
			return supportOp[layer][variable][value];
		}

		/// <summary>
		/// Only adds a support for newly accomplished facts. If the fact has already been accomplished before, then this method should not be called on that fact.
		/// Support is an operator that accomplished the fact. Operator is described by its index in the previous op-layer.
		/// </summary>
		/// <param name="layer"></param>
		/// <param name="variable"></param>
		/// <param name="value"></param>
		/// <param name="support"></param>
		private void addSupport(int layer, int variable, int value, int support)
		{
			if (!supportOp.ContainsKey(layer))
				supportOp.Add(layer, new Dictionary<int, Dictionary<int, List<int>>>());
			if (!supportOp[layer].ContainsKey(variable))
				supportOp[layer].Add(variable, new Dictionary<int, List<int>>());
			if (!supportOp[layer][variable].ContainsKey(value))
				supportOp[layer][variable].Add(value, new List<int>());
			supportOp[layer][variable][value].Add(support);
		}

		public void computePlanningGraph(IState state)
		{
			stateLayers.Clear();
			OpsLayers.Clear();
			supportOp.Clear();
			isCutOff = false;
			IState s = new SASStateRedBlack((SASState)state, rbProblem);
			stateLayers.Add(s);

			while (!rbProblem.IsGoalState(stateLayers[stateLayers.Count - 1]))
			{
				bool addedSomething = false;
				s = stateLayers[stateLayers.Count - 1].Clone();

				List<IOperator> newOpLayer = new List<IOperator>();
				var transitions = rbProblem.GetApplicableRelevantTransitions(s);
				for (int o = 0; o < transitions.Count; o++)
				{
					var op = (SASOperator)transitions.GetAppliedOperator(o);
					s = op.Apply(s);
					foreach (var effect in op.GetEffects())
					{
						SASState sasState = (SASState)stateLayers[stateLayers.Count - 1];
						if (!sasState.HasValue(effect.GetEff().variable, effect.GetEff().value))
						{
							addSupport(OpsLayers.Count, effect.GetEff().variable, effect.GetEff().value, o);
							addedSomething = true;
						}
					}
					newOpLayer.Add(op);
				}

				OpsLayers.Add(newOpLayer);
				stateLayers.Add(s);
				if (!addedSomething)
				{
					isCutOff = true;
					break;
				}
				if (stateLayers.Count > cutOffLimit)
				{
					isCutOff = true;
					break;
				}
			}

			/*
            for (int i = 0; i < rbDom.getVariablesCount(); i++)
            {
                for (int j = 0; j < rbDom.variablesDomainsRange[i]; j++)
                {
                    if (stateLayers[stateLayers.Count - 1].hasValue(i, j))
                    {
                        var support = getSupport(stateLayers.Count - 2, i, j);
                    }

                }
            }*/
		}

		public PlanningGraphComputation(IPlanningProblem d)
		{
			this.rbProblem = SASProblemRedBlack.CreateFromFile(((SASProblem)d).GetInputFilePath());
			rbProblem.MakeAllAbstracted();
			this.OpsLayers = new List<List<IOperator>>();
			this.stateLayers = new List<IState>();
			this.supportOp = new Dictionary<int, Dictionary<int, Dictionary<int, List<int>>>>();
		}
	}

	class FFHeuristic : Heuristic
	{
		private double hint = -1;

		private PlanningGraphComputation PG;
		//private Red_BlackDomain rbDom;

		//Dalo by se asi urychlit: misto dictionary pouzit normalne pole, mit tam vsechny promenne (ne jen ty potrebne), ale u tech nepotrebnych by ten list byl prazdny
		//Ty listy by se nevytvarely vzdycky znoval, ale pouze by se Clearovaly. Navic se daji ty listy nahradit HashSet aby addNewGoalRequest bylo rychlejsi

		private Dictionary<int, List<int>> notAchievedGoals = new Dictionary<int, List<int>>(),
				notAchievedGoalsNew = new Dictionary<int, List<int>>(),
				dummyVar;
		private List<SASOperator> relaxedPlan = new List<SASOperator>();

		private void addNewGoalRequest(Dictionary<int, List<int>> notAchievedGoals, int variable, int value)
		{
			if (!notAchievedGoals.ContainsKey(variable))
			{
				notAchievedGoals.Add(variable, new List<int>());
				notAchievedGoals[variable].Add(value);
				return;
			}
			if (!notAchievedGoals[variable].Contains(value))
			{
				notAchievedGoals[variable].Add(value);
			}
		}

		public List<SASOperator> getRelaxedPlan(IState state)
		{
			relaxedPlan.Clear();

			PG.computePlanningGraph(state);

			if (PG.isCutOff)
				return relaxedPlan;

			notAchievedGoals.Clear();
			notAchievedGoalsNew.Clear();
			foreach (var item in problem.GetGoalConditions())
			{
				notAchievedGoalsNew.Add(item.variable, new List<int>());
				notAchievedGoalsNew[item.variable].Add(item.value);
			}
			for (int i = PG.stateLayers.Count - 1; i >= 0; i--)
			{
				dummyVar = notAchievedGoals;
				notAchievedGoals = notAchievedGoalsNew;
				notAchievedGoalsNew = dummyVar;
				//value swapping using the third variable as a placeholder
				notAchievedGoalsNew.Clear();

				foreach (var variable in notAchievedGoals.Keys)
				{
					foreach (var value in notAchievedGoals[variable])
					{
						var support = PG.getSupport(i, variable, value);
						if (support == null)
						{
							addNewGoalRequest(notAchievedGoalsNew, variable, value);
						}
						else
						{
							foreach (var supp in support)
							{
								SASOperator op = (SASOperator)PG.OpsLayers[i][supp];
								relaxedPlan.Insert(0, op);

								var preconds = op.GetPreconditions();
								for (int precondIndex = 0; precondIndex < preconds.Count; precondIndex++)
								{
									addNewGoalRequest(notAchievedGoalsNew, preconds[precondIndex].variable, preconds[precondIndex].value);
								}

								foreach (var effect in op.GetEffects())
								{
									foreach (var effCond in effect.GetConditions())
									{
										addNewGoalRequest(notAchievedGoalsNew, effCond.variable, effCond.value);
									}
								}
							}
						}
					}
				}
			}

			return relaxedPlan;
		}

		protected override double evaluate(IState state)
		{
			if (hint != -1)
			{
				double res = hint;
				hint = -1;
				return res;
			}

			PG.computePlanningGraph(state);
			int result = 0;

			if (PG.isCutOff)
				return double.PositiveInfinity;

			notAchievedGoals.Clear();
			notAchievedGoalsNew.Clear();
			foreach (var item in problem.GetGoalConditions())
			{
				notAchievedGoalsNew.Add(item.variable, new List<int>());
				notAchievedGoalsNew[item.variable].Add(item.value);
			}
			for (int i = PG.stateLayers.Count - 1; i >= 0; i--)
			{
				dummyVar = notAchievedGoals;
				notAchievedGoals = notAchievedGoalsNew;
				notAchievedGoalsNew = dummyVar;
				//value swapping using the third variable as a placeholder
				notAchievedGoalsNew.Clear();

				foreach (var variable in notAchievedGoals.Keys)
				{
					foreach (var value in notAchievedGoals[variable])
					{
						var support = PG.getSupport(i, variable, value);
						if (support == null)
						{
							addNewGoalRequest(notAchievedGoalsNew, variable, value);
						}
						else
						{
							foreach (var supp in support)
							{
								result += support.Count;
								SASOperator op = (SASOperator)PG.OpsLayers[i][supp];

								var preconds = op.GetPreconditions();
								for (int precondIndex = 0; precondIndex < preconds.Count; precondIndex++)
								{
									addNewGoalRequest(notAchievedGoalsNew, preconds[precondIndex].variable, preconds[precondIndex].value);
								}

								foreach (var effect in op.GetEffects())
								{
									foreach (var effCond in effect.GetConditions())
									{
										addNewGoalRequest(notAchievedGoalsNew, effCond.variable, effCond.value);
									}
								}
							}
						}
					}
				}
			}

			return result;
		}

		public override string getDescription()
		{
			return "Fast Forward heuristic";
		}

		public FFHeuristic(SASProblem d)
		{
			this.problem = d;
			//this.rbDom = Red_BlackDomain.createFromFile(d.getProblemName());
			//rbDom.makeAllAbstracted();
			this.PG = new PlanningGraphComputation(d);
		}

		public override void sethFFValueForNextState(double ffHeurVal)
		{
			this.hint = ffHeurVal;
		}
	}

	class RBHeuristic : Heuristic
	{
		KnowledgeHolder domainKnowledge;
		FFHeuristic ffHeuristic;
		private int planValue;
		List<SASOperator> relaxedPlan,
			unrelaxedPlan;
		///// <summary>
		///// R = set of currentlly achieved red values, B = set of black variables reacheable according to R
		///// </summary>
		Dictionary<int, HashSet<int>> R, B;
		Dictionary<int, int> toAccomplish;
		IState currentState;

		private void accomplish()
		{

		}

		private void unrelax()
		{
			unrelaxedPlan.Clear();
			unrelaxedPlan.Add(relaxedPlan[0]);
			currentState = relaxedPlan[0].Apply(currentState);
			for (int i = 1; i < relaxedPlan.Count; i++)
			{
				toAccomplish.Clear();

				var preconds = relaxedPlan[i].GetPreconditions();
				for (int j = 0; j < preconds.Count; j++)
				{
					if (!problem.IsVariableAbstracted(preconds[j].variable))
					{
						toAccomplish.Add(preconds[j].variable, preconds[j].value);
					}
				}
				accomplish();
				unrelaxedPlan.Add(relaxedPlan[i]);
				currentState = relaxedPlan[i].Apply(currentState);
			}
			toAccomplish.Clear();
			foreach (var item in problem.GetGoalConditions())
			{
				if (!problem.IsVariableAbstracted(item.variable))
					toAccomplish.Add(item.variable, item.value);
			}
			accomplish();
		}

		private void reset()
		{
			planValue = 0;
			foreach (var item in R.Values)
			{
				item.Clear();
			}

			foreach (var item in B.Values)
			{
				item.Clear();
			}
		}

		private void init(SASProblem d)
		{
			this.domainKnowledge = KnowledgeHolder.compute(d);
			this.ffHeuristic = new FFHeuristic(d);
			this.toAccomplish = new Dictionary<int, int>();
			//R = new Dictionary<int, HashSet<int>>();
			//B = new Dictionary<int, HashSet<int>>();
			//for (int i = 0; i < d.getVariablesCount(); i++)
			//{
			//    if (d.isAbstracted(i))
			//    {
			//        R.Add(i, new HashSet<int>());
			//        R[i].Add(d.initialState.getValue(i));
			//    }
			//    else
			//    {
			//        B.Add(i, new HashSet<int>());
			//        B[i].Add(d.initialState.getValue(i));
			//    }
			//}
		}

		protected override double evaluate(IState state)
		{
			reset();
			this.currentState = state;
			relaxedPlan = ffHeuristic.getRelaxedPlan(state);
			unrelax();

			return planValue;
			//TODO            
		}

		public override string getDescription()
		{
			return "Red-Black Heuristic";
		}

		public RBHeuristic(SASProblem d)
		{
			init(d);
		}
	}

	class WeightedHeuristic : Heuristic
	{
		private Heuristic h;
		private double weight;

		public WeightedHeuristic(Heuristic h, double weight)
		{
			this.h = h;
			this.weight = weight;
		}

		protected override double evaluate(IState state)
		{
			return weight * h.getValue(state);
		}

		public override string getDescription()
		{
			return "weighted " + h.getDescription() + ". Weight = " + weight;
		}
	}

	class SumHeuristic : Heuristic
	{
		private List<Heuristic> h;

		public SumHeuristic(List<Heuristic> h)
		{
			this.h = h;
		}

		protected override double evaluate(IState state)
		{
			return h.Sum(s => s.getValue(state));
		}

		public override string getDescription()
		{
			return "sum of (" + string.Join(", ", h.Select(s => s.getDescription())) + ")";
		}
	}

	class MaxHeuristic : Heuristic
	{
		private List<Heuristic> h;

		public MaxHeuristic(List<Heuristic> h)
		{
			this.h = h;
		}

		protected override double evaluate(IState state)
		{
			return h.Max(s => s.getValue(state));
		}

		public override string getDescription()
		{
			return "max of (" + string.Join(", ", h.Select(s => s.getDescription())) + ")";
		}
	}

	class MinHeuristic : Heuristic
	{
		private List<Heuristic> h;

		public MinHeuristic(List<Heuristic> h)
		{
			this.h = h;
		}

		protected override double evaluate(IState state)
		{
			return h.Min(s => s.getValue(state));
		}

		public override string getDescription()
		{
			return "min of (" + string.Join(", ", h.Select(s => s.getDescription())) + ")";
		}
	}

	#region Noisy heuristics

	class NoisyHeuristic : Heuristic
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

	abstract class NoiseGenerator
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

	enum NoiseGenerationType
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

	#endregion Noisy heuristics 

	#region Learning heuristics

	abstract class LearningHeuristic<T> : Heuristic
	{
		protected Dictionary<T, int> trainingSamples;

		public virtual void addTrainingSamples(List<T> samples)
		{
			foreach (var item in samples)
			{
				if (!trainingSamples.ContainsKey(item))
					trainingSamples.Add(item, 0);
				trainingSamples[item]++;
			}
		}

		/// <summary>
		/// Method has to be called before the heuristic is used. After this method is called, no more training samples can be added.
		/// </summary>
		public abstract void train();

	}

	class HistogramBasedTrainingSample
	{
		/// <summary>
		/// real distance = shosrtest path to the goal from the state. HeuristicValue = value of the heuristic for the state, 
		/// variablesCount = number of variables in the state (we assume that training samples may come from different problems).
		/// </summary>
		public int realDistance, heuristicValue, variablesCount;

		public HistogramBasedTrainingSample(int realDistance, int heuristicValue, int variablesCount)
		{
			this.realDistance = realDistance;
			this.heuristicValue = heuristicValue;
			this.variablesCount = variablesCount;
		}

		public HistogramBasedTrainingSample(int realDistance, int heuristicValue)
		{
			this.realDistance = realDistance;
			this.heuristicValue = heuristicValue;
		}

		public override bool Equals(object obj)
		{
			HistogramBasedTrainingSample other = (HistogramBasedTrainingSample)obj;
			return this.heuristicValue == other.heuristicValue && this.realDistance == other.realDistance && this.variablesCount == other.variablesCount;
		}

		public override int GetHashCode()
		{
			return ((this.heuristicValue * 1000) + this.variablesCount * 1000) + this.realDistance;
		}
	}
	/*
	abstract class HistogramBasedLearningHeuristic : LearningHeuristic<HistogramBasedTrainingSample>
	{
		public static Dictionary<HistogramBasedTrainingSample, int> createTrainingSamples(List<HistogramVisualizer.Histograms> histograms)
		{
			//histograms are indexed by REAL goal value and contain dictionary of (heuristic estimate, count)
			Dictionary<HistogramBasedTrainingSample, int> result = new Dictionary<HistogramBasedTrainingSample, int>();
			foreach (var hist in histograms)
			{
				SASProblem p = SASProblem.CreateFromFile(@"./../tests/benchmarksSAS_ALL/" + hist.domain + "/" + hist.problem + ".sas");
				var item = hist.histograms;
				foreach (var realValue in item.Keys)
				{
					foreach (var heurValue in item[realValue].Keys)
					{
						var sample = new HistogramBasedTrainingSample(realValue, heurValue, p.GetVariablesCount());
						if (!result.ContainsKey(sample))
							result.Add(sample, 0);
						result[sample] += item[realValue][heurValue];
					}
				}
			}
			return result;
		}
	}

	class RegresionLearningHeuristic : LearningHeuristic<HistogramBasedTrainingSample>
	{
		/// <summary>
		/// The regression model represents the dependency of coefficient "c" on heuristic value and other parameters. The coefficient is computed as (realValue / heuristicValue) 
		/// and it is used as a multiplicator. By multiplying the heuristic value by "c" we should be close to real value.
		/// </summary>
		protected RegressionModel m;
		protected Heuristic baseHeuristic;
		protected int polynomialDegree = 3;
		protected double[] args;

		public override string getDescription()
		{
			return "Regresion heuristic";
		}

		public override void train()
		{
			args = new double[] { 0, 0 };
			this.m = trainRegressionModel(this.trainingSamples);
		}

		protected override double evaluate(IState state)
		{
			double heurVal = baseHeuristic.getValue(state);
			args[0] = heurVal;
			args[1] = this.problem.GetVariablesCount();
			return m.eval(args) * heurVal;
		}

		protected RegressionModel trainRegressionModel(Dictionary<HistogramBasedTrainingSample, int> trainingSamples)
		{
			Console.WriteLine("Heur\tVariables\tReal");
			foreach (var item in trainingSamples.Keys)
			{
				Console.WriteLine(item.heuristicValue + "," + item.variablesCount + "," + item.realDistance);
			}

			return new MultiPolynomialRegressionModel(this.polynomialDegree);

			List<double[]> trainingInputs = new List<double[]>();
			List<double> trainingResults = new List<double>();

			var problemSizes = trainingSamples.Keys.Select(t => t.variablesCount).Distinct();
			foreach (var size in problemSizes)
			{
				var relevantItems = trainingSamples.Keys.Where(t => t.variablesCount == size);
				var heurValues = relevantItems.Select(t => t.heuristicValue).Distinct();
				foreach (var heurVal in heurValues)
				{
					var items = relevantItems.Where(t => t.heuristicValue == heurVal);
					double mean = items.Average(t => t.realDistance);
					double trainResult = heurVal != 0 ? mean / heurVal : 1d;
					double[] trainInputs = new double[] { heurVal, size };
					trainingInputs.Add(trainInputs);
					trainingResults.Add(trainResult);
				}
			}

			RegressionModel m = new MultiPolynomialRegressionModel(this.polynomialDegree);
			m.trainModel(trainingInputs, trainingResults);
			return m;
		}

		public RegresionLearningHeuristic(Dictionary<HistogramBasedTrainingSample, int> samples, Heuristic baseHeuristic, SASProblem problem)
		{
			this.problem = problem;
			this.trainingSamples = samples;
			this.baseHeuristic = baseHeuristic;
		}
	}

	abstract class RegressionModel
	{
		protected Func<double[], double> evaluationFunction;
		protected Func<double, double> evaluationFunction2;

		public virtual double eval(double[] inputs)
		{
			return evaluationFunction(inputs);
		}

		public abstract void trainModel(List<double[]> batchOfInputs, List<double> batchOfTargets);
	}

	class MultiLinearRegressionModel : RegressionModel
	{
		public override void trainModel(List<double[]> batchOfInputs, List<double> batchOfTargets)
		{
			this.evaluationFunction = Fit.MultiDimFunc(batchOfInputs.ToArray(), batchOfTargets.ToArray(), true);
		}
	}

	class MultiPolynomialRegressionModel : RegressionModel
	{
		int degree;
		List<Func<double[], double>> kernelFunctions;

		public override void trainModel(List<double[]> batchOfInputs, List<double> batchOfTargets)
		{
			//createKernels(batchOfInputs.First().Length);
			//this.evaluationFunction = Fit.LinearMultiDimFunc(batchOfInputs.ToArray(), batchOfTargets.ToArray(), kernelFunctions.ToArray());
			//var p = Fit.LinearMultiDim(batchOfInputs.ToArray(), batchOfTargets.ToArray(), kernelFunctions.ToArray());
			this.evaluationFunction2 = Fit.PolynomialFunc(batchOfInputs.Select(t => t[0]).ToArray(), batchOfTargets.ToArray(), 2);
			this.evaluationFunction = (d => evaluationFunction2(d[0]));
			var p = Fit.Polynomial(batchOfInputs.Select(t => t[0]).ToArray(), batchOfTargets.ToArray(), 2);
		}

		protected virtual void createKernels(int numberOfvariables)
		{
			this.kernelFunctions = new List<Func<double[], double>>();
			kernelFunctions.AddRange(createPolynomialKernels(this.degree, numberOfvariables));
		}

		/// <summary>
		/// Creates all combiantions of functions in a form x_i * x_j * x_k * ... . E.g. for degree = 2 and numberOfVariables = 3 it will create a list of functions
		/// x_1 * x_1, x_1 * x_2, x_1 * x_3, x_2 * x_2, x_2 * x_3, x_3 * x_3
		/// </summary>
		/// <param name="degree"></param>
		/// <param name="numberOfVariables"></param>
		/// <returns></returns>
		protected List<Func<double[], double>> createPolynomialKernels(int degree, int numberOfVariables)
		{
			var res = new List<Func<double[], double>>();
			for (int i = 1; i <= degree; i++)
			{
				res.AddRange(createPolynomialKernelsRecur(0, numberOfVariables, i, new List<int>()).ToList());
			}
			res.Add(a => 1);
			return res;
		}

		private IEnumerable<Func<double[], double>> createPolynomialKernelsRecur(int currentVar, int maxVariables, int maxDegree, List<int> selectedIndices)
		{
			if (selectedIndices.Count == maxDegree)
			{
				yield return new Func<double[], double>(d =>
				{
					double result = 1;
					foreach (var index in selectedIndices)
					{
						result *= d[index];
					}
					return result;
				});
			}
			else
			{
				for (int i = currentVar; i < maxVariables; i++)
				{
					selectedIndices.Add(i);
					foreach (var item in createPolynomialKernelsRecur(i, maxVariables, maxDegree, selectedIndices))
					{
						yield return item;
					}
					selectedIndices.RemoveAt(selectedIndices.Count - 1);
				}
			}
		}

		public MultiPolynomialRegressionModel(int degree)
		{
			this.degree = degree;
		}
	}

	class RegHeuristic : Heuristic
	{
		private Heuristic h;
		private double[] polynomialCoefficients;

		public RegHeuristic(Heuristic h)
		{
			this.h = h;
			this.polynomialCoefficients = new double[] { 0.13361382, -4.66144875, 2.35220582 };
		}

		protected override double evaluate(IState state)
		{
			double heurVal = h.getValue(state);
			return heurVal * (polynomialCoefficients[2] * heurVal + polynomialCoefficients[1]) + polynomialCoefficients[0];
		}

		public override string getDescription()
		{
			return "Regression heuristic ";
		}
	}
	*/

	class NNHeuristic : Heuristic
	{
		public BrightWireNN network;
		FFHeuristic heur;
		List<float> nnInputs;
		double nextFFHeurResult = -1;

		public override string getDescription()
		{
			return "Neural net heuristic";
		}

		protected override double evaluate(IState state)
		{
			float heurVal = nextFFHeurResult == -1 ? (float)heur.getValue(state) : (float)nextFFHeurResult;
			nextFFHeurResult = -1;
			nnInputs[nnInputs.Count - 1] = heurVal;
			double networkVal = network.eval(nnInputs);
			return networkVal >= 0 ? networkVal + (double)heurVal / 10000 : heurVal;    //breaking ties in this heuristic values by values of the inner heuristic 
		}

		public NNHeuristic(SASProblem p)
		{
			this.heur = new FFHeuristic(p);
			var features = FeaturesCalculator.generateFeaturesFromProblem(p);
			features.Add(0);    //feature dependent on the state;
			nnInputs = features.Select(d => (float)d).ToList();
		}

		public NNHeuristic(SASProblem p, string trainedNetworkFile, bool useNetwork = true)
			: this(p)
		{
			if (useNetwork)
				this.network = BrightWireNN.load(trainedNetworkFile);
		}

		public override void sethFFValueForNextState(double heurVal)
		{
			this.nextFFHeurResult = heurVal;
		}
	}

	class SimpleFFNetHeuristic : Heuristic
	{
		IState originalState;
		Func<Microsoft.Msagl.Drawing.Node, float[]> labelingFunc;
		int labelSize;
		GraphsFeatureGenerator gen;
		List<(float[,] weights, float[] biases)> netParams;
		DataNormalizer normalizer;
		public List<(TrainingSample, double output)> newSamples;

		DomainDependentSolvers.DomainDependentSolver solver;
		bool storeStates = false;
		bool useFFHeuristicAsFeature = false;
		TargetTransformationType targeTransformation;
		FFHeuristic ffH;

		public SimpleFFNetHeuristic(string featuresGeneratorPath, string savedNetworkPath, SASProblem problem, bool useFFHeuristicAsFeature, TargetTransformationType targeTransformation)
		{
			this.problem = problem;
			originalState = problem.GetInitialState();
			var labelingData = UtilsMethods.getLabelingFunction(KnowledgeExtraction.computeObjectGraph(problem).toMSAGLGraph());
			this.labelingFunc = labelingData.labelingFunc;
			this.labelSize = labelingData.labelSize;
			this.gen = GraphsFeatureGenerator.load(featuresGeneratorPath);
			var parms = Network.loadParams(savedNetworkPath);
			netParams = parms.Item1;
			normalizer = DataNormalizer.loadFromParams(parms.Item2);
			this.useFFHeuristicAsFeature = useFFHeuristicAsFeature;
			if (this.useFFHeuristicAsFeature)
				this.ffH = new FFHeuristic(problem);
			this.targeTransformation = targeTransformation;
		}

		/// <summary>
		/// If the solver is given, the heuristic will store all states on which it was evaluated as samples (it will store its features as well as target, for computing the target, the <paramref name="solver"/> will be used).
		/// </summary>
		/// <param name="featuresGeneratorPath"></param>
		/// <param name="savedNetworkPath"></param>
		/// <param name="problem"></param>
		/// <param name="solver"></param>
		public SimpleFFNetHeuristic(string featuresGeneratorPath, string savedNetworkPath, SASProblem problem, bool useFFHeuristicAsFeature, TargetTransformationType targeTransformation, DomainDependentSolvers.DomainDependentSolver solver) :
			this(featuresGeneratorPath, savedNetworkPath, problem, useFFHeuristicAsFeature, targeTransformation)
		{
			this.solver = solver;
			this.storeStates = true;
			this.newSamples = new List<(TrainingSample, double output)>();
		}

		public override string getDescription()
		{
			return "FF-net heuristic";
		}

		protected override double evaluate(IState state)
		{
			this.originalState = problem.GetInitialState();
			problem.SetInitialState(state);
			
			var msaglGraph = KnowledgeExtraction.computeObjectGraph(problem);
			//PADDUtils.GraphVisualization.GraphVis.showGraph(msaglGraph.toMSAGLGraph());
			MyLabeledGraph graph = MyLabeledGraph.createFromMSAGLGraph(msaglGraph.toMSAGLGraph(), this.labelingFunc, this.labelSize);
			//var mmg = graph.toMSAGLGraph(true);
			//PADDUtils.GraphVisualization.GraphVis.showGraph(mmg);

			var features = getFeatures(state, graph);

			TrainingSample s = null;
			if (storeStates)
			{
				solver.SetProblem(problem);
				var realGoalDistance = solver.Search(quiet:true);
				var targets = new float[] { (float)realGoalDistance };
				var splitted = problem.GetInputFilePath().Split(System.IO.Path.DirectorySeparatorChar);
				string stateInfo = splitted[splitted.Length - 2] + "_" + splitted[splitted.Length - 1] + "_" + state.ToString();
				s = new TrainingSample(features, targets);
				s.userData = stateInfo;
			}
			if (normalizer != null)
				features = normalizer.Transform(features, true);
			
			var netOutput = Network.executeByParams(netParams, features);
			if (normalizer != null)
				netOutput = normalizer.ReverseTransform(netOutput, false);

			netOutput = TargetTransformationTypeHelper.reverseTransform(netOutput, targeTransformation);

			if (storeStates)
			{
				newSamples.Add((s, netOutput.Single()));
			}

			problem.SetInitialState(originalState);
			return netOutput.Single();
		}

		protected float[] getFeatures(IState state, MyLabeledGraph graph)
		{
			var features = gen.getFeatures(graph);
			if (useFFHeuristicAsFeature)
			{
				var ffVal = ffH.getValue(state);
				features = features.Append((float)ffVal).ToArray();
			}
			return features;
		}
	}

	#endregion

}
