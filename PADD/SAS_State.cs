using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Basic implementation of the SAS+ state in the SAS+ planning problem. A state is defined by enumeration of all values
    /// of the SAS+ planning problem.
    /// </summary>
    public class SASState : IState
    {
        /// <summary>
        /// Reference to the parent SAS+ planning problem.
        /// </summary>
        protected SASProblem parentProblem;

        /// <summary>
        /// Values of all the variables in the SAS+ state.
        /// </summary>
        protected int[] stateValues;

        /// <summary>
        /// Constructs the SAS+ state instance.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="stateValues">Values of the state.</param>
        public SASState(SASProblem problem, int[] values)
        {
            parentProblem = problem;
            stateValues = values;
        }

		public int[] GetVariablesRanges()
		{
			return parentProblem.GetVariablesRanges();
		}

        /// <summary>
        /// Checks whether the state satisfy the goal conditions of the planning problem.
        /// </summary>
        /// <returns>True if the state is meeting the problem goal conditions.</returns>
        public bool IsMeetingGoalConditions()
        {
            foreach (var item in parentProblem.GetGoalConditions())
            {
                if (!HasValue(item.variable, item.value))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Checks the number of not-fulfilled goal conditions of the planning problem. Used by search heuristics.
        /// </summary>
        /// <returns>Number of not-fulfilled goal conditions.</returns>
        public int GetNotAccomplishedGoalsCount()
        {
            int result = 0;
            foreach (var item in parentProblem.GetGoalConditions())
            {
                if (!HasValue(item.variable, item.value))
                    result++;
            }
            return result;
        }

        /// <summary>
        /// Checks whether the state has given value for the specified variable.
        /// </summary>
        /// <param name="variable">Variable to be checked.</param>
        /// <param name="value">Corresponding value to be checked.</param>
        /// <returns>True if the given variable has the given value, false otherwise.</returns>
        public virtual bool HasValue(int variable, int value)
        {
            return value == -1 || stateValues[variable] == value;
        }

        /// <summary>
        /// Sets the specified value to the given variable in the state.
        /// </summary>
        /// <param name="variable">Target variable.</param>
        /// <param name="value">Value to be assigned.</param>
        public virtual void SetValue(int variable, int value)
        {
            stateValues[variable] = value;
        }

        /// <summary>
        /// Gets the corresponding value for the specified variable.
        /// </summary>
        /// <param name="variable">Target variable.</param>
        /// <returns>Value for the specified variable.</returns>
        public virtual int GetValue(int variable)
        {
            return stateValues[variable];
        }

        /// <summary>
        /// Gets all the values of the SAS+ state. Variable can have more values, if it is abstracted.
        /// </summary>
        /// <returns>Array of values for all the variables.</returns>
        public virtual int[] GetAllValues()
        {
            return stateValues;
        }

        /// <summary>
        /// Gets a list of all values for the specified variable. Variable can have more values, if it is abstracted.
        /// </summary>
        /// <param name="variable">Target variable.</param>
        /// <returns>List of values for the given variable.</returns>
        public virtual List<int> GetAllValues(int variable)
        {
            return new List<int> { stateValues[variable] };
        }

        /// <summary>
        /// Makes a deep copy of the state.
        /// </summary>
        /// <returns>Deep copy of the state.</returns>
        public virtual IState Clone()
        {
            int[] newStateValues = new int[stateValues.Length];

            for (int i = 0; i < stateValues.Length; i++)
                newStateValues[i] = stateValues[i];

            return new SASState(parentProblem, newStateValues);
        }

        /// <summary>
        /// Constructs a hash code used in the dictionaries, maps etc.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return ArrayEqualityComparer.comparer.GetHashCode(stateValues);
        }

        /// <summary>
        /// Checks the equality of objects.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            SASState s = obj as SASState;
            if (s == null)
                return false;
            return ArrayEqualityComparer.comparer.Equals(stateValues, s.stateValues);
        }

        /// <summary>
        /// Constructs a string representing the state.
        /// </summary>
        /// <returns>String representation of the state.</returns>
        public override string ToString()
        {
            string result = "";
            result += "[";
            for (int i = 0; i < stateValues.Length; i++)
                result += (stateValues[i] + " ");
            result += "]";
            return result;
        }
    }

    /// <summary>
    /// Extended implementation of the SAS+ state in the SAS+ planning problem. Used in red-black variant of the SAS+ problem (SASProblemRedBlack).
    /// </summary>
    public class SASStateRedBlack : SASState
    {
        /*
        /// <summary>
        /// Reference to the parent SAS+ planning problem.
        /// </summary>
        private new SASProblemRedBlack parentProblem;
         */

        /// <summary>
        /// Values of all the variables in the SAS+ state. Every variable can have a list of values in the same time (if they are abstracted).
        /// </summary>
        private new List<int>[] stateValues;

        /// <summary>
        /// Constructs red-black variant of SAS+ state.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        public SASStateRedBlack(SASProblemRedBlack problem) : base(null, null)
        {
            parentProblem = problem;
            stateValues = new List<int>[problem.GetVariablesCount()];
        }

        /// <summary>
        /// Constructs red-black variant of SAS+ state from the default SAS+ state implementation.
        /// </summary>
        /// <param name="state">Destination state.</param>
        /// <param name="problem">Parent planning problem.</param>
        public SASStateRedBlack(SASState state, SASProblemRedBlack problem) : base(null, null)
        {
            parentProblem = problem;
            stateValues = new List<int>[problem.GetVariablesCount()];
            for (int i = 0; i < problem.GetVariablesCount(); i++)
            {
                stateValues[i] = new List<int>();
                stateValues[i].Add(state.GetValue(i));
            }
            this.parentProblem = problem;
        }

        /// <summary>
        /// Gets a size of the red-black SAS+ state. That means actual count of all values used in all variables.
        /// </summary>
        /// <returns>Size of the red-black state.</returns>
        public int Size()
        {
            int result = 0;
            foreach (var item in stateValues)
                result += item.Count;
            return result;
        }

        /// <summary>
        /// Checks whether the state has given value for the specified variable.
        /// </summary>
        /// <param name="variable">Variable to be checked.</param>
        /// <param name="value">Corresponding value to be checked.</param>
        /// <returns>True if the given variable has the given value, false otherwise.</returns>
        public override bool HasValue(int variable, int value)
        {
            return value == -1 || stateValues[variable].Contains(value);
        }

        /// <summary>
        /// Sets the specified value to the given variable in the state.
        /// </summary>
        /// <param name="variable">Target variable.</param>
        /// <param name="value">Value to be assigned.</param>
        public override void SetValue(int variable, int value)
        {
            if (parentProblem.IsVariableAbstracted(variable))
            {
                if (!stateValues[variable].Contains(value))
                    stateValues[variable].Add(value);
            }
            else stateValues[variable][0] = value;
        }

        /// <summary>
        /// Gets the corresponding value for the specified variable.
        /// </summary>
        /// <param name="variable">Target variable.</param>
        /// <returns>Value for the specified variable.</returns>
        public override int GetValue(int variable)
        {
            return stateValues[variable][0];
        }

        /// <summary>
        /// Gets all the values of the SAS+ state. Variable can have more values, if it is abstracted.
        /// </summary>
        /// <returns>Array of values for all the variables.</returns>
        public override int[] GetAllValues()
        {
            return null;
        }

        /// <summary>
        /// Gets a list of all values for the specified variable. Variable can have more values, if it is abstracted.
        /// </summary>
        /// <param name="variable">Target variable.</param>
        /// <returns>List of values for the given variable.</returns>
        public override List<int> GetAllValues(int variable)
        {
            return stateValues[variable];
        }

        /// <summary>
        /// Makes a deep copy of the state.
        /// </summary>
        /// <returns>Deep copy of the state.</returns>
        public override IState Clone()
        {
            SASStateRedBlack result = new SASStateRedBlack((SASProblemRedBlack)parentProblem);
            for (int i = 0; i < stateValues.Length; i++)
                result.stateValues[i] = new List<int>(stateValues[i]);
            return result;
        }

        /// <summary>
        /// Constructs a hash code used in the dictionaries, maps etc.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return ListArrayEqualityComparer.comparer.GetHashCode(stateValues);
        }

        /// <summary>
        /// Checks the equality of objects.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            SASStateRedBlack s = obj as SASStateRedBlack;
            if (s == null)
                return false;
            return ListArrayEqualityComparer.comparer.Equals(stateValues, s.stateValues);
        }

        /// <summary>
        /// Constructs a string representing the state.
        /// </summary>
        /// <returns>String representation of the state.</returns>
        public override string ToString()
        {
            string result = "";
            result += "[";
            for (int i = 0; i < stateValues.Length; i++)
            {
                result += "{";
                for (int j = 0; j < stateValues[i].Count; j++)
                {
                    result += (stateValues[i][j] + " ");
                }
                result += "}, ";
            }
            result += "]";
            return result;
        }
    }

    /// <summary>
    /// Extended implementation of the SAS+ state in the SAS+ planning problem, allowing only one abstraction to be worked with at any time.
    /// For example, there cannot be two instances of SASStateAbstracted that would have different sets of not abstracted variables.
    /// </summary>
    public class SASStateAbstracted : SASState
    {
        /// <summary>
        /// Indices of not abstracted variables.
        /// </summary>
        public static Dictionary<int, int> notAbstractedVariablesIndices;

        /// <summary>
        /// Constructs abstracted variant of SAS+ state.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        public SASStateAbstracted(SASProblem problem) : base(problem, new int[SASStateAbstracted.notAbstractedVariablesIndices.Keys.Count])
        {
        }

        /// <summary>
        /// Sets the not abstracted set of variables for this kind of SAS+ states.
        /// </summary>
        /// <param name="variables">Set of variables.</param>
        public static void SetNotAbstractedVariables(HashSet<int> variables)
        {
            SASStateAbstracted.notAbstractedVariablesIndices = new Dictionary<int, int>();

            int i = 0;
            foreach (var item in variables)
                SASStateAbstracted.notAbstractedVariablesIndices.Add(item, i++);
        }

        /// <summary>
        /// Checks whether the specified variable is abstracted.
        /// </summary>
        /// <param name="variable">Variable to be checked.</param>
        /// <returns>True if the given variable is abstracted, false otherwise.</returns>
        private bool IsAbstracted(int variable)
        {
            return !SASStateAbstracted.notAbstractedVariablesIndices.ContainsKey(variable);
        }

        /// <summary>
        /// Checks whether the state has given value for the specified variable.
        /// </summary>
        /// <param name="variable">Variable to be checked.</param>
        /// <param name="value">Corresponding value to be checked.</param>
        /// <returns>True if the given variable has the given value, false otherwise.</returns>
        public override bool HasValue(int variable, int value)
        {
            if (IsAbstracted(variable))
                return true;
            return base.HasValue(SASStateAbstracted.notAbstractedVariablesIndices[variable], value);
        }

        /// <summary>
        /// Sets the specified value to the given variable in the state.
        /// </summary>
        /// <param name="variable">Target variable.</param>
        /// <param name="value">Value to be assigned.</param>
        public override void SetValue(int variable, int value)
        {
            if (IsAbstracted(variable))
                return;
            base.SetValue(SASStateAbstracted.notAbstractedVariablesIndices[variable], value);
        }

        /// <summary>
        /// Gets the corresponding value for the specified variable.
        /// </summary>
        /// <param name="variable">Target variable.</param>
        /// <returns>Value for the specified variable.</returns>
        public override int GetValue(int variable)
        {
            if (IsAbstracted(variable))
                return -1;
            return stateValues[notAbstractedVariablesIndices[variable]];
        }

        /// <summary>
        /// Makes a deep copy of the state.
        /// </summary>
        /// <returns>Deep copy of the state.</returns>
        public new IState Clone()
        {
            SASStateAbstracted result = new SASStateAbstracted(parentProblem);
            result.stateValues = new int[stateValues.Length];
            Array.Copy(stateValues, result.stateValues, stateValues.Length);

            return result;
        }

        /// <summary>
        /// Constructs a hash code used in the dictionaries, maps etc.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Checks the equality of objects.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            SASStateAbstracted s = obj as SASStateAbstracted;
            if (s == null)
                return false;
            return ArrayEqualityComparer.comparer.Equals(stateValues, s.stateValues);
        }
    }

	/// <summary>
	/// Extension of SAS state, where variables are allowed to have another value: a wild card (-1), that indicates, that the value might be arbitrary. Is used in backward planning.
	/// </summary>
	public class RelativeState : SASState
	{
		public RelativeState(SASProblem problem, int[] values) : base(problem, values)
		{
		}

		/// <summary>
		/// If wild card is present, then this method returns FALSE!! I.e. Wild card is not considered "any value" in this test. The reason is that this method is called when deciding backwards applicability, 
		/// and if operator is applied, the value of its effect can not be wildcard.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public override bool HasValue(int variable, int value)
		{
			throw new Exception();
			{
				//this should not be called at all
			}
			//return stateValues[variable] == value;
		}

		public override IState Clone()
		{
			return new RelativeState(this.parentProblem, this.stateValues.ToArray());
		}

	}
}
