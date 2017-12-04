using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// List of error identificators returned by PDDLProblemLoaderException.
    /// </summary>
    public enum PDDLErrorID
    {
        FileNotStartWithDefineBlock,
        FileHasMoreDefineBlocks,
        DefineDomainNameMissing,
        DefineDomainMultipleDomainNames,
        DefineDomainDuplicateSegments,
        DefineDomainNoAction,
        DefineProblemNameMissing,
        DefineProblemMultipleProblemNames,
        DefineProblemDuplicateSegments,
        DefineProblemMissingMappedDomain,
        DefineProblemMissingInit,
        DefineProblemMissingGoal,
        FileHasBadBracketing,
        InvalidDomainBlock,
        DomainProblemMismatch,
        InvalidProblemBlock,
        UnsupportedRequirement,
        MissingReqTypingForTypesDef,
        DuplicatePredicateDefined,
        DuplicateFunctionDefined,
        ActionMissingParameters,
        ActionMissingPreconditions,
        ActionMissingEffects,
        ActionEmptyParameters,
        ActionEmptyPreconditions,
        ActionEmptyEffects,
        TypedListInvalidVariable,
        TypedListMissingTypingReq,
        TypedListInvalidTyping,
        TypedListInvalidConstant,
        TypedListUnknownType,
        TypedListMissingTyping,
        MissingReqNegativePrecond,
        MissingReqDisjunctivePrecond,
        MissingReqConditionalEffects,
        MissingReqUniversalPrecond,
        MissingReqExistentialPrecond,
        MissingReqEquality,
        MissingReqObjFluentsForAssignOp,
        MissingReqNumFluentsForEqOp,
        MissingReqNumFluentsForAssignOp,
        MissingReqNumFluentsForRelOp,
        MissingReqNumFluentsForIncrDecrOp,
        ExprEqualsHasMixOfNumAndNonNumExprs,
        ExprAssignOpHasMixOfNumAndNonNumExprs,
        ExprRelationalOpHasNonNumericArgs,
        ExprIncrDecrOpHasNonNumericArgs,
        ExprEqualsParamsTypeMismatch,
        ExprAssignOpParamsTypeMismatch,
        MissingReqNumFluentsForFuncDef,
        MissingReqObjFluentsForFuncDef,
        ExprLogicalOperatorArgsCount,
        ExprNotOperatorArgsCount,
        ExprImplyOperatorArgsCount,
        ExprWhenOperatorArgsCount,
        ExprQuantifiersArgsCount,
        ExprVariableNameDuplicit,
        ExprUndefinedPredicate,
        ExprUndefinedFunction,
        ExprPredicateArgsCount,
        ExprFunctionArgsCount,
        ExprPredicateUnknownVariable,
        ExprFunctionUnknownVariable,
        ExprPredicateUnknownConstant,
        ExprFunctionUnknownConstant,
        ExprPredicateArgTypeMismatch,
        ExprFunctionArgTypeMismatch,
        ExprEqualsArgsCount,
        ExprAssignOpArgsCount,
        ExprIncrDecrOpArgsCount,
        ExprAssignOpFirstArgFunc,
        ExprIncrDecrOpFirstArgFunc,
        ExprRelationalOpArgsCount,
        ExprInvalidVariableOutsidePredicate,
        ExprInitCanHaveOnlyPredicates,
        ExprInitCannotHaveLiftedParams,
        ExprInitFluentAssignNotStartWithFunc,
        ExprInitFluentAssignValueIsNotConst,
        ExprInitFluentAssignParamsCount,
        ExprInitFluentAssignUnknownConstant,
        ExprInitFluentAssignReturnType,
        ExprEffectMultiLevelAndOperator,
        ExprEffectOrOperator,
        ExprEffectImplyOperator,
        ExprEffectNotOperator,
        ExprEffectWhenOperatorInCondEff,
        ExprPrecondWhenOperator,
        ExprEffectExistsOperator,
        ExprEffectForallOperatorReqMissing,
        ExprEffectForallOperatorInCondEff,
        ExprEqualsInEffect,
        ExprAssignOpInPrecond,
        ExprIncrDecrOpInPrecond,
        ExprRelationalOpInEffect,
        ExprCannotBeListOfExprs,
        ExprInvalidToken,
        InvalidNameForType,
        InvalidNameForPredicate,
        InvalidNameForConstant,
        InvalidNameForVariable,
        InvalidNameForFunction,
        PredicateNameUsedForFunction,
        FunctionNameUsedForPredicate,
        InvalidTypeRedefinitionObject,
        InvalidTypeRedefinitionNumber,
        InvalidTypeRedefinition,
        PredOrFuncNameIsKeyword,
        ActionCostsInvalidFunctionSpec,
        ActionCostsCanOnlyIncrease,
        ActionCostsCannotBeInPrecond,
        ActionCostsNotInitedToZero,
        ExprArithmeticBinaryOpArgsCount,
        ExprArithmeticMultiOpArgsCount,
        ExprArithmeticMinusOpArgsCount,
    };

    /// <summary>
    /// Exceptions returned by PDDLProblemLoader class. Represent violations to the PDDL domain/problem correctness.
    /// </summary>
    public class PDDLProblemLoaderException : Exception
    {
        /// <summary>
        /// Translates an error identifier to a corresponding string representation.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <param name="strParams">Additional string parameters.</param>
        /// <returns>String representation of the exception.</returns>
        private static string ErrorIDToMessage(PDDLErrorID errorID, string[] strParams)
        {
            string errMsg = ErrorIDToMessage(errorID);
            if (strParams != null)
                return String.Format(errMsg, strParams);
            return errMsg;
        }

        /// <summary>
        /// Translates an error identifier to a corresponding string representation.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <returns>String representation of the exception.</returns>
        private static string ErrorIDToMessage(PDDLErrorID errorID)
        {
            switch (errorID)
            {
                case PDDLErrorID.FileNotStartWithDefineBlock:           return "PDDL file needs to begin with a 'define' block.";
                case PDDLErrorID.FileHasMoreDefineBlocks:               return "PDDL {0} file can contain only a single PDDL {0} description.";
                case PDDLErrorID.DefineDomainNameMissing:               return "PDDL domain's define block has to start with a domain name specified.";
                case PDDLErrorID.DefineDomainMultipleDomainNames:       return "PDDL domain's define block cannot have multiple names specified.";
                case PDDLErrorID.DefineDomainDuplicateSegments:         return "PDDL domain's define block contains multiple '{0}' blocks.";
                case PDDLErrorID.DefineDomainNoAction:                  return "PDDL domain's define block contains no actions specified.";
                case PDDLErrorID.DefineProblemNameMissing:              return "PDDL problem's define block has to start with a problem name specified.";
                case PDDLErrorID.DefineProblemMultipleProblemNames:     return "PDDL problem's define block cannot have multiple names specified.";
                case PDDLErrorID.DefineProblemDuplicateSegments:        return "PDDL problem's define block contains multiple '{0}' blocks.";
                case PDDLErrorID.DefineProblemMissingMappedDomain:      return "PDDL problem's define block has to have mapped domain specified.";
                case PDDLErrorID.DefineProblemMissingInit:              return "PDDL problem's define block has to have initial state specified.";
                case PDDLErrorID.DefineProblemMissingGoal:              return "PDDL problem's define block has to have goal conditions specified.";
                case PDDLErrorID.FileHasBadBracketing:                  return "Error while reading PDDL file (bad bracketing?).";
                case PDDLErrorID.InvalidDomainBlock:                    return "Invalid block '{0}' as a part of PDDL domain definition.";
                case PDDLErrorID.DomainProblemMismatch:                 return "PlanningProblem name for the PDDL problem doesn't match the defined domain.";
                case PDDLErrorID.InvalidProblemBlock:                   return "Invalid block '{0}' as a part of PDDL problem definition.";
                case PDDLErrorID.UnsupportedRequirement:                return "Specified requirement '{0}' for the PDDL domain is not supported.";
                case PDDLErrorID.MissingReqTypingForTypesDef:           return "PDDL domain contains types definition, but it doesn't contain ':typing' requirement.";
                case PDDLErrorID.DuplicatePredicateDefined:             return "Duplicated predicate '{0}' specified in a domain definition.";
                case PDDLErrorID.DuplicateFunctionDefined:              return "Duplicated function '{0}' specified in a domain definition.";
                case PDDLErrorID.ActionMissingParameters:               return "An action definition needs to start with ':parameters' clause.";
                case PDDLErrorID.ActionMissingPreconditions:            return "An action needs to have ':precondition' clause specified before effects specification.";
                case PDDLErrorID.ActionMissingEffects:                  return "An action needs to have ':effect' clause specified after preconditions specification.";
                case PDDLErrorID.ActionEmptyParameters:                 return "An action cannot have empty ':parameters' clause (use '()' instead).";
                case PDDLErrorID.ActionEmptyPreconditions:              return "An action cannot have empty ':precondition' clause (use '()' instead).";
                case PDDLErrorID.ActionEmptyEffects:                    return "An action cannot have empty ':effect' clause (use '()' instead).";
                case PDDLErrorID.TypedListInvalidVariable:              return "Invalid variable token in the list.";
                case PDDLErrorID.TypedListMissingTypingReq:             return "Invalid use of a typing token (PDDL domain doesn't contain ':typing' requirement).";
                case PDDLErrorID.TypedListInvalidTyping:                return "Invalid use of a typing token.";
                case PDDLErrorID.TypedListInvalidConstant:              return "Invalid use of a non-variable token '{0}'.";
                case PDDLErrorID.TypedListUnknownType:                  return "Invalid use of an unknown type '{0}'.";
                case PDDLErrorID.TypedListMissingTyping:                return "Missing (some) types for the list (PDDL domain contains ':typing' requirement).";
                case PDDLErrorID.MissingReqNegativePrecond:             return "Operator 'not' used, but ':negative-preconditions' requirement not specified.";
                case PDDLErrorID.MissingReqDisjunctivePrecond:          return "Operator 'or'/'imply' used, but ':disjunctive-preconditions' requirement not specified.";
                case PDDLErrorID.MissingReqConditionalEffects:          return "Conditional effect used, but ':conditional-effects' requirement not specified.";
                case PDDLErrorID.MissingReqUniversalPrecond:            return "Universal quantifier used, but ':universal-preconditions' requirement not specified.";
                case PDDLErrorID.MissingReqExistentialPrecond:          return "Existential quantifier used, but ':existential-preconditions' requirement not specified.";
                case PDDLErrorID.MissingReqEquality:                    return "Equals operator ('=') used, but ':equality' requirement not specified.";
                case PDDLErrorID.MissingReqObjFluentsForAssignOp:       return "Operator 'assign' with object arguments used, but ':object-fluents' requirement not specified.";
                case PDDLErrorID.MissingReqNumFluentsForEqOp:           return "Equals operator ('=') used with numeric expressions, but ':numeric-fluents' requirement not specified. ";
                case PDDLErrorID.MissingReqNumFluentsForAssignOp:       return "Operator 'assign' used with numeric expressions, but ':numeric-fluents' requirement not specified. ";
                case PDDLErrorID.MissingReqNumFluentsForRelOp:          return "Relational operators used, but ':numeric-fluents' requirement not specified.";
                case PDDLErrorID.MissingReqNumFluentsForIncrDecrOp:     return "Operator '{0}' used, but ':numeric-fluents' requirement not specified.";
                case PDDLErrorID.ExprEqualsHasMixOfNumAndNonNumExprs:   return "Equals operator ('=') uses an invalid mix of numeric and non-numeric parameters.";
                case PDDLErrorID.ExprAssignOpHasMixOfNumAndNonNumExprs: return "Operator 'assign' uses an invalid mix of numeric and non-numeric parameters.";
                case PDDLErrorID.ExprRelationalOpHasNonNumericArgs:     return "Relational operator ('{0}') uses non-numeric parameter(s).";
                case PDDLErrorID.ExprIncrDecrOpHasNonNumericArgs:       return "Operator '{0}' uses non-numeric parameter(s).";
                case PDDLErrorID.ExprEqualsParamsTypeMismatch:          return "Equals operator ('=') uses parameters with different types.";
                case PDDLErrorID.ExprAssignOpParamsTypeMismatch:        return "Operator 'assign' uses parameters with different types.";
                case PDDLErrorID.MissingReqNumFluentsForFuncDef:        return "Function with numeric return type defined, but ':numeric-fluents' requirement not specified.";
                case PDDLErrorID.MissingReqObjFluentsForFuncDef:        return "Function with object return type defined, but ':object-fluents' requirement not specified.";
                case PDDLErrorID.ExprLogicalOperatorArgsCount:          return "Logical operator with no parameters ({0}) used in an expression.";
                case PDDLErrorID.ExprNotOperatorArgsCount:              return "Logical operator 'NOT' has to have a single parameter defined.";
                case PDDLErrorID.ExprImplyOperatorArgsCount:            return "Logical operator 'IMPLY' has to have exactly two parameters defined.";
                case PDDLErrorID.ExprWhenOperatorArgsCount:             return "Conditional operator 'WHEN' has to have exactly two parameters defined (preconditions and effects).";
                case PDDLErrorID.ExprQuantifiersArgsCount:              return "Universal/existential quantifier has to have exactly two parameters (an arguments list and an expression to be applied).";
                case PDDLErrorID.ExprVariableNameDuplicit:              return "Multiple use of the same variable name ({0}).";
                case PDDLErrorID.ExprUndefinedPredicate:                return "Usage of an undefined predicate '{0}'.";
                case PDDLErrorID.ExprUndefinedFunction:                 return "Usage of an undefined function '{0}'.";
                case PDDLErrorID.ExprPredicateArgsCount:                return "Number of parameters for the predicate '{0}' doesn't match with the specification in ':predicates' section.";
                case PDDLErrorID.ExprFunctionArgsCount:                 return "Number of parameters for the function '{0}' doesn't match with the specification in ':functions' section.";
                case PDDLErrorID.ExprPredicateUnknownVariable:          return "Predicate '{0}' uses unknown variable '{1}'.";
                case PDDLErrorID.ExprFunctionUnknownVariable:           return "Function '{0}' uses unknown variable '{1}'.";
                case PDDLErrorID.ExprPredicateUnknownConstant:          return "Predicate '{0}' uses unknown constant '{1}'.";
                case PDDLErrorID.ExprFunctionUnknownConstant:           return "Function '{0}' uses unknown constant '{1}'.";
                case PDDLErrorID.ExprPredicateArgTypeMismatch:          return "Parameter '{1}' of predicate '{0}' doesn't match the required type. ";
                case PDDLErrorID.ExprFunctionArgTypeMismatch:           return "Parameter '{1}' of function '{0}' doesn't match the required type. ";
                case PDDLErrorID.ExprEqualsArgsCount:                   return "Equals operator ('=') has to have exactly two parameters defined.";
                case PDDLErrorID.ExprAssignOpArgsCount:                 return "Operator 'assign' has to have exactly two parameters defined.";
                case PDDLErrorID.ExprIncrDecrOpArgsCount:               return "Operator '{0}' has to have exactly two parameters defined.";
                case PDDLErrorID.ExprAssignOpFirstArgFunc:              return "First argument of the operator 'assign' has to be a function.";
                case PDDLErrorID.ExprIncrDecrOpFirstArgFunc:            return "First argument of the operator '{0}' has to be a function.";
                case PDDLErrorID.ExprRelationalOpArgsCount:             return "Relational operator ('{0}') has to have exactly two parameters defined.";
                case PDDLErrorID.ExprInvalidVariableOutsidePredicate:   return "Invalid use of variable '{0}' in an expression (variables can refer only to predicate or function parameters).";
                case PDDLErrorID.ExprInitCanHaveOnlyPredicates:         return "Only predicates or fluents assignments can be part of ':init' block.";
                case PDDLErrorID.ExprInitCannotHaveLiftedParams:        return "Lifted parameters ('{0}') cannot be part of ':init' block.";
                case PDDLErrorID.ExprInitFluentAssignNotStartWithFunc:  return "Function assignment with '=' operator in ':init' block has to start with a function.";
                case PDDLErrorID.ExprInitFluentAssignValueIsNotConst:   return "Only constant values can be assigned to a function within ':init' block.";
                case PDDLErrorID.ExprInitFluentAssignParamsCount:       return "Function assignment with '=' operator in ':init' block has to have exactly two parameters (function and assigned value).";
                case PDDLErrorID.ExprInitFluentAssignUnknownConstant:   return "Unknown constant '{0}' used for function assignment in ':init' block.";
                case PDDLErrorID.ExprInitFluentAssignReturnType:        return "Function assignment value '{0}' doesn't match return type of the function '{1}'.";
                case PDDLErrorID.ExprEffectMultiLevelAndOperator:       return "Only single-level 'and' operator is allowed in action effects.";
                case PDDLErrorID.ExprEffectOrOperator:                  return "Operator 'or' cannot be part of action effects.";
                case PDDLErrorID.ExprEffectImplyOperator:               return "Operator 'imply' cannot be part of action effects.";
                case PDDLErrorID.ExprEffectNotOperator:                 return "Operator 'not' is allowed in action effects only as an atomic effect.";
                case PDDLErrorID.ExprEffectWhenOperatorInCondEff:       return "Operator 'when' cannot be a part of another conditional effect.";
                case PDDLErrorID.ExprPrecondWhenOperator:               return "Conditional effects cannot be part of action preconditions or goal conditions.";
                case PDDLErrorID.ExprEffectExistsOperator:              return "Operator 'exists' cannot be part of action effects.";
                case PDDLErrorID.ExprEffectForallOperatorReqMissing:    return "Operator 'forall' can be a part of action effects only if ':conditional-effects' requirement is specified.";
                case PDDLErrorID.ExprEffectForallOperatorInCondEff:     return "Operator 'forall' cannot be a part of a conditional effect (result of 'when' operator).";
                case PDDLErrorID.ExprEqualsInEffect:                    return "Equality operator '=' cannot be a part of action effects.";
                case PDDLErrorID.ExprAssignOpInPrecond:                 return "Operator 'assign' cannot be a part of action effects.";
                case PDDLErrorID.ExprIncrDecrOpInPrecond:               return "Operator '{0}' cannot be a part of action effects.";
                case PDDLErrorID.ExprRelationalOpInEffect:              return "Relational operator '{0}' cannot be a part of action effects.";
                case PDDLErrorID.ExprCannotBeListOfExprs:               return "List of expressions is not a valid expression (missing preceding operator?).";
                case PDDLErrorID.ExprInvalidToken:                      return "Invalid expression token '{0}'.";
                case PDDLErrorID.InvalidNameForType:                    return "Invalid name '{0}' for a type (use letters, numbers, '-', '_').";
                case PDDLErrorID.InvalidNameForPredicate:               return "Invalid name '{0}' for a predicate (use letters, numbers, '-', '_').";
                case PDDLErrorID.InvalidNameForConstant:                return "Invalid name '{0}' for a constant (use letters, numbers, '-', '_').";
                case PDDLErrorID.InvalidNameForVariable:                return "Invalid name '{0}' for a variable (use letters, numbers, '-', '_').";
                case PDDLErrorID.InvalidNameForFunction:                return "Invalid name '{0}' for a function (use letters, numbers, '-', '_').";
                case PDDLErrorID.PredicateNameUsedForFunction:          return "Predicate name '{0}' is already used for a function.";
                case PDDLErrorID.FunctionNameUsedForPredicate:          return "Function name '{0}' is already used for a predicate.";
                case PDDLErrorID.InvalidTypeRedefinitionObject:         return "Type 'object' is a default base type and cannot be redefined.";
                case PDDLErrorID.InvalidTypeRedefinitionNumber:         return "Type 'number' is a default type for numeric fluents and cannot be redefined when :numeric-fluents are used in the domain.";
                case PDDLErrorID.InvalidTypeRedefinition:               return "Type '{0}' is defined multiple times (or used incorrectly in type hierarchy).";
                case PDDLErrorID.PredOrFuncNameIsKeyword:               return "Name '{0}' used for predicate/function is a reserved keyword and cannot be used.";
                case PDDLErrorID.ActionCostsInvalidFunctionSpec:        return "If ':action-costs' requirement is specified, function called 'total-cost' has to be zero-arity numeric function.";
                case PDDLErrorID.ActionCostsCanOnlyIncrease:            return "Action cost function can only be used with 'increase' operator in action effects.";
                case PDDLErrorID.ActionCostsCannotBeInPrecond:          return "Action cost function cannot be used in action preconditions or goal conditions.";
                case PDDLErrorID.ActionCostsNotInitedToZero:            return "Action cost function has to be initialized to zero in ':init' block.";
                case PDDLErrorID.ExprArithmeticBinaryOpArgsCount:       return "Arithmetic operator '{0}' has to have exactly two parameters.";
                case PDDLErrorID.ExprArithmeticMultiOpArgsCount:        return "Arithmetic operator '{0}' has to have at least two parameters.";
                case PDDLErrorID.ExprArithmeticMinusOpArgsCount:        return "Arithmetic operator '-' has to have either one, or two parameters.";

                default:
                    Debug.Assert(false);
                    return "";
            }
        }

        /// <summary>
        /// Exception constructor.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        public PDDLProblemLoaderException(PDDLErrorID errorID) : this(errorID, (string[])null)
        {
        }

        /// <summary>
        /// Exception constructor with an additional string parameter specified by a caller.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <param name="errParam1">Additional string parameter.</param>
        public PDDLProblemLoaderException(PDDLErrorID errorID, string errParam1) : this(errorID, new string[] { errParam1 })
        {
        }

        /// <summary>
        /// Exception constructor with additional string parameters specified by a caller.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <param name="errParam1">Additional string parameter.</param>
        /// <param name="errParam2">Additional string parameter.</param>
        public PDDLProblemLoaderException(PDDLErrorID errorID, string errParam1, string errParam2) : this(errorID, new string[] { errParam1, errParam2 })
        {
        }

        /// <summary>
        /// Exception constructor with additional string parameters specified by a caller.
        /// </summary>
        /// <param name="errorID">Error identifier.</param>
        /// <param name="errParams">Additional string parameter.</param>
        public PDDLProblemLoaderException(PDDLErrorID errorID, string[] errParams) : base(ErrorIDToMessage(errorID, errParams))
        {
            this.errorID = errorID;
        }

        /// <summary>
        /// Error identifier for the exception.
        /// </summary>
        public PDDLErrorID errorID;
    }
}
