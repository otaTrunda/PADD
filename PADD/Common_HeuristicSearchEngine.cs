using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Heuristic search status flags.
    /// </summary>
    public enum SearchStatus
    {
        InProgress,
        NotStarted,
        SolutionFound,
        TimeLimitExceeded,
        MemoryLimitExceeded,
        NoSolutionExist,
        Canceled
    }

    /// <summary>
    /// Base class for the heuristic search engines.
    /// </summary>
    public abstract class HeuristicSearchEngine
    {
        /// <summary>
        /// Reference to the planning problem.
        /// </summary>
        protected IPlanningProblem problem;

        /// <summary>
        /// Used heuristics.
        /// </summary>
        protected Heuristic heuristic;

        /// <summary>
        /// Flag indicating the status of the search.
        /// </summary>
        public SearchStatus searchStatus;

        /// <summary>
        /// Solution to the planning problem. After procedure search() ends, this object contains a solution (sequence of operators).
        /// </summary>
        protected SolutionPlan solution;

        /// <summary>
        /// Gets the search status.
        /// </summary>
        /// <returns>Search status.</returns>
        public SearchStatus GetSearchStatus()
        {
            return searchStatus;
        }

        /// <summary>
        /// Gets the solution to the planning problem.
        /// </summary>
        /// <returns>Solution to the planning problem.</returns>
        public SolutionPlan GetSolution()
        {
            return solution;
        }

        /// <summary>
        /// Sets the planning problem to be solved.
        /// </summary>
        /// <param name="problem">Input planning problem.</param>
        public void SetProblem(IPlanningProblem problem)
        {
            this.problem = problem;
        }

        /// <summary>
        /// Sets the heuristic to be used.
        /// </summary>
        /// <param name="heuristic">Heuristic to be used.</param>
        public void SetHeuristic(Heuristic heuristic)
        {
            this.heuristic = heuristic;
        }

        /// <summary>
        /// Prints a message to the console and to the log, if the quiet flag is off.
        /// </summary>
        /// <param name="message">Message to be printed.</param>
        /// <param name="quiet">Are we in quiet mode?</param>
        protected void PrintMessage(string message, bool quiet = false)
        {
			if (!quiet)
			{
				//Console.WriteLine(message);	//not necessary anymore since the logger writes to console on its own
				Program.logger.Log(message);
			}
        }

        public virtual string getDescription()
        {
            return "Heuristic search engine";
        }

        /// <summary>
        /// Launches the search engine.
        /// </summary>
        /// <param name="quiet">Are we in a quiet mode (no console messages)?</param>
        /// <returns>The cost of plan if one was found or -1 otherwise.</returns>
        public abstract int Search(bool quiet = false);
    }
}
