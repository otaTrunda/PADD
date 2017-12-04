using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Implementation of SAS+ goal conditions.
    /// </summary>
    public class SASGoalConditions : IEnumerable<SASVariableValuePair>
    {
        /// <summary>
        /// Goal conditions list.
        /// </summary>
        private IList<SASVariableValuePair> goalConditionsList;

        /// <summary>
        /// Constructs SAS+ goal conditions object.
        /// </summary>
        /// <param name="goalConditionsData">Goal conditions data.</param>
        public SASGoalConditions(SASInputData.GoalConditions goalConditionsData)
        {
            goalConditionsList = new List<SASVariableValuePair>();
            foreach (var goalCond in goalConditionsData.Conditions)
                goalConditionsList.Add(new SASVariableValuePair(goalCond.Variable, goalCond.Value));
        }

        /// <summary>
        /// Gets the goal condition item at the specified index.
        /// </summary>
        /// <param name="goalCondIdx">Goal condition index.</param>
        /// <returns>Goal condition item at the specified index.</returns>
        public SASVariableValuePair GetGoalCondition(int goalCondIdx)
        {
            return this[goalCondIdx];
        }

        /// <summary>
        /// Gets the goal condition item by specified variable.
        /// </summary>
        /// <param name="variable">Variable ID.</param>
        /// <returns>Goal condition item by specified index.</returns>
        public int GetGoalValueForVariable(int variable)
        {
            foreach (var goalCond in goalConditionsList)
            {
                if (goalCond.variable == variable)
                    return goalCond.value;
            }
            return -1;
        }

        /// <summary>
        /// Checks whether the specified variable is included in goal conditions.
        /// </summary>
        /// <param name="variable">Variable ID.</param>
        /// <returns>True, if the variable is included in goal conditions. False otherwise.</returns>
        public bool IsVariableAffected(int variable)
        {
            return goalConditionsList.Any(cond => cond.variable == variable);
        }

        /// <summary>
        /// Gets the goal condition item at the specified index. Short version of getGoalCondition(int).
        /// </summary>
        /// <param name="goalCondIdx">Goal condition index.</param>
        /// <returns>Goal condition item at the specified index.</returns>
        public SASVariableValuePair this[int goalCondIdx]
        {
            get { return goalConditionsList[goalCondIdx]; }
        }

        /// <summary>
        /// Gets the number of goal conditions.
        /// </summary>
        /// <returns>Number of goal conditions.</returns>
        public int Count
        {
            get { return goalConditionsList.Count; }
        }

        /// <summary>
        /// Gets enumerator for the collection of goal conditions.
        /// </summary>
        /// <returns>Enumerator of goal condition entries.</returns>
        public IEnumerator<SASVariableValuePair> GetEnumerator()
        {
            for (int i = 0; i < goalConditionsList.Count; ++i)
                yield return goalConditionsList[i];
        }

        /// <summary>
        /// Gets enumerator for the collection of goal conditions.
        /// </summary>
        /// <returns>Enumerator of goal condition entries.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
