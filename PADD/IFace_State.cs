using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Common interface for a state in the planning problem.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Checks whether the state satisfy the goal conditions of the planning problem.
        /// </summary>
        /// <returns>True if the state is meeting the problem goal conditions.</returns>
        bool IsMeetingGoalConditions();

        /// <summary>
        /// Checks the number of not-fulfilled goal conditions of the planning problem. Used by search heuristics.
        /// </summary>
        /// <returns>Number of not-fulfilled goal conditions.</returns>
        int GetNotAccomplishedGoalsCount();

        /// <summary>
        /// Makes a deep copy of the state.
        /// </summary>
        /// <returns>Deep copy of the state.</returns>
        IState Clone();
    }
}
