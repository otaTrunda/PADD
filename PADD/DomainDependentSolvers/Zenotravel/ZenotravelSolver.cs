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
			return solver.solve(p);
		}

		protected override void init()
		{
			this.p = ZenoTravelProblem.loadFromSAS(this.sasProblem);
		}

		public ZenotravelSolver()
		{
			solver = new ZenotravelSpecialSolver();

			CycleFounder.testElementaryCycles();
		}
	}
}
