using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Operator manager in the SAS+ planning problem. Handles the access to the SAS+ operators.
    /// </summary>
    public class SASOperatorsManager
    {
        /// <summary>
        /// Reference to the parent SAS+ planning problem.
        /// </summary>
        private SASProblem parentProblem;

        /// <summary>
        /// List of (grounded) operators in the SAS+ planning problem.
        /// </summary>
        private List<SASOperator> operatorsList;

		/// <summary>
		/// Just a placeholder to avoid creating new object every time
		/// </summary>
		private List<Predecessor> predecessorPlaceHolder;

        /// <summary>
        /// Flag whether the metric (i.e. operator costs) is used in the SAS+ planning problem.
        /// </summary>
        private bool metricUsed;

        /// <summary>
        /// Instance of operator decision tree. Shouldn't be accessed directly, but via getOperatorDecisionTreeInstance().
        /// </summary>
        private SASOperatorDecisionTree operatorDecisionTreeInst;

        /// <summary>
        /// Transition triggers for forward transitions ("history" of returned transitions).
        /// </summary>
        private Dictionary<SASState, int> substitTriggers;

        /// <summary>
        /// Constructs SAS+ operators manager.
        /// </summary>
        /// <param name="problem">Parent SAS+ planning problem.</param>
        public SASOperatorsManager(SASProblem problem, List<SASInputData.Operator> operatorsData, bool isMetricUsed)
        {
            parentProblem = problem;

            operatorsList = new List<SASOperator>();
            for (int opIndex = 0; opIndex < operatorsData.Count; ++opIndex)
                operatorsList.Add(new SASOperator(problem, operatorsData[opIndex], opIndex));

            metricUsed = isMetricUsed;
            operatorDecisionTreeInst = null;
            substitTriggers = new Dictionary<SASState, int>();
			predecessorPlaceHolder = new List<Predecessor>();
        }

        /// <summary>
        /// Gets the list of all operators of the SAS+ planning problem.
        /// </summary>
        /// <returns>List of operators.</returns>
        public List<SASOperator> GetOperatorsList()
        {
            return operatorsList;
        }

        /// <summary>
        /// Flag whether the metric (i.e. operator costs) is used in the SAS+ planning problem.
        /// </summary>
        /// <returns>True if metric is used, false otherwise.</returns>
        public bool IsMetricUsed()
        {
            return metricUsed;
        }

        /// <summary>
        /// Returns the instance of operator decision tree. Lazy evaluated.
        /// </summary>
        /// <returns>Instance of operator decision tree.</returns>
        private SASOperatorDecisionTree GetOperatorDecisionTreeInstance()
        {
            if (operatorDecisionTreeInst == null)
                operatorDecisionTreeInst = new SASOperatorDecisionTree(parentProblem);
            return operatorDecisionTreeInst;
        }

        /// <summary>
        /// Gets a list of forward transitions (successors) from the specified state. Only maxNumSucc transitions are returned.
        /// Repeated calls of this method returns next successors. If all successors have been returned, then an empty list is
        /// returned and the next call will start from the beginning.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <param name="maxNumSucc">Maximum number of returned successors.</param>
        /// <returns>A collection of transitions - applicable operators and corresponding successor states.</returns>
        public Successors GetNextSuccessors(SASState state, int maxNumSucc)
        {
            int triggerIdx, outputTriggerIdx;
            bool stateTriggered = substitTriggers.TryGetValue(state, out triggerIdx);
            outputTriggerIdx = triggerIdx;

            if (!stateTriggered)
                triggerIdx = 0;

            var successors = GetOperatorDecisionTreeInstance().GetNextSuccessors(state, maxNumSucc, triggerIdx);

            outputTriggerIdx += successors.Count;
            if (successors.Count < maxNumSucc)
                outputTriggerIdx = 0;

            if (outputTriggerIdx == 0 && stateTriggered)
                substitTriggers.Remove(state);
            else if (stateTriggered)
                substitTriggers[state] = outputTriggerIdx;
            else
                substitTriggers.Add(state, outputTriggerIdx);

            return successors;
        }

        /// <summary>
        /// Gets a random forward transition (successor) from the specified state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>A pair of applicable operator and corresponding successor state.</returns>
        public Successor GetRandomSuccessor(SASState state)
        {
            return GetOperatorDecisionTreeInstance().GetRandomSuccessor(state);
        }

        /// <summary>
        /// Gets a list of all forward transitions (successors) from the specified state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>A collection of transitions - applicable operators and corresponding successor states.</returns>
        public Successors GetAllSuccessors(SASState state)
        {
            return GetOperatorDecisionTreeInstance().GetNextSuccessors(state, int.MaxValue, 0);
        }

        /// <summary>
        /// Gets a list of possible transitions (predecessors) to the specified state. Only maxNumPred transitions are returned.
        /// Repeated calls of this method returns next predecessors. If all predecessors have been returned, then an empty list is
        /// returned and the next call will start from the beginning.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <param name="maxNumPred">Maximal number of returned predecessors.</param>
        /// <returns>A collection of transitions - possible predecessors and corresponding operators to be applied.</returns>
        public Predecessors GetNextPredecessors(SASState state, int maxNumPred)
        {
            return null;
        }

        /// <summary>
        /// Gets a random predecessor to the specified state.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <returns>A pair of possible predecessor and corresponding operator to be applied.</returns>
        public Predecessor GetRandomPredecessor(SASState state)
        {
            return null;
        }

        /// <summary>
        /// Gets a list of all predecessors to the specified state.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <returns>A collection of transitions - possible predecessors and corresponding operators to be applied.</returns>
        public Predecessors GetAllPredecessors(SASState state)
        {
			List<Predecessor> result = new List<Predecessor>();
			foreach (var op in this.operatorsList)
			{
				List<IState> predecessors = op.ApplyBackwards(state);
				foreach (var pred in predecessors)
				{
					result.Add(new Predecessor(pred, op));						
				}
			}
			Predecessors p = new Predecessors(result);
            return p;
        }

        /// <summary>
        /// Gets a list of all applicable relevant operators to the given state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>List of all applicable relevant operators to the given state.</returns>
        public Successors GetApplicableRelevantTransitions(SASState state)
        {
            return GetOperatorDecisionTreeInstance().GetApplicableRelevantTransitions(state);
        }
        
        /// <summary>
        /// Gets a single applicable relevant operator to the given state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>Applicable relevant operator to the given state.</returns>
        public Successor GetApplicableRelevantTransition(SASState state)
        {
            return GetOperatorDecisionTreeInstance().GetRandomApplicableRelevantTransition(state);
        }

        /// <summary>
        /// Resets the triggers in transitions space - calling of getNextSuccessors and getNextPredecessors on any state will begin
        /// from the first available applicable grounded operator (the "history" of returned transitions is cleared).
        /// </summary>
        public void ResetTransitionsTriggers()
        {
            substitTriggers.Clear();
        }

		/// <summary>
		/// Returns all relative predecessors of given state. Predecessors are computed by fixing operator's preconditions. Other values might be arbitrary. RelativeStates are used here to denote a set of states.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public Predecessors GetRelativePredecessors(IState state)
		{
			predecessorPlaceHolder.Clear();
			foreach (var op in this.operatorsList)
			{
				var predecessor = op.ApplyBackwardsRelative(state);
				if (predecessor == null)
					continue;
				predecessorPlaceHolder.Add(new Predecessor(predecessor, op));
			}
			return new Predecessors(predecessorPlaceHolder);
		}
	}

    /// <summary>
    /// Implementation of SAS+ operator decision tree.
    /// </summary>
    public class SASOperatorDecisionTree
    {
        /// <summary>
        /// Root node of the SAS+ operator decision tree.
        /// </summary>
        private ISASOperatorTreeNode root;

        /// <summary>
        /// Reference to the collection of mutex groups defined in the parent SAS+ planning problem.
        /// </summary>
        private SASMutexGroups mutexGroups;

        /// <summary>
        /// Visitor instance for collecting applicable operators out of SAS+ operator decision tree.
        /// Shouldn't be accessed directly, but via getApplicableOpCollectorInstance() instead.
        /// </summary>
        SASOperatorApplicableSelector applicableOpCollector;

        /// <summary>
        /// Visitor instance for finding a random applicable operator out of SAS+ operator decision tree.
        /// Shouldn't be accessed directly, but via getApplicableRandomOpCollectorInstance() instead.
        /// </summary>
        SASOperatorRandomApplicableSelector applicableRandomOpCollector;

        /// <summary>
        /// Visitor instance for collecting relevant operators out of SAS+ operator decision tree.
        /// Shouldn't be accessed directly, but via getRelevantOpCollectorInstance() instead.
        /// </summary>
        SASOperatorRelevantSelector relevantOpCollector;

        /// <summary>
        /// Visitor instance for finding a random relevant applicable operator out of SAS+ operator decision tree.
        /// Shouldn't be accessed directly, but via getRelevantRandomOpCollectorInstance() instead.
        /// </summary>
        SASOperatorRandomRelevantSelector relevantRandomOpCollector;

        /// <summary>
        /// Gets an instance of visitor for collecting applicable operators out of SAS+ operator decision tree.
        /// </summary>
        /// <returns>Instance of a collector of applicable operators.</returns>
        private SASOperatorApplicableSelector GetApplicableOpCollectorInstance()
        {
            if (applicableOpCollector == null)
                applicableOpCollector = new SASOperatorApplicableSelector(mutexGroups);
            return applicableOpCollector;
        }

        /// <summary>
        /// Gets an instance of visitor for finding a random applicable operator out of SAS+ operator decision tree.
        /// </summary>
        /// <returns>Instance of collector finding applicable operator.</returns>
        private SASOperatorRandomApplicableSelector GetApplicableRandomOpCollectorInstance()
        {
            if (applicableRandomOpCollector == null)
                applicableRandomOpCollector = new SASOperatorRandomApplicableSelector(mutexGroups);
            return applicableRandomOpCollector;
        }

        /// <summary>
        /// Gets an instance of visitor for collecting relevant operators out of SAS+ operator decision tree.
        /// </summary>
        /// <returns>Instance of a collector of relevant operators.</returns>
        private SASOperatorRelevantSelector GetRelevantOpCollectorInstance()
        {
            if (relevantOpCollector == null)
                relevantOpCollector = new SASOperatorRelevantSelector(mutexGroups);
            return relevantOpCollector;
        }

        /// <summary>
        /// Gets an instance of visitor for finding a random relevant applicable operator out of SAS+ operator decision tree.
        /// </summary>
        /// <returns>Instance of collector finding relevant applicable operator.</returns>
        private SASOperatorRandomRelevantSelector GetRelevantRandomOpCollectorInstance()
        {
            if (relevantRandomOpCollector == null)
                relevantRandomOpCollector = new SASOperatorRandomRelevantSelector(mutexGroups);
            return relevantRandomOpCollector;
        }

        /// <summary>
        /// Constructs the SAS+ operator decision tree.
        /// </summary>
        /// <param name="problem">Parent SAS+ planning problem.</param>
        public SASOperatorDecisionTree(SASProblem problem)
        {
            BuildTree(problem);
        }

        /// <summary>
        /// Builds the operator decision tree from the parent planning problem.
        /// </summary>
        /// <param name="problem">Parent SAS+ planning problem.</param>
        public void BuildTree(SASProblem problem)
        {
            mutexGroups = problem.GetMutexGroups();

            List<int> remainingVars = new List<int>();
            for (int i = 0; i < problem.GetVariablesCount(); i++)
                remainingVars.Add(i);
            root = BuildTree(problem, problem.GetOperators(), remainingVars);
        }

        /// <summary>
        /// Builds the operator decision tree from the given operators and variables.
        /// </summary>
        /// <param name="problem">Parent SAS+ planning problem.</param>
        /// <param name="availableOperators">List of operators to be processed.</param>
        /// <param name="decisionVariables">List of variables that we have currently available.</param>
        /// <returns>Node of the operator decision tree.</returns>
        private ISASOperatorTreeNode BuildTree(SASProblem problem, List<SASOperator> availableOperators, List<int> decisionVariables)
        {
            // if we have no remaining operators, then create an empty leaf node
            if (availableOperators.Count == 0)
                return new SASOperatorTreeLeafNode();

            // if we have no remaining decision variables, then create a leaf node containing the current operators list
            if (decisionVariables.Count == 0)
                return new SASOperatorTreeLeafNode(availableOperators);

            // find the current decision variable - i.e. available variable with the maximal domain range
            int maximalDomainRainge = decisionVariables.Max(i => problem.GetVariableDomainRange(i));
            int decisionVariable = decisionVariables.First(i => problem.GetVariableDomainRange(i) == maximalDomainRainge);

            // prepare collections for sorting out the available operators
            List<SASOperator>[] opsByPreconditions = new List<SASOperator>[problem.GetVariableDomainRange(decisionVariable)];
            List<SASOperator> remainingOperators = new List<SASOperator>();
            for (int i = 0; i < opsByPreconditions.Length; i++)
                opsByPreconditions[i] = new List<SASOperator>();

            // sort out the available operators by the current decision variable; if the preconditions of the operator are affected
            // by the decision variable, put it into the opsByPreconditions collection, or into the remainingOperators otherwise
            foreach (var item in availableOperators)
            {
                var preconds = item.GetPreconditions();
                bool precondVarFound = false;
                for (int i = 0; i < preconds.Count; ++i)
                {
                    if (preconds[i].variable == decisionVariable)
                    {
                        int precondValue = preconds[i].value;
                        if (precondValue != -1)
                            opsByPreconditions[precondValue].Add(item);
                        else
                            remainingOperators.Add(item);

                        precondVarFound = true;
                        break;
                    }
                }

                if (!precondVarFound)
                    remainingOperators.Add(item);
            }

            // the decision variable has zero impact - continue without it
            if (remainingOperators.Count == availableOperators.Count)
            {
                decisionVariables.Remove(decisionVariable);
                var res = BuildTree(problem, availableOperators, decisionVariables);
                decisionVariables.Add(decisionVariable);
                return res;
            }

            // prepare a new decision node - create decision subtrees for all the possible values of the current decision variable
            decisionVariables.Remove(decisionVariable);
            ISASOperatorTreeNode[] successorsByValues = new ISASOperatorTreeNode[problem.GetVariableDomainRange(decisionVariable)];
            for (int i = 0; i < problem.GetVariableDomainRange(decisionVariable); i++)
                successorsByValues[i] = BuildTree(problem, opsByPreconditions[i], decisionVariables);

            // now, create a decision subtree where the value of the current decision variable doesn't matter
            ISASOperatorTreeNode dontCareSuccessor = BuildTree(problem, remainingOperators, decisionVariables);
            decisionVariables.Add(decisionVariable);

            // return the new subtree
            return new SASOperatorTreeDecisionNode(decisionVariable, successorsByValues, dontCareSuccessor);
        }

        /// <summary>
        /// Gets a collection of successors for the given SAS+ state. Only maxNumSucc transitions are returned.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <param name="maxNumSucc">Maximal number of returned successors.</param>
        /// <param name="triggerIdx">Input trigger (how many operators supposed to be skipped).</param>
        /// <returns>Collection of successors for the given state.</returns>
        public Successors GetNextSuccessors(SASState state, int maxNumSucc, int triggerIdx)
        {
            return GetApplicableOpCollectorInstance().GetApplicableOperators(state, root, triggerIdx, maxNumSucc);
        }

        /// <summary>
        /// Gets a random successor for the given SAS+ state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Successor for the given state.</returns>
        public Successor GetRandomSuccessor(SASState state)
        {
            return GetApplicableRandomOpCollectorInstance().GetRandomApplicableOperator(state, root);
        }

        /// <summary>
        /// Gets a collection of relevant operators to the given SAS+ state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Collection of relevant operators for the given state.</returns>
        public Successors GetApplicableRelevantTransitions(SASState state)
        {
            return GetRelevantOpCollectorInstance().GetApplicableRelevantTransitions(state, root);
        }

        /// <summary>
        /// Gets a random relevant successor for the given SAS+ state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Relevant successor for the given state.</returns>
        public Successor GetRandomApplicableRelevantTransition(SASState state)
        {
            return GetRelevantRandomOpCollectorInstance().GetRandomApplicableRelevantTransition(state, root);
        }
    }

}
