using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Common interface for PDDLState factories. When a user wants to use a new state implementation, he just creates two
    /// new classes - first one implementing PDDLState and the second one implementing PDDLStateFactory. The factory is then
    /// used as a parameter of PDDLProblem.createFromFile(string, string, PDDLDesignatorFactory, PDDLStateFactory) method.
    /// </summary>
    public interface IPDDLStateFactory
    {
        /// <summary>
        /// Creates an instance of PDDL state.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="predicates">Collection of predicates of the state.</param>
        /// <param name="functions">Collection of functions and their corresponding values in the state.</param>
        /// <returns>Instance of PDDL state.</returns>
        IPDDLState CreateState(PDDLProblem problem, ICollection<IPDDLDesignator> predicates, IDictionary<IPDDLDesignator, int> functions);
    }

    /// <summary>
    /// PDDLState factory for the PDDLStateDefault implementation.
    /// </summary>
    public class PDDLStateFactory : IPDDLStateFactory
    {
        /// <summary>
        /// Creates an instance of PDDL state.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="predicates">Collection of predicates of the state.</param>
        /// <param name="functions">Collection of functions and their corresponding values in the state.</param>
        /// <returns>Instance of PDDL state.</returns>
        public IPDDLState CreateState(PDDLProblem problem, ICollection<IPDDLDesignator> predicates, IDictionary<IPDDLDesignator, int> functions)
        {
            return new PDDLStateDefault(problem, new HashSet<IPDDLDesignator>(predicates), new Dictionary<IPDDLDesignator, int>(functions));
        }
    }

    /// <summary>
    /// Common interface for a state in the PDDL planning problem. A state is basically defined by a set of PDDL predicates,
    /// and PDDL function values. The function values not specifically stated for corresponding function designator
    /// are undefined in this state.
    /// </summary>
    public interface IPDDLState : IState
    {
        /// <summary>
        /// Gets the set of predicates of the state.
        /// </summary>
        /// <returns>Set of PDDL predicates.</returns>
        ICollection<IPDDLDesignator> GetPredicates();

        /// <summary>
        /// Gets the set of functions of the state.
        /// </summary>
        /// <returns>Set of PDDL functions.</returns>
        IDictionary<IPDDLDesignator, int> GetFunctions();

        /// <summary>
        /// Checks whether the state contains requested predicate.
        /// </summary>
        /// <param name="pred">Predicate to be checked.</param>
        /// <returns>True if the state contains the predicate, false otherwise.</returns>
        bool HasPredicate(IPDDLDesignator pred);

        /// <summary>
        /// Gets the function value corresponding to the requested function designator.
        /// </summary>
        /// <param name="funcName">Requested function designator.</param>
        /// <returns>Value of inputed function designator, if defined in the state. Returns -1, if value undefined in the state.</returns>
        int GetFunctionValue(IPDDLDesignator funcName);

        /// <summary>
        /// Defines a new value for the requested function designator in the state.
        /// </summary>
        /// <param name="funcName">Requested function designator.</param>
        /// <param name="assignedVal">Value to be assigned.</param>
        void AssignFunc(IPDDLDesignator funcName, int assignedVal);

        /// <summary>
        /// Increase the value of the requested numerical function by the specified value.
        /// </summary>
        /// <param name="funcName">Requested numeric function designator.</param>
        /// <param name="incrVal">Value to be increased by.</param>
        void IncreaseFunc(IPDDLDesignator funcName, int incrVal);

        /// <summary>
        /// Decrease the value of the requested numerical function by the specified value.
        /// </summary>
        /// <param name="funcName">Requested numeric function designator.</param>
        /// <param name="decrVal">Value to be decreased by.</param>
        void DecreaseFunc(IPDDLDesignator funcName, int decrVal);
    }

    /// <summary>
    /// Default implementation of a state in the PDDL planning problem, using HashSet as a container for predicates and
    /// Dictionary as a container for mapping functions to their corresponding values.
    /// </summary>
    public class PDDLStateDefault : IPDDLState
    {
        /// <summary>
        /// Reference to the parent PDDL planning problem.
        /// </summary>
        private PDDLProblem problem;

        /// <summary>
        /// Set of predicates in the state.
        /// </summary>
        private HashSet<IPDDLDesignator> predicates;

        /// <summary>
        /// Set of function designators and their corresponding values in the state.
        /// </summary>
        private Dictionary<IPDDLDesignator, int> functions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="problem">Reference to the parent problem.</param>
        /// <param name="predicates">Set of predicates in the state.</param>
        /// <param name="functions">Set of function designators and their corresponding values in the state.</param>
        public PDDLStateDefault(PDDLProblem problem, HashSet<IPDDLDesignator> predicates, Dictionary<IPDDLDesignator, int> functions)
        {
            this.problem = problem;
            this.predicates = predicates;
            this.functions = functions;
        }

        /// <summary>
        /// Gets the set of predicates of the state.
        /// </summary>
        /// <returns>Set of PDDL predicates.</returns>
        public ICollection<IPDDLDesignator> GetPredicates()
        {
            return predicates;
        }

        /// <summary>
        /// Gets the set of functions of the state.
        /// </summary>
        /// <returns>Set of PDDL functions.</returns>
        public IDictionary<IPDDLDesignator, int> GetFunctions()
        {
            return functions;
        }

        /// <summary>
        /// Checks whether the state contains requested predicate.
        /// </summary>
        /// <param name="pred">Predicate to be checked.</param>
        /// <returns>True if the state contains the predicate, false otherwise.</returns>
        public bool HasPredicate(IPDDLDesignator pred)
        {
            return predicates.Contains(pred);
        }

        /// <summary>
        /// Gets the function value corresponding to the requested function designator.
        /// </summary>
        /// <param name="funcName">Requested function designator.</param>
        /// <returns>Value of inputed function designator, if defined in the state. Returns -1, if value undefined in the state.</returns>
        public int GetFunctionValue(IPDDLDesignator funcName)
        {
            int funcVal;
            if (!functions.TryGetValue(funcName, out funcVal))
                funcVal = -1;
            return funcVal;
        }

        /// <summary>
        /// Defines a new value for the requested function designator in the state.
        /// </summary>
        /// <param name="funcName">Requested function designator.</param>
        /// <param name="assignedVal">Value to be assigned.</param>
        public void AssignFunc(IPDDLDesignator funcName, int assignedVal)
        {
            functions[funcName] = assignedVal;
        }

        /// <summary>
        /// Increase the value of the requested numerical function by the specified value.
        /// </summary>
        /// <param name="funcName">Requested numeric function designator.</param>
        /// <param name="incrVal">Value to be increased by.</param>
        public void IncreaseFunc(IPDDLDesignator funcName, int incrVal)
        {
            if (!functions.ContainsKey(funcName))
                functions[funcName] = incrVal;
            else
                functions[funcName] += incrVal;
        }

        /// <summary>
        /// Decrease the value of the requested numerical function by the specified value.
        /// </summary>
        /// <param name="funcName">Requested numeric function designator.</param>
        /// <param name="decrVal">Value to be decreased by.</param>
        public void DecreaseFunc(IPDDLDesignator funcName, int decrVal)
        {
            if (!functions.ContainsKey(funcName))
                functions[funcName] = -decrVal;
            else
                functions[funcName] -= decrVal;
        }

        /// <summary>
        /// Checks whether the state satisfy the goal conditions of the planning problem.
        /// </summary>
        /// <returns>True if the state is meeting the problem goal conditions.</returns>
        public bool IsMeetingGoalConditions()
        {
            return PDDLExpressionEval.EvaluateLogicalExpression(problem, this, problem.GetGoalConditions());
        }

        /// <summary>
        /// Checks the number of not-fulfilled goal conditions of the PDDL problem. Used by search heuristics. See
        /// FulfilledConditionsCountVisitor for details about cases with complicated goal conditions (complex logical expressions).
        /// </summary>
        /// <returns>Number of not-fulfilled goal conditions.</returns>
        public int GetNotAccomplishedGoalsCount()
        {
            return PDDLExpressionEval.CountFulfilledConditions(problem, this, problem.GetGoalConditions()).Item2;
        }

        /// <summary>
        /// Makes a deep copy of the state.
        /// </summary>
        /// <returns>Deep copy of the state.</returns>
        public IState Clone()
        {
            return new PDDLStateDefault(problem, new HashSet<IPDDLDesignator>(predicates), new Dictionary<IPDDLDesignator, int>(functions));
        }

        /// <summary>
        /// Constructs a hash code used in the dictionaries, maps etc.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var pred in predicates)
                hash += pred.GetHashCode();
            foreach (var func in functions)
            {
                hash += 7 * func.Key.GetHashCode();
                hash += 17 * func.Value.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Checks the equality of objects.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            PDDLStateDefault s = obj as PDDLStateDefault;
            if (s == null)
                return false;
            if (!predicates.SetEquals(s.predicates))
                return false;
            return (functions.Intersect(s.functions).Count() == functions.Union(functions).Count());
        }

        /// <summary>
        /// Constructs a string representing the state.
        /// </summary>
        /// <returns>String representation of the state.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("State: {");
            bool bFirst = true;
            foreach (var pred in predicates)
            {
                if (bFirst)
                    bFirst = false;
                else
                    sb.Append(", ");

                sb.Append(problem.GetIDManager().GetPredicatesMapping().GetStringForPredicateID(pred.GetPrefixID()));
                sb.Append("(");
                for (int i = 0; i < pred.GetParamCount(); ++i)
                {
                    sb.Append(problem.GetIDManager().GetConstantsMapping().GetStringForConstID(pred.GetParam(i)));
                }

                sb.Append(")");
            }

            foreach (var func in functions)
            {
                if (bFirst)
                    bFirst = false;
                else
                    sb.Append(", ");

                sb.Append(problem.GetIDManager().GetFunctionsMapping().GetStringForFunctionID(func.Key.GetPrefixID()));
                sb.Append("(");

                for (int i = 0; i < func.Key.GetParamCount(); ++i)
                    sb.Append(problem.GetIDManager().GetConstantsMapping().GetStringForConstID(func.Key.GetParam(i)));

                sb.Append(") = ");
                sb.Append(func.Value);
            }

            sb.Append("}");
            return sb.ToString();
        }
    }
}
