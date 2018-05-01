using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Implementation of variables in the SAS+ planning problem.
    /// </summary>
    public class SASVariables
    {
        /// <summary>
        /// List of variables.
        /// </summary>
        private IList<SASVariable> variablesList;

        /// <summary>
        /// Constructs SAS+ variables data object.
        /// </summary>
        /// <param name="variablesData"></param>
        public SASVariables(List<SASInputData.Variable> variablesData)
        {
            variablesList = new List<SASVariable>();
            foreach (var variableData in variablesData)
                variablesList.Add(new SASVariable(variableData));
        }

        /// <summary>
        /// Gets the variable data at the specified index.
        /// </summary>
        /// <param name="variableIdx">Variable index.</param>
        /// <returns>Variable data at the given index.</returns>
        public SASVariable GetVariable(int variableIdx)
        {
            return this[variableIdx];
        }

        /// <summary>
        /// Gets the variable data at the specified index. Short version of getVariable(int).
        /// </summary>
        /// <param name="variableIdx">Variable index.</param>
        /// <returns>Variable data at the given index.</returns>
        public SASVariable this[int variableIdx]
        {
            get { return variablesList[variableIdx]; }
        }

        /// <summary>
        /// Gets the number of variables.
        /// </summary>
        /// <returns>Number of variables.</returns>
        public int Count
        {
            get { return variablesList.Count; }
        }
    }

    /// <summary>
    /// Implementation of a variable in the SAS+ planning problem.
    /// </summary>
    public class SASVariable
    {
        /// <summary>
        /// Variable name in the SAS+ planning problem.
        /// </summary>
        private string variableName;

        /// <summary>
        /// Used axiom layer of the variable in the SAS+ planning problem (-1, if no axiom layer used).
        /// </summary>
        private int variableAxiomLayer;

        /// <summary>
        /// Domain range of the variable in the SAS+ planning problem..
        /// </summary>
        private int variableDomainRange;

        /// <summary>
        /// Meaning of assigned values to the variable (in form of symbolic names) in the SAS+ planning problem.
        /// An index in the list is an ID of the assigned value. The list size equals variable's domain range.
        /// </summary>
        public List<string> valuesSymbolicMeaning;

        /// <summary>
        /// Constructs SAS+ variable data object.
        /// </summary>
        /// <param name="variableData">Variable data.</param>
        public SASVariable(SASInputData.Variable variableData)
        {
            variableName = variableData.Name;
            variableAxiomLayer = variableData.AxiomLayer;
            variableDomainRange = variableData.DomainRange;
            valuesSymbolicMeaning = variableData.ValuesSymbolicMeaning;
        }

        /// <summary>
        /// Gets variable name in the SAS+ planning problem.
        /// </summary>
        /// <returns>Variable name.</returns>
        public string GetName()
        {
            return variableName;
        }

        /// <summary>
        /// Gets used axiom layer of the variable in the SAS+ planning problem.
        /// </summary>
        /// <returns>Corresponding axiom layer index, if used. Otherwise -1.</returns>
        public int GetAxiomLayer()
        {
            return variableAxiomLayer;
        }

        /// <summary>
        /// Gets domain range of the variable (i.e. maximal value + 1).
        /// </summary>
        /// <returns>Domain range of the variable.</returns>
        public int GetDomainRange()
        {
            return variableDomainRange;
        }

        /// <summary>
        /// Gets a string representing symbolic meaning of the specified value of the variable.
        /// </summary>
        /// <returns>Symbolic meaning of the given value.</returns>
        public string GetValueSymbolicMeaning(int value)
        {
            return valuesSymbolicMeaning[value];
        }
	}
}
