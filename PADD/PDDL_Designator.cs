using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Common interface for PDDLDesignator factories. When a user wants to use a new designator implementation, he just creates two
    /// new classes - first one implementing PDDLDesignator and the second one implementing PDDLDesignatorFactory. The factory is then
    /// used as a parameter of PDDLProblem.createFromFile(string, string, PDDLDesignatorFactory, PDDLStateFactory) method.
    /// </summary>
    public interface IPDDLDesignatorFactory
    {
        /// <summary>
        /// Creates an instance of PDDL designator (predicate, or function).
        /// </summary>
        /// <param name="predID">Predicate/function ID.</param>
        /// <param name="paramIDs">List of parameters.</param>
        /// <param name="paramIDsIsVar">Flag whether a specified parameter is variable or not.</param>
        /// <returns>Instance of PDDL designator.</returns>
        IPDDLDesignator CreateDesignator(int predID, int[] paramIDs, bool[] paramIDsIsVar = null);
    }

    /// <summary>
    /// PDDLDesignator factory for the PDDLDesignatorDefault implementation.
    /// </summary>
    public class PDDLDesignatorFactory : IPDDLDesignatorFactory
    {
        /// <summary>
        /// Creates an instance of PDDL designator (predicate, or function).
        /// </summary>
        /// <param name="predID">Predicate/function ID.</param>
        /// <param name="paramIDs">List of parameters.</param>
        /// <param name="paramIDsIsVar">Flag whether a specified parameter is variable or not.</param>
        /// <returns>Instance of PDDL designator.</returns>
        public IPDDLDesignator CreateDesignator(int predID, int[] paramIDs, bool[] paramIDsIsVar = null)
        {
            return new PDDLDesignator(predID, paramIDs, paramIDsIsVar);
        }
    }

    /// <summary>
    /// PDDLDesignator factory for the PDDLDesignatorSmart implementation.
    /// </summary>
    public class PDDLDesignatorFactorySmart : IPDDLDesignatorFactory
    {
        /// <summary>
        /// Creates an instance of PDDL designator (predicate, or function).
        /// </summary>
        /// <param name="predID">Predicate/function ID.</param>
        /// <param name="paramIDs">List of parameters.</param>
        /// <param name="paramIDsIsVar">Flag whether a specified parameter is variable or not.</param>
        /// <returns>Instance of PDDL designator.</returns>
        public IPDDLDesignator CreateDesignator(int predID, int[] paramIDs, bool[] paramIDsIsVar = null)
        {
            return new PDDLDesignatorSmart(predID, paramIDs, paramIDsIsVar);
        }
    }

    /// <summary>
    /// Common interface for designator implementations. These designators are used to represent either predicates or functions
    /// in PDDL states of the PDDL planning problem. E.g. married(john, ?a) for a partially grounded predicate, or distance(Toronto,
    /// Boston) for a function. All the stored data are in the form of integer IDs, which are mapped in the PDDL planning problem.
    /// </summary>
    public interface IPDDLDesignator
    {
        /// <summary>
        /// Gets the prefix of the designator, e.g. for "married(john, ?a)", "married" is the prefix.
        /// </summary>
        /// <returns>Mapped ID of the designator prefix.</returns>
        int GetPrefixID();

        /// <summary>
        /// Gets the specified parameter of the designator, e.g. the 1st parameter for "married(john, ?a)" is "john".
        /// </summary>
        /// <param name="idx">Index of the requested parameter. Should be checked on getParamCount().</param>
        /// <returns>Mapped ID of the designator parameter.</returns>
        int GetParam(int idx);

        /// <summary>
        /// Gets the number of designator parameters. Can be used for iteration over all parameters.
        /// </summary>
        /// <returns>The number of designator parameters.</returns>
        int GetParamCount();

        /// <summary>
        /// Checks whether the specified parameter is lifted (i.e. it's a variable), or grounded (i.e. it's a constant).
        /// E.g. the 1st parameter of "married(john, ?a)" is constant and the 2nd is variable.
        /// </summary>
        /// <param name="idx">Index of the requested parameter. Should be checked on getParamCount().</param>
        /// <returns>True if requested parameter is variable (lifted). False otherwise.</returns>
        bool IsParamVar(int idx);

        /// <summary>
        /// Does a substitution (grounding process) of the requested parameter. Should be checked on isParamVar(idx).
        /// For the resulting designator, isParamVar(idx) will be false.
        /// </summary>
        /// <param name="idx">Index of the requested parameter. Should be checked on getParamCount().</param>
        /// <param name="substit">Substitution in the context of PDDL operator (i.e. mapping parameter variables to constants).</param>
        void SubstituteParam(int idx, PDDLOperatorSubstitution substit);

        /// <summary>
        /// Clones the current designator object.
        /// </summary>
        /// <returns>A deep copy of the current designator object.</returns>
        IPDDLDesignator Clone();
    }

    /// <summary>
    /// Advanced implementation of the designator object, using only one 32-bit number to represent the whole predicate or function designator.
    /// Coding/decoding is done by some by bit-shifts and applying bit masks. Meaning of the actual bits may change.
    /// </summary>
    public class PDDLDesignatorSmart : IPDDLDesignator
    {
        /// <summary>
        /// Coded 32-bit representation of the designator.
        /// </summary>
        private int pred;

        /// <summary>
        /// Number of bits for prefix.
        /// </summary>
        private static int gIdBitCount = 5;
        
        /// <summary>
        /// Number of bits for each parameter.
        /// </summary>
        private static int gParamBitCount = 8;
        
        /// <summary>
        /// Number of parameters.
        /// </summary>
        private static int gParamCount = 3;

        /// <summary>
        /// Changes the meaning of the actual bits. Default: 5bits for prefixID + 3 * (8bits for params + 1bit IsVar) = 32-bit.
        /// </summary>
        public static void ChangeGlobalRepresentation(int idBitCount, int paramBitCount, int paramCount)
        {
            gIdBitCount = idBitCount;
            gParamBitCount = paramBitCount;
            gParamCount = paramCount;

            System.Diagnostics.Debug.Assert(gIdBitCount + gParamCount * (gParamBitCount + 1) == sizeof(int));
        }

        /// <summary>
        /// Constructor. Composes the coded number by some bit magic.
        /// </summary>
        /// <param name="predID">Prefix ID of the designator.</param>
        /// <param name="paramIDs">Parameters IDs of the designator.</param>
        /// <param name="isVar">Specify isVar property of the designator parameters.</param>
        public PDDLDesignatorSmart(int predID, int[] paramIDs, bool[] isVar = null)
        {
            // compose the coded number by shifting and applying OR
            pred = (predID << (32 - gIdBitCount));
            for (int i = 0; i < gParamCount; ++i)
            {
                pred |= ((isVar != null && i < isVar.Length && isVar[i]) ? 1 : 0) << gParamCount - 1 - i;
                pred |= ((i < paramIDs.Length) ? paramIDs[i] : GetMaxParamValue()) << gParamCount + (gParamCount - 1 - i) * gParamBitCount;
            }
        }

        /// <summary>
        /// Copy-constructor.
        /// </summary>
        /// <param name="predicate">Source object to be copied.</param>
        public PDDLDesignatorSmart(PDDLDesignatorSmart designator)
        {
            this.pred = designator.pred;
        }

        /// <summary>
        /// Gets the prefix of the designator, e.g. for "married(john, ?a)", "married" is the prefix.
        /// </summary>
        /// <returns>Mapped ID of the designator prefix.</returns>
        public int GetPrefixID()
        {
            // highest _idBitCount bits
            return ((pred >> 32 - gIdBitCount) & ((1 << gIdBitCount) - 1));
        }

        /// <summary>
        /// Gets the specified parameter of the designator, e.g. the 1st parameter for "married(john, ?a)" is "john".
        /// </summary>
        /// <param name="idx">Index of the requested parameter. Should be checked on getParamCount().</param>
        /// <returns>Mapped ID of the designator parameter.</returns>
        public int GetParam(int idx)
        {
            // shift _paramCount lowest bits (isVar), shift _paramBitCount bits for every higher-indexed param, and apply _paramBitCount bit mask
            return ((pred >> gParamCount + gParamBitCount * (gParamCount - 1 - idx)) & ((1 << gParamBitCount) - 1));
        }

        /// <summary>
        /// Gets the number of designator parameters. Can be used for iteration over all parameters.
        /// </summary>
        /// <returns>The number of designator parameters.</returns>
        public int GetParamCount()
        {
            // number of params
            return gParamCount;
        }

        /// <summary>
        /// Checks whether the specified parameter is lifted (i.e. it's a variable), or grounded (i.e. it's a constant).
        /// E.g. the 1st parameter of "married(john, ?a)" is constant and the 2nd is variable.
        /// </summary>
        /// <param name="idx">Index of the requested parameter. Should be checked on getParamCount().</param>
        /// <returns>True if requested parameter is variable (lifted). False otherwise.</returns>
        public bool IsParamVar(int idx)
        {
            // lowest _paramCount bits
            return (pred & (1 << (gParamCount - 1 - idx))) != 0;
        }

        /// <summary>
        /// Does a substitution (grounding process) of the requested parameter. Should be checked on isParamVar(idx).
        /// For the resulting designator, isParamVar(idx) will be false.
        /// </summary>
        /// <param name="idx">Index of the requested parameter. Should be checked on getParamCount().</param>
        /// <param name="substit">Substitution in the context of PDDL operator (i.e. mapping parameter variables to constants).</param>
        public void SubstituteParam(int idx, PDDLOperatorSubstitution substit)
        {
            // get the value and check whether it is actually used in the coded representation
            int varID = GetParam(idx);
            if (varID != GetMaxParamValue()) // parameter is used
                SubstituteParam(idx, substit.GetValue(varID));
        }

        /// <summary>
        /// Does the actual subtitution (grounding) of the specified parameter.
        /// </summary>
        /// <param name="idx">Index of the requested parameter.</param>
        /// <param name="val">Value to be subtitued.</param>
        private void SubstituteParam(int idx, int val)
        {
            // zero param bits
            pred = pred & ~(((1 << gParamBitCount) - 1) << (gParamCount + gParamBitCount * (gParamCount - 1 - idx)));

            // write new value
            pred = pred | (val << gParamCount + gParamBitCount * (gParamCount - 1 - idx));

            // zero isVar() bit
            pred = pred & ~(1 << (gParamCount - 1 - idx));
        }

        /// <summary>
        /// Gets special value for padding (when parameter is actually not used in the coded representation). 
        /// </summary>
        /// <returns>Special padding value used in coded representation.</returns>
        private static int GetMaxParamValue()
        {
            return (1 << gParamBitCount) - 1;
        }

        /// <summary>
        /// Clones the current designator object.
        /// </summary>
        /// <returns>A deep copy of the current designator object.</returns>
        public IPDDLDesignator Clone()
        {
            return new PDDLDesignatorSmart(this);
        }

        /// <summary>
        /// Constructs a hash code used in the dictionaries, maps etc.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return pred;
        }

        /// <summary>
        /// Checks the equality of objects.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PDDLDesignatorSmart))
                return false;
            return pred == ((PDDLDesignatorSmart)obj).pred;
        }
    }

    /// <summary>
    /// Simple straight-forward implementation of the designator object, using integer arrays.
    /// </summary>
    public class PDDLDesignator : IPDDLDesignator
    {
        /// <summary>
        /// PlanningProblem-specific designator prefix ID.
        /// </summary>
        private int ID;

        /// <summary>
        /// Constant/variable ID of the designator parameters.
        /// </summary>
        private int[] paramIDs;

        /// <summary>
        /// Distinguishing whether the specific parameter is a constant, or a variable.
        /// </summary>
        private bool[] isVar;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="predID">Prefix ID of the designator.</param>
        /// <param name="paramIDs">Parameters IDs of the designator.</param>
        /// <param name="isVar">Specify isVar property of the designator parameters.</param>
        public PDDLDesignator(int ID, int[] paramIDs, bool[] isVar = null)
        {
            this.ID = ID;
            this.paramIDs = paramIDs;
            this.isVar = isVar;
        }

        /// <summary>
        /// Gets the prefix of the designator, e.g. for "married(john, ?a)", "married" is the prefix.
        /// </summary>
        /// <returns>Mapped ID of the designator prefix.</returns>
        public int GetPrefixID()
        {
            return ID;
        }

        /// <summary>
        /// Gets the specified parameter of the designator, e.g. the 1st parameter for "married(john, ?a)" is "john".
        /// </summary>
        /// <param name="idx">Index of the requested parameter. Should be checked on getParamCount().</param>
        /// <returns>Mapped ID of the designator parameter.</returns>
        public int GetParam(int idx)
        {
            return paramIDs[idx];
        }

        /// <summary>
        /// Gets the number of designator parameters. Can be used for iteration over all parameters.
        /// </summary>
        /// <returns>The number of designator parameters.</returns>
        public int GetParamCount()
        {
            return paramIDs.Length;
        }

        /// <summary>
        /// Checks whether the specified parameter is lifted (i.e. it's a variable), or grounded (i.e. it's a constant).
        /// E.g. the 1st parameter of "married(john, ?a)" is constant and the 2nd is variable.
        /// </summary>
        /// <param name="idx">Index of the requested parameter. Should be checked on getParamCount().</param>
        /// <returns>True if requested parameter is variable (lifted). False otherwise.</returns>
        public bool IsParamVar(int idx)
        {
            if (isVar == null)
                return false;
            return isVar[idx];
        }

        /// <summary>
        /// Does a substitution (grounding process) of the requested parameter. Should be checked on isParamVar(idx).
        /// For the resulting designator, isParamVar(idx) will be false.
        /// </summary>
        /// <param name="idx">Index of the requested parameter. Should be checked on getParamCount().</param>
        /// <param name="substit">Substitution in the context of PDDL operator (i.e. mapping parameter variables to constants).</param>
        public void SubstituteParam(int idx, PDDLOperatorSubstitution substit)
        {
            int varID = GetParam(idx);
            paramIDs[idx] = substit.GetValue(varID);
        }

        /// <summary>
        /// Clones the current designator object.
        /// </summary>
        /// <returns>A deep copy of the current designator object.</returns>
        public IPDDLDesignator Clone()
        {
            int[] clonedParamIDs = new int[paramIDs.Length];
            paramIDs.CopyTo(clonedParamIDs, 0);

            bool[] clonedIsVar = null;
            if (isVar != null)
            {
                clonedIsVar = new bool[isVar.Length];
                isVar.CopyTo(clonedIsVar, 0);
            }

            return new PDDLDesignator(ID, clonedParamIDs, clonedIsVar);
        }

        /// <summary>
        /// Constructs a hash code used in the dictionaries, maps etc.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return 17 * ID.GetHashCode() + ArrayEqualityComparer.comparer.GetHashCode(paramIDs);
        }

        /// <summary>
        /// Checks the equality of objects.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            PDDLDesignator other = obj as PDDLDesignator;
            if (other == null)
                return false;
            if (ID != other.ID)
                return false;
            return ArrayEqualityComparer.comparer.Equals(paramIDs, other.paramIDs);
        }
    }
}
