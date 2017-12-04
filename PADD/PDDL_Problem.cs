using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Implementation of the PDDL model of the planning problem.
    /// </summary>
    public class PDDLProblem : IPlanningProblem
    {
        /// <summary>
        /// Name of the PDDL domain.
        /// </summary>
        private string domainName;

        /// <summary>
        /// Name of the PDDL problem.
        /// </summary>
        private string problemName;

        /// <summary>
        /// Initial state of the PDDL problem.
        /// </summary>
        private IPDDLState initialState;

        /// <summary>
        /// Goal conditions of the PDDL problem.
        /// </summary>
        private IPDDLLogicalExpression goalConditions;

        /// <summary>
        /// Set of rigid relations (invariant during planning search). Determined automatically from the PDDL initial inputs.
        /// </summary>
        private HashSet<IPDDLDesignator> rigidRelations;

        /// <summary>
        /// List of lifted operators in the PDDL planning problem.
        /// </summary>
        private List<PDDLOperatorLifted> liftedOperators;

        /// <summary>
        /// Manager for grounding the lifted operators. Generates grounded operator versions on-the-fly.
        /// </summary>
        private PDDLOperatorGroundingManager operatorGroundingManager;

        /// <summary>
        /// Manager for the ID mappings used in the PDDL planning problem. Handles identifiers of predicates, functions, types and constants.
        /// </summary>
        private PDDLIdentifierMappingsManager idManager;

        /// <summary>
        /// Constructs the PDDL planning problem.
        /// </summary>
        /// <param name="domainName">Name of the PDDL domain.</param>
        /// <param name="problemName">Name of the PDDL problem.</param>
        /// <param name="initialPredicates">Initial predicates of the problem.</param>
        /// <param name="initialFunctions">Initiial functions of the problem.</param>
        /// <param name="goalConditions">Goal conditions of the problem.</param>
        /// <param name="opNames">List of operator names.</param>
        /// <param name="opInputParams">List of operator input parameters.</param>
        /// <param name="opPreconds">List of operator preconditions.</param>
        /// <param name="opEffects">List of operator effects.</param>
        /// <param name="idManager">Mapping of IDs.</param>
        /// <param name="stateFactory">PDDL state factory. If not specified, default state implementation is used.</param>
        public PDDLProblem(string domainName, string problemName, List<IPDDLDesignator> initialPredicates, 
            Dictionary<IPDDLDesignator, int> initialFunctions, IPDDLLogicalExpression goalConditions, List<string> opNames,
            List<PDDLOperatorLifted.InputParams> opInputParams, List<PDDLOperatorLifted.Preconditions> opPreconds,
            List<PDDLOperatorLifted.Effects> opEffects, PDDLIdentifierMappingsManager idManager, IPDDLStateFactory stateFactory = null)
        {
            this.domainName = domainName;
            this.problemName = problemName;

            HashSet<IPDDLDesignator> rigidRelations, initStatePreds;
            DetermineRigidRelations(initialPredicates, opEffects, out rigidRelations, out initStatePreds);

            if (stateFactory == null)
                stateFactory = new PDDLStateFactory();

            this.initialState = stateFactory.CreateState(this, initStatePreds, initialFunctions);
            this.goalConditions = goalConditions;
            this.rigidRelations = rigidRelations;

            this.liftedOperators = new List<PDDLOperatorLifted>();
            for (int i = 0; i < opNames.Count; ++i)
                this.liftedOperators.Add(new PDDLOperatorLifted(this, opNames[i], opInputParams[i], opPreconds[i], opEffects[i], i));

            this.operatorGroundingManager = new PDDLOperatorGroundingManager(this);
            this.idManager = idManager;
        }

        /// <summary>
        /// Determines the rigid (invariant) relations of the PDDL problem from the initial predicates.
        /// </summary>
        /// <param name="initialPredicates">List of initial predicates.</param>
        /// <param name="opEffects">List of operator effects.</param>
        /// <param name="rigidRelations">Set of rigid relations.</param>
        /// <param name="initStatePreds">Set of fluent relations for the initial state.</param>
        private void DetermineRigidRelations(List<IPDDLDesignator> initialPredicates, List<PDDLOperatorLifted.Effects> opEffects,
            out HashSet<IPDDLDesignator> rigidRelations, out HashSet<IPDDLDesignator> initStatePreds)
        {
            rigidRelations = new HashSet<IPDDLDesignator>();
            initStatePreds = new HashSet<IPDDLDesignator>();

            foreach (var pred in initialPredicates)
            {
                bool fluentRelation = false;
                foreach (var effect in opEffects)
                {
                    if (effect.CanInfluencePred(pred))
                    {
                        fluentRelation = true;
                        break;
                    }
                }

                if (fluentRelation)
                    initStatePreds.Add(pred);
                else
                    rigidRelations.Add(pred);
            }
        }

        /// <summary>
        /// Creates an instance of PDDL planning problem from the PDDL input files.
        /// </summary>
        /// <param name="domainFile">PDDL domain file.</param>
        /// <param name="problemFile">PDDL problem file.</param>
        /// <param name="designatorFactory">PDDL designator factory. If not specified, default designator implementation is used.</param>
        /// <param name="stateFactory">PDDL state factory. If not specified, default state implementation is used.</param>
        /// <returns>Instance of PDDL planning problem.</returns>
        public static PDDLProblem CreateFromFile(string domainFile, string problemFile, IPDDLDesignatorFactory designatorFactory = null, IPDDLStateFactory stateFactory = null)
        {
            return PDDLProblemLoader.CreateProblemInstanceFromFiles(domainFile, problemFile, designatorFactory, stateFactory);
        }

        /// <summary>
        /// Gets the planning problem name.
        /// </summary>
        /// <returns>The planning problem name.</returns>
        public string GetProblemName()
        {
            return domainName + " - " + problemName;
        }

        /// <summary>
        /// Gets the initial state of the planning problem.
        /// </summary>
        /// <returns>The initial state.</returns>
        public IState GetInitialState()
        {
            return initialState;
        }

        /// <summary>
        /// Checks whether the specified state is meeting goal conditions of the planning problem.
        /// </summary>
        /// <param name="state">A state to be checked.</param>
        /// <returns>True if the specified state is a goal state of the problem, false otherwise.</returns>
        public bool IsGoalState(IState state)
        {
            return state.IsMeetingGoalConditions();
        }

        /// <summary>
        /// Gets a list of forward transitions (successors) from the specified state. Only maxNumSucc transitions are returned.
        /// Repeated calls of this method returns next successors. If all successors have been returned, then an empty list is
        /// returned and the next call will start from the beginning.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <param name="maxNumSucc">Maximum number of returned successors.</param>
        /// <returns>A collection of transitions - applicable operators and corresponding successor states.</returns>
        public Successors GetNextSuccessors(IState state, int maxNumSucc)
        {
            return operatorGroundingManager.GetSuccessors((IPDDLState)state, maxNumSucc);
        }

        /// <summary>
        /// Gets a random forward transition (successor) from the specified state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>A pair of applicable operator and corresponding successor state.</returns>
        public Successor GetRandomSuccessor(IState state)
        {
            return operatorGroundingManager.GetRandomSuccessor((IPDDLState)state);
        }

        /// <summary>
        /// Gets a list of all forward transitions (successors) from the specified state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>A collection of transitions - applicable operators and corresponding successor states.</returns>
        public Successors GetAllSuccessors(IState state)
        {
            return operatorGroundingManager.GetAllSuccessors((IPDDLState)state);
        }

        /// <summary>
        /// Gets a list of possible transitions (predecessors) to the specified state. Only maxNumPred transitions are returned.
        /// Repeated calls of this method returns next predecessors. If all predecessors have been returned, then an empty list is
        /// returned and the next call will start from the beginning.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <param name="maxNumPred">Maximal number of returned predecessors.</param>
        /// <returns>A collection of transitions - possible predecessors and corresponding operators to be applied.</returns>
        public Predecessors GetNextPredecessors(IState state, int maxNumPred)
        {
            return operatorGroundingManager.GetPredecessors((IPDDLState)state, maxNumPred);
        }

        /// <summary>
        /// Gets a random predecessor to the specified state.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <returns>A pair of possible predecessor and corresponding operator to be applied.</returns>
        public Predecessor GetRandomPredecessor(IState state)
        {
            return operatorGroundingManager.GetRandomPredecessor((IPDDLState)state);
        }

        /// <summary>
        /// Gets a list of all predecessors to the specified state.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <returns>A collection of transitions - possible predecessors and corresponding operators to be applied.</returns>
        public Predecessors GetAllPredecessors(IState state)
        {
            return operatorGroundingManager.GetAllPredecessors((IPDDLState)state);
        }

        /// <summary>
        /// Resets the triggers in transitions space - calling of getNextSuccessors and getNextPredecessors on any state will begin
        /// from the first available applicable grounded operator (the "history" of returned transitions is cleared).
        /// </summary>
        public void ResetTransitionsTriggers()
        {
            operatorGroundingManager.ResetAllTriggers();
        }

        /// <summary>
        /// Gets the goal conditions of the PDDL problem in form of logical expression.
        /// </summary>
        /// <returns>Goal conditions as a logical expression..</returns>
        public IPDDLLogicalExpression GetGoalConditions()
        {
            return goalConditions;
        }

        /// <summary>
        /// Gets the set of rigid (invariant) relations of the PDDL problem.
        /// </summary>
        /// <returns>Set of rigid relations.</returns>
        public HashSet<IPDDLDesignator> GetRigidRelations()
        {
            return rigidRelations;
        }

        /// <summary>
        /// Gets the list of lifted operator versions of the PDDL planning problem.
        /// </summary>
        /// <returns>List of lifted operators.</returns>
        public List<PDDLOperatorLifted> GetLiftedOperators()
        {
            return liftedOperators;
        }

        /// <summary>
        /// Gets the general ID manager for handling identifiers of predicates, functions, types and constants.
        /// </summary>
        /// <returns>General ID manager.</returns>
        public PDDLIdentifierMappingsManager GetIDManager()
        {
            return idManager;
        }
    }
}
