using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers
{
	class GripperSolver : DomainDependentSolver
	{
		(int variable, int value) robotInRoomA;
		SASState initialState => (SASState)(sasProblem.GetInitialState());

		protected override void init()
		{
			robotInRoomA.variable = int.Parse(allVariables.Where(v => v.valuesSymbolicMeaning.Any(str => str.Contains("at-robby(rooma)"))).Single().GetName().Substring("var".Length));
			robotInRoomA.value = allVariables[this.robotInRoomA.variable].valuesSymbolicMeaning.FindIndex(str => str.Contains("at-robby(rooma)"));
		}

		public override double Search(bool quiet = false)
		{
			int ballsInA = ballsAtA(initialState);
			int ballsHolding = holdingBalls(initialState);
			bool robotInA = isInA(initialState);

			int result = 0;
			int processedBallsFromA = 0;

			if (!robotInA)
			{
				result += ballsHolding; //one action for every ball to drop it in B
			}
			else
			{
				if (ballsInA > 0)
				{
					if (ballsHolding == 2)
					{
						result += 3;	//one action to move to B, two actions to drop balls
					}

					if (ballsHolding == 1 || (ballsHolding == 0 && ballsInA == 1))
					{
						result += 4;    //one action to pick-up another ball in A, one action to move to B, two actions to drop balls
						processedBallsFromA += 1;
					}

					if (ballsHolding == 0 && ballsInA > 1)
					{
						result += 5;    //two actions to pick-up two balls in A, one action to move to B, two actions to drop balls
						processedBallsFromA += 2;
					}
				}
				else if (ballsHolding > 0)
				{
					result += ballsHolding + 1;	//one action to move to B, one action for every ball to drop it.
				}
			}
			ballsInA -= processedBallsFromA;
			//now lets assume that the robot is in B with both grippers free
			if (ballsInA > 0)
			{
				int travels = (int)Math.Ceiling(ballsInA / 2d) * 2; //two move-actions for every pair of balls in A. (If the number of balls is odd, it is the same as if it were greater by one.)
				result += travels + ballsInA * 2; //two actions for every ball in A (pick-up, drop) and the required number of move-actions.
			}
			return result;
		}

		protected bool isInA(SASState state)
		{
			return state.GetValue(robotInRoomA.variable) == robotInRoomA.value;
		}


		protected int holdingBalls(SASState state)
		{
			return Enumerable.Range(0, allVariables.Count).Where(i => getSymbolicMeaning(i, state).Contains("carry(")).Count();
		}

		protected int ballsAtA(SASState state)
		{
			return Enumerable.Range(0, allVariables.Count).Where(i => (getSymbolicMeaning(i, state).Contains("at(") && getSymbolicMeaning(i, state).Contains("rooma"))).Count();
		}

		public GripperSolver()
		{
			
		} 
	}
}
