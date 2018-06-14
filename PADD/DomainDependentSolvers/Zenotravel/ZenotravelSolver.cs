using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers.Zenotravel
{
	class ZenotravelSolver : DomainDependentSolver
	{
		ZenoTravelProblem p;
		ZenotravelSpecialSolver solver;

		public override double Search(bool quiet = false)
		{
			solver.quiet = quiet;
			var value = solver.solve(p);
			//solver.showTravelGraph(p);
			return value;
		}

		protected override void init()
		{
			this.p = ZenoTravelProblem.loadFromSAS(this.sasProblem);
		}

		public ZenotravelSolver()
		{
			//solver = new ZenoHillClimbingSolver(HillClimbingMode.FirstChoice, 1000, new Random(123), 10);
			solver = new ZenotravelGASolver(new Random());
			//solver = new ZenotravelTestSolver();
		}

		/// <summary>
		/// Gets the plan corresponding to the last "Search" call.
		/// </summary>
		/// <returns></returns>
		public List<string> getPDDLPlan()
		{
			return solver.getPDDLPlan();
		}
	}
}
