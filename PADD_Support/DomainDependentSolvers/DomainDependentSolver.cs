﻿using PADD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers
{
	public abstract class DomainDependentSolver
	{
		protected abstract void init();
		public SASProblem sasProblem;
		public List<SASVariable> allVariables;

		public bool canFindPlans { get; protected set; }

		public abstract double Search(bool quiet = false);

		/// <summary>
		/// Returns plan for the previous call of "Search"
		/// </summary>
		/// <returns></returns>
		public abstract List<string> getPDDLPlan();

		public void SetProblem(IPlanningProblem problem)
		{
			sasProblem = (SASProblem)problem;
			allVariables = Enumerable.Range(0, sasProblem.variablesData.Count).Select(i => sasProblem.variablesData.GetVariable(i)).ToList();
			init();
		}

		public string getSymbolicMeaning(int variable, int value)
		{
			return allVariables[variable].valuesSymbolicMeaning[value];
		}

		public string getSymbolicMeaning(int variable, SASState state)
		{
			return allVariables[variable].valuesSymbolicMeaning[state.GetValue(variable)];
		}


		public DomainDependentSolver()
		{
			this.canFindPlans = true;
		}
	}
}