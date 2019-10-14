using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Implementation of the SAS+ operator. Handles the evaluation of preconditions and the application of effects.
    /// </summary>
    public class SASOperator : IOperator
    {
		private static List<IState> emptyPredecessorsList = new List<IState>();
		private HashSet<int> hashSetPlaceholder = new HashSet<int>();

        /// <summary>
        /// Name of the operator.
        /// </summary>
        private string operatorName;

        /// <summary>
        /// Preconditions of the SAS+ operator.
        /// </summary>
        private SASOperatorPreconditions operatorPreconditions;

        /// <summary>
        /// Effects of the SAS+ operator.
        /// </summary>
        private SASOperatorEffects operatorEffects;

        /// <summary>
        /// The cost of the operator in the SAS+ planning problem.
        /// </summary>
        private int operatorCost;

        /// <summary>
        /// Index of the operator in the list of all operators in the SAS+ planning problem.
        /// </summary>
        private int operatorOrderIndex;

        /// <summary>
        /// Reference to the parent SAS+ planning problem.
        /// </summary>
        private SASProblem parentProblem;

        /// <summary>
        /// Constructs the SAS+ operator.
        /// </summary>
        /// <param name="problem">Parent planning problem.</param>
        /// <param name="opData">Operator data.</param>
        /// <param name="orderIndex">Operator order index.</param>
        public SASOperator(SASProblem problem, SASInputData.Operator opData, int orderIndex)
        {
            operatorName = opData.Name;
            operatorPreconditions = new SASOperatorPreconditions(opData.Preconditions);
            operatorEffects = new SASOperatorEffects(opData.Effects);
            operatorCost = opData.Cost;
            operatorOrderIndex = orderIndex;
            parentProblem = problem;
        }

        /// <summary>
        /// Gets the operator name.
        /// </summary>
        /// <returns>String representing the name of the operator.</returns>
        public string GetName()
        {
            return operatorName;
        }

		public override string ToString()
		{
			return operatorName;
		}

		/// <summary>
		/// Gets SAS+ operator preconditions.
		/// </summary>
		/// <returns>SAS+ operator preconditions.</returns>
		public SASOperatorPreconditions GetPreconditions()
        {
            return operatorPreconditions;
        }

        /// <summary>
        /// Gets SAS+ operator effects.
        /// </summary>
        /// <returns>SAS+ operator effects.</returns>
        public SASOperatorEffects GetEffects()
        {
            return operatorEffects;
        }

        /// <summary>
        /// Gets a cost of the operator.
        /// </summary>
        /// <returns>Operator cost.</returns>
        public int GetCost()
        {
            return operatorCost;
        }

        /// <summary>
        /// Gets the operator ID in the planning problem.
        /// </summary>
        /// <returns>Operator ID in the planning problem.</returns>
        public int GetOrderIndex()
        {
            return operatorOrderIndex;
        }

        /// <summary>
        /// Checks whether the operator is relevant (in context of planning search) to the given state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator is relevant to the given state, false otherwise.</returns>
        public bool IsRelevant(IState state)
        {
            return operatorEffects.IsRelevant((SASState)state);
        }

        /// <summary>
        /// Checks whether the operator is applicable (in context of planning search) to the given state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator is applicable to the given state, false otherwise.</returns>
        public bool IsApplicable(IState state)
        {
            return operatorPreconditions.IsApplicable((SASState)state);
        }

        /// <summary>
        /// Checks whether the operator can be predecessor to the given state in the planning search process.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator can be predecessor to the given state, false otherwise.</returns>
        public bool CanBePredecessor(IState state)
        {
            return operatorEffects.CanBePredecessor((SASState)state);
        }

        /// <summary>
        /// Applies the operator to the given state. The result is a new state (successor).
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Successor state to the given state.</returns>
        public IState Apply(IState state)
        {
            SASState newState = operatorEffects.Apply((SASState)state);
            parentProblem.ApplyAxiomRules(newState);
            return newState;
        }

        /// <summary>
        /// Applies the operator backwards to the given state. The result is a set of states (all possible predecessors).
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Possible predecessor states to the given state.</returns>
        public List<IState> ApplyBackwards(IState state)
        {
			if (!CanBePredecessor(state))
				return emptyPredecessorsList;
			SASState currentState = (SASState)state;
			Dictionary<int, int> fixedVariables = new Dictionary<int, int>();

			for (int i = 0; i < this.parentProblem.GetVariablesCount(); i++)
			{
				//variables not mentioned in the effects has to remain the same
				if (!this.GetEffects().Any(e => e.GetEff().variable == i))
				{
					fixedVariables.Add(i, currentState.GetValue(i));
				}
			}

			foreach (var item in this.operatorPreconditions)
			{
				if (!fixedVariables.ContainsKey(item.variable))
					fixedVariables.Add(item.variable, item.value);
				else if (fixedVariables[item.variable] != item.value)
					return emptyPredecessorsList;
			}

#if DEBUG
			//this only works if the operators don't have conditional effects
			if (operatorEffects.Any(eff => eff.GetConditions().Count > 0))
				throw new Exception();
#endif
			throw new NotImplementedException();
			//TODO.. !!
			/*
			var result = StateSpaceEnumerator.getAllStatesMeetingConditions(fixedVariables, this.parentProblem).ToList();

#if DEBUG
			foreach (var item in result)
			{
				if (!this.Apply(item).Equals(state))
				{
					throw new Exception();
				}
			}
#endif
			return result;
			*/
		}

		/// <summary>
		/// Returns a relative state that describes all predecessors of given state using this operator, i.e. all states, that when this operators is aplied to them, they yield given state. 
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public RelativeState ApplyBackwardsRelative(IState state)
		{
			RelativeState currentState = (RelativeState)state;
			hashSetPlaceholder.Clear();

			//checks backwards applicability and backwards relevance of operator
			//operator must contribute to currently fixed variables - i.e. some variable in current state whose value is not wildcard must be in operators effects, AND
			//operator's effects must not be in conflict with other fixed variables - i.e. for all variables in operator's effects, either in given state this variables has required value, or it has wildcard.

			//!! Operator also have hidden effects!!! : some variables might be present in its preconditions but not in effects. These variables, however, should be considered operator effect, 
			//since they will have predetermined value after applying the operator.
			bool hasContributed = false;
			foreach (var item in this.operatorEffects)
			{
				int variable = item.GetEff().variable;
				hashSetPlaceholder.Add(variable);	//collects all variables that are present in some effect
				int requiredVal = item.GetEff().value;
				int presentVal = currentState.GetValue(variable);
				if (presentVal == -1)
					continue;   //wildcard = it is not a conflict but it doesn't contribute.
				if (presentVal != requiredVal)
					return null;    //this is a conflict, operator is not backward applicable to given state.
				hasContributed = true; //this means that the value is the same as required and not a wildcard
			}
			if (!hasContributed)
				return null;

			//now "hidden effects" are checked:
			foreach (var item in this.operatorPreconditions)
			{
				int variable = item.variable;
				if (!hashSetPlaceholder.Contains(variable)) //is hiddent effect
				{
					int requiredVal = item.value;
					int presentVal = currentState.GetValue(variable);
					if (presentVal == -1)
						continue;   //wildcard = it is not a conflict
					if (presentVal != requiredVal)
						return null;  //this is a conflict, operator is not backward applicable to given state.
				}
			}


			//operator is backwards applicable, now we construct the result
			//first, we replace effect variables by wildcards. Operator sets the variables, but in the predecessor their value might be arbitrary (unless they are in preconditions) so we no longer require that they hold the value.
			//then we fix preconditions of the operator. (They don't need to hold in the effect, so we replace current values by preconditions even if the were already fixed!)

			RelativeState result = (RelativeState)currentState.Clone();

			foreach (var item in this.operatorEffects)
			{
				result.SetValue(item.GetEff().variable, -1);
			}

			foreach (var item in this.operatorPreconditions)
			{
				result.SetValue(item.variable, item.value);
			}

#if DEBUG
			//this only works if operators don't have conditional effects
			if (operatorEffects.Any(eff => eff.GetConditions().Count > 0))
				throw new Exception();
#endif
			return result;
		}



		public override int GetHashCode()
		{
			return this.GetOrderIndex();
		}

		public override bool Equals(object obj)
		{
			var @operator = obj as SASOperator;
			return @operator != null &&
				   operatorName == @operator.operatorName &&
				   operatorOrderIndex == @operator.operatorOrderIndex;
		}
	}

    /// <summary>
    /// Structure representing a simple variable-value pair.
    /// </summary>
    public struct SASVariableValuePair
    {
        /// <summary>
        /// Variable ID.
        /// </summary>
        public int variable;

        /// <summary>
        /// Corresponding value ID.
        /// </summary>
        public int value;

        /// <summary>
        /// Constructs variable-value pair.
        /// </summary>
        /// <param name="variable">Variable ID.</param>
        /// <param name="value">Value.</param>
        public SASVariableValuePair(int variable, int value)
        {
            this.variable = variable;
            this.value = value;
        }
    }

    /// <summary>
    /// Implementation of SAS+ operator preconditions.
    /// </summary>
    public class SASOperatorPreconditions : IEnumerable<SASVariableValuePair>
    {
        /// <summary>
        /// Collection of conditions (variable-value pairs) that have to be met.
        /// </summary>
        private IList<SASVariableValuePair> conditions;

        /// <summary>
        /// Constructs the operator preconditions.
        /// </summary>
        /// <param name="precondData">Preconditions data.</param>
        public SASOperatorPreconditions(List<SASInputData.Operator.Precondition> precondData)
        {
            conditions = new List<SASVariableValuePair>();
            for (int idx = 0; idx < precondData.Count; ++idx)
                conditions.Add(new SASVariableValuePair(precondData[idx].Condition.Variable, precondData[idx].Condition.Value));
        }

        /// <summary>
        /// Constructs the operator preconditions.
        /// </summary>
        /// <param name="precondData">Preconditions data.</param>
        public SASOperatorPreconditions(List<SASInputData.VariableValuePair> precondData)
        {
            conditions = new List<SASVariableValuePair>();
            for (int idx = 0; idx < precondData.Count; ++idx)
                conditions.Add(new SASVariableValuePair(precondData[idx].Variable, precondData[idx].Value));
        }

        /// <summary>
        /// Constructs the operator preconditions.
        /// </summary>
        /// <param name="varValPairs">List of variable-value pairs representing the preconditions.</param>
        public SASOperatorPreconditions(List<SASVariableValuePair> varValPairs)
        {
            conditions = new List<SASVariableValuePair>(varValPairs);
        }

        /// <summary>
        /// Checks whether the operator is applicable (in context of planning search) to the given state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator is applicable to the given state, false otherwise.</returns>
        public bool IsApplicable(SASState state)
        {
            return conditions.All(cond => state.HasValue(cond.variable, cond.value));
        }

		[Obsolete]
        /// <summary>
        /// Applies the operator backwards to the given state. The result is a set of states (possible predecessors).
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Possible predecessor states to the given state.</returns>
        public List<IState> ApplyBackwards(SASState state)
        {
			return null;
        }

        /// <summary>
        /// Accessor to the actual conditions via [] operator.
        /// </summary>
        /// <param name="arrIdx">Array index.</param>
        /// <returns>Variable-value pair representing a single condition.</returns>
        public SASVariableValuePair this[int arrIdx]
        {
            get { return conditions[arrIdx]; }
        }

        /// <summary>
        /// Gets the number of all conditions.
        /// </summary>
        /// <returns>Number of conditions.</returns>
        public int Count
        {
            get { return conditions.Count; }
        }

        /// <summary>
        /// Gets enumerator for the collection of preconditions.
        /// </summary>
        /// <returns>Enumerator of variable-value pair entries.</returns>
        public IEnumerator<SASVariableValuePair> GetEnumerator()
        {
            for (int i = 0; i < conditions.Count; ++i)
                yield return conditions[i];
        }

        /// <summary>
        /// Gets enumerator for the collection of preconditions.
        /// </summary>
        /// <returns>Enumerator of variable-value pair entries.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Implementation of SAS+ operator effects.
    /// </summary>
    public class SASOperatorEffects : IEnumerable<SASOperatorEffect>
    {
        /// <summary>
        /// Collection of actual effects to be applied.
        /// </summary>
        private IList<SASOperatorEffect> effects;

        /// <summary>
        /// Constructs the operator effects.
        /// </summary>
        /// <param name="effData">Effects data.</param>
        public SASOperatorEffects(List<SASInputData.Operator.Effect> effData)
        {
            effects = new List<SASOperatorEffect>();
            for (int effIdx = 0; effIdx < effData.Count; ++effIdx)
                effects.Add(new SASOperatorEffect(effData[effIdx]));
        }

        /// <summary>
        /// Checks whether the operator is relevant (in context of planning search) to the given state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator is relevant to the given state, false otherwise.</returns>
        public bool IsRelevant(SASState state)
        {
            return effects.Any(e => !state.HasValue(e.GetEff().variable, e.GetEff().value));
        }

        /// <summary>
        /// Checks whether the operator can be predecessor to the given state in the planning search process.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator can be predecessor to the given state, false otherwise.</returns>
        public bool CanBePredecessor(SASState state)
        {
            return effects.All(e => state.HasValue(e.GetEff().variable, e.GetEff().value));
        }

        /// <summary>
        /// Applies the operator to the given state. The result is a new state (successor).
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Successor state to the given state.</returns>
        public SASState Apply(SASState state)
        {
            SASState resState = (SASState)state.Clone();
            foreach (var effect in effects)
            {
                if (effect.IsApplicable(state)) // check applicability to the original state!
                    resState.SetValue(effect.GetEff().variable, effect.GetEff().value);
            }
            return resState;
        }

        /// <summary>
        /// Accessor to the actual effects via [] operator.
        /// </summary>
        /// <param name="arrIdx">Array index.</param>
        /// <returns>Single effect to be applied.</returns>
        public SASOperatorEffect this[int arrIdx]
        {
            get { return effects[arrIdx]; }
        }

        /// <summary>
        /// Gets the number of all effects.
        /// </summary>
        /// <returns>Number of effects.</returns>
        public int Count
        {
            get { return effects.Count; }
        }

        /// <summary>
        /// Gets enumerator for the collection of effects.
        /// </summary>
        /// <returns>Enumerator of single effect entries.</returns>
        public IEnumerator<SASOperatorEffect> GetEnumerator()
        {
            for (int i = 0; i < effects.Count; ++i)
                yield return effects[i];
        }

        /// <summary>
        /// Gets enumerator for the collection of effects.
        /// </summary>
        /// <returns>Enumerator of single effect entries.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Implementation of single SAS+ operator effect.
    /// </summary>
    public class SASOperatorEffect
    {
        /// <summary>
        /// Conditions to be met for the effect (in case of conditional effect).
        /// </summary>
        private SASOperatorPreconditions conditions;

        /// <summary>
        /// Actual effect to be applied (variable-value pair).
        /// </summary>
        private SASVariableValuePair effect;

        /// <summary>
        /// Constructs the SAS+ operator effect.
        /// </summary>
        /// <param name="effData">Effect data.</param>
        public SASOperatorEffect(SASInputData.Operator.Effect effData)
        {
            conditions = new SASOperatorPreconditions(effData.Conditions);
            effect = new SASVariableValuePair(effData.ActualEffect.Variable, effData.ActualEffect.Value);
        }

        /// <summary>
        /// Constructs the SAS+ operator effect.
        /// </summary>
        /// <param name="conditions">Effect conditions.</param>
        /// <param name="effect">Actual effect.</param>
        public SASOperatorEffect(SASOperatorPreconditions conditions, SASVariableValuePair effect)
        {
            this.conditions = conditions;
            this.effect = effect;
        }

		/// <summary>
		/// Gets the effect conditions.
		/// </summary>
		/// <returns>Conditions of the effect.</returns>
		public SASOperatorPreconditions GetConditions()
        {
            return conditions;
        }

        /// <summary>
        /// Gets the actual effect (variable-value pair).
        /// </summary>
        /// <returns>Variable-value pair of the actual effect.</returns>
        public SASVariableValuePair GetEff()
        {
            return effect;
        }

		/// <summary>
		/// Checks whether the effect can actually be applied to the given state (in case of conditional effect).
		/// </summary>
		/// <param name="state">SAS+ state.</param>
		/// <returns>True if the effect can be applied on the given state, false otherwise.</returns>
		public bool IsApplicable(SASState state)
        {
            return conditions.IsApplicable(state);
        }

        /// <summary>
        /// Constructs a string representing the effect.
        /// </summary>
        /// <returns>String representation of the effect.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var condition in conditions)
            {
                sb.Append(condition.variable);
                sb.Append("=");
                sb.Append(condition.value);
                sb.Append(",");
            }

            sb.Append(" -> ");
            sb.Append(effect.variable);
            sb.Append(":=");
            sb.Append(effect.value);

            return sb.ToString();
        }

        /// <summary>
        /// Gets the string describing preconditions of the conditional effect.
        /// </summary>
        /// <returns>String label for PlanningProblem transition graph.</returns>
        public string ToStringEffectCondition()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var condition in conditions)
            {
                sb.Append(condition.variable);
                sb.Append("=");
                sb.Append(condition.value);
                sb.Append(",");
            }

            return sb.ToString();
        }

		
	}
}
