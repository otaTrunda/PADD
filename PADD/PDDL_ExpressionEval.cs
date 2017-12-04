using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Main expression evaluation class. Provides static methods for the evalution of PDDL logical or arithmetic expressions.
    /// </summary>
    public static class PDDLExpressionEval
    {
        /// <summary>
        /// Instance of the condition evaluation visitor.
        /// </summary>
        private static ConditionEvalVisitor conditionEvalVisitor = new ConditionEvalVisitor();

        /// <summary>
        /// Instance of fulfilled conditions count visitor.
        /// </summary>
        private static FulfilledConditionsCountVisitor conditionsCountVisitor = new FulfilledConditionsCountVisitor();

        /// <summary>
        /// Evaluates PDDL logical expression.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="state">Reference state.</param>
        /// <param name="expr">Logical expression.</param>
        /// <returns>True if the expression evaluates as true, false otherwise.</returns>
        public static bool EvaluateLogicalExpression(PDDLProblem problem, IPDDLState state, IPDDLLogicalExpression expr)
        {
            return conditionEvalVisitor.Evaluate(problem, state, expr);
        }

        /// <summary>
        /// Evaluates PDDL logical expression.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="state">Reference state.</param>
        /// <param name="expr">Logical expression.</param>
        /// <param name="substit">Used substitution.</param>
        /// <returns>True if the expression evaluates as true, false otherwise.</returns>
        public static bool EvaluateLogicalExpression(PDDLProblem problem, IPDDLState state, IPDDLLogicalExpression expr, PDDLOperatorSubstitution substit)
        {
            return conditionEvalVisitor.Evaluate(problem, state, expr, substit);
        }

        /// <summary>
        /// Performs fulfilled and not-fulfilled conditions count on PDDL logical expression.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="state">Reference state.</param>
        /// <param name="expr">Logical expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        public static Tuple<int, int> CountFulfilledConditions(PDDLProblem problem, IPDDLState state, IPDDLLogicalExpression expr)
        {
            return conditionsCountVisitor.Evaluate(problem, state, expr);
        }

        /// <summary>
        /// Performs fulfilled and not-fulfilled conditions count on PDDL logical expression.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="state">Reference state.</param>
        /// <param name="expr">Logical expression.</param>
        /// <param name="substit">Used substitution.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        public static Tuple<int,int> CountFulfilledConditions(PDDLProblem problem, IPDDLState state, IPDDLLogicalExpression expr, PDDLOperatorSubstitution substit)
        {
            return conditionsCountVisitor.Evaluate(problem, state, expr, substit);
        }
    }

    /// <summary>
    /// Common interface for visitors evaluating PDDL logical expressions.
    /// </summary>
    public interface IExpressionEvalVisitor
    {
        /// <summary>
        /// Visits and evaluates predicate expression.
        /// </summary>
        /// <param name="expr">Predicate expression.</param>
        /// <returns>True if the specified expression evaluates as true, false otherwise.</returns>
        bool Visit(PredicateExpr expr);

        /// <summary>
        /// Visits and evaluates equals expression.
        /// </summary>
        /// <param name="expr">Equals expression.</param>
        /// <returns>True if the specified expression evaluates as true, false otherwise.</returns>
        bool Visit(EqualsExpr expr);

        /// <summary>
        /// Visits and evaluates exists expression.
        /// </summary>
        /// <param name="expr">Exists expression.</param>
        /// <returns>True if the specified expression evaluates as true, false otherwise.</returns>
        bool Visit(ExistsExpr expr);

        /// <summary>
        /// Visits and evaluates forall expression.
        /// </summary>
        /// <param name="expr">Forall expression.</param>
        /// <returns>True if the specified expression evaluates as true, false otherwise.</returns>
        bool Visit(ForallExpr expr);

        /// <summary>
        /// Visits and evaluates relational operator expression.
        /// </summary>
        /// <param name="expr">Relational operator expression.</param>
        /// <returns>True if the specified expression evaluates as true, false otherwise.</returns>
        bool Visit(RelationalOperatorExpr expr);
    }

    /// <summary>
    /// Conditional evaluation visitor. Evaluates PDDL logical expressions (e.g. operator preconditions, or goal conditions).
    /// </summary>
    public class ConditionEvalVisitor : IExpressionEvalVisitor
    {
        /// <summary>
        /// Parent PDDL planning problem.
        /// </summary>
        private PDDLProblem problem;

        /// <summary>
        /// Reference state.
        /// </summary>
        private IPDDLState refState;

        /// <summary>
        /// Currently used operator substitution.
        /// </summary>
        private PDDLOperatorSubstitution substit;

        /// <summary>
        /// Conditional evaluation visitor for inner (repeated) evaluations. Inited on-the-fly. Don't access directly,
        /// use getAuxCondEvalVisitorInstance() instead.
        /// </summary>
        private ConditionEvalVisitor auxCondEvalVisitor = null;

        /// <summary>
        /// Accessor for inner conditional evaluation visitor. Lazy initiated.
        /// </summary>
        /// <returns>Conditional evaluation visitor.</returns>
        private ConditionEvalVisitor GetAuxCondEvalVisitorInstance()
        {
            if (auxCondEvalVisitor == null)
                auxCondEvalVisitor = new ConditionEvalVisitor();
            return auxCondEvalVisitor;
        }

        /// <summary>
        /// Arithmetic expression evaluation visitor for inner (repeated) evaluations. Inited on-the-fly. Don't access directly,
        /// use getAuxArithmEvalVisitorInstance() instead.
        /// </summary>
        private ArithmeticExpressionEvalVisitor auxArithmEvalVisitor = null;

        /// <summary>
        /// Accessor for arithmetic expression evaluation visitor. Lazy initiated.
        /// </summary>
        /// <returns>Arithmetic expression evaluation visitor.</returns>
        private ArithmeticExpressionEvalVisitor GetAuxArithmEvalVisitorInstance()
        {
            if (auxArithmEvalVisitor == null)
                auxArithmEvalVisitor = new ArithmeticExpressionEvalVisitor();
            return auxArithmEvalVisitor;
        }

        /// <summary>
        /// List of substitutions for quantified sub-expressions. Used for the evaluation of forall and exists expressions.
        /// </summary>
        private List<PDDLOperatorSubstitution> quantifExprSubstitList;

        /// <summary>
        /// Accessor for list of substitutions used in quantified sub-expressions.
        /// </summary>
        /// <returns>List of substitutions used in quantififed sub-expressions.</returns>
        private List<PDDLOperatorSubstitution> GetQuantifExprSubstitListInstance()
        {
            if (quantifExprSubstitList == null)
                quantifExprSubstitList = new List<PDDLOperatorSubstitution>();
            return quantifExprSubstitList;
        }

        /// <summary>
        /// Constructs the conditional evaluation visitor.
        /// </summary>
        public ConditionEvalVisitor()
        {
        }

        /// <summary>
        /// Evaluates PDDL logical expression.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="refState">Reference state.</param>
        /// <param name="expr">Logical expression.</param>
        /// <returns>True if the expression evaluates as true, false otherwise.</returns>
        public bool Evaluate(PDDLProblem problem, IPDDLState refState, IPDDLLogicalExpression expr)
        {
            return Evaluate(problem, refState, expr, new PDDLOperatorSubstitution(new int[0]));
        }

        /// <summary>
        /// Evaluates PDDL logical expression.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="refState">Reference state.</param>
        /// <param name="expr">Logical expression.</param>
        /// <param name="substit">Used substitution.</param>
        /// <returns>True if the expression evaluates as true, false otherwise.</returns>
        public bool Evaluate(PDDLProblem problem, IPDDLState refState, IPDDLLogicalExpression expr, PDDLOperatorSubstitution substit)
        {
            this.problem = problem;
            this.refState = refState;
            this.substit = substit;
            return expr.Accept(this);
        }

        /// <summary>
        /// Visits and evaluates predicate expression.
        /// </summary>
        /// <param name="expr">Predicate expression.</param>
        /// <returns>True if the specified expression evaluates as true, false otherwise.</returns>
        public bool Visit(PredicateExpr expr)
        {
            IPDDLDesignator pred = PDDLOperatorSubstitution.MakeSubstituedDesignator(expr.predicate, substit);
            return (problem.GetRigidRelations().Contains(pred) || refState.HasPredicate(pred));
        }

        /// <summary>
        /// Visits and evaluates equals expression.
        /// </summary>
        /// <param name="expr">Equals expression.</param>
        /// <returns>True if the specified expression evaluates as true, false otherwise.</returns>
        public bool Visit(EqualsExpr expr)
        {
            if (expr.functions.Length == 2) // two functions
            {
                IPDDLDesignator func = PDDLOperatorSubstitution.MakeSubstituedDesignator(expr.functions[0], substit);
                int funcVal = refState.GetFunctionValue(func);

                IPDDLDesignator func2 = PDDLOperatorSubstitution.MakeSubstituedDesignator(expr.functions[1], substit);
                int funcVal2 = refState.GetFunctionValue(func2);

                return (funcVal == funcVal2);
            }
            else if (expr.functions.Length == 0) // two primitives (const/var)
            {
                IPDDLDesignator pred = PDDLOperatorSubstitution.MakeSubstituedDesignator(expr.predicate, substit);
                return (pred.GetParam(0) == pred.GetParam(1));
            }
            else // single function and single primitive
            {
                IPDDLDesignator func = PDDLOperatorSubstitution.MakeSubstituedDesignator(expr.functions[0], substit);
                int funcVal = refState.GetFunctionValue(func);

                IPDDLDesignator pred = PDDLOperatorSubstitution.MakeSubstituedDesignator(expr.predicate, substit);
                return (pred.GetParam(0) == funcVal);
            }
        }

        /// <summary>
        /// Visits and evaluates exists expression.
        /// </summary>
        /// <param name="expr">Exists expression.</param>
        /// <returns>True if the specified expression evaluates as true, false otherwise.</returns>
        public bool Visit(ExistsExpr expr)
        {
            var subExprSubstits = GetQuantifExprSubstitListInstance();
            QuantifierGrounder.GetAllExtendedSubstitutions(problem, substit, expr.varIDToTypeIDMapping, subExprSubstits);
            foreach (var sub in subExprSubstits)
            {
                if (GetAuxCondEvalVisitorInstance().Evaluate(problem, refState, expr.child, sub))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Visits and evaluates forall expression.
        /// </summary>
        /// <param name="expr">Forall expression.</param>
        /// <returns>True if the specified expression evaluates as true, false otherwise.</returns>
        public bool Visit(ForallExpr expr)
        {
            var subExprSubstits = GetQuantifExprSubstitListInstance();
            QuantifierGrounder.GetAllExtendedSubstitutions(problem, substit, expr.varIDToTypeIDMapping, subExprSubstits);
            foreach (var sub in subExprSubstits)
            {
                if (!GetAuxCondEvalVisitorInstance().Evaluate(problem, refState, expr.child, sub))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Visits and evaluates relational operator expression.
        /// </summary>
        /// <param name="expr">Relational operator expression.</param>
        /// <returns>True if the specified expression evaluates as true, false otherwise.</returns>
        public bool Visit(RelationalOperatorExpr expr)
        {
            int value1 = GetAuxArithmEvalVisitorInstance().Evaluate(expr.leftNumExpr, substit, refState);
            int value2 = GetAuxArithmEvalVisitorInstance().Evaluate(expr.rightNumExpr, substit, refState);
            return expr.ApplyOperation(value1, value2);
        }
    }

    /// <summary>
    /// Auxiliary class for grounding extra parameters of quantified sub-expressions.
    /// </summary>
    public static class QuantifierGrounder
    {
        /// <summary>
        /// Fills the return list with all possible extended substitutions for the quantified sub-expression (specified with the mapping of variable IDs to type IDs).
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="substit">Base operator substitution.</param>
        /// <param name="varIDToTypeIDMapping">Mapping variable IDs to type IDs.</param>
        /// <param name="retList">Return list to be filled.</param>
        public static void GetAllExtendedSubstitutions(PDDLProblem problem, PDDLOperatorSubstitution substit, Dictionary<int, int> varIDToTypeIDMapping, List<PDDLOperatorSubstitution> retList)
        {
            // create basic extended substitution
            int newSubstitLength = substit.GetVarCount() + varIDToTypeIDMapping.Count;
            int[] basicExtendedSubstitArr = new int[newSubstitLength];

            // copy original substitution
            for (int varID = 0; varID < substit.GetVarCount(); ++varID)
                basicExtendedSubstitArr[varID] = substit.GetValue(varID);

            // init values for the grounder
            QuantifierGrounder.problem = problem;
            QuantifierGrounder.retList = retList;
            QuantifierGrounder.retList.Clear();
            QuantifierGrounder.currSubstit = basicExtendedSubstitArr;
            QuantifierGrounder.mappingVarsToType = varIDToTypeIDMapping;

            // ground rest of the lifted params
            GetAllExtendedSubstitutions(substit.GetVarCount());
        }

        /// <summary>
        /// Parent planning problem.
        /// </summary>
        private static PDDLProblem problem;

        /// <summary>
        /// Return list of extended operator substitutions.
        /// </summary>
        private static List<PDDLOperatorSubstitution> retList;

        /// <summary>
        /// Currently processed substitution.
        /// </summary>
        private static int[] currSubstit;

        /// <summary>
        /// Mapping of variable ID to type IDs.
        /// </summary>
        private static Dictionary<int, int> mappingVarsToType;

        /// <summary>
        /// Fills the return list of all extended substitutions.
        /// </summary>
        /// <param name="startIdx">Index of parameter to start grounding from.</param>
        private static void GetAllExtendedSubstitutions(int startIdx)
        {
            DoGroundInputParam(startIdx);
        }

        /// <summary>
        /// Grounds the specified parameter of a current substitution.
        /// </summary>
        /// <param name="index">Parameter index.</param>
        private static void DoGroundInputParam(int index)
        {
            if (index >= currSubstit.Length) // last possible index, we have a substitution 
            {
                retList.Add(new PDDLOperatorSubstitution(currSubstit));
                return;
            }

            int typeID = mappingVarsToType[index];

            // try all constants (their IDs) by the current parameter type

            foreach (var constID in problem.GetIDManager().GetConstantsIDForType(typeID))
            {
                currSubstit[index] = constID;
                DoGroundInputParam(index + 1);
            }
        }
    }

    /// <summary>
    /// Common interface for visitors performing some property count on PDDL logical expression.
    /// </summary>
    public interface IExpressionPropCountVisitor
    {
        /// <summary>
        /// Visits and performs a property count on predicate expression.
        /// </summary>
        /// <param name="expr">Predicate expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        Tuple<int, int> Visit(PredicateExpr expr);

        /// <summary>
        /// Visits and performs a property count on equals expression.
        /// </summary>
        /// <param name="expr">Equals expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        Tuple<int, int> Visit(EqualsExpr expr);

        /// <summary>
        /// Visits and performs a property count on exists expression.
        /// </summary>
        /// <param name="expr">Exists expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        Tuple<int, int> Visit(ExistsExpr expr);

        /// <summary>
        /// Visits and performs a property count on forall expression.
        /// </summary>
        /// <param name="expr">Forall expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        Tuple<int, int> Visit(ForallExpr expr);

        /// <summary>
        /// Visits and performs a property count on relational operator expression.
        /// </summary>
        /// <param name="expr">Relational operator expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        Tuple<int, int> Visit(RelationalOperatorExpr expr);
    }

    /// <summary>
    /// Fulfilled/not-fulfilled conditions count visitor (e.g. (and (true) (true) (false)) has 2 fulfilled sub-expressions and 1 not-fulfilled).
    /// </summary>
    public class FulfilledConditionsCountVisitor : IExpressionPropCountVisitor
    {
        /// <summary>
        /// Parent PDDL planning problem.
        /// </summary>
        private PDDLProblem problem;

        /// <summary>
        /// Reference state.
        /// </summary>
        private IPDDLState refState;

        /// <summary>
        /// Currently used operator substitution.
        /// </summary>
        private PDDLOperatorSubstitution substit;

        /// <summary>
        /// Conditional evaluation visitor for inner (repeated) evaluations. Inited on-the-fly. Don't access directly,
        /// use getAuxCondEvalVisitorInstance() instead.
        /// </summary>
        private ConditionEvalVisitor auxCondEvalVisitor = null;

        /// <summary>
        /// Accessor for inner conditional evaluation visitor. Lazy initiated.
        /// </summary>
        /// <returns>Conditional evaluation visitor.</returns>
        private ConditionEvalVisitor GetAuxCondEvalVisitorInstance()
        {
            if (auxCondEvalVisitor == null)
                auxCondEvalVisitor = new ConditionEvalVisitor();
            return auxCondEvalVisitor;
        }

        /// <summary>
        /// Condition count visitor for inner (repeated) evaluations. Inited on-the-fly. Don't access directly,
        /// use getAuxCondCountVisitorInstance() instead.
        /// </summary>
        private FulfilledConditionsCountVisitor auxCondCountVisitor = null;

        /// <summary>
        /// Accessor for condition count visitor. Lazy initiated.
        /// </summary>
        /// <returns>Condition count visitor.</returns>
        private FulfilledConditionsCountVisitor GetAuxCondCountVisitorInstance()
        {
            if (auxCondCountVisitor == null)
                auxCondCountVisitor = new FulfilledConditionsCountVisitor();
            return auxCondCountVisitor;
        }

        /// <summary>
        /// List of substitutions for quantified sub-expressions. Used for the evaluation of forall and exists expressions.
        /// </summary>
        private List<PDDLOperatorSubstitution> quantifExprSubstitList;

        /// <summary>
        /// Accessor for list of substitutions used in quantified sub-expressions.
        /// </summary>
        /// <returns>List of substitutions used in quantififed sub-expressions.</returns>
        private List<PDDLOperatorSubstitution> GetQuantifExprSubstitListInstance()
        {
            if (quantifExprSubstitList == null)
                quantifExprSubstitList = new List<PDDLOperatorSubstitution>();
            return quantifExprSubstitList;
        }

        /// <summary>
        /// Constructs conditions count visitor.
        /// </summary>
        public FulfilledConditionsCountVisitor()
        {
        }

        /// <summary>
        /// Performs a property count on PDDL logical expression.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="refState">Reference state.</param>
        /// <param name="expr">Logical expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        public Tuple<int, int> Evaluate(PDDLProblem problem, IPDDLState refState, IPDDLLogicalExpression expr)
        {
            return Evaluate(problem, refState, expr, new PDDLOperatorSubstitution(new int[0]));
        }

        /// <summary>
        /// Performs a property count on PDDL logical expression.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="refState">Reference state.</param>
        /// <param name="expr">Logical expression.</param>
        /// <param name="substit">Used substitution.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        public Tuple<int, int> Evaluate(PDDLProblem problem, IPDDLState refState, IPDDLLogicalExpression expr, PDDLOperatorSubstitution substit)
        {
            this.problem = problem;
            this.refState = refState;
            this.substit = substit;
            return expr.Accept(this);
        }

        /// <summary>
        /// Visits and performs a property count on predicate expression.
        /// </summary>
        /// <param name="expr">Predicate expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        public Tuple<int, int> Visit(PredicateExpr expr)
        {
            return VisitPrimitiveExpr(expr);
        }

        /// <summary>
        /// Visits and performs a property count on equals expression.
        /// </summary>
        /// <param name="expr">Equals expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        public Tuple<int, int> Visit(EqualsExpr expr)
        {
            return VisitPrimitiveExpr(expr);
        }

        /// <summary>
        /// Visits and performs a property count on exists expression.
        /// </summary>
        /// <param name="expr">Exists expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        public Tuple<int, int> Visit(ExistsExpr expr)
        {
            int minFulfilled = int.MaxValue;
            int minNotFulfilled = int.MaxValue;

            var subExprSubstits = GetQuantifExprSubstitListInstance();
            QuantifierGrounder.GetAllExtendedSubstitutions(problem, substit, expr.varIDToTypeIDMapping, subExprSubstits);
            foreach (var sub in subExprSubstits)
            {
                var childPropertyCounts = GetAuxCondCountVisitorInstance().Evaluate(problem, refState, expr.child, sub);
                minFulfilled = Math.Min(minFulfilled, childPropertyCounts.Item1);
                minNotFulfilled = Math.Min(minNotFulfilled, childPropertyCounts.Item2);
            }

            return Tuple.Create(minFulfilled, minNotFulfilled);
        }

        /// <summary>
        /// Visits and performs a property count on forall expression.
        /// </summary>
        /// <param name="expr">Forall expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        public Tuple<int, int> Visit(ForallExpr expr)
        {
            int fulfilled = 0;
            int notFulfilled = 0;

            var subExprSubstits = GetQuantifExprSubstitListInstance();
            QuantifierGrounder.GetAllExtendedSubstitutions(problem, substit, expr.varIDToTypeIDMapping, subExprSubstits);
            foreach (var sub in subExprSubstits)
            {
                var childPropertyCounts = GetAuxCondCountVisitorInstance().Evaluate(problem, refState, expr.child, sub);
                fulfilled += childPropertyCounts.Item1;
                notFulfilled += childPropertyCounts.Item2;
            }

            return Tuple.Create(fulfilled, notFulfilled);
        }

        /// <summary>
        /// Visits and performs a property count on relational operator expression.
        /// </summary>
        /// <param name="expr">Relational operator expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        public Tuple<int, int> Visit(RelationalOperatorExpr expr)
        {
            return VisitPrimitiveExpr(expr);
        }

        /// <summary>
        /// Visits and performs a property count on a primitive (not quantififed) logical expression.
        /// </summary>
        /// <param name="expr">Logical expression.</param>
        /// <returns>Tuple (property satisfied count, property not satisfied count).</returns>
        private Tuple<int, int> VisitPrimitiveExpr(IPDDLLogicalExpression expr)
        {
            bool isFulfilled = GetAuxCondEvalVisitorInstance().Evaluate(problem, refState, expr, substit);
            return isFulfilled ? Tuple.Create(1, 0) : Tuple.Create(0, 1);
        }
    }

    /// <summary>
    /// Common interface for evaluating PDDL arithmetic expressions.
    /// </summary>
    public interface IArithmeticExpressionVisitor
    {
        /// <summary>
        /// Visits and evaluates numeric function expression.
        /// </summary>
        /// <param name="expr">Nueric function expression.</param>
        /// <returns>Evaluated numeric value.</returns>
        int Visit(FunctionExpr expr);
    }

    /// <summary>
    /// Arithmetic expression evaluation visitor.
    /// </summary>
    public class ArithmeticExpressionEvalVisitor : IArithmeticExpressionVisitor
    {
        /// <summary>
        /// Currently used operator substitution.
        /// </summary>
        private PDDLOperatorSubstitution substit;

        /// <summary>
        /// Reference state.
        /// </summary>
        private IPDDLState refState;

        /// <summary>
        /// Constructs arithmetic expression evaluation visitor.
        /// </summary>
        public ArithmeticExpressionEvalVisitor()
        {
        }

        /// <summary>
        /// Evaluates PDDL arithmetic expression.
        /// </summary>
        /// <param name="expr">Arithmetic expression.</param>
        /// <param name="substit">Used substitution.</param>
        /// <param name="refState">Reference state.</param>
        /// <returns>Evaluated numeric value.</returns>
        public int Evaluate(IPDDLArithmeticExpression expr, PDDLOperatorSubstitution substit, IPDDLState refState)
        {
            this.substit = substit;
            this.refState = refState;
            return expr.Accept(this);
        }

        /// <summary>
        /// Visits and evaluates numeric function expression.
        /// </summary>
        /// <param name="expr">Nueric function expression.</param>
        /// <returns>Evaluated numeric value.</returns>
        public int Visit(FunctionExpr expr)
        {
            IPDDLDesignator func = PDDLOperatorSubstitution.MakeSubstituedDesignator(expr.function, substit);
            return refState.GetFunctionValue(func);
        }
    }
}
