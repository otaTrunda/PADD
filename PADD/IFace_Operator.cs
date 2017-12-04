using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD
{
    /// <summary>
    /// Common interface for an operator in the planning problem.
    /// </summary>
    public interface IOperator
    {
        /// <summary>
        /// Checks whether the operator is relevant (in context of planning search) to the given state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator is relevant to the given state, false otherwise.</returns>
        bool IsRelevant(IState state);

        /// <summary>
        /// Checks whether the operator is applicable (in context of planning search) to the given state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator is applicable to the given state, false otherwise.</returns>
        bool IsApplicable(IState state);

        /// <summary>
        /// Checks whether the operator can be predecessor to the given state in the planning search process.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator can be predecessor to the given state, false otherwise.</returns>
        bool CanBePredecessor(IState state);

        /// <summary>
        /// Applies the operator to the given state. The result is a new state (successor).
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Successor state to the given state.</returns>
        IState Apply(IState state);

        /// <summary>
        /// Applies the operator backwards to the given state. The result is a set of states (possible predecessors).
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Possible predecessor states to the given state.</returns>
        List<IState> ApplyBackwards(IState state);

        /// <summary>
        /// Gets a cost of the operator.
        /// </summary>
        /// <returns>Operator cost.</returns>
        int GetCost();

        /// <summary>
        /// Gets the operator ID in the planning problem.
        /// </summary>
        /// <returns>Operator ID in the planning problem.</returns>
        int GetOrderIndex();
    }
}
