using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Common interface representing PDDL logical expressions.
    /// </summary>
    public interface IPDDLLogicalExpression
    {
        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        bool Accept(IExpressionEvalVisitor visitor);

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        Tuple<int, int> Accept(IExpressionPropCountVisitor visitor);
    }

    /// <summary>
    /// Logical expression - OR.
    /// </summary>
    public class OrExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Children expressions.
        /// </summary>
        public IPDDLLogicalExpression[] children;

        /// <summary>
        /// Constructs the OR expression.
        /// </summary>
        /// <param name="children">Arguments of the expression.</param>
        public OrExpr(IPDDLLogicalExpression[] children)
        {
            this.children = children;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            foreach (var child in children)
            {
                if (child.Accept(visitor))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            int minFulfilled = int.MaxValue;
            int minNotFulfilled = int.MaxValue;

            foreach (var child in children)
            {
                var childPropertyCounts = child.Accept(visitor);
                minFulfilled = Math.Min(minFulfilled, childPropertyCounts.Item1);
                minNotFulfilled = Math.Min(minNotFulfilled, childPropertyCounts.Item2);
            }

            return Tuple.Create(minFulfilled, minNotFulfilled);
        }
    }

    /// <summary>
    /// Logical expression - AND.
    /// </summary>
    public class AndExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Children expressions.
        /// </summary>
        public IPDDLLogicalExpression[] children;

        /// <summary>
        /// Constructs the AND expression.
        /// </summary>
        /// <param name="children">Arguments of the expression.</param>
        public AndExpr(IPDDLLogicalExpression[] children)
        {
            this.children = children;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            foreach (var child in children)
            {
                if (!child.Accept(visitor))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            int fulfilled = 0;
            int notFulfilled = 0;

            foreach (var child in children)
            {
                var childPropertyCounts = child.Accept(visitor);
                fulfilled += childPropertyCounts.Item1;
                notFulfilled += childPropertyCounts.Item2;
            }

            return Tuple.Create(fulfilled, notFulfilled);
        }
    }

    /// <summary>
    /// Logical expression - IMPLY.
    /// </summary>
    public class ImplyExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Left child expression.
        /// </summary>
        public IPDDLLogicalExpression leftChild;

        /// <summary>
        /// Right child expression.
        /// </summary>
        public IPDDLLogicalExpression rightChild;

        /// <summary>
        /// Constructs the IMPLY expression.
        /// </summary>
        /// <param name="leftChild">Left argument of the expression.</param>
        /// <param name="rightChild">Right argument of the expression.</param>
        public ImplyExpr(IPDDLLogicalExpression leftChild, IPDDLLogicalExpression rightChild)
        {
            this.leftChild = leftChild;
            this.rightChild = rightChild;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            // (a imply b) ~ (not(a) or b)
            return !leftChild.Accept(visitor) || rightChild.Accept(visitor);
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            var leftChildPropCounts = leftChild.Accept(visitor);
            var rightChildPropCounts = rightChild.Accept(visitor);

            // fulfilled: min(notFulfilled(a),fulfilled(b)); notFulfilled: min(fulfilled(a),notFulfilled(b))
            return Tuple.Create(Math.Min(leftChildPropCounts.Item2, rightChildPropCounts.Item1),
                Math.Min(leftChildPropCounts.Item1, rightChildPropCounts.Item2));
        }
    }

    /// <summary>
    /// Logical expression - NOT.
    /// </summary>
    public class NotExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Child expression to be negated.
        /// </summary>
        public IPDDLLogicalExpression child;

        /// <summary>
        /// Constructs the NOT expression.
        /// </summary>
        /// <param name="child">An argument of the expression.</param>
        public NotExpr(IPDDLLogicalExpression child)
        {
            this.child = child;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            return !child.Accept(visitor);
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            var childPropCounts = child.Accept(visitor);

            return Tuple.Create(childPropCounts.Item2, childPropCounts.Item1);
        }
    }

    /// <summary>
    /// Logical expression - FORALL.
    /// </summary>
    public class ForallExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Child expression.
        /// </summary>
        public IPDDLLogicalExpression child;

        /// <summary>
        /// Mapping variable used in the FORALL declaration to their coresponding types.
        /// </summary>
        public Dictionary<int, int> varIDToTypeIDMapping;

        /// <summary>
        /// Constructs the FORALL expression.
        /// </summary>
        /// <param name="child">An argument of the expression.</param>
        /// <param name="varIDToTypeIDMapping">Mapping variables to the corresponding types.</param>
        public ForallExpr(IPDDLLogicalExpression child, Dictionary<int, int> varIDToTypeIDMapping)
        {
            this.child = child;
            this.varIDToTypeIDMapping = varIDToTypeIDMapping;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            return visitor.Visit(this);
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }

    /// <summary>
    /// Logical expression - EXISTS.
    /// </summary>
    public class ExistsExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Child expression.
        /// </summary>
        public IPDDLLogicalExpression child;

        /// <summary>
        /// Mapping variable used in the EXISTS declaration to their coresponding types.
        /// </summary>
        public Dictionary<int, int> varIDToTypeIDMapping;

        /// <summary>
        /// Constructs the EXISTS expression.
        /// </summary>
        /// <param name="child">An argument of the expression.</param>
        /// <param name="varIDToTypeIDMapping">Mapping variables to the corresponding types.</param>
        public ExistsExpr(IPDDLLogicalExpression child, Dictionary<int, int> varIDToTypeIDMapping)
        {
            this.child = child;
            this.varIDToTypeIDMapping = varIDToTypeIDMapping;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            return visitor.Visit(this);
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }

    /// <summary>
    /// Logical expression - WHEN (conditional effect).
    /// </summary>
    public class WhenExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Conditional preconditions.
        /// </summary>
        public IPDDLLogicalExpression conditionalPreconds;

        /// <summary>
        /// Conditional effects.
        /// </summary>
        public IPDDLLogicalExpression conditionalEffects;

        /// <summary>
        /// Constructs the WHEN expression.
        /// </summary>
        /// <param name="conditionalPreconds">Conditional preconditions.</param>
        /// <param name="conditionalEffects">Conditional effects.</param>
        public WhenExpr(IPDDLLogicalExpression conditionalPreconds, IPDDLLogicalExpression conditionalEffects)
        {
            this.conditionalPreconds = conditionalPreconds;
            this.conditionalEffects = conditionalEffects;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            return false; // never directly evaluated
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            return Tuple.Create(0,0); // never directly evaluated
        }
    }

    /// <summary>
    /// Logical expression - predicate.
    /// </summary>
    public class PredicateExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Actual predicate object. Can be grounded or (partialy) lifted.
        /// </summary>
        public IPDDLDesignator predicate;

        /// <summary>
        /// Constructs the predicate expression.
        /// </summary>
        /// <param name="predicate">Predicate object.</param>
        public PredicateExpr(IPDDLDesignator predicate)
        {
            this.predicate = predicate;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            return visitor.Visit(this);
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }

    /// <summary>
    /// Logical expression - EQUALS (=).
    /// </summary>
    public class EqualsExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Predicate object.
        /// </summary>
        public IPDDLDesignator predicate;

        /// <summary>
        /// Function(s) data.
        /// </summary>
        public IPDDLDesignator[] functions;

        /// <summary>
        /// Constructs the EQUALS expression.
        /// </summary>
        /// <param name="predicate">Predicate object.</param>
        /// <param name="functions">Functions objects.</param>
        public EqualsExpr(IPDDLDesignator predicate, IPDDLDesignator[] functions)
        {
            this.predicate = predicate;
            this.functions = functions;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            return visitor.Visit(this);
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }

    /// <summary>
    /// Special type of logical expression - empty (e.g. empty goal conditions).
    /// </summary>
    public class EmptyExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            return true; // always true
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            return Tuple.Create(1, 0);
        }
    }

    /// <summary>
    /// Base class for the numeric fluents modifier expressions.
    /// </summary>
    public abstract class NumFluentModifExprBase : IPDDLLogicalExpression
    {
        /// <summary>
        /// Numeric function.
        /// </summary>
        public IPDDLDesignator function;

        /// <summary>
        /// A value to be assigned.
        /// </summary>
        public IPDDLArithmeticExpression assignment;

        /// <summary>
        /// Constructs the numeric fluents modifier expression.
        /// </summary>
        /// <param name="function">Numeric function.</param>
        /// <param name="assignment">A value to be assigned.</param>
        public NumFluentModifExprBase(IPDDLDesignator function, IPDDLArithmeticExpression assignment)
        {
            this.function = function;
            this.assignment = assignment;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            return false; // never directly evaluated
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            return Tuple.Create(0, 0); // never directly evaluated
        }
    }

    /// <summary>
    /// Numeric fluent modifier expression - INCREASE.
    /// </summary>
    public class IncreaseExpr : NumFluentModifExprBase
    {
        /// <summary>
        /// Constructs the INCREASE expression.
        /// </summary>
        /// <param name="function">Numeric function.</param>
        /// <param name="assignment">A value to be assigned.</param>
        public IncreaseExpr(IPDDLDesignator function, IPDDLArithmeticExpression assignment) : base(function, assignment)
        { }
    }

    /// <summary>
    /// Numeric fluent modifier expression - DECREASE.
    /// </summary>
    public class DecreaseExpr : NumFluentModifExprBase
    {
        /// <summary>
        /// Constructs the DECREASE expression.
        /// </summary>
        /// <param name="function">Numeric function.</param>
        /// <param name="assignment">A value to be assigned.</param>
        public DecreaseExpr(IPDDLDesignator function, IPDDLArithmeticExpression assignment) : base(function, assignment)
        { }
    }

    /// <summary>
    /// Numeric fluent modifier expression - ASSIGN.
    /// </summary>
    public class NumAssignExpr : NumFluentModifExprBase
    {
        /// <summary>
        /// Constructs the numeric ASSIGN expression.
        /// </summary>
        /// <param name="function">Numeric function.</param>
        /// <param name="assignment">A value to be assigned.</param>
        public NumAssignExpr(IPDDLDesignator function, IPDDLArithmeticExpression assignment) : base(function, assignment)
        { }
    }

    /// <summary>
    /// Object fluent modifier expression - ASSIGN.
    /// </summary>
    public class ObjAssignExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Object function.
        /// </summary>
        public IPDDLDesignator function;

        /// <summary>
        /// Assigned parameter.
        /// </summary>
        public int assignPar;

        /// <summary>
        /// Is assigned parameter a variable?
        /// </summary>
        public bool assignParIsVar;

        /// <summary>
        /// Assigned parameter as a function.
        /// </summary>
        public IPDDLDesignator assignedFunction;

        /// <summary>
        /// Constructs the object ASSIGN expression.
        /// </summary>
        /// <param name="function">Object function.</param>
        /// <param name="assignPar">A value to be assigned.</param>
        /// <param name="assignParIsVar">Is assigned parameter a variable?</param>
        public ObjAssignExpr(IPDDLDesignator function, int assignPar, bool assignParIsVar)
        {
            this.function = function;
            this.assignPar = assignPar;
            this.assignParIsVar = assignParIsVar;
            this.assignedFunction = null;
        }

        /// <summary>
        /// Constructs the object ASSIGN expression.
        /// </summary>
        /// <param name="function">Object function.</param>
        /// <param name="assignedFunction">A value to be assigned.</param>
        public ObjAssignExpr(IPDDLDesignator function, IPDDLDesignator assignedFunction)
        {
            this.function = function;
            this.assignedFunction = assignedFunction;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            return false; // never directly evaluated
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            return Tuple.Create(0, 0); // never directly evaluated
        }
    }

    /// <summary>
    /// Base class for the relational operator expressions.
    /// </summary>
    public abstract class RelationalOperatorExpr : IPDDLLogicalExpression
    {
        /// <summary>
        /// Left numeric expression.
        /// </summary>
        public IPDDLArithmeticExpression leftNumExpr;

        /// <summary>
        /// Right numeric expression.
        /// </summary>
        public IPDDLArithmeticExpression rightNumExpr;

        /// <summary>
        /// Constructs the relation operator expression.
        /// </summary>
        /// <param name="leftNumExpr">Left numeric expression.</param>
        /// <param name="rightNumExpr">Right numeric expression.</param>
        public RelationalOperatorExpr(IPDDLArithmeticExpression leftNumExpr, IPDDLArithmeticExpression rightNumExpr)
        {
            this.leftNumExpr = leftNumExpr;
            this.rightNumExpr = rightNumExpr;
        }

        /// <summary>
        /// Accepts a visitor evaluating the logical expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>True if the expression is logically true. False otherwise.</returns>
        public bool Accept(IExpressionEvalVisitor visitor)
        {
            return visitor.Visit(this);
        }

        /// <summary>
        /// Accepts a visitor counting some specific property of the logical expression.
        /// </summary>
        /// <param name="visitor">Property counting visitor.</param>
        /// <returns>Number of expression nodes fulfilling and non-fulfilling some condition.</returns>
        public Tuple<int, int> Accept(IExpressionPropCountVisitor visitor)
        {
            return visitor.Visit(this);
        }

        /// <summary>
        /// Applies the concrete relational operator to evaluate logical value.
        /// </summary>
        /// <param name="value1">Left value.</param>
        /// <param name="value2">Right value.</param>
        /// <returns>True if the concrete operation evaluates as true, false otherwise.</returns>
        public abstract bool ApplyOperation(int value1, int value2);
    }

    /// <summary>
    /// Logical expression - "equals" relational operator
    /// </summary>
    public class RelationalOperatorEqExpr : RelationalOperatorExpr
    {
        /// <summary>
        /// Constructs the "equals" relational expression.
        /// </summary>
        /// <param name="leftNumExpr">Left numeric expression.</param>
        /// <param name="rightNumExpr">Right numeric expression.</param>
        public RelationalOperatorEqExpr(IPDDLArithmeticExpression leftNumExpr, IPDDLArithmeticExpression rightNumExpr) : base(leftNumExpr, rightNumExpr)
        {
        }

        /// <summary>
        /// Applies the concrete relational operator to evaluate logical value.
        /// </summary>
        /// <param name="value1">Left value.</param>
        /// <param name="value2">Right value.</param>
        /// <returns>True if the concrete operation evaluates as true, false otherwise.</returns>
        public override bool ApplyOperation(int value1, int value2)
        {
            return value1 == value2;
        }
    }

    /// <summary>
    /// Logical expression - "lesser than" relational operator
    /// </summary>
    public class RelationalOperatorLTExpr : RelationalOperatorExpr
    {
        /// <summary>
        /// Constructs the "lesser than" relational expression.
        /// </summary>
        /// <param name="leftNumExpr">Left numeric expression.</param>
        /// <param name="rightNumExpr">Right numeric expression.</param>
        public RelationalOperatorLTExpr(IPDDLArithmeticExpression leftNumExpr, IPDDLArithmeticExpression rightNumExpr) : base(leftNumExpr, rightNumExpr)
        {
        }

        /// <summary>
        /// Applies the concrete relational operator to evaluate logical value.
        /// </summary>
        /// <param name="value1">Left value.</param>
        /// <param name="value2">Right value.</param>
        /// <returns>True if the concrete operation evaluates as true, false otherwise.</returns>
        public override bool ApplyOperation(int value1, int value2)
        {
            return value1 < value2;
        }
    }

    /// <summary>
    /// Logical expression - "lesser than or equal to" relational operator
    /// </summary>
    public class RelationalOperatorLTEExpr : RelationalOperatorExpr
    {
        /// <summary>
        /// Constructs the "lesser than or equal to" relational expression.
        /// </summary>
        /// <param name="leftNumExpr">Left numeric expression.</param>
        /// <param name="rightNumExpr">Right numeric expression.</param>
        public RelationalOperatorLTEExpr(IPDDLArithmeticExpression leftNumExpr, IPDDLArithmeticExpression rightNumExpr) : base(leftNumExpr, rightNumExpr)
        {
        }

        /// <summary>
        /// Applies the concrete relational operator to evaluate logical value.
        /// </summary>
        /// <param name="value1">Left value.</param>
        /// <param name="value2">Right value.</param>
        /// <returns>True if the concrete operation evaluates as true, false otherwise.</returns>
        public override bool ApplyOperation(int value1, int value2)
        {
            return value1 <= value2;
        }
    }

    /// <summary>
    /// Logical expression - "greater than" relational operator
    /// </summary>
    public class RelationalOperatorGTExpr : RelationalOperatorExpr
    {
        /// <summary>
        /// Constructs the "greater than" relational expression.
        /// </summary>
        /// <param name="leftNumExpr">Left numeric expression.</param>
        /// <param name="rightNumExpr">Right numeric expression.</param>
        public RelationalOperatorGTExpr(IPDDLArithmeticExpression leftNumExpr, IPDDLArithmeticExpression rightNumExpr) : base(leftNumExpr, rightNumExpr)
        {
        }

        /// <summary>
        /// Applies the concrete relational operator to evaluate logical value.
        /// </summary>
        /// <param name="value1">Left value.</param>
        /// <param name="value2">Right value.</param>
        /// <returns>True if the concrete operation evaluates as true, false otherwise.</returns>
        public override bool ApplyOperation(int value1, int value2)
        {
            return value1 > value2;
        }
    }

    /// <summary>
    /// Logical expression - "greater than or equal to" relational operator
    /// </summary>
    public class RelationalOperatorGTEExpr : RelationalOperatorExpr
    {
        /// <summary>
        /// Constructs the "greater than or equal to" relational expression.
        /// </summary>
        /// <param name="leftNumExpr">Left numeric expression.</param>
        /// <param name="rightNumExpr">Right numeric expression.</param>
        public RelationalOperatorGTEExpr(IPDDLArithmeticExpression leftNumExpr, IPDDLArithmeticExpression rightNumExpr) : base(leftNumExpr, rightNumExpr)
        {
        }

        /// <summary>
        /// Applies the concrete relational operator to evaluate logical value.
        /// </summary>
        /// <param name="value1">Left value.</param>
        /// <param name="value2">Right value.</param>
        /// <returns>True if the concrete operation evaluates as true, false otherwise.</returns>
        public override bool ApplyOperation(int value1, int value2)
        {
            return value1 >= value2;
        }
    }

    /// <summary>
    /// Common interface for arithmetic expressions.
    /// </summary>
    public interface IPDDLArithmeticExpression
    {
        /// <summary>
        /// Accepts a visitor evaluating the arithmetic expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>Result value of the arithmetic expression.</returns>
        int Accept(IArithmeticExpressionVisitor visitor);
    }

    /// <summary>
    /// Arithmetic expression - plus.
    /// </summary>
    public class PlusExpr : IPDDLArithmeticExpression
    {
        /// <summary>
        /// Children arithmetic expressions.
        /// </summary>
        public IPDDLArithmeticExpression[] childrenExprs;

        /// <summary>
        /// Constructs the "plus" arithmetic expression.
        /// </summary>
        /// <param name="childrenExprs">Children arithmetic expressions.</param>
        public PlusExpr(IPDDLArithmeticExpression[] childrenExprs)
        {
            this.childrenExprs = childrenExprs;
        }

        /// <summary>
        /// Accepts a visitor evaluating the arithmetic expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>Result value of the arithmetic expression.</returns>
        public int Accept(IArithmeticExpressionVisitor visitor)
        {
            int sum = 0;

            foreach (var expr in childrenExprs)
                sum += expr.Accept(visitor);

            return sum;
        }
    }

    /// <summary>
    /// Arithmetic expression - minus.
    /// </summary>
    public class MinusExpr : IPDDLArithmeticExpression
    {
        /// <summary>
        /// Left arithmetic expression.
        /// </summary>
        public IPDDLArithmeticExpression leftExpr;

        /// <summary>
        /// Right arithmetic expression.
        /// </summary>
        public IPDDLArithmeticExpression rightExpr;

        /// <summary>
        /// Constructs the "minus" arithmetic expression.
        /// </summary>
        /// <param name="leftExpr">Left arithmetic expression.</param>
        /// <param name="rightExpr">Right arithmetic expression.</param>
        public MinusExpr(IPDDLArithmeticExpression leftExpr, IPDDLArithmeticExpression rightExpr)
        {
            this.leftExpr = leftExpr;
            this.rightExpr = rightExpr;
        }

        /// <summary>
        /// Accepts a visitor evaluating the arithmetic expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>Result value of the arithmetic expression.</returns>
        public int Accept(IArithmeticExpressionVisitor visitor)
        {
            return leftExpr.Accept(visitor) - rightExpr.Accept(visitor);
        }
    }

    /// <summary>
    /// Arithmetic expression - unary minus.
    /// </summary>
    public class UnaryMinus : IPDDLArithmeticExpression
    {
        /// <summary>
        /// Child arithmetic expression.
        /// </summary>
        public IPDDLArithmeticExpression expr;

        /// <summary>
        /// Constructs the "unary minus" arithmetic expression.
        /// </summary>
        /// <param name="expr">Child arithmetic expression.</param>
        public UnaryMinus(IPDDLArithmeticExpression expr)
        {
            this.expr = expr;
        }

        /// <summary>
        /// Accepts a visitor evaluating the arithmetic expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>Result value of the arithmetic expression.</returns>
        public int Accept(IArithmeticExpressionVisitor visitor)
        {
            return -(expr.Accept(visitor));
        }
    }

    /// <summary>
    /// Arithmetic expression - multiply.
    /// </summary>
    public class MulExpr : IPDDLArithmeticExpression
    {
        /// <summary>
        /// Children arithmetic expressions.
        /// </summary>
        public IPDDLArithmeticExpression[] childrenExprs;

        /// <summary>
        /// Constructs the "multiply" arithmetic expression.
        /// </summary>
        /// <param name="childrenExprs">Children arithmetic expressions.</param>
        public MulExpr(IPDDLArithmeticExpression[] childrenExprs)
        {
            this.childrenExprs = childrenExprs;
        }

        /// <summary>
        /// Accepts a visitor evaluating the arithmetic expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>Result value of the arithmetic expression.</returns>
        public int Accept(IArithmeticExpressionVisitor visitor)
        {
            int product = 1;

            foreach (var expr in childrenExprs)
                product *= expr.Accept(visitor);

            return product;
        }
    }

    /// <summary>
    /// Arithmetic expression - divide.
    /// </summary>
    public class DivExpr : IPDDLArithmeticExpression
    {
        /// <summary>
        /// Left arithmetic expression.
        /// </summary>
        public IPDDLArithmeticExpression leftExpr;

        /// <summary>
        /// Right arithmetic expression.
        /// </summary>
        public IPDDLArithmeticExpression rightExpr;

        /// <summary>
        /// Constructs the "divide" arithmetic expression.
        /// </summary>
        /// <param name="leftExpr">Left arithmetic expression.</param>
        /// <param name="rightExpr">Right arithmetic expression.</param>
        public DivExpr(IPDDLArithmeticExpression leftExpr, IPDDLArithmeticExpression rightExpr)
        {
            this.leftExpr = leftExpr;
            this.rightExpr = rightExpr;
        }

        /// <summary>
        /// Accepts a visitor evaluating the arithmetic expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>Result value of the arithmetic expression.</returns>
        public int Accept(IArithmeticExpressionVisitor visitor)
        {
            return leftExpr.Accept(visitor) / rightExpr.Accept(visitor);
        }
    }

    /// <summary>
    /// Arithmetic expression - numeric constant.
    /// </summary>
    public class ConstExpr : IPDDLArithmeticExpression
    {
        /// <summary>
        /// Numeric constant value.
        /// </summary>
        public int constVal;

        /// <summary>
        /// Constructs the numeric constant arithmetic expression.
        /// </summary>
        /// <param name="constVal">Numeric constant value.</param>
        public ConstExpr(int constVal)
        {
            this.constVal = constVal;
        }

        /// <summary>
        /// Accepts a visitor evaluating the arithmetic expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>Result value of the arithmetic expression.</returns>
        public int Accept(IArithmeticExpressionVisitor visitor)
        {
            return constVal;
        }
    }

    /// <summary>
    /// Arithmetic expression - numeric function.
    /// </summary>
    public class FunctionExpr : IPDDLArithmeticExpression
    {
        /// <summary>
        /// Numeric function.
        /// </summary>
        public IPDDLDesignator function;

        /// <summary>
        /// Constructs the numeric function arithmetic expression.
        /// </summary>
        /// <param name="function">Numeric function.</param>
        public FunctionExpr(IPDDLDesignator function)
        {
            this.function = function;
        }

        /// <summary>
        /// Accepts a visitor evaluating the arithmetic expression.
        /// </summary>
        /// <param name="visitor">Evaluation visitor.</param>
        /// <returns>Result value of the arithmetic expression.</returns>
        public int Accept(IArithmeticExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
