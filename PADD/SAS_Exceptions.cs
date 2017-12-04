using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// List of error identificators returned by SASInputDataLoaderException.
    /// </summary>
    public enum SASErrorID
    {
        InvalidSectionStart,
        InvalidSectionEnd,
        UnsupportedVersion,
        UnsignedIntegerExpected,
        UnsignedIntegerOrMinusOneExpected,
        InvalidMetric,
        InvalidVariableDomRange,
        InvalidNumList,
        InvalidVaribleUsed,
        InvalidValueForVarible,
        DuplicateItemsInMutexGroup,
        InitStateInvalidToMutexes,
        VarCannotBeAffectedByOperator,
        VarCannotBeAffectedByAxiomRule,

        /// ...
    };

    /// <summary>
    /// Exceptions returned by SASProblemLoader class. Represent violations to the input SAS+ problem correctness.
    /// </summary>
    public class SASInputDataLoaderException : Exception
    {
        /// <summary>
        /// Translates an error identifier to a corresponding string representation.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <param name="lineNo">Line number of the input file.</param>
        /// <param name="strParams">Additional string parameters.</param>
        /// <returns>String representation of the exception.</returns>
        private static string ErrorIDToMessage(SASErrorID errorID, int lineNo, string[] strParams)
        {
            string errMsg = "Line " + lineNo + ": " + ErrorIDToMessage(errorID);
            if (strParams != null)
                return String.Format(errMsg, strParams);
            return errMsg;
        }

        /// <summary>
        /// Translates an error identifier to a corresponding string representation.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <returns>String representation of the exception.</returns>
        private static string ErrorIDToMessage(SASErrorID errorID)
        {
            switch (errorID)
            {
                case SASErrorID.InvalidSectionStart:                return "Section start '{0}' expected in the SAS+ input file, but '{1}' found.";
                case SASErrorID.InvalidSectionEnd:                  return "Section end '{0}' expected in the SAS+ input file, but '{1}' found.";
                case SASErrorID.UnsupportedVersion:                 return "Version {0} of the SAS+ input file is not supported.";
                case SASErrorID.UnsignedIntegerExpected:            return "Unsigned integer number expected, but '{0}' found in the SAS+ input file.";
                case SASErrorID.UnsignedIntegerOrMinusOneExpected:  return "Unsigned integer number (or -1) expected, but '{0}' found in the SAS+ input file.";
                case SASErrorID.InvalidMetric:                      return "Metric section can only contain values 0 or 1.";
                case SASErrorID.InvalidVariableDomRange:            return "Domain range of a variable cannot be empty.";
                case SASErrorID.InvalidNumList:                     return "Expected list of unsigned integer numbers of length {0}, but '{1}' found.";
                case SASErrorID.InvalidVaribleUsed:                 return "Variable '{0}' was not defined.";
                case SASErrorID.InvalidValueForVarible:             return "Value '{0}' is not within defined domain range of variable '{1}'.";
                case SASErrorID.DuplicateItemsInMutexGroup:         return "Mutex group {0} contains duplicated mutex item ({1},{2}).";
                case SASErrorID.InitStateInvalidToMutexes:          return "Initial state doesn't comply with defined mutex groups constraints.";
                case SASErrorID.VarCannotBeAffectedByOperator:      return "Variable {0} cannot be affected by an operator (it has defined axiom layer).";
                case SASErrorID.VarCannotBeAffectedByAxiomRule:     return "Variable {0} cannot be affected by an axiom rule (it hasn't defined axiom layer).";
                /// ...

                default:
                    Debug.Assert(false);
                    return "";
            }
        }

        /// <summary>
        /// Exception constructor.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <param name="lineNo">Line number of the input file.</param>
        public SASInputDataLoaderException(SASErrorID errorID, int lineNo) : this(errorID, lineNo,(string[])null)
        {
        }

        /// <summary>
        /// Exception constructor with an additional string parameter specified by a caller.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <param name="lineNo">Line number of the input file.</param>
        /// <param name="errParam1">Additional string parameter.</param>
        public SASInputDataLoaderException(SASErrorID errorID, int lineNo, string errParam1) : this(errorID, lineNo, new string[] { errParam1 })
        {
        }

        /// <summary>
        /// Exception constructor with additional string parameters specified by a caller.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <param name="lineNo">Line number of the input file.</param>
        /// <param name="errParam1">Additional string parameter.</param>
        /// <param name="errParam2">Additional string parameter.</param>
        public SASInputDataLoaderException(SASErrorID errorID, int lineNo, string errParam1, string errParam2) : this(errorID, lineNo, new string[] { errParam1, errParam2 })
        {
        }

        /// <summary>
        /// Exception constructor with additional string parameters specified by a caller.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <param name="lineNo">Line number of the input file.</param>
        /// <param name="errParams">Additional string parameter.</param>
        public SASInputDataLoaderException(SASErrorID errorID, int lineNo, string[] errParams) : base(ErrorIDToMessage(errorID, lineNo, errParams))
        {
            this.errorID = errorID;
        }

        /// <summary>
        /// Error identifier for the exception.
        /// </summary>
        public SASErrorID errorID;
    }
}
