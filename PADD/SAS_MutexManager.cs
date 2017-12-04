using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Implementation of the mutex manager. Provides methods for checking operators applicability with regards to defined mutex groups in the
    /// SAS+ planning problem.
    /// </summary>
    public class SASMutexManager
    {
        /// <summary>
        /// Reference state.
        /// </summary>
        private SASState refState;

        /// <summary>
        /// Actual mutex groups of the SAS+ planning problem.
        /// </summary>
        private SASMutexGroups mutexGroups;

        /// <summary>
        /// Current locks for each mutex group corresponding to the reference state. E.g. 'refStateLocks[0] = 2' means
        /// that the first mutex group has lock on the item with index 2.
        /// </summary>
        private int[] refStateLocks;

        /// <summary>
        /// Current locks for each mutex group corresponding to the current operator. Cannot lock an item which is already
        /// locked in refStateLocks.
        /// </summary>
        private int[] currOperatorLocks;

        /// <summary>
        /// Constructs the mutex manager.
        /// </summary>
        /// <param name="definedMutexGroups">Mutex groups of the SAS+ planning problem.</param>
        public SASMutexManager(SASMutexGroups definedMutexGroups)
        {
            refState = null;
            mutexGroups = definedMutexGroups;
            refStateLocks = new int[mutexGroups.Count];
            currOperatorLocks = new int[mutexGroups.Count];
        }

        /// <summary>
        /// Tries to lock the specified operator mutex item in the given mutex group.
        /// </summary>
        /// <param name="groupIdx">Mutex group index.</param>
        /// <param name="itemIdx">Mutex item index.</param>
        /// <returns>True, if the specified item has been successfully locked (or had been locked before). False otherwise.</returns>
        private bool TryLockOperatorMutex(int groupIdx, int itemIdx)
        {
            // try both refState locks and operator locks
            var currentLockValue = refStateLocks[groupIdx];
            if (currentLockValue == -1)
                currentLockValue = currOperatorLocks[groupIdx];

            // already locked on the same item
            if (currentLockValue == itemIdx)
                return true;

            // already locked on a different item = violation of group constraints
            if (currentLockValue != -1)
                return false;

            currOperatorLocks[groupIdx] = itemIdx;
            return true;
        }

        /// <summary>
        /// Tries to lock the specified reference state mutex item in the given mutex group.
        /// </summary>
        /// <param name="groupIdx">Mutex group index.</param>
        /// <param name="itemIdx">Mutex item index.</param>
        /// <returns>True, if the specified item has been successfully locked, false otherwise.</returns>
        private bool TryLockRefStateMutex(int groupIdx, int itemIdx)
        {
            if (refStateLocks[groupIdx] != -1)
                return false;

            refStateLocks[groupIdx] = itemIdx;
            return true;
        }

        /// <summary>
        /// Releases all locks of all mutex groups corresponding to the reference state.
        /// </summary>
        private void ReleaseRefStateLocks()
        {
            for (int i = 0; i < refStateLocks.Length; ++i)
                refStateLocks[i] = -1;
        }

        /// <summary>
        /// Releases all locks of all mutex groups corresponding to the current operator.
        /// </summary>
        private void ReleaseCurrOperatorLocks()
        {
            for (int i = 0; i < currOperatorLocks.Length; ++i)
                currOperatorLocks[i] = -1;
        }

        /// <summary>
        /// Collects active mutexes in the given state and sets the reference state for the subsequent checkOperatorApplicability(SASOperator)
        /// calls. We assume that the specified state is correct, i.e. has at most one active mutex.
        /// </summary>
        /// <param name="state">Reference state.</param>
        public void SetReferenceStateLocks(SASState state)
        {
            // set reference state
            refState = state;

            // go through the state and lock active mutexes
            EvaluateState(state, true);
        }

        /// <summary>
        /// Evaluates the given for compliance with mutex groups. Goes through the state and locks all active mutexes.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <param name="minimalEvaluation">In case of min. eval., we are sure the state is correct, so we find only 1st active mutex.</param>
        /// <returns>True, if the state complies with mutex groups. False otherwise.</returns>
        private bool EvaluateState(SASState state, bool minimalEvaluation)
        {
            // release previous reference state locks and operator locks
            ReleaseRefStateLocks();
            ReleaseCurrOperatorLocks();

            // find active mutexes in the given state
            for (int groupIdx = 0; groupIdx < mutexGroups.Count; ++groupIdx)
            {
                var mutexGroup = mutexGroups[groupIdx];

                for (int itemIdx = 0; itemIdx < mutexGroup.Count; ++itemIdx)
                {
                    var mutexItem = mutexGroup[itemIdx];
                    if (state.HasValue(mutexItem.variable, mutexItem.value))
                    {
                        if (!TryLockRefStateMutex(groupIdx, itemIdx))
                            return false;

                        // in case of minimal evaluation, there shouldn't be any more active mutexes (assuming the state is correct)
                        if (minimalEvaluation)
                            break;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Checks the operator for additional applicability. Corresponding successors have to comply with mutex constraints in the SAS+ planning
        /// problem. We assume, that an init call of setReferenceState(SASState) has been done.
        /// </summary>
        /// <param name="oper">Operator to be checked.</param>
        /// <returns>True, if the operator is applicable with regards to defined mutex groups. False otherwise.</returns>
        public bool CheckOperatorApplicability(SASOperator oper)
        {
            //TODO!!! nefunguje
            return true;

            ReleaseCurrOperatorLocks();

            for (int groupIdx = 0; groupIdx < mutexGroups.Count; ++groupIdx)
            {
                var mutexGroup = mutexGroups[groupIdx];

                foreach (var eff in oper.GetEffects())
                {
                    if (eff.IsApplicable(refState)) // effect can be conditional
                    {
                        int itemIdx = mutexGroup.TryFindAffectedMutexItem(eff.GetEff());
                        if (itemIdx != -1)
                        {
                            if (!TryLockOperatorMutex(groupIdx, itemIdx))
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether the given state complies with mutex constraints in the SAS+ planning problem.
        /// </summary>
        /// <param name="state">State to be checked.</param>
        /// <returns>True, if the state complies with mutex constraints. False otherwise.</returns>
        public bool ValidateState(SASState state)
        {
            // full evaluation - if there are more active mutexes, the function fails
            bool retVal = EvaluateState(state, false);

            ReleaseRefStateLocks();
            return retVal;
        }
    }
}
