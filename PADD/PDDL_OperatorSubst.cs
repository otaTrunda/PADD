using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Struct representing PDDL operator substitution (concrete grounding for lifted operators).
    /// </summary>
    public struct PDDLOperatorSubstitution
    {
        /// <summary>
        /// Primitive representation of substitution - semantics: index of the array represents an input param var ID,
        /// the value itself is the ID of a substitued constant (e.g. arr[0] = 3 means that the first input parameter
        /// of the operator has a substitued constant with ID=3).
        /// </summary>
        private int[] arr;

        /// <summary>
        /// Constructs PDDL substitution object.
        /// </summary>
        /// <param name="substit">Substitution in the form of an array.</param>
        public PDDLOperatorSubstitution(int[] substit)
        {
            this.arr = new int[substit.Length];
            substit.CopyTo(this.arr, 0);
        }

        /// <summary>
        /// Get substitued const ID for the specified variable ID.
        /// </summary>
        /// <param name="varID">Variable ID.</param>
        /// <returns>Substitued constant ID for the specified variable ID.</returns>
        public int GetValue(int varID)
        {
            return arr[varID];
        }

        /// <summary>
        /// Get number of substitute variables.
        /// </summary>
        /// <returns>Number of substitued variables.</returns>
        public int GetVarCount()
        {
            return arr.Length;
        }

        /// <summary>
        /// Constructs a deep copy of the object.
        /// </summary>
        /// <returns>Cloned substitution.</returns>
        public PDDLOperatorSubstitution Clone()
        {
            return new PDDLOperatorSubstitution(arr);
        }

        /// <summary>
        /// Substitutes the specified designator (predicate/function) with the given substitution.
        /// </summary>
        /// <param name="pred">Designator to be substituted.</param>
        /// <param name="substit">Substitution to be used.</param>
        /// <returns>New substitued designator. When the substitution is empty, then the same actual designator is returned.</returns>
        public static IPDDLDesignator MakeSubstituedDesignator(IPDDLDesignator pred, PDDLOperatorSubstitution substit)
        {
            if (substit.arr.Length == 0)
                return pred;

            IPDDLDesignator substituedPred = pred.Clone();
            for (int i = 0; i < substituedPred.GetParamCount(); ++i)
            {
                if (substituedPred.IsParamVar(i))
                    substituedPred.SubstituteParam(i, substit);
            }
            return substituedPred;
        }

        /// <summary>
        /// Constructs a hash code used in the dictionaries, maps etc.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return ArrayEqualityComparer.comparer.GetHashCode(arr);
        }

        /// <summary>
        /// Checks the equality of objects.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PDDLOperatorSubstitution))
                return false;
            return ArrayEqualityComparer.comparer.Equals(arr, ((PDDLOperatorSubstitution)obj).arr);
        }
    }
}
