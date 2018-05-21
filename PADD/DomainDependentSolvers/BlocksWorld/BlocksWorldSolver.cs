using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers.BlocksWorld
{
	class BlocksWorldSolver : DomainDependentSolver
	{
		BlocksWorldProblem p;
		public BlocksWorldSolver()
		{
		}

		public override double Search(bool quiet = false)
		{
			bool draw = true;
			return p.simulate(draw);
		}

		protected override void init()
		{
			this.p = new BlocksWorldProblem(this.sasProblem);
		}
	}
}
