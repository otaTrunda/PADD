using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Common interface for SASProblem factories. When a user wants to use an extended problem implementation, he just creates
    /// two new classes - first one extending SASProblem and the second one implementing SASProblemFactory. The factory is then
    /// used as a parameter of SASProblemLoader.createFromFile(string, SASProblemFactory) method.
    /// </summary>
    public interface ISASProblemFactory
    {
        /// <summary>
        /// Creates an instance of SAS+ planning problem.
        /// </summary>
        /// <param name="inputData">Loaded input data for the SAS+ planning problem.</param>
        /// <returns>Instance of SAS+ planning problem.</returns>
        SASProblem CreateProblem(SASInputData inputData);
    }

    /// <summary>
    /// SASProblem factory for the default SASProblem implementation.
    /// </summary>
    public class SASProblemFactory : ISASProblemFactory
    {
        /// <summary>
        /// Creates an instance of SAS+ planning problem.
        /// </summary>
        /// <param name="inputData">Loaded input data for the SAS+ planning problem.</param>
        /// <returns>Instance of SAS+ planning problem.</returns>
        public SASProblem CreateProblem(SASInputData inputData)
        {
            return new SASProblem(inputData);
        }
    }

    /// <summary>
    /// SASProblem factory for the SASProblemRedBlack implementation.
    /// </summary>
    public class SASProblemFactoryRedBlack : ISASProblemFactory
    {
        /// <summary>
        /// Custom container implementation for the black variable set.
        /// </summary>
        private ICollection<int> customBlackVarsSetInst;

        /// <summary>
        /// Constructs the factory for red-black variant of SASProblem.
        /// </summary>
        /// <param name="customBlackVarsSetImpl">Custom black variable set implementation.</param>
        public SASProblemFactoryRedBlack(ICollection<int> customBlackVarsSetImpl = null)
        {
            if (customBlackVarsSetImpl == null)
                customBlackVarsSetImpl = new HashSet<int>(); // default implementation

            customBlackVarsSetInst = customBlackVarsSetImpl;
        }

        /// <summary>
        /// Creates an instance of SAS+ planning problem.
        /// </summary>
        /// <param name="inputData">Loaded input data for the SAS+ planning problem.</param>
        /// <returns>Instance of SAS+ planning problem.</returns>
        public SASProblem CreateProblem(SASInputData inputData)
        {
            return new SASProblemRedBlack(inputData, customBlackVarsSetInst);
        }
    }

    /// <summary>
    /// Implementation of the SAS+ model of the planning problem.
    /// </summary>
    public class SASProblem : IPlanningProblem
    {
		/// <summary>
		/// Is used as a return object of some methods so that the mothods don't need to create new list object every time.
		/// </summary>
		private List<SASOperator> operatorsPlaceholder = new List<SASOperator>();

		/// <summary>
		/// Name of the SAS+ planning problem.
		/// </summary>
		protected string problemName;

        /// <summary>
        /// Initial state of the SAS+ planning problem.
        /// </summary>
        protected SASState initialState;

        /// <summary>
        /// Goal conditions of the SAS+ planning problem.
        /// </summary>
        protected SASGoalConditions goalConditions;

        /// <summary>
        /// Operators manager of the SAS+ planning problem.
        /// </summary>
        protected SASOperatorsManager operatorsManager;

        /// <summary>
        /// Info data about variables used in the SAS+ planning problem.
        /// </summary>
        protected SASVariables variablesData;

        /// <summary>
        /// Mutually exclusive constraints in the SAS+ planning problem.
        /// </summary>
        protected SASMutexGroups mutexGroups;

        /// <summary>
        /// Axiom rules in the SAS+ planning problem. Stored in ascending order by axiom layer index.
        /// </summary>
        protected SASAxiomRules axiomRules;

        /// <summary>
        /// Path to the corresponding SAS+ input file.
        /// </summary>
        protected string inputFilePath;

		/// <summary>
		/// Indicates wheater rigidity of variables has already been computed and stored or not.
		/// </summary>
		protected bool[] isRigidityDetermined;

		protected bool[] _isRigid;

		/// <summary>
		/// Constructs the SAS+ planning problem. Expecting data containers from SASProblemLoader.
		/// </summary>
		/// <param name="inputData">Loaded input data for the SAS+ planning problem.</param>
		public SASProblem(SASInputData inputData)
        {
            problemName = inputData.ProblemName;
            initialState = new SASState(this, inputData.InitState.StateValues.ToArray());
            goalConditions = new SASGoalConditions(inputData.GoalConds);
            operatorsManager = new SASOperatorsManager(this, inputData.Operators, inputData.IsMetricUsed);
            variablesData = new SASVariables(inputData.Variables);
            mutexGroups = new SASMutexGroups(inputData.MutexGroups);
            axiomRules = new SASAxiomRules(inputData.Axioms, inputData.Variables, initialState);
            inputFilePath = inputData.InputFilePath;
			isRigidityDetermined = new bool[inputData.Variables.Count];
			_isRigid = new bool[inputData.Variables.Count];

            // initial application of axiom rules
            ApplyAxiomRules(initialState);
        }

        /// <summary>
        /// Creates an instance of SAS+ planning problem from the SAS+ input file.
        /// </summary>
        /// <param name="problemFile">SAS+ input file.</param>
        /// <returns>Instance of the SAS+ planning problem.</returns>
        public static SASProblem CreateFromFile(string problemFile)
        {
            return new SASProblem(SASInputDataLoader.LoadFromFile(problemFile));
        }

        /// <summary>
        /// Gets the planning problem name.
        /// </summary>
        /// <returns>The planning problem name.</returns>
        public string GetProblemName()
        {
            return problemName;
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
            return operatorsManager.GetNextSuccessors((SASState)state, maxNumSucc);
        }

        /// <summary>
        /// Gets a random forward transition (successor) from the specified state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>A pair of applicable operator and corresponding successor state.</returns>
        public Successor GetRandomSuccessor(IState state)
        {
            return operatorsManager.GetRandomSuccessor((SASState)state);
        }

        /// <summary>
        /// Gets a list of all forward transitions (successors) from the specified state.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>A collection of transitions - applicable operators and corresponding successor states.</returns>
        public Successors GetAllSuccessors(IState state)
        {
            return operatorsManager.GetAllSuccessors((SASState)state);
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
            return operatorsManager.GetNextPredecessors((SASState)state, maxNumPred);
        }

        /// <summary>
        /// Gets a random predecessor to the specified state.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <returns>A pair of possible predecessor and corresponding operator to be applied.</returns>
        public Predecessor GetRandomPredecessor(IState state)
        {
            return operatorsManager.GetRandomPredecessor((SASState)state);
        }

        /// <summary>
        /// Gets a list of all predecessors to the specified state.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <returns>A collection of transitions - possible predecessors and corresponding operators to be applied.</returns>
        public Predecessors GetAllPredecessors(IState state)
        {
            return operatorsManager.GetAllPredecessors((SASState)state);
        }

        /// <summary>
        /// Gets a list of all applicable relevant operators.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>List of applicable relevant operators to the given state.</returns>
        public Successors GetApplicableRelevantTransitions(IState state)
        {
            return operatorsManager.GetApplicableRelevantTransitions((SASState)state);
        }

        /// <summary>
        /// Gets a random applicable relevant operator.
        /// </summary>
        /// <param name="state">Original state.</param>
        /// <returns>Random applicable relevant operator to the given state.</returns>
        public Successor GetApplicableRelevantTransition(IState state)
        {
            return operatorsManager.GetApplicableRelevantTransition((SASState)state);
        }

        /// <summary>
        /// Resets the triggers in transitions space - calling of getNextSuccessors and getNextPredecessors on any state will begin
        /// from the first available applicable grounded operator (the "history" of returned transitions is cleared).
        /// </summary>
        public void ResetTransitionsTriggers()
        {
            operatorsManager.ResetTransitionsTriggers();
        }

        /// <summary>
        /// Gets the list of all operators of the SAS+ planning problem.
        /// </summary>
        /// <returns>List of operators.</returns>
        public List<SASOperator> GetOperators()
        {
            return operatorsManager.GetOperatorsList();
        }

		/// <summary>
		/// Returns list of operators that has some goal conditions as their effects.
		/// </summary>
		/// <returns></returns>
		public List<SASOperator> GetGoalRelevantOperators()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a set of operators that can occur on the last place of a plan. I.e. operators, that add some goal conditions and do not destroy any goal conditions.
		/// </summary>
		/// <returns></returns>
		public List<SASOperator> GetOperatorsThatCanBeLast()
		{
			operatorsPlaceholder.Clear();
			foreach (var item in GetOperators())
			{
				bool isRelevant = false,
					destroysGoal = false;
				foreach (var eff in item.GetEffects())
				{
					if (goalConditions.Any(cond => cond.variable == eff.GetEff().variable))
					{
						if (goalConditions.Where(cond => cond.variable == eff.GetEff().variable).Single().value == eff.GetEff().value)
							isRelevant = true;
						else
						{
							destroysGoal = true;
							break;
						}
					}
				}
				if (isRelevant && !destroysGoal)
					operatorsPlaceholder.Add(item);
			}
			return operatorsPlaceholder;
		}

        /// <summary>
        /// Flag whether the metric (i.e. operator costs) is used in the SAS+ planning problem.
        /// </summary>
        /// <returns>True if metric is used, false otherwise.</returns>
        public bool IsMetricUsed()
        {
            return operatorsManager.IsMetricUsed();
        }

        /// <summary>
        /// Gets a number of all variables used in the SAS+ planning problem.
        /// </summary>
        /// <returns>Number of all variables.</returns>
        public int GetVariablesCount()
        {
            return variablesData.Count;
        }

        /// <summary>
        /// Gets a domain range of the specified varible in the SAS+ planning problem.
        /// </summary>
        /// <param name="varIndex">Variable index.</param>
        /// <returns>Domain range of the given variable.</returns>
        public int GetVariableDomainRange(int varIndex)
        {
            return variablesData[varIndex].GetDomainRange();
        }

        /// <summary>
        /// Checks whether the specified variable is abstracted in the SAS+ planning problem.
        /// </summary>
        /// <param name="varIndex">Variable index.</param>
        /// <returns>True if the given variable is abstracted, false otherwise.</returns>
        public virtual bool IsVariableAbstracted(int varIndex)
        {
            return false;
        }

        /// <summary>
        /// Sets the initial state of the SAS+ planning problem.
        /// </summary>
        /// <param name="state">New initial state.</param>
        public void SetInitialState(IState state)
        {
            initialState = (SASState)state;
        }

        /// <summary>
        /// Gets goal conditions of the SAS+ planning problem.
        /// </summary>
        /// <returns>Goal conditions (variable-value pairs) of the planning problem.</returns>
        public SASGoalConditions GetGoalConditions()
        {
            return goalConditions;
        }

        /// <summary>
        /// Gets data about variables defined in SAS+ planning problem.
        /// </summary>
        /// <returns>Variables data of the planning problem.</returns>
        public SASVariables GetVariablesData()
        {
            return variablesData;
        }

        /// <summary>
        /// Gets mutex groups of the SAS+ planning problem.
        /// </summary>
        /// <returns>Mutex groups of the planning problem.</returns>
        public SASMutexGroups GetMutexGroups()
        {
            return mutexGroups;
        }

        /// <summary>
        /// Gets axiom rules of the SAS+ planning problem.
        /// </summary>
        /// <returns>Axiom rules of the planning problem.</returns>
        public SASAxiomRules GetAxiomRules()
        {
            return axiomRules;
        }

        /// <summary>
        /// Gets input file path corresponding to the SAS+ planning problem.
        /// </summary>
        /// <returns></returns>
        public string GetInputFilePath()
        {
            return inputFilePath;
        }

        /// <summary>
        /// Applies defined axiom rules of the SAS+ planning problem to the given state.
        /// </summary>
        /// <param name="state">State the axiom rules will be applied to.</param>
        public void ApplyAxiomRules(SASState state)
        {
            axiomRules.Apply(state);
        }

		/// <summary>
		/// Gets rigidity of variable. Variable is rigid if it is never assigned to by any operator. It will always have the same value as it has in the initial state.
		/// Testing rigidity is time-demanding, but once computed, the result is stored and not recomputed next time.
		/// </summary>
		/// <returns>True if the variable is rigid or false othervise.</returns>
		public bool isRigid(int variableID)
		{
			if (isRigidityDetermined[variableID])
				return _isRigid[variableID];
			_isRigid[variableID] = !(GetOperators().Any(op => op.GetEffects().Any(eff => eff.GetEff().variable == variableID)));
			isRigidityDetermined[variableID] = true;
			return _isRigid[variableID];
		}
	}

    /// <summary>
    /// Extended version of standard SAS+ planning problem implementation, handling "red" (abstracted) and "black" (non-abstracted)
    /// variables. Used in heuristics.
    /// </summary>
    public class SASProblemRedBlack : SASProblem
    {
        /// <summary>
        /// Set of black (non-abstracted) variables. Not contained variables are red.
        /// </summary>
        protected ICollection<int> blackVariables;

        /// <summary>
        /// Constructs red-black variant of SAS+ planning problem. All variables are initially black (non-abstracted).
        /// </summary>
        /// <param name="inputData">Loaded input data for the SAS+ planning problem.</param>
        /// <param name="blackVarsSetImpl">Black variables set implementation.</param>
        public SASProblemRedBlack(SASInputData inputData, ICollection<int> blackVarsSetImpl) : base(inputData)
        {
            blackVariables = blackVarsSetImpl;
            if (blackVariables == null)
                blackVariables = new List<int>();

            for (int i = 0; i < GetVariablesCount(); i++)
                blackVariables.Add(i);

            initialState = new SASStateRedBlack(this);
        }

        /// <summary>
        /// Creates an instance of SAS+ red-black planning problem.
        /// </summary>
        /// <param name="file">SAS+ input file.</param>
        /// <returns>Instance of the SAS+ red-black planning problem.</returns>
        public static new SASProblemRedBlack CreateFromFile(string file)
        {
            return new SASProblemRedBlack(SASInputDataLoader.LoadFromFile(file), null); // TODO!!!! null sem nepatri
        }

        /// <summary>
        /// Is given variable abstracted?
        /// </summary>
        /// <param name="variable">Variable to be checked.</param>
        /// <returns>True if the variable is abstracted, false otherwise.</returns>
        public override bool IsVariableAbstracted(int variable)
        {
            if (blackVariables.Contains(variable))
                return false;
            return true;
        }

        /// <summary>
        /// Marks specified variable as abstracted.
        /// </summary>
        /// <param name="variableToAbstract">Variable to be marked as abstracted.</param>
        public void AddAbstraction(int variableToAbstract)
        {
            blackVariables.Remove(variableToAbstract);
        }

        /// <summary>
        /// Marks all variables as abstracted.
        /// </summary>
        public void MakeAllAbstracted()
        {
            blackVariables.Clear();
        }

        /// <summary>
        /// Marks all variables as non-abstracted.
        /// </summary>
        public void MakeAllNonAbstracted()
        {
            for (int i = 0; i < GetVariablesCount(); i++)
            {
                blackVariables.Add(i);
            }
        }
    }
}
