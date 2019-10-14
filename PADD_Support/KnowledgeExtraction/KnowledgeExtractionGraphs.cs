using System;
using System.Collections.Generic;
using System.Linq;
using PAD.Planner.SAS;

namespace PADD_Support.KnowledgeExtraction
{
	public class KnowledgeExtractionGraphs
	{
		public static CausualGraph computeCausualGraph(PAD.Planner.SAS.Problem problem)
		{
			CausualGraph result = new CausualGraph();
			result.vertices = new List<int>(problem.Variables.Count);
			result.isEdge = new bool[problem.Variables.Count, problem.Variables.Count];
			result.isMentionedInGoal = new bool[problem.Variables.Count];
			for (int i = 0; i < problem.Variables.Count; i++)
			{
				result.vertices.Add(i);
				result.isMentionedInGoal[i] = problem.GoalConditions.Any(g => g.GetVariable() == i);
			}

			foreach (PAD.Planner.SAS.IOperator item in problem.Operators)
			{
				foreach (var precond in item.GetPreconditions())
				{
					foreach (var eff in item.GetEffects())
					{
						if (eff.GetAssignment().GetVariable() != precond.GetVariable())
							result.setEdge(precond.GetVariable(), eff.GetAssignment().GetVariable());
					}
				}
				foreach (var eff in item.GetEffects())
				{
					ConditionalEffect condEff = eff as ConditionalEffect;
					if (condEff != null)
					{
						foreach (var cond in condEff.Conditions)
						{
							if (eff.GetAssignment().GetVariable() != cond.GetVariable())
								result.setEdge(cond.GetVariable(), eff.GetAssignment().GetVariable());
						}
					}
				}
				foreach (var eff in item.GetEffects())
				{
					foreach (var eff2 in item.GetEffects())
					{
						if (eff.GetAssignment().GetVariable() != eff2.GetAssignment().GetVariable())
							result.setEdge(eff.GetAssignment().GetVariable(), eff2.GetAssignment().GetVariable());
					}
				}
			}

			return result;
		}

		public static DomainTransitionGraph computeDTG(PAD.Planner.SAS.Problem problem, int variable)
		{
			DomainTransitionGraph result = new DomainTransitionGraph();
			result.variable = variable;
			result.vertices = new List<int>();
			result.edges = new List<GraphEdge>();

			if (problem.GoalConditions.Any(g => g.GetVariable() == variable))
				result.goalValue = problem.GoalConditions.Where(g => g.GetVariable() == variable).Single().GetValue();
			for (int i = 0; i < problem.Variables[variable].GetDomainRange(); i++)
			{
				result.vertices.Add(i);
			}
			foreach (PAD.Planner.SAS.IOperator item in problem.Operators)
			{
				foreach (var eff in item.GetEffects())
				{
					if (eff.GetAssignment().GetVariable() == variable)
					{
						int targetValue = eff.GetAssignment().GetValue();
						int originalValue = -1;

						List<IAssignment> listOutsideConditions = new List<IAssignment>();
						List<IAssignment> listOutsideEffects = new List<IAssignment>();

						foreach (var eff1 in item.GetEffects())
						{
							if (eff1.GetAssignment().GetVariable() != eff.GetAssignment().GetVariable())
								listOutsideEffects.Add(eff1.GetAssignment());
						}

						ConditionalEffect condEff = eff as ConditionalEffect;
						if (condEff != null)
						{
							foreach (var cond in condEff.Conditions)
							{
								if (cond.GetVariable() == variable)
									originalValue = cond.GetValue();
								else
									listOutsideConditions.Add(new Assignment(cond.GetVariable(), cond.GetValue()));
							}
						}

						foreach (var precond in item.GetPreconditions())
						{
							if (precond.GetVariable() == variable)
								originalValue = precond.GetValue();
							else
								listOutsideConditions.Add(new Assignment(precond.GetVariable(), precond.GetValue()));
						}

						if (originalValue != -1)
						{
							GraphEdge e = new GraphEdge();
							e.from = originalValue;
							e.to = targetValue;
							e.outsideCondition = new ConditionalEffect(new PAD.Planner.SAS.Conditions(listOutsideConditions), new Assignment(-1, -1));
							e.outsideEffect = new ConditionalEffect(new PAD.Planner.SAS.Conditions(listOutsideEffects), new Assignment(-1, -1));
							e.op = item;
							result.edges.Add(e);
						}
						else
						{
							foreach (var val in result.vertices)
							{
								if (val == targetValue)
									continue;
								GraphEdge e = new GraphEdge();
								e.from = val;
								e.to = targetValue;
								e.outsideCondition = new ConditionalEffect(new PAD.Planner.SAS.Conditions(listOutsideConditions), new Assignment(-1, -1));
								e.outsideEffect = new ConditionalEffect(new PAD.Planner.SAS.Conditions(listOutsideEffects), new Assignment(-1, -1));
								e.op = item;
								result.edges.Add(e);
							}
						}
					}
				}
			}
			result.computeRSE_Invertibility();
			return result;
		}

		public static PredicateConstantGraph computeObjectGraph(PAD.Planner.SAS.Problem p)
		{
			//var pddlProblemPath = Utils.FileSystemUtils.getPDDLProblemPath(p.GetInputFilePath());
			//var pp = PDDLProblem.CreateFromFile(pddlProblemPath.domainFile, pddlProblemPath.problemFile);

			var t = translateSASProblemToPDDL(p);
			var pp = t;
			return new PredicateConstantGraph(pp);
		}

		/// <summary>
		/// Gets SaSproblem and produces a PDDLProblem description that
		/// is equivalent to current SASState. It returns content of a PDDL problem file (that when parsed would have the same initial state as the SaSProblem.
		/// </summary>
		/// <param name="p"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		public static PAD.Planner.PDDL.Problem translateSASProblemToPDDL(PAD.Planner.SAS.Problem s)
		{
			var pddlProblemPath = PADDUtils.FileSystemUtils.getPDDLProblemPath(s.GetInputFilePath());
			string originalText = System.IO.File.ReadAllText(pddlProblemPath.problemFile).Replace("(:INIT", "(:init");

			string PDDLStateInitRegion = originalText.Split(new string[] { "(:init" }, StringSplitOptions.RemoveEmptyEntries).Skip(1).First().
				Split(new string[] { "(:" }, StringSplitOptions.RemoveEmptyEntries).First();
			List<string> predicates = PDDLStateInitRegion.Split('(').Select(r => r.Replace(")", "").Trim()).Where(q => !string.IsNullOrWhiteSpace(q)).ToList();
			predicates = predicates.Select(q =>
			{
				int firstSpace = q.IndexOf(" ");
				if (firstSpace < 0)
				{
					firstSpace = q.Length;
					return q.Insert(firstSpace, "(");
				}
				else return q.Remove(firstSpace, 1).Insert(firstSpace, "(");
			}).Select(q => q.Replace(" ", ", ") + ")").ToList();

			List<string> newPredicates = new List<string>();
			var initialState = s.InitialState;
			for (int i = 0; i < initialState.GetAllValues().Length; i++)
			{
				List<string> corresponding = predicates.Where(q => s.Variables[i].Values.Contains("Atom " + q.ToLower())).ToList();
				predicates.RemoveAll(p => corresponding.Contains(p));
				newPredicates.Add(s.Variables[i].Values[initialState.GetAllValues()[i]]);
			}
			predicates.AddRange(newPredicates.Where(x => x.Substring(0, "Atom ".Length) == "Atom "));
			predicates = predicates.Select(p => p.Replace("Atom ", "").Replace("(", " ").Replace(",", "").Replace(")", "").Trim()).ToList();
			string tempFileName = System.IO.Path.GetTempFileName();

			string text = originalText.Split(new string[] { "(:init" }, StringSplitOptions.RemoveEmptyEntries).First() + "(:init\n";
			text += string.Join("\n", predicates.Select(q => "\t(" + q + ")"));
			text += "\n)\n(:";
			text += string.Join("(:", originalText.Split(new string[] { "(:init" }, StringSplitOptions.RemoveEmptyEntries).Skip(1).First().Split(new string[] { "(:" }, StringSplitOptions.RemoveEmptyEntries).Skip(1));

			System.IO.File.WriteAllText(tempFileName, text);

			var result = new PAD.Planner.PDDL.Problem(pddlProblemPath.domainFile, tempFileName);
			System.IO.File.Delete(tempFileName);
			return result;
		}
	}

}
