using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Implementation of the (grounded) PDDL operator. Consists of a reference to the lifted operator version and a concrete operator substitution.
    /// </summary>
    public class PDDLOperator : IOperator
    {
        /// <summary>
        /// Reference to the lifted version of the operator.
        /// </summary>
        private PDDLOperatorLifted refOpLifted;

        /// <summary>
        /// Concrete substitution for the lifted operator.
        /// </summary>
        private PDDLOperatorSubstitution substit;

        /// <summary>
        /// Operator cost - is evaluated in the moment of grounding.
        /// </summary>
        private int operatorCost;

        /// <summary>
        /// Constructs an instance of the grounded PDDL operator.
        /// </summary>
        /// <param name="refOpLifted">Reference to the lifted PDDL operator.</param>
        /// <param name="substit">Concrete PDDL operator substitution.</param>
        /// <param name="sourceState">Source state from which is the operator grounded.</param>
        public PDDLOperator(PDDLOperatorLifted refOpLifted, PDDLOperatorSubstitution substit, IPDDLState sourceState)
        {
            this.refOpLifted = refOpLifted;
            this.substit = substit;
            this.operatorCost = refOpLifted.GetCost(sourceState, substit);
        }

        /// <summary>
        /// Gets the substitution of the operator.
        /// </summary>
        /// <returns>PDDL operator substitution.</returns>
        public PDDLOperatorSubstitution GetSubstitution()
        {
            return substit;
        }

        /// <summary>
        /// Checks whether the operator is relevant (in context of planning search) to the given state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator is relevant to the given state, false otherwise.</returns>
        public bool IsRelevant(IState state)
        {
            return refOpLifted.IsRelevant((IPDDLState)state, substit);
        }

        /// <summary>
        /// Checks whether the operator is applicable (in context of planning search) to the given state.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator is applicable to the given state, false otherwise.</returns>
        public bool IsApplicable(IState state)
        {
            return refOpLifted.IsApplicable((IPDDLState)state, substit);
        }

        /// <summary>
        /// Checks whether the operator can be predecessor to the given state in the planning search process.
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>True if the operator can be predecessor to the given state, false otherwise.</returns>
        public bool CanBePredecessor(IState state)
        {
            return refOpLifted.CanBePredecessor((IPDDLState)state, substit);
        }

        /// <summary>
        /// Applies the operator to the given state. The result is a new state (successor).
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Successor state to the given state.</returns>
        public IState Apply(IState state)
        {
            return refOpLifted.Apply((IPDDLState)state, substit);
        }

        /// <summary>
        /// Applies the operator backwards to the given state. The result is a set of states (possible predecessors).
        /// </summary>
        /// <param name="state">Reference state.</param>
        /// <returns>Possible predecessor states to the given state.</returns>
        public List<IState> ApplyBackwards(IState state)
        {
            return refOpLifted.ApplyBackwards((IPDDLState)state, substit);
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
            return refOpLifted.GetOrderIndex();
        }

        /// <summary>
        /// Constructs a hash code used in the dictionaries, maps etc.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return refOpLifted.GetHashCode() + 31 * substit.GetHashCode();
        }

        /// <summary>
        /// Checks the equality of objects.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            PDDLOperator op = obj as PDDLOperator;
            if (op == null)
                return false;

            if (refOpLifted != op.refOpLifted)
                return false;

            if (!substit.Equals(op.substit))
                return false;

            return true;
        }

        /// <summary>
        /// Constructs a string representing the operator.
        /// </summary>
        /// <returns>String representation of the operator.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(refOpLifted.GetName());
            sb.Append("(");

            bool bFirst = true;
            for (int varID = 0; varID < substit.GetVarCount(); ++varID)
            {
                int conID = substit.GetValue(varID);
                if (bFirst)
                    bFirst = false;
                else
                    sb.Append(",");

                sb.Append(refOpLifted.GetProblem().GetIDManager().GetConstantsMapping().GetStringForConstID(conID));
            }

            sb.Append(")");
            return sb.ToString();
        }
    }
}
