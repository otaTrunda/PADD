using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Implementation of mutex groups defined in the SAS+ planning problem.
    /// </summary>
    public class SASMutexGroups
    {
        /// <summary>
        /// List of the actual mutex groups defined.
        /// </summary>
        private IList<SASMutexGroup> mutexGroupsList;

        /// <summary>
        /// Constructs SAS+ mutex groups.
        /// </summary>
        /// <param name="mutexGroupsData">Mutex groups data.</param>
        public SASMutexGroups(List<SASInputData.MutexGroup> mutexGroupsData)
        {
            mutexGroupsList = new List<SASMutexGroup>();
            foreach (var mutexGroupData in mutexGroupsData)
                mutexGroupsList.Add(new SASMutexGroup(mutexGroupData));
        }

        /// <summary>
        /// Gets the mutex group at the specified index.
        /// </summary>
        /// <param name="mutexGroupIdx">Mutex group index.</param>
        /// <returns>Mutex group at the specified index.</returns>
        public SASMutexGroup GetMutexGroup(int mutexGroupIdx)
        {
            return this[mutexGroupIdx];
        }

        /// <summary>
        /// Gets the mutex group at the specified index. Short version of getMutexGroup(int).
        /// </summary>
        /// <param name="mutexGroupIdx">Mutex group index.</param>
        /// <returns>Mutex group at the specified index.</returns>
        public SASMutexGroup this[int mutexGroupIdx]
        {
            get { return mutexGroupsList[mutexGroupIdx]; }
        }

        /// <summary>
        /// Gets the number of mutex groups.
        /// </summary>
        /// <returns>Number of mutex groups.</returns>
        public int Count
        {
            get { return mutexGroupsList.Count; }
        }
    }

    /// <summary>
    /// Implementation of a single SAS+ mutex group.
    /// </summary>
    public class SASMutexGroup
    {
        /// <summary>
        /// Actual mutex items in the current group.
        /// </summary>
        private IList<SASVariableValuePair> mutexItems;

        /// <summary>
        /// Constructs a SAS+ mutex group.
        /// </summary>
        /// <param name="mutexData">Mutex group data.</param>
        public SASMutexGroup(SASInputData.MutexGroup mutexData)
        {
            mutexItems = new List<SASVariableValuePair>();
            foreach (var mutexItem in mutexData.Contraints)
                mutexItems.Add(new SASVariableValuePair(mutexItem.Variable, mutexItem.Value));
        }

        /// <summary>
        /// Tries to find affected mutex item in the group and return its index.
        /// </summary>
        /// <param name="varID">Variable index.</param>
        /// <returns>Affected mutex item index. If none found, return -1.</returns>
        public int TryFindAffectedMutexItem(SASVariableValuePair varValPair)
        {
            for (int itemIdx = 0; itemIdx < mutexItems.Count; ++itemIdx)
            {
                if (mutexItems[itemIdx].Equals(varValPair))
                    return itemIdx;
            }
            return -1;
        }

        /// <summary>
        /// Gets the actual mutex item (variable-value pair).
        /// </summary>
        /// <param name="mutexItemIdx">Mutex item index.</param>
        /// <returns>Variable-value pair representing a single mutex item in the group.</returns>
        public SASVariableValuePair GetMutexItem(int mutexItemIdx)
        {
            return this[mutexItemIdx];
        }

        /// <summary>
        /// Gets the actual mutex item (variable-value pair). Short version of getMutexItem(int).
        /// </summary>
        /// <param name="mutexItemIdx">Mutex item index.</param>
        /// <returns>Variable-value pair representing a single mutex item in the group.</returns>
        public SASVariableValuePair this[int mutexItemIdx]
        {
            get { return mutexItems[mutexItemIdx]; }
        }

        /// <summary>
        /// Gets the number of all items in the group.
        /// </summary>
        /// <returns>Number of mutex items.</returns>
        public int Count
        {
            get { return mutexItems.Count; }
        }
    }
}
