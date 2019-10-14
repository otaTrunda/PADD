using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers.BlocksWorld
{
	public class BlocksWorldSolver : DomainDependentSolver
	{
		BlocksWorldProblem p;
		public BlocksWorldSolver()
		{
		}

		public override List<string> getPDDLPlan()
		{
			List<string> result = new List<string>();
			foreach (var item in this.p.actions)
			{
				if (p.blockInHoist == null || p.blockInHoist.ID != item.block1ID)   //need to be picked-up
				{
					if (item.blockBelowTheFirst == null) //is currently on the table
					{
						result.Add("(" + "pick-up " + p.blocksByIDs[item.block1ID].originalName + ")");
					}
					else //is currently on other block
					{
						result.Add("(" + "unstack " + p.blocksByIDs[item.block1ID].originalName + " " + item.blockBelowTheFirst.originalName + ")");
					}
				}

				if (item.type == BlocksActionType.moveBlockToBlock)
				{
					//stack on other block:
					result.Add("(" + "stack " + p.blocksByIDs[item.block1ID].originalName + " " + p.blocksByIDs[item.block2ID].originalName + ")");
				}
				if (item.type == BlocksActionType.moveBlockToTable)
				{
					//put on table:
					result.Add("(" + "put-down " + p.blocksByIDs[item.block1ID].originalName + ")");
				}
			}
			return result;
		}

		public override double Search(bool quiet = false)
		{
			bool draw = false;
			this.p = new BlocksWorldProblem(this.sasProblem);
			return p.simulate(draw);
		}

		protected override void init()
		{
			this.p = new BlocksWorldProblem(this.sasProblem);
		}
	}
}
