using System.Collections.Generic;

namespace PADD
{
    /// <summary>
    /// Data structure representing loaded SAS+ input file. Can be filled programmatically/manually and used in the same manner.
    /// </summary>
    public class SASInputData
    {
        /// <summary>
        /// Name of the SAS+ problem (implicitly extracted from filename, e.g. "exampleName" from "exampleName.sas").
        /// </summary>
        public string ProblemName { set; get; }

        /// <summary>
        /// Version of the SAS+ input file.
        /// </summary>
        public int InputFileVersion { set; get; }

        /// <summary>
        /// File path corresponding to the SAS+ input file.
        /// </summary>
        public string InputFilePath { set; get; }

        /// <summary>
        /// Flag whether metric (action costs) is used in the SAS+ problem, or not.
        /// </summary>
        public bool IsMetricUsed { set; get; }

        /// <summary>
        /// List of variables data read from the input SAS+ file.
        /// </summary>
        public List<Variable> Variables { set; get; }

        /// <summary>
        /// List of mutex groups data read from the input SAS+ file.
        /// </summary>
        public List<MutexGroup> MutexGroups { set; get; }

        /// <summary>
        /// Initial state data read from the intput SAS+ file.
        /// </summary>
        public InitialState InitState { set; get; }

        /// <summary>
        /// Goal conditions data read from the input SAS+ file.
        /// </summary>
        public GoalConditions GoalConds { set; get; }

        /// <summary>
        /// List of operators data read from the input SAS+ file.
        /// </summary>
        public List<Operator> Operators { set; get; }

        /// <summary>
        /// List of axiom rule read from the input SAS+ file.
        /// </summary>
        public List<AxiomRule> Axioms { set; get; }

        /// <summary>
        /// Constructs an empty SAS+ input data structure.
        /// </summary>
        public SASInputData()
        {
            Init();
        }

        /// <summary>
        /// Inits the basic values for the structure.
        /// </summary>
        public void Init()
        {
            ProblemName = "";
            InputFileVersion = 0;
            InputFilePath = "";
            IsMetricUsed = false;
            Variables = new List<Variable>();
            MutexGroups = new List<MutexGroup>();
            InitState = new InitialState();
            GoalConds = new GoalConditions();
            Operators = new List<Operator>();
            Axioms = new List<AxiomRule>();
        }

        /// <summary>
        /// Data structure representing a single variable in the loaded SAS+ input file.
        /// </summary>
        public class Variable
        {
            /// <summary>
            /// Constructs data of a single variable loaded from the SAS+ input file.
            /// </summary>
            /// <param name="name">Variable name.</param>
            /// <param name="axiomLayer">Variable axiom layer.</param>
            /// <param name="domainRange">Variable domain range.</param>
            /// <param name="valuesSymbolicMeaning">Values symbolic meaning.</param>
            public Variable(string name, int axiomLayer, int domainRange, List<string> valuesSymbolicMeaning)
            {
                Name = name;
                AxiomLayer = axiomLayer;
                DomainRange = domainRange;
                ValuesSymbolicMeaning = valuesSymbolicMeaning;
            }

            /// <summary>
            /// Variable name in the loaded SAS+ problem.
            /// </summary>
            public string Name { set; get; }

            /// <summary>
            /// Variable axiom layer in the loaded SAS+ problem. Affects the order of axiomatic inference. Equals -1 for non-axiomatic variables.
            /// </summary>
            public int AxiomLayer { set; get; }

            /// <summary>
            /// Domain range of the variable in the loaded SAS+ problem. E.g. a variable with domain range of 4 can get values from {0,1,2,3}.
            /// </summary>
            public int DomainRange { set; get; }

            /// <summary>
            /// Meaning of specific values of the variable (in form of symbolic names) in the loaded SAS+ problem.
            /// An index in the list is an ID of the assigned value. The list size equals variable's domain range.
            /// </summary>
            public List<string> ValuesSymbolicMeaning { set; get; }
        }

        /// <summary>
        /// Data structure representing a single mutex group in the loaded SAS+ input file.
        /// </summary>
        public class MutexGroup
        {
            /// <summary>
            /// Constructs data of a single mutex group loaded from the SAS+ input file.
            /// </summary>
            /// <param name="constraints">Mutex group constraints.</param>
            public MutexGroup(List<VariableValuePair> constraints)
            {
                Contraints = constraints;
            }

            /// <summary>
            /// Mutual exclusion constraints in the form of a group of variable-value mappings.
            /// </summary>
            public List<VariableValuePair> Contraints { set; get; }
        }

        /// <summary>
        /// Data structure representing the initial state in the loaded SAS+ input file.
        /// </summary>
        public class InitialState
        {
            /// <summary>
            /// Constructs data of the initial state loaded from the SAS+ input file.
            /// </summary>
            /// <param name="stateValues">Initial state values.</param>
            public InitialState(List<int> stateValues = null)
            {
                if (stateValues == null)
                    stateValues = new List<int>();
                StateValues = stateValues;
            }

            /// <summary>
            /// Values of the initial state.
            /// </summary>
            public List<int> StateValues { set; get; }
        }

        /// <summary>
        /// Data structure representing the goal conditions in the loaded SAS+ input file.
        /// </summary>
        public class GoalConditions
        {
            /// <summary>
            /// Constructs data of goal conditions loaded from the SAS+ input file.
            /// </summary>
            /// <param name="conditions">Goal conditions.</param>
            public GoalConditions(List<VariableValuePair> conditions = null)
            {
                if (conditions == null)
                    conditions = new List<VariableValuePair>();
                Conditions = conditions;
            }

            /// <summary>
            /// Goal conditions in the form of a group of variable-value mappings.
            /// </summary>
            public List<VariableValuePair> Conditions { set; get; }
        }

        /// <summary>
        /// Data structure representing a single operator in the loaded SAS+ input file.
        /// </summary>
        public class Operator
        {
            /// <summary>
            /// Constructs data of a single operator loaded from the SAS+ input file.
            /// </summary>
            /// <param name="name">Operator name.</param>
            /// <param name="preconditions">Operator preconditions.</param>
            /// <param name="effects">Operator effects.</param>
            /// <param name="cost">Operator cost.</param>
            public Operator(string name, List<Precondition> preconditions, List<Effect> effects, int cost)
            {
                Name = name;
                Preconditions = preconditions;
                Effects = effects;
                Cost = cost;
            }

            /// <summary>
            /// Operator name in the loaded SAS+ problem.
            /// </summary>
            public string Name { set; get; }

            /// <summary>
            /// List of conditions for the operator to be applied.
            /// </summary>
            public List<Precondition> Preconditions { set; get; }

            /// <summary>
            /// List of effects of the operator.
            /// </summary>
            public List<Effect> Effects { set; get; }

            /// <summary>
            /// Cost of the operator in the loaded SAS+ problem.
            /// </summary>
            public int Cost { set; get; }

            /// <summary>
            /// Encapsulation of SAS+ operator's single precondition data.
            /// </summary>
            public class Precondition
            {
                /// <summary>
                /// Constructs the operator precondition.
                /// </summary>
                /// <param name="variable">Variable.</param>
                /// <param name="value">Value.</param>
                public Precondition(int variable, int value)
                {
                    Condition = new VariableValuePair(variable, value);
                }

                /// <summary>
                /// Actual condition in the form of a variable-value mapping.
                /// </summary>
                public VariableValuePair Condition { set; get; }
            }

            /// <summary>
            /// Encapsulation of SAS+ operator's single effect data.
            /// </summary>
            public class Effect
            {
                /// <summary>
                /// Constructs the operator effect.
                /// </summary>
                /// <param name="conditions">Effect conditions.</param>
                /// <param name="effVariable">Effect variable.</param>
                /// <param name="effValue">Effect value.</param>
                public Effect(List<Precondition> conditions, int effVariable, int effValue)
                {
                    Conditions = conditions;
                    ActualEffect = new VariableValuePair(effVariable, effValue);
                }

                /// <summary>
                /// List of additional conditions for the effect to be applied (in case of conditional effect).
                /// </summary>
                public List<Precondition> Conditions { set; get; }

                /// <summary>
                /// Actual effect (variable and the new value to be assigned).
                /// </summary>
                public VariableValuePair ActualEffect { set; get; }
            }
        }

        /// <summary>
        /// Data structure representing a single axiom rule in the loaded SAS+ input file.
        /// </summary>
        public class AxiomRule
        {
            /// <summary>
            /// Constructs data of a single axiom rule loaded from the SAS+ input file.
            /// </summary>
            /// <param name="conditions">Axiom rule conditions.</param>
            /// <param name="effect">Axiom rule effect.</param>
            public AxiomRule(List<VariableValuePair> conditions, VariableValuePair effect)
            {
                Conditions = conditions;
                Effect = effect;
            }

            /// <summary>
            /// List of conditions for the axiom rule to be applied.
            /// </summary>
            public List<VariableValuePair> Conditions { set; get; }

            /// <summary>
            /// The effect of the axiom rule.
            /// </summary>
            public VariableValuePair Effect { set; get; }
        }

        /// <summary>
        /// Data structure representing a variable and a corresponding assigned value.
        /// </summary>
        public struct VariableValuePair
        {
            private int _var, _val;
            /// <summary>
            /// Constructs the variable-value pair.
            /// </summary>
            /// <param name="variable">Variable.</param>
            /// <param name="value">Value.</param>
            public VariableValuePair(int variable, int value)
            {
                _var = variable;
                _val = value;
            }

            /// <summary>
            /// Variable.
            /// </summary>
            public int Variable
            {
                get
                {
                    return _var;
                }
            }

            /// <summary>
            /// Mapped value.
            /// </summary>
            public int Value
            {
                get
                {
                    return _val;
                }
            }
        }
    }
}
