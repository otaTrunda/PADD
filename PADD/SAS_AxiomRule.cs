using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Implementation of axiom rules in the SAS+ planning problem. Provides methods for applying axiom rules to the specified state.
    /// </summary>
    public class SASAxiomRules
    {
        /// <summary>
        /// List of axiom layers with axiom rules.
        /// </summary>
        private IList<IList<SASAxiomRule>> axiomLayers;

        /// <summary>
        /// Initial values of the affected variables.
        /// </summary>
        private IList<SASVariableValuePair> initValues;

        /// <summary>
        /// Constructs the axiom rules in the SAS+ planning problem.
        /// </summary>
        /// <param name="axiomsData">Axioms data.</param>
        /// <param name="variablesData">Variables data.</param>
        /// <param name="initState">Initial state.</param>
        public SASAxiomRules(List<SASInputData.AxiomRule> axiomsData, List<SASInputData.Variable> variablesData, SASState initState)
        {
            axiomLayers = new List<IList<SASAxiomRule>>();

            foreach (var axiomData in axiomsData)
            {
                SASAxiomRule axiomRule = new SASAxiomRule(axiomData, variablesData);
                int axiomLayer = axiomRule.GetAxiomLayer();

                while (axiomLayer >= axiomLayers.Count)
                    axiomLayers.Add(new List<SASAxiomRule>());
                axiomLayers[axiomLayer].Add(axiomRule);
            }

            initValues = new List<SASVariableValuePair>();
            for (int i = 0; i < variablesData.Count; ++i)
            {
                if (variablesData[i].AxiomLayer != -1)
                    initValues.Add(new SASVariableValuePair(i, initState.GetValue(i)));
            }
        }

        /// <summary>
        /// Applies defined axiom rules of the SAS+ planning problem to the given state.
        /// </summary>
        /// <param name="state">State the axiom rules will be applied to.</param>
        public void Apply(SASState state)
        {
            // firstly, set initial values for affected variables
            foreach (var initValue in initValues)
            {
                state.SetValue(initValue.variable, initValue.value);
            }

            // then, we evaluate rules on each axiom layer
            foreach (var axiomLayer in axiomLayers)
            {
                bool someRuleApplied = true;

                // we repeat applying the rules on the current layer, until there is no change
                while (someRuleApplied)
                {
                    someRuleApplied = false;
                    foreach (var axiomRule in axiomLayer)
                    {
                        if (axiomRule.IsApplicable(state))
                        {
                            if (axiomRule.Apply(state))
                                someRuleApplied = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the list of axiom rules at the specified axiom layer.
        /// </summary>
        /// <param name="axiomLayerIdx">Index of axiom layer.</param>
        /// <returns>List of axiom rules at the given axiom layer.</returns>
        public IList<SASAxiomRule> GetAxiomRuleList(int axiomLayerIdx)
        {
            return this[axiomLayerIdx];
        }

        /// <summary>
        /// Gets the list of axiom rules at the specified axiom layer. Short version of getAxiomRuleList(int).
        /// </summary>
        /// <param name="axiomLayerIdx">Index of axiom layer.</param>
        /// <returns>List of axiom rules at the given axiom layer.</returns>
        public IList<SASAxiomRule> this[int axiomLayerIdx]
        {
            get { return axiomLayers[axiomLayerIdx]; }
        }

        /// <summary>
        /// Gets the number of axiom layers.
        /// </summary>
        /// <returns>Number of axiom layers.</returns>
        public int Count
        {
            get { return axiomLayers.Count; }
        }
    }

    /// <summary>
    /// Implementation of an axiom rule in the SAS+ planning problem.
    /// </summary>
    public class SASAxiomRule
    {
        /// <summary>
        /// Conditions to be met for axiom rule to be applied.
        /// </summary>
        private SASOperatorPreconditions axiomRuleconditions;

        /// <summary>
        /// "Rule head" - actual effect of the axiom rule.
        /// </summary>
        private SASVariableValuePair axiomRuleHead;

        /// <summary>
        /// Axiom layer - affects the order of axiom rules applications.
        /// </summary>
        private int axiomLayerNo;

        /// <summary>
        /// Constructs axiom rule.
        /// </summary>
        /// <param name="axiomData">Axiom rule data.</param>
        /// <param name="variableData">Variables data.</param>
        public SASAxiomRule(SASInputData.AxiomRule axiomData, List<SASInputData.Variable> variablesData)
        {
            axiomRuleconditions = new SASOperatorPreconditions(axiomData.Conditions);
            axiomRuleHead = new SASVariableValuePair(axiomData.Effect.Variable, axiomData.Effect.Value);

            int affectedVariable = axiomRuleHead.variable;
            axiomLayerNo = variablesData[affectedVariable].AxiomLayer;
        }

        /// <summary>
        /// Gets axiom rule conditions.
        /// </summary>
        /// <returns>Axiom rule conditions.</returns>
        public SASOperatorPreconditions GetConditions()
        {
            return axiomRuleconditions;
        }

        /// <summary>
        /// Gets the axiom rule head.
        /// </summary>
        /// <returns>Axiom rule head.</returns>
        public SASVariableValuePair GetRuleHead()
        {
            return axiomRuleHead;
        }

        /// <summary>
        /// Gets the axiom layer.
        /// </summary>
        /// <returns>Axiom layer of the rule.</returns>
        public int GetAxiomLayer()
        {
            return axiomLayerNo;
        }

        /// <summary>
        /// Checks whether the axiom rule is applicable to the specified state.
        /// </summary>
        /// <param name="state">State to be checked.</param>
        /// <returns>True if the axiom rule is applicable, false otherwise.</returns>
        public bool IsApplicable(SASState state)
        {
            return axiomRuleconditions.IsApplicable(state);
        }

        /// <summary>
        /// Applies the rule to the specified state. Doesn't clone the passed state, just modifying it.
        /// </summary>
        /// <param name="state">State the rule will be applied to.</param>
        /// <returns>True if the affected variable was really changed, false otherwise.</returns>
        public bool Apply(SASState state)
        {
            if (state.HasValue(axiomRuleHead.variable, axiomRuleHead.value))
                return false;

            state.SetValue(axiomRuleHead.variable, axiomRuleHead.value);
            return true;
        }
    }
}
