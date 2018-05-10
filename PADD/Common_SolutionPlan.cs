using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Representation of the solution plan for the PDDL/SAS+ planning problem.
    /// Plan is a sequence of operations from the initial state to a goal state.
    /// </summary>
    public class SolutionPlan
    {
        /// <summary>
        /// Sequence of applied operators composing a plan.
        /// </summary>
        private List<IOperator> operatorSequence;

        /// <summary>
        /// Constructs an empty plan.
        /// </summary>
        public SolutionPlan()
        {
            operatorSequence = new List<IOperator>();
        }

        /// <summary>
        /// Constructs a plan based on the entered operator sequence.
        /// </summary>
        /// <param name="operatorSequence">Sequence of operators composing a solution plan.</param>
        public SolutionPlan(List<IOperator> operatorSequence)
        {
            this.operatorSequence = operatorSequence;
        }

        /// <summary>
        /// Adds a new step in the solution plan.
        /// </summary>
        /// <param name="op">New operator inserted to the end of the plan.</param>
        public void AppendOperator(IOperator op)
        {
            operatorSequence.Add(op);
        }

        /// <summary>
        /// Returns a list of operator IDs composing the solution plan.
        /// </summary>
        /// <returns>List of operator IDs.</returns>
        public List<int> GetOperatorSeqIndices()
        {
            List<int> retList = new List<int>();
            foreach (var op in operatorSequence)
                retList.Add(op.GetOrderIndex());
            return retList;
        }

        /// <summary>
        /// Construct a string representing the solution plan.
        /// </summary>
        /// <returns>String representation of the plan.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<");
            bool first = true;

            foreach (var op in operatorSequence)
            {
                if (first)
                    first = false;
                else
                    sb.Append(", ");
                sb.Append(op.ToString());
            }
            sb.Append(">");

            return sb.ToString();
        }

		/// <summary>
		/// Returns the sequence of states startig with the given state such that successsive states are created by applying respective operators. 
		/// If the given state is the initial state of the problem, this will return a path to a goal state - i.e. the last state in the result will be a goal state.
		/// </summary>
		/// <param name="initialState"></param>
		/// <returns></returns>
		public List<IState> getSequenceOfStates(IState initialState)
		{
			List<IState> result = new List<IState>();
			IState currentState = initialState;
			result.Add(currentState);
			foreach (var item in this.operatorSequence)
			{
				currentState = item.Apply(currentState);
				result.Add(currentState);
			}
			return result;
		}

		public int getCost()
		{
			return operatorSequence.Sum(op => op.GetCost());
		}

	}
}
