using PADD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PAD.Planner.SAS;

namespace PADD.DomainDependentSolvers
{
	public abstract class DomainDependentSolver
	{
		protected abstract void init();
		public Problem sasProblem;
		public List<Variable> allVariables;

		public bool canFindPlans { get; protected set; }

		public abstract double Search(bool quiet = false);

		/// <summary>
		/// Returns plan for the previous call of "Search"
		/// </summary>
		/// <returns></returns>
		public abstract List<string> getPDDLPlan();

		public void SetProblem(PAD.Planner.IProblem problem)
		{
			sasProblem = (Problem)problem;
			allVariables = Enumerable.Range(0, sasProblem.Variables.Count).Select(i => sasProblem.Variables[i]).ToList();
			init();
		}

		public string getSymbolicMeaning(int variable, int value)
		{
			return allVariables[variable].Values[value];
		}

		public string getSymbolicMeaning(int variable, IState state)
		{
			return allVariables[variable].Values[state.GetValue(variable)];
		}


		public DomainDependentSolver()
		{
			this.canFindPlans = true;
		}
	}
}
