using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Lifted version of the PDDL operator. With a specific substitution we get a grounded version (PDDLOperator).
    /// </summary>
    public class PDDLOperatorLifted
    {
        /// <summary>
        /// Reference to the parent PDDL planning problem.
        /// </summary>
        private PDDLProblem problem;

        /// <summary>
        /// Name of the operator.
        /// </summary>
        private string operatorName;

        /// <summary>
        /// ID of the operator.
        /// </summary>
        private int operatorID;

        /// <summary>
        /// Input parameters of the operator (variables).
        /// </summary>
        private InputParams inputParams;

        /// <summary>
        /// Preconditions for the operator applicability.
        /// </summary>
        private Preconditions preconds;

        /// <summary>
        /// Effects of the operator application.
        /// </summary>
        private Effects effects;

        /// <summary>
        /// Constructs the lifted PDDL operator.
        /// </summary>
        /// <param name="problem">Reference to the parent planning problem.</param>
        /// <param name="operatorName">Name of the operator.</param>
        /// <param name="inputParams">Input parameters of the operator.</param>
        /// <param name="preconds">Preconditions of the operator.</param>
        /// <param name="effects">Effects of the operator.</param>
        /// <param name="operatorID">Operator ID.</param>
        public PDDLOperatorLifted(PDDLProblem problem, string operatorName, InputParams inputParams, Preconditions preconds, Effects effects, int operatorID)
        {
            this.problem = problem;
            this.operatorName = operatorName;
            this.inputParams = inputParams;
            this.preconds = preconds;
            this.effects = effects;
            this.operatorID = operatorID;
        }

        /// <summary>
        /// Gets the reference to the parent planning problem.
        /// </summary>
        /// <returns>Parent planning problem.</returns>
        public PDDLProblem GetProblem()
        {
            return problem;
        }

        /// <summary>
        /// Gets the operator name.
        /// </summary>
        /// <returns>Operator name.</returns>
        public string GetName()
        {
            return operatorName;
        }

        /// <summary>
        /// Gets the input parameters of the operator.
        /// </summary>
        /// <returns>Input parameters.</returns>
        public InputParams GetInputParams()
        {
            return inputParams;
        }

        /// <summary>
        /// Gets the effects of the operator.
        /// </summary>
        /// <returns>Operator effects.</returns>
        public Effects GetEffects()
        {
            return effects;
        }

        /// <summary>
        /// Checks whether the operator is relevant (in context of planning search) to the given state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <param name="substit">Operator substitution.</param>
        /// <returns>True if the operator is relevant to the given state, false otherwise.</returns>
        public bool IsRelevant(IPDDLState state, PDDLOperatorSubstitution substit)
        {
            return effects.IsRelevant(substit, state);
        }

        /// <summary>
        /// Checks whether the operator is applicable (in context of planning search) to the given state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <param name="substit">Operator substitution.</param>
        /// <returns>True if the operator is applicable to the given state, false otherwise.</returns>
        public bool IsApplicable(IPDDLState state, PDDLOperatorSubstitution substit)
        {
            return preconds.IsApplicable(problem, substit, state);
        }

        /// <summary>
        /// Checks whether the operator can be predecessor to the given state in the planning search process.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <param name="substit">Operator substitution.</param>
        /// <returns>True if the operator can be predecessor to the given state, false otherwise.</returns>
        public bool CanBePredecessor(IPDDLState state, PDDLOperatorSubstitution substit)
        {
            return effects.CanBePredecessor(substit, state);
        }

        /// <summary>
        /// Applies the operator to the given state. The result is a new state (successor).
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <param name="substit">Operator substitution.</param>
        /// <returns>Successor state to the given state.</returns>
        public IPDDLState Apply(IPDDLState state, PDDLOperatorSubstitution substit)
        {
            return effects.ApplyEffects(problem, substit, state);
        }

        /// <summary>
        /// Applies the operator backwards to the given state. The result is a set of states (possible predecessors).
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <param name="substit">Operator substitution.</param>
        /// <returns>Possible predecessor states to the given state.</returns>
        public List<IState> ApplyBackwards(IPDDLState state, PDDLOperatorSubstitution substit)
        {
            List<IState> retList = new List<IState>();

            List<IState> candidateStates = effects.ApplyBackwards(problem, substit, state);
            foreach (var predState in candidateStates)
            {
                if (IsApplicable((IPDDLState)predState, substit))
                    retList.Add(predState);
            }

            return retList;
        }

        /// <summary>
        /// Gets a cost of the operator.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <param name="substit">Operator substitution.</param>
        /// <returns>Operator cost.</returns>
        public int GetCost(IPDDLState state, PDDLOperatorSubstitution substit)
        {
            return effects.EvaluteOperatorCost(substit, state);
        }

        /// <summary>
        /// Gets the operator ID in the planning problem.
        /// </summary>
        /// <returns>Operator ID in the planning problem.</returns>
        public int GetOrderIndex()
        {
            return operatorID;
        }

        /// <summary>
        /// Count of all potential substitutions for the lifted PDDL operator. Lazy evaluated.
        /// </summary>
        private int numAllPotentialSubstitutions = -1;

        /// <summary>
        /// Gets the number of all potential substitutions for the lifted PDDL operator.
        /// </summary>
        /// <returns>Number of possible substitutions.</returns>
        public int GetNumberOfAllPossibleSubstitutions()
        {
            if (numAllPotentialSubstitutions == -1)
            {
                numAllPotentialSubstitutions = 1;
                for (int i = 0; i < inputParams.GetNumberOfParams(); ++i)
                {
                    int iTypeID = inputParams.GetVarTypeID(i);
                    numAllPotentialSubstitutions *= problem.GetIDManager().GetConstantsIDForType(iTypeID).Count;
                }
            }
            return numAllPotentialSubstitutions;
        }

        /// <summary>
        /// Input parameters (variables) of the PDDL operator.
        /// </summary>
        public class InputParams
        {
            /// <summary>
            /// Problem-specific type IDs of the corresponding parameters (variables).
            /// </summary>
            private int[] varTypeIDs;

            /// <summary>
            /// Constructs input parameters of the operator.
            /// </summary>
            /// <param name="varTypeIDs">Type IDs of the parameters.</param>
            public InputParams(int[] varTypeIDs)
            {
                this.varTypeIDs = varTypeIDs;
            }

            /// <summary>
            /// Gets the type ID of the specified parameter.
            /// </summary>
            /// <param name="index">Index of the parameter.</param>
            /// <returns>Type ID of the parameter.</returns>
            public int GetVarTypeID(int index)
            {
                return varTypeIDs[index];
            }

            /// <summary>
            /// Gets the number of all input parameters.
            /// </summary>
            /// <returns>Number of input parameters.</returns>
            public int GetNumberOfParams()
            {
                return varTypeIDs.Length;
            }
        }

        /// <summary>
        /// Preconditions of the PDDL operator.
        /// </summary>
        public class Preconditions
        {
            /// <summary>
            /// Logical expression representing the preconditions of the operator. If evaluated as true, the operator is applicable.
            /// </summary>
            private IPDDLLogicalExpression expr;

            /// <summary>
            /// Constructs the operator preconditions.
            /// </summary>
            /// <param name="expr">Logical expression representing the preconditions.</param>
            public Preconditions(IPDDLLogicalExpression expr)
            {
                this.expr = expr;
            }

            /// <summary>
            /// Checks whether the parent operator is applicable to the reference state with the specified substitution.
            /// </summary>
            /// <param name="problem">Parent planning problem.</param>
            /// <param name="substit">Operator substitution.</param>
            /// <param name="state">Reference state.</param>
            /// <returns>True, if the operator is applicable, false otherwise.</returns>
            public bool IsApplicable(PDDLProblem problem, PDDLOperatorSubstitution substit, IPDDLState state)
            {
                return PDDLExpressionEval.EvaluateLogicalExpression(problem, state, expr, substit);
            }
        }

        /// <summary>
        /// Effects of the PDDL operator.
        /// </summary>
        public class Effects
        {
            /// <summary>
            /// Sets of operator effects (possitive of negative). 
            /// </summary>
            private HashSet<IPDDLDesignator> effectsPos, effectsNeg;

            /// <summary>
            /// Preconditions for the conditional effects.
            /// </summary>
            private Preconditions[] conditionalPrecond;

            /// <summary>
            /// Effects for the conditional effects.
            /// </summary>
            private Effects[] conditionalEffects;

            /// <summary>
            /// ID mappings of 'forall' operations.
            /// </summary>
            private Dictionary<int, int>[] forallIDMappings;

            /// <summary>
            /// Effects of 'forall' operations.
            /// </summary>
            private Effects[] forallEffects;

            /// <summary>
            /// Numeric function effects (increase, decrease, assign operations).
            /// </summary>
            private Tuple<IPDDLDesignator, IPDDLArithmeticExpression>[] increaseFunc, decreaseFunc, numAssignFunc;

            /// <summary>
            /// Object function effects (assign operation).
            /// </summary>
            private Tuple<IPDDLDesignator, IPDDLDesignator, int, bool>[] objAssignFunc;

            /// <summary>
            /// Numeric expression representing the cost of the lifted operator (needs to be evaluated with specific substitution).
            /// </summary>
            private IPDDLArithmeticExpression operatorCost = null;

            /// <summary>
            /// Arithmetic expression evaluation visitor for inner (repeated) evaluations.
            /// </summary>
            private ArithmeticExpressionEvalVisitor innerArithmEvalVisitor;

            /// <summary>
            /// List of substitutions for quantified sub-expressions. Used for the evaluation of forall and exists expressions.
            /// </summary>
            private List<PDDLOperatorSubstitution> quantifExprSubstitList;

            /// <summary>
            /// Constructs the effects of the PDDL operator.
            /// </summary>
            /// <param name="problem">Parent planning problem.</param>
            /// <param name="effectsExpr">Complex expression representing the effects.</param>
            public Effects(PDDLIdentifierMappingsManager idManager, IPDDLLogicalExpression effectsExpr)
            {
                effectsPos = new HashSet<IPDDLDesignator>();
                effectsNeg = new HashSet<IPDDLDesignator>();

                // convert effects expression into a simplier representation

                List<Preconditions> listCondPrecond = new List<Preconditions>();
                List<Effects> listCondEff = new List<Effects>();

                List<Dictionary<int, int>> listForallIDMappings = new List<Dictionary<int, int>>();
                List<Effects> listForallEffects = new List<Effects>();

                List<Tuple<IPDDLDesignator, IPDDLArithmeticExpression>> increaseFunc = new List<Tuple<IPDDLDesignator, IPDDLArithmeticExpression>>();
                List<Tuple<IPDDLDesignator, IPDDLArithmeticExpression>> decreaseFunc = new List<Tuple<IPDDLDesignator, IPDDLArithmeticExpression>>();
                List<Tuple<IPDDLDesignator, IPDDLArithmeticExpression>> numAssignFunc = new List<Tuple<IPDDLDesignator, IPDDLArithmeticExpression>>();
                List<Tuple<IPDDLDesignator, IPDDLDesignator, int, bool>> objAssignFunc = new List<Tuple<IPDDLDesignator, IPDDLDesignator, int, bool>>();

                IPDDLLogicalExpression[] rootExprs = null;
                if (effectsExpr is AndExpr)
                {
                    AndExpr rootAnd = effectsExpr as AndExpr;
                    rootExprs = rootAnd.children;
                }
                else
                {
                    rootExprs = new IPDDLLogicalExpression[1] { effectsExpr };
                }

                int totalCostFuncID = idManager.GetFunctionsMapping().GetFunctionID("total-cost");

                foreach (var child in rootExprs)
                {
                    if (child is NotExpr)
                    {
                        NotExpr notExpr = child as NotExpr;
                        PredicateExpr negPred = notExpr.child as PredicateExpr;
                        if (negPred != null)
                            effectsNeg.Add(negPred.predicate);
                    }
                    else if (child is PredicateExpr)
                    {
                        PredicateExpr posPred = child as PredicateExpr;
                        effectsPos.Add(posPred.predicate);
                    }
                    else if (child is ForallExpr)
                    {
                        ForallExpr forallExpr = child as ForallExpr;
                        listForallIDMappings.Add(forallExpr.varIDToTypeIDMapping);
                        listForallEffects.Add(new Effects(idManager, forallExpr.child));
                    }
                    else if (child is WhenExpr)
                    {
                        WhenExpr whenExpr = child as WhenExpr;
                        listCondPrecond.Add(new Preconditions(whenExpr.conditionalPreconds));
                        listCondEff.Add(new Effects(idManager, whenExpr.conditionalEffects));
                    }
                    else if (child is IncreaseExpr)
                    {
                        IncreaseExpr incrExpr = child as IncreaseExpr;
                        increaseFunc.Add(Tuple.Create(incrExpr.function, incrExpr.assignment));

                        if (totalCostFuncID == incrExpr.function.GetPrefixID())
                            operatorCost = incrExpr.assignment;

                    }
                    else if (child is DecreaseExpr)
                    {
                        DecreaseExpr decrExpr = child as DecreaseExpr;
                        decreaseFunc.Add(Tuple.Create(decrExpr.function, decrExpr.assignment));
                    }
                    else if (child is NumAssignExpr)
                    {
                        NumAssignExpr numAssignExpr = child as NumAssignExpr;
                        numAssignFunc.Add(Tuple.Create(numAssignExpr.function, numAssignExpr.assignment));
                    }
                    else if (child is ObjAssignExpr)
                    {
                        ObjAssignExpr objAssignExpr = child as ObjAssignExpr;
                        objAssignFunc.Add(Tuple.Create(objAssignExpr.function, objAssignExpr.assignedFunction, objAssignExpr.assignPar, objAssignExpr.assignParIsVar));
                    }
                }

                conditionalPrecond = listCondPrecond.ToArray();
                conditionalEffects = listCondEff.ToArray();

                forallIDMappings = listForallIDMappings.ToArray();
                forallEffects = listForallEffects.ToArray();

                this.increaseFunc = increaseFunc.ToArray();
                this.decreaseFunc = decreaseFunc.ToArray();
                this.numAssignFunc = numAssignFunc.ToArray();
                this.objAssignFunc = objAssignFunc.ToArray();

                this.innerArithmEvalVisitor = new ArithmeticExpressionEvalVisitor();
                this.quantifExprSubstitList = new List<PDDLOperatorSubstitution>();
            }

            /// <summary>
            /// Applies the effects of the parent operator with the specified substitution to the reference state. The result is a new state (successor).
            /// </summary>
            /// <param name="problem">Parent planning problem.</param>
            /// <param name="substit">Operator substitution.</param>
            /// <param name="state">Reference state.</param>
            /// <returns>Successor state to the given state.</returns>
            public IPDDLState ApplyEffects(PDDLProblem problem, PDDLOperatorSubstitution substit, IPDDLState state)
            {
                IPDDLState newState = (IPDDLState)state.Clone();
                ICollection<IPDDLDesignator> newStatePreds = newState.GetPredicates();

                foreach (var effNeg in effectsNeg)
                    newStatePreds.Remove(PDDLOperatorSubstitution.MakeSubstituedDesignator(effNeg, substit));
                foreach (var effPos in effectsPos)
                    newStatePreds.Add(PDDLOperatorSubstitution.MakeSubstituedDesignator(effPos, substit));

                for (int i = 0; i < forallEffects.Length; ++i)
                {
                    var forallEffect = forallEffects[i];
                    var varIDMapping = forallIDMappings[i];

                    var newSubstits = quantifExprSubstitList;
                    QuantifierGrounder.GetAllExtendedSubstitutions(problem, substit, varIDMapping, newSubstits);
                    foreach (var newSubstit in newSubstits)
                    {
                        newState = forallEffect.ApplyEffects(problem, newSubstit, newState);
                    }
                }

                for (int i = 0; i < conditionalEffects.Length; ++i)
                {
                    if (conditionalPrecond[i].IsApplicable(problem, substit, state))
                        newState = conditionalEffects[i].ApplyEffects(problem, substit, newState);
                }

                foreach (var incr in increaseFunc)
                {
                    int incrVal = innerArithmEvalVisitor.Evaluate(incr.Item2, substit, state);
                    newState.IncreaseFunc(PDDLOperatorSubstitution.MakeSubstituedDesignator(incr.Item1, substit), incrVal);
                }
                foreach (var decr in decreaseFunc)
                {
                    int decrVal = innerArithmEvalVisitor.Evaluate(decr.Item2, substit, state);
                    newState.DecreaseFunc(PDDLOperatorSubstitution.MakeSubstituedDesignator(decr.Item1, substit), decrVal);
                }
                foreach (var assign in numAssignFunc)
                {
                    int assignVal = innerArithmEvalVisitor.Evaluate(assign.Item2, substit, state);
                    newState.AssignFunc(PDDLOperatorSubstitution.MakeSubstituedDesignator(assign.Item1, substit), assignVal);
                }
                foreach (var assign in objAssignFunc)
                {
                    int assignVal;
                    if (assign.Item2 != null) // assignedFunc
                        assignVal = state.GetFunctionValue(PDDLOperatorSubstitution.MakeSubstituedDesignator(assign.Item2, substit));
                    else if (!assign.Item4) // !isVar
                        assignVal = assign.Item3; // const
                    else
                        assignVal = substit.GetValue(assign.Item3); // get substitued const
                    newState.AssignFunc(PDDLOperatorSubstitution.MakeSubstituedDesignator(assign.Item1, substit), assignVal);
                }

                return newState;
            }

            /// <summary>
            /// Evaluates the parent operator cost with the specified substitution, grounded from the reference state.
            /// </summary>
            /// <param name="substit">Operator substitution.</param>
            /// <param name="state">Reference state.</param>
            /// <returns>Cost of the operator.</returns>
            public int EvaluteOperatorCost(PDDLOperatorSubstitution substit, IPDDLState state)
            {
                if (operatorCost == null)
                    return 1;
                return innerArithmEvalVisitor.Evaluate(operatorCost, substit, state);
            }

            /// <summary>
            /// Applies the effects of the parent operator with the specified substitution backwards from the destination state. The results are possible predecessors.
            /// </summary>
            /// <param name="problem">Parent planning problem.</param>
            /// <param name="substit">Operator substitution.</param>
            /// <param name="state">Destination state.</param>
            /// <returns>Possible predecessor states to the given state.</returns>
            public List<IState> ApplyBackwards(PDDLProblem problem, PDDLOperatorSubstitution substit, IPDDLState state)
            {
                List<IState> retList = new List<IState>();

                List<IPDDLDesignator> effects = new List<IPDDLDesignator>();

                IPDDLState baseState = (IPDDLState)state.Clone();
                ICollection<IPDDLDesignator> baseStatePreds = baseState.GetPredicates();

                foreach (var pred in effectsPos)
                {
                    IPDDLDesignator subsPred = PDDLOperatorSubstitution.MakeSubstituedDesignator(pred, substit);
                    effects.Add(subsPred);
                    baseStatePreds.Remove(subsPred);
                }
                foreach (var pred in effectsNeg)
                {
                    effects.Add(PDDLOperatorSubstitution.MakeSubstituedDesignator(pred, substit));
                }

                GetPredStates(baseState, 0, effects, retList);
                return retList;
            }

            /// <summary>
            /// Get possible predecessor states to the specified state. Implemented as divide-and-conquer.
            /// </summary>
            /// <param name="baseState">Reference state.</param>
            /// <param name="effIdx">Index of the effect to be applied.</param>
            /// <param name="effects">Available effects.</param>
            /// <param name="retList">Possible predecessors - return list.</param>
            private void GetPredStates(IPDDLState baseState, int effIdx, List<IPDDLDesignator> effects, List<IState> retList)
            {
                if (effIdx >= effects.Count)
                {
                    retList.Add(baseState);
                    return;
                }

                IPDDLState newStateEffectNotAdded = (IPDDLState)baseState.Clone();
                GetPredStates(newStateEffectNotAdded, effIdx + 1, effects, retList);

                IPDDLState newStateEffectAdded = (IPDDLState)baseState.Clone();
                newStateEffectAdded.GetPredicates().Add(effects[effIdx]);
                GetPredStates(newStateEffectAdded, effIdx + 1, effects, retList);
            }

            /// <summary>
            /// Checks whether the parent operator is relevant (in context of planning search) to the given state with the specified substitution.
            /// </summary>
            /// <param name="substit">Operator substitution.</param>
            /// <param name="state">Reference state.</param>
            /// <returns>True if the operator is relevant to the given state, false otherwise.</returns>
            public bool IsRelevant(PDDLOperatorSubstitution substit, IPDDLState state)
            {
                foreach (var pred in effectsNeg)
                {
                    IPDDLDesignator substituedPred = PDDLOperatorSubstitution.MakeSubstituedDesignator(pred, substit);
                    if (state.HasPredicate(substituedPred))
                        return false;
                }

                foreach (var pred in effectsPos)
                {
                    IPDDLDesignator substituedPred = PDDLOperatorSubstitution.MakeSubstituedDesignator(pred, substit);
                    if (!state.HasPredicate(substituedPred))
                        return true;
                }

                return false;
            }

            /// <summary>
            /// Checks whether the parent operator can be predecessor to the given state with the specified substitution.
            /// </summary>
            /// <param name="substit">Operator substitution.</param>
            /// <param name="state">Reference state.</param>
            /// <returns>True if the operator can be predecessor to the given state, false otherwise.</returns>
            public bool CanBePredecessor(PDDLOperatorSubstitution substit, IPDDLState state)
            {
                foreach (var pred in effectsPos)
                {
                    IPDDLDesignator substituedPred = PDDLOperatorSubstitution.MakeSubstituedDesignator(pred, substit);
                    if (!state.GetPredicates().Contains(substituedPred))
                        return false;
                }

                foreach (var pred in effectsNeg)
                {
                    IPDDLDesignator substituedPred = PDDLOperatorSubstitution.MakeSubstituedDesignator(pred, substit);
                    if (state.GetPredicates().Contains(substituedPred))
                        return false;
                }

                return true;
            }
            
            /// <summary>
            /// Checks whether the parent operator can influence the specified predicate.
            /// </summary>
            /// <param name="pred">Reference predicate.</param>
            /// <returns>True if the operator can influence the predicate.</returns>
            public bool CanInfluencePred(IPDDLDesignator pred)
            {
                foreach (var predItem in effectsPos)
                {
                    if (predItem.GetPrefixID() == pred.GetPrefixID())
                        return true;
                }

                foreach (var predItem in effectsNeg)
                {
                    if (predItem.GetPrefixID() == pred.GetPrefixID())
                        return true;
                }

                return false;
            }
        }
    }
}
