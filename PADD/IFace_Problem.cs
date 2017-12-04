using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PADD
{
    /// <summary>
    /// Common interface for a model of the planning problem.
    /// </summary>
    public interface IPlanningProblem
    {
        /// <summary>
        /// Gets the planning problem name.
        /// </summary>
        /// <returns>The planning problem name.</returns>
        string GetProblemName();

        /// <summary>
        /// Gets the initial state of the planning problem.
        /// </summary>
        /// <returns>The initial state.</returns>
        IState GetInitialState();

        /// <summary>
        /// Checks whether the specified state is meeting goal conditions of the planning problem.
        /// </summary>
        /// <param name="state">A state to be checked.</param>
        /// <returns>True if the specified state is a goal state of the problem, false otherwise.</returns>
        bool IsGoalState(IState state);

        /// <summary>
        /// Gets a list of forward transitions (successors) from the specified state. Only maxNumSucc transitions are returned.
        /// Repeated calls of this method returns next successors. If all successors have been returned, then null value is
        /// returned and the next call will start from the beginning.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <param name="maxNumSucc">Maximum number of returned successors.</param>
        /// <returns>A collection of transitions - applicable operators and corresponding successor states.</returns>
        Successors GetNextSuccessors(IState state, int maxNumSucc);

        /// <summary>
        /// Gets a random forward transition (successor) from the specified state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>A pair of applicable operator and corresponding successor state.</returns>
        Successor GetRandomSuccessor(IState state);

        /// <summary>
        /// Gets a list of all forward transitions (successors) from the specified state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>A collection of transitions - applicable operators and corresponding successor states.</returns>
        Successors GetAllSuccessors(IState state);

        /// <summary>
        /// Gets a list of possible transitions (predecessors) to the specified state. Only maxNumPred transitions are returned.
        /// Repeated calls of this method returns next predecessors. If all predecessors have been returned, then null value is
        /// returned and the next call will start from the beginning.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <param name="maxNumPred">Maximal number of returned predecessors.</param>
        /// <returns>A collection of transitions - possible predecessors and corresponding operators to be applied.</returns>
        Predecessors GetNextPredecessors(IState state, int maxNumPred);

        /// <summary>
        /// Gets a random predecessor to the specified state.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <returns>A pair of possible predecessor and corresponding operator to be applied.</returns>
        Predecessor GetRandomPredecessor(IState state);

        /// <summary>
        /// Gets a list of all predecessors to the specified state.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <returns>A collection of transitions - possible predecessors and corresponding operators to be applied.</returns>
        Predecessors GetAllPredecessors(IState state);

        /// <summary>
        /// Resets the triggers in transitions space - calling of getNextSuccessors and getNextPredecessors on any state will begin
        /// from the first available applicable grounded operator (the "history" of returned transitions is cleared).
        /// </summary>
        void ResetTransitionsTriggers();
    }
}
