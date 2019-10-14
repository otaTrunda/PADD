using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace PADD
{
	public abstract class Heuristic
	{
		protected SASProblem problem;
		protected abstract double evaluate(IState state);

		protected virtual double evaluate(IState state, IState predecessor, IOperator op)
		{
			return evaluate(state);
		}

		public abstract string getDescription();

		/// <summary>
		/// If true, the instance will store information about how many times the heuristic has been called and what is the minimal and average value of all heuristic calls. 
		/// These results may be accessed through "statistics". If set to false, evaluation should be slightly faster.
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

		public double getValue(IState state, IState predecessor = null, IOperator op = null)
		{
			if (doMeasures)
			{
				double val = evaluate(state, predecessor, op);
				if (!double.IsInfinity(val))    //infinity heuristic indicates dead-end. we don't want those included in computation of average heuristic value
				{
					statistics.heuristicCalls++;
					statistics.sumOfHeuristicVals += val;
					if (val < statistics.bestHeuristicValue)
						statistics.bestHeuristicValue = val;
				}
				return val;
			}
			else return evaluate(state, predecessor, op);
		}

		/// <summary>
		/// Sets the value of FF heuristic for the state on which the next "getValue" will be called.
		/// </summary>
		public virtual void sethFFValueForNextState(double heurVal)
		{
		}

		public Heuristic()
		{
			doMeasures = true;
#if DEBUG
			doMeasures = true;
#endif
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

	public class BlindHeuristic : Heuristic
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

	public class NotAccomplishedGoalCount : Heuristic
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

	public class AbstractStateSizeHeuristic : Heuristic
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

	public class DeleteRelaxationHeuristic_Perfect : Heuristic
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

	public class PlannigGraphLayersHeuristic : Heuristic
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

	public class PlanningGraphComputation
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

	public class FFHeuristic : Heuristic
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

	public class WeightedHeuristic : Heuristic
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

	public class SumHeuristic : Heuristic
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

	public class WeightedSumHeuristic : Heuristic
	{
		private List<(Heuristic heur, double weight)> h;
		private Random r;

		public static double noiseMax, weightMax, minweight, const4, const5;


		public WeightedSumHeuristic(List<(Heuristic heur, double weight)> h, bool normalizeToUnit = true)
		{
			this.r = new Random(123);
			this.h = h;
			if (normalizeToUnit)
			{
				double sum = h.Sum(q => q.weight);
				this.h = h.Select(q => (q.heur, q.weight / sum)).ToList();
			}
		}

		protected override double evaluate(IState state)
		{
			if (r.NextDouble() < h.First().weight)
				return h.First().heur.getValue(state);
			return h.Sum(s => s.weight * s.heur.getValue(state));
		}

		public override string getDescription()
		{
			return "sum of (" + string.Join(", ", h.Select(s => "(" + s.weight + " * " + s.heur.getDescription() + ")")) + ")";
		}
	}

	public class MaxHeuristic : Heuristic
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

	public class MinHeuristic : Heuristic
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

}
