using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Manager for grounding of lifted versions of PDDL operators. Finds possible substitutions and corresponding transitions.
    /// </summary>
    public class PDDLOperatorGroundingManager
    {
        /// <summary>
        /// Parent PDDL planning problem.
        /// </summary>
        private PDDLProblem problem;

        /// <summary>
        /// Substitution triggers for forward transitions (substitution "history").
        /// </summary>
        private Dictionary<IPDDLState, int> substitTriggers = new Dictionary<IPDDLState, int>();

        /// <summary>
        /// Substitution triggers for backward transitions (substitution "history").
        /// </summary>
        private Dictionary<IPDDLState, Tuple<int, int>> substitTriggersBackwards = new Dictionary<IPDDLState, Tuple<int, int>>();

        /// <summary>
        /// Randomizer for generation of random transitions.
        /// </summary>
        private Random randomizer = new Random();

        /// <summary>
        /// Auxiliary grounder object for creation of concrete substitutions.
        /// </summary>
        private Grounder grounder;

        /// <summary>
        /// Constructs the PDDL grounding manager object.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        public PDDLOperatorGroundingManager(PDDLProblem problem)
        {
            this.problem = problem;
            this.grounder = new Grounder(problem);
        }

        /// <summary>
        /// Reset the substitution triggers - calling of getNextSuccessors and getNextPredecessors on any state will begin
        /// from the beginning (the substitution "history" is cleared).
        /// </summary>
        public void ResetAllTriggers()
        {
            substitTriggers.Clear();
            substitTriggersBackwards.Clear();
        }

        /// <summary>
        /// Gets a list of forward transitions (successors) from the specified state. Only maxNumSucc transitions are returned.
        /// Repeated calls of this method returns next successors. If all successors have been returned, then an empty list is
        /// returned and the next call will start from the beginning.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <param name="maxNumSucc">Maximum number of returned successors.</param>
        /// <returns>A collection of transitions - applicable operators and corresponding successor states.</returns>
        public Successors GetSuccessors(IPDDLState state, int maxNumSucc)
        {
            IList<Successor> succList = new List<Successor>();

            var transitions = GetTransitions(state, maxNumSucc, false);
            foreach (var transition in transitions)
            {
                PDDLOperator op = transition.Item1;
                //PDDLState newState = transition.Item2;
                succList.Add(new Successor(state, op));
            }

            return new Successors(succList);
        }

        /// <summary>
        /// Gets a random forward transition (successor) from the specified state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>A pair of applicable operator and corresponding successor state.</returns>
        public Successor GetRandomSuccessor(IPDDLState state)
        {
            var transition = GetRandomTransition(state, false);
            return new Successor(state, transition.Item1);
        }

        /// <summary>
        /// Gets a list of all forward transitions (successors) from the specified state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>A collection of transitions - applicable operators and corresponding successor states.</returns>
        public Successors GetAllSuccessors(IPDDLState state)
        {
            substitTriggers.Remove(state);
            var retList = GetSuccessors(state, Int32.MaxValue);
            substitTriggers.Remove(state);
            return retList;
        }

        /// <summary>
        /// Gets a list of possible transitions (predecessors) to the specified state. Only maxNumPred transitions are returned.
        /// Repeated calls of this method returns next predecessors. If all predecessors have been returned, then an empty list is
        /// returned and the next call will start from the beginning.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <param name="maxNumPred">Maximal number of returned predecessors.</param>
        /// <returns>A collection of transitions - possible predecessors and corresponding operators to be applied.</returns>
        public Predecessors GetPredecessors(IPDDLState state, int maxNumPred)
        {
            IList<Predecessor> predList = new List<Predecessor>();

            var transitions = GetTransitions(state, maxNumPred, true);
            foreach (var transition in transitions)
            {
                PDDLOperator op = transition.Item1;
                IPDDLState newState = transition.Item2;
                predList.Add(new Predecessor(newState, op));
            }

            return new Predecessors(predList);
        }

        /// <summary>
        /// Gets a random predecessor to the specified state.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <returns>A pair of possible predecessor and corresponding operator to be applied.</returns>
        public Predecessor GetRandomPredecessor(IPDDLState state)
        {
            var transition = GetRandomTransition(state, true);
            return new Predecessor(transition.Item2, transition.Item1);
        }

        /// <summary>
        /// Gets a list of all predecessors to the specified state.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <returns>A collection of transitions - possible predecessors and corresponding operators to be applied.</returns>
        public Predecessors GetAllPredecessors(IPDDLState state)
        {
            substitTriggersBackwards.Remove(state);
            var retList = GetPredecessors(state, Int32.MaxValue);
            substitTriggersBackwards.Remove(state);
            return retList;
        }

        /// <summary>
        /// Gets a list of transitions from/to the specified state. Only maxNumInstances transitions are returned.
        /// Repeated calls of this method returns next transitions. If all transitions have been returned, then an empty list is
        /// returned and the next call will start from the beginning.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <param name="maxNumInstances">Maximal number of returned transitions.</param>
        /// <param name="isPredecessors">Forward or backward transition.</param>
        /// <returns>A list of transitions - forward/backward applicable operators and corresponding successor/predecessor states.</returns>
        private List<Tuple<PDDLOperator, IPDDLState>> GetTransitions(IPDDLState state, int maxNumInstances, bool isPredecessors = false)
        {
            bool stateTriggered;
            int substitTrigger = 0;
            int instanceTrigger = 0;

            if (!isPredecessors)
            {
                stateTriggered = substitTriggers.TryGetValue(state, out substitTrigger);
            }
            else
            {
                Tuple<int, int> substitAndInstanceTrigger;
                stateTriggered = substitTriggersBackwards.TryGetValue(state, out substitAndInstanceTrigger);
                if (stateTriggered)
                {
                    substitTrigger = substitAndInstanceTrigger.Item1;
                    instanceTrigger = substitAndInstanceTrigger.Item2;
                }
            }

            if (!stateTriggered)
            {
                substitTrigger = 0;
                instanceTrigger = 0;
            }

            grounder.SetParameters(state, substitTrigger, instanceTrigger, maxNumInstances, isPredecessors);
            List<Tuple<PDDLOperator, IPDDLState>> transitions = grounder.GetTransitions();

            int newSubstitTrigger = grounder.GetOutputSubstitTrigger();
            int newInstanceTrigger = grounder.GetOutputInstanceTrigger();

            // the last substitution has some pending instances - decrement the substit. trigger
            if (newInstanceTrigger > 0)
                --newSubstitTrigger;

            // if there are no applicable substitutions anymore, return an empty list
            // and reset the state trigger (so, the next call of this method will try to ground from the beginning)
            if (transitions.Count == 0)
            {
                newSubstitTrigger = 0;
                newInstanceTrigger = 0;
            }

            if (!isPredecessors)
            {
                if (newSubstitTrigger == 0 && stateTriggered)
                    substitTriggers.Remove(state);
                else if (stateTriggered)
                    substitTriggers[state] = newSubstitTrigger;
                else
                    substitTriggers.Add(state, newSubstitTrigger);
            }
            else
            {
                if (newSubstitTrigger == 0 && stateTriggered)
                    substitTriggersBackwards.Remove(state);
                else if (stateTriggered)
                    substitTriggersBackwards[state] = Tuple.Create(newSubstitTrigger, newInstanceTrigger);
                else
                    substitTriggersBackwards.Add(state, Tuple.Create(newSubstitTrigger, newInstanceTrigger));
            }

            return transitions;
        }

        /// <summary>
        /// Gets a random transition from/to the specified state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <param name="isPredecessor">Forward or backward transition.</param>
        /// <returns>A pair of forward/backward applicable operator and corresponding successor/predecessor state.</returns>
        private Tuple<IOperator, IState> GetRandomTransition(IPDDLState state, bool isPredecessor = false)
        {
            //int randomTrigger = 0;

            // firstly, test whether some successor/predecessor even exist
            grounder.SetParameters(state, 0, 0, 1, isPredecessor);
            List<Tuple<PDDLOperator, IPDDLState>> transitions = grounder.GetTransitions();
            if (transitions.Count == 0)
                return null;

            // store the first possible successor/predecessor operator
            Tuple<IOperator, IState> firstFoundTransition = Tuple.Create((IOperator)transitions[0].Item1, (IState)transitions[0].Item2);

            // now, try to get random successor/predecessor operator
            // (if there couldn't be found anything for a long time, return the first found)
            int cycleNum = 0;
            int cycleGuard = 130;

            transitions.Clear();
            while (transitions.Count == 0)
            {
                if (cycleNum++ > cycleGuard)
                    break;

                int randomSubstitTrigger = randomizer.Next(GetNumAllPotentialSubstitutions());
                bool randomizeOrderForward = (randomizer.Next(2) == 1);
                bool randomizeInstanceTrigger = isPredecessor;

                grounder.SetParameters(state, randomSubstitTrigger, 0, 1, isPredecessor, randomizeOrderForward, randomizeInstanceTrigger);
                transitions = grounder.GetTransitions();
                if (transitions.Count != 0)
                    return Tuple.Create((IOperator)transitions[0].Item1, (IState)transitions[0].Item2);
            }

            return firstFoundTransition;
        }

        /// <summary>
        /// Count of all potential substitutions for lifted PDDL operators. Lazy evaluated.
        /// </summary>
        private int numAllPotentialSubstitutions = -1;

        /// <summary>
        /// Gets the number of all potential substitutions for lifted PDDL operators.
        /// </summary>
        /// <returns>Number of all possible substitutions.</returns>
        private int GetNumAllPotentialSubstitutions()
        {
            if (numAllPotentialSubstitutions == -1)
            {
                numAllPotentialSubstitutions = 0;
                foreach (var op in problem.GetLiftedOperators())
                    numAllPotentialSubstitutions += op.GetNumberOfAllPossibleSubstitutions();
            }

            return numAllPotentialSubstitutions;
        }

        /// <summary>
        /// Auxiliary grounded object for finding concrete substitutions of PDDL operators.
        /// </summary>
        private class Grounder
        {
            /// <summary>
            /// Current substitution - stores the data to be returned.
            /// </summary>
            private int[] currSubstit;

            /// <summary>
            /// Reference to the parent PDDL operator.
            /// </summary>
            private PDDLOperatorLifted currOp;

            /// <summary>
            /// Current substitution trigger.
            /// </summary>
            private int currSubsTrigger;

            /// <summary>
            /// Current instance trigger.
            /// </summary>
            private int currInstanceTrigger;

            /// <summary>
            /// Reference to the parent planning problem.
            /// </summary>
            private PDDLProblem problem;

            /// <summary>
            /// Reference state to be grounded from.
            /// </summary>
            private IPDDLState state;

            /// <summary>
            /// Input substitution trigger.
            /// </summary>
            private int inputSubsTrigger;

            /// <summary>
            /// Input instance trigger.
            /// </summary>
            private int inputInstanceTrigger;

            /// <summary>
            /// Forward/backward applicability (direction of transition).
            /// </summary>
            private bool backwardsApplicability;

            /// <summary>
            /// List of transitions to be returned.
            /// </summary>
            private List<Tuple<PDDLOperator, IPDDLState>> retList;

            /// <summary>
            /// Number of instances to be generated.
            /// </summary>
            private int numInstancesToGener;

            /// <summary>
            /// Grounding direction in the substitution space.
            /// </summary>
            private bool groundingOrderForward;

            /// <summary>
            /// Should the instance trigger be randomized?
            /// </summary>
            private bool randomizeInstanceTrigger;

            /// <summary>
            /// Randomizer for the instance trigger.
            /// </summary>
            private static Random instanceRandomizer = new Random();

            /// <summary>
            /// Constructs the operator grounder.
            /// </summary>
            /// <param name="problem">Parent planning problem.</param>
            public Grounder(PDDLProblem problem)
            {
                this.problem = problem;
            }

            /// <summary>
            /// Sets the current grounder parameters.
            /// </summary>
            /// <param name="state">Reference state.</param>
            /// <param name="inputSubsTrigger">Input substitution trigger.</param>
            /// <param name="inputInstanceTrigger">Input instance trigger (random transitions).</param>
            /// <param name="maxNumInstances">Number of requested substitution instances.</param>
            /// <param name="backwardsApplicability">Is backward transition requested?</param>
            /// <param name="groundingOrderForward">Grounding direction (forward/backward).</param>
            /// <param name="randomizeInstanceTrigger">Randomize instance trigger?</param>
            public void SetParameters(IPDDLState state, int inputSubsTrigger, int inputInstanceTrigger, int maxNumInstances, bool backwardsApplicability = false, bool groundingOrderForward = true, bool randomizeInstanceTrigger = false)
            {
                this.state = state;
                this.inputSubsTrigger = inputSubsTrigger;
                this.inputInstanceTrigger = inputInstanceTrigger;
                this.backwardsApplicability = backwardsApplicability;

                this.retList = new List<Tuple<PDDLOperator, IPDDLState>>();
                this.numInstancesToGener = maxNumInstances;
                this.groundingOrderForward = groundingOrderForward;
                this.randomizeInstanceTrigger = randomizeInstanceTrigger;
            }

            /// <summary>
            /// Gets a list of transitions from/to the previously specified state and other parameters.
            /// </summary>
            /// <returns>A list of transitions - applicable operators and corresponding states.</returns>
            public List<Tuple<PDDLOperator, IPDDLState>> GetTransitions()
            {
                currSubsTrigger = 0;
                currInstanceTrigger = 0;

                foreach (var op in problem.GetLiftedOperators())
                {
                    if (numInstancesToGener <= 0)
                        break;

                    currSubstit = new int[op.GetInputParams().GetNumberOfParams()];
                    currOp = op;

                    DoGroundInputParam(0);
                }
                return retList;
            }

            /// <summary>
            /// Gets the output substitution trigger.
            /// </summary>
            /// <returns>Output substitution trigger.</returns>
            public int GetOutputSubstitTrigger()
            {
                return currSubsTrigger;
            }

            /// <summary>
            /// Gets the output instance trigger.
            /// </summary>
            /// <returns>Output instance trigger.</returns>
            public int GetOutputInstanceTrigger()
            {
                return currInstanceTrigger;
            }

            /// <summary>
            /// Grounds the specified parameter of the operator.
            /// </summary>
            /// <param name="index">Index of operator parameter.</param>
            private void DoGroundInputParam(int index)
            {
                if (numInstancesToGener <= 0)
                    return;

                if (index >= currSubstit.Length) // last possible index, we have a substitution 
                {
                    // we want to skip some substitution (requested by inputSubsTrigger)
                    if (inputSubsTrigger > currSubsTrigger++)
                        return;

                    // test substitution on applicability (and possibly apply and store)
                    PDDLOperatorSubstitution currSubstitInst = new PDDLOperatorSubstitution(currSubstit);
                    if (backwardsApplicability && currOp.CanBePredecessor(state, currSubstitInst))
                    {
                        PDDLOperator op = new PDDLOperator(currOp, currSubstitInst, state);
                        var predStates = currOp.ApplyBackwards(state, currSubstitInst);

                        if (randomizeInstanceTrigger)
                            inputInstanceTrigger = instanceRandomizer.Next(predStates.Count);
                        currInstanceTrigger = 0;

                        foreach (var predState in predStates)
                        {
                            if (numInstancesToGener <= 0)
                                return;

                            // we want to skip some instances (requested by inputInstanceTrigger)
                            if (inputInstanceTrigger > currInstanceTrigger++)
                                continue;

                            retList.Add(Tuple.Create(op, (IPDDLState)predState));
                            --numInstancesToGener;
                        }

                        currInstanceTrigger = 0;
                    }
                    else if (!backwardsApplicability && currOp.IsApplicable(state, currSubstitInst))
                    {
                        PDDLOperator op = new PDDLOperator(currOp, currSubstitInst, state);
                        IPDDLState succState = currOp.Apply(state, currSubstitInst);
                        retList.Add(Tuple.Create(op, succState));
                        --numInstancesToGener;
                    }

                    return; // interrupt recursion
                }

                int typeID = currOp.GetInputParams().GetVarTypeID(index);

                // try all constants (their IDs) by the current parameter type

                List<int> consts = problem.GetIDManager().GetConstantsIDForType(typeID);
                if (!groundingOrderForward)
                    consts.Reverse();

                foreach (var con in consts)
                {
                    if (numInstancesToGener <= 0)
                        break;

                    currSubstit[index] = con;
                    DoGroundInputParam(index + 1);
                }
            }
        }
    }
}
