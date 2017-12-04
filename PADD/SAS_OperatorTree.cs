using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Common interface for the nodes of SAS+ operator decision tree.
    /// </summary>
    public interface ISASOperatorTreeNode
    {
        /// <summary>
        /// Accepts evaluating visitor.
        /// </summary>
        /// <param name="visitor">Evaluating visitor.</param>
        void Accept(ISASOperatorTreeVisitor visitor);
    }

    /// <summary>
    /// Inner tree node of the SAS+ operator decision tree. Implies the actual setting of the tree to be traversed and evaluated.
    /// Each inner node has a decision variable and references for the subtrees representing each of the possible values of the decision
    /// variable (and also a subtree, where the decision variable actually doesn't matter).
    /// </summary>
    public class SASOperatorTreeDecisionNode : ISASOperatorTreeNode
    {
        /// <summary>
        /// The decision variable of the tree node.
        /// </summary>
        public int decisionVariable;

        /// <summary>
        /// The subtrees representing each possible value the decision variable.
        /// </summary>
        public ISASOperatorTreeNode[] successorsByValues;

        /// <summary>
        /// The subtree for the operators where the decision varaible doesn't matter.
        /// </summary>
        public ISASOperatorTreeNode dontCareSuccessor;

        /// <summary>
        /// Constructs a new SAS+ operator decision tree inner node.
        /// </summary>
        /// <param name="decisionVariable">Decision variable of the current subtree.</param>
        /// <param name="successorsByValues">Subtrees for all the possible values of the decision variable.</param>
        /// <param name="dontCareSuccessor">Subtree where the decision variable doesn't matter.</param>
        public SASOperatorTreeDecisionNode(int decisionVariable, ISASOperatorTreeNode[] successorsByValues, ISASOperatorTreeNode dontCareSuccessor)
        {
            this.decisionVariable = decisionVariable;
            this.successorsByValues = successorsByValues;
            this.dontCareSuccessor = dontCareSuccessor;
        }

        /// <summary>
        /// Accepts evaluating visitor.
        /// </summary>
        /// <param name="visitor">Evaluating visitor.</param>
        public void Accept(ISASOperatorTreeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Leaf tree node of the SAS+ operator decision tree. It holds the actual operators to be returned.
    /// </summary>
    public class SASOperatorTreeLeafNode : ISASOperatorTreeNode
    {
        /// <summary>
        /// The list of operators.
        /// </summary>
        public List<SASOperator> operatorsList;

        /// <summary>
        /// Constructs an empty SAS+ operator decision tree leaf node.
        /// </summary>
        /// <param name="operators">List of operators for the node.</param>
        public SASOperatorTreeLeafNode(List<SASOperator> operators = null)
        {
            if (operators == null)
                operators = new List<SASOperator>();
            operatorsList = operators;
        }

        /// <summary>
        /// Accepts evaluating visitor.
        /// </summary>
        /// <param name="visitor">Evaluating visitor.</param>
        public void Accept(ISASOperatorTreeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Common interface for the traversing of SAS+ operator decision tree.
    /// </summary>
    public interface ISASOperatorTreeVisitor
    {
        /// <summary>
        /// Visits the inner node of the SAS+ operator decision tree.
        /// </summary>
        /// <param name="treeNode">Inner node of the tree.</param>
        void Visit(SASOperatorTreeDecisionNode treeNode);

        /// <summary>
        /// Visits the leaf node of the SAS+ operator decision tree.
        /// </summary>
        /// <param name="treeNode">Leaf node of the tree.</param>
        void Visit(SASOperatorTreeLeafNode treeNode);
    }

    /// <summary>
    /// SAS+ operator decision tree visitor for collecting applicable operators.
    /// </summary>
    public class SASOperatorApplicableSelector : ISASOperatorTreeVisitor
    {
        /// <summary>
        /// Reference state.
        /// </summary>
        private SASState refState;

        /// <summary>
        /// Return list of operators.
        /// </summary>
        private IList<Successor> retList;

        /// <summary>
        /// Input trigger (how many operators to return supposed to be skipped).
        /// </summary>
        private int inputTrigger;

        /// <summary>
        /// Number of operators left to return.
        /// </summary>
        private int numOfOperatorsToReturn;

        /// <summary>
        /// Mutex manager. Successors have to comply with mutex constraints in the SAS+ planning problem.
        /// </summary>
        private SASMutexManager mutexManager;

        /// <summary>
        /// Constructs the SAS+ operator decision tree visitor for collecting applicable operators.
        /// </summary>
        public SASOperatorApplicableSelector(SASMutexGroups mutexGroups)
        {
            mutexManager = new SASMutexManager(mutexGroups);
			this.retList = new List<Successor>();			
        }

        /// <summary>
        /// Gets the list of applicable operators in the specified SAS+ operator decision tree.
        /// </summary>
        /// <param name="referState">Reference state.</param>
        /// <param name="treeNode">Decision tree root node.</param>
        /// <param name="inputTrigger">Input trigger.</param>
        /// <param name="numOfOperatorsToReturn">Number of operators to return.</param>
        /// <returns>List of applicable operators to the given state.</returns>
        public Successors GetApplicableOperators(SASState referState, ISASOperatorTreeNode treeNode, int inputOperTrigger, int operatorsToReturn)
        {
            refState = referState;
			retList.Clear();
			//retList = new List<Successor>();
			inputTrigger = inputOperTrigger;
            numOfOperatorsToReturn = operatorsToReturn;

            mutexManager.SetReferenceStateLocks(refState);

            treeNode.Accept(this);
            return new Successors(retList);
        }

        /// <summary>
        /// Visits the inner node of the SAS+ operator decision tree.
        /// </summary>
        /// <param name="treeNode">Inner node of the tree.</param>
        public void Visit(SASOperatorTreeDecisionNode treeNode)
        {
            if (numOfOperatorsToReturn == 0)
                return;

            treeNode.successorsByValues[refState.GetValue(treeNode.decisionVariable)].Accept(this);
            treeNode.dontCareSuccessor.Accept(this);
        }

        /// <summary>
        /// Visits the leaf node of the SAS+ operator decision tree.
        /// </summary>
        /// <param name="treeNode">Leaf node of the tree.</param>
        public void Visit(SASOperatorTreeLeafNode treeNode)
        {
            if (numOfOperatorsToReturn == 0)
                return;

            int skipNum = 0;
            if (inputTrigger > 0)
            {
                int left = (inputTrigger - treeNode.operatorsList.Count);
                if (left >= 0)
                {
                    inputTrigger = left;
                    return;
                }
                else
                {
                    skipNum = inputTrigger;
                    inputTrigger = 0;
                }
            }

            for (int i = skipNum; i < treeNode.operatorsList.Count; ++i)
            {
                SASOperator op = treeNode.operatorsList[i];
                //TODO tohle nejak blbne.. co to ma presne delat?
                //if (!mutexManager.CheckOperatorApplicability(op))
                //    continue;

                retList.Add(new Successor(refState, op));
                --numOfOperatorsToReturn;

                if (numOfOperatorsToReturn == 0)
                    break;
            }
        }
    }

    /// <summary>
    /// SAS+ operator decision tree visitor for finding a random applicable operator.
    /// </summary>
    public class SASOperatorRandomApplicableSelector : ISASOperatorTreeVisitor
    {
        /// <summary>
        /// Reference state.
        /// </summary>
        private SASState refState;

        /// <summary>
        /// Found random operator to be returned.
        /// </summary>
        private IOperator retOperator;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private Random randomizer;

        /// <summary>
        /// Mutex manager. Successors have to comply with mutex constraints in the SAS+ planning problem.
        /// </summary>
        private SASMutexManager mutexManager;

        /// <summary>
        /// Constructs the SAS+ operator decision tree visitor for finding a random applicable operator.
        /// </summary>
        public SASOperatorRandomApplicableSelector(SASMutexGroups mutexGroups)
        {
            randomizer = new Random();
            mutexManager = new SASMutexManager(mutexGroups);
        }

        /// <summary>
        /// Gets a random applicable operator in the specified SAS+ operator decision tree.
        /// </summary>
        /// <param name="referState">Reference state.</param>
        /// <param name="treeNode">Decision tree root node.</param>
        /// <returns>An applicable operator to the given state.</returns>
        public Successor GetRandomApplicableOperator(SASState referState, ISASOperatorTreeNode treeNode)
        {
            refState = referState;
            retOperator = null;
            mutexManager.SetReferenceStateLocks(refState);

            treeNode.Accept(this);

            if (retOperator != null)
                return new Successor(refState, retOperator);
            return null;
        }

        /// <summary>
        /// Visits the inner node of the SAS+ operator decision tree.
        /// </summary>
        /// <param name="treeNode">Inner node of the tree.</param>
        public void Visit(SASOperatorTreeDecisionNode treeNode)
        {
            int selectedSubtree = randomizer.Next(2);

            if (selectedSubtree == 0)
            {
                treeNode.successorsByValues[refState.GetValue(treeNode.decisionVariable)].Accept(this);
                if (retOperator != null) // result already found
                    return;
                treeNode.dontCareSuccessor.Accept(this);
            }
            else
            {
                treeNode.dontCareSuccessor.Accept(this);
                if (retOperator != null) // result already found
                    return;
                treeNode.successorsByValues[refState.GetValue(treeNode.decisionVariable)].Accept(this);
            }
        }

        /// <summary>
        /// Visits the leaf node of the SAS+ operator decision tree.
        /// </summary>
        /// <param name="treeNode">Leaf node of the tree.</param>
        public void Visit(SASOperatorTreeLeafNode treeNode)
        {
            if (retOperator != null) // result already found
                return;

            List<int> opIndices = new List<int>(treeNode.operatorsList.Count);
            for (int i = 0; i < treeNode.operatorsList.Count; ++i)
                opIndices.Add(i);

            while (opIndices.Count > 0)
            {
                int selectedIdx = randomizer.Next(opIndices.Count);
                SASOperator selectedOp = treeNode.operatorsList[opIndices[selectedIdx]];
                if (mutexManager.CheckOperatorApplicability(selectedOp))
                {
                    retOperator = selectedOp;
                    return;
                }
                opIndices.RemoveAt(selectedIdx);
            }
        }
    }

    /// <summary>
    /// SAS+ operator decision tree visitor for collecting relevant operators.
    /// </summary>
    public class SASOperatorRelevantSelector : ISASOperatorTreeVisitor
    {
        /// <summary>
        /// Reference state.
        /// </summary>
        private SASState refState;

        /// <summary>
        /// Return list of operators.
        /// </summary>
        private IList<Successor> retList;

        /// <summary>
        /// Mutex manager. Successors have to comply with mutex constraints in the SAS+ planning problem.
        /// </summary>
        private SASMutexManager mutexManager;

        /// <summary>
        /// Constructs the SAS+ operator decision tree visitor for collecting relevant operators.
        /// </summary>
        public SASOperatorRelevantSelector(SASMutexGroups mutexGroups)
        {
            mutexManager = new SASMutexManager(mutexGroups);
        }

        /// <summary>
        /// Gets the list of relevant operators in the specified SAS+ operator decision tree.
        /// </summary>
        /// <param name="referState">Reference state.</param>
        /// <param name="treeNode">Decision tree root node.</param>
        /// <returns>List of relevant operators to the given state.</returns>
        public Successors GetApplicableRelevantTransitions(SASState referState, ISASOperatorTreeNode treeNode)
        {
            refState = referState;
            retList = new List<Successor>();

            mutexManager.SetReferenceStateLocks(refState);

            treeNode.Accept(this);
            return new Successors(retList);
        }

        /// <summary>
        /// Visits the inner node of the SAS+ operator decision tree.
        /// </summary>
        /// <param name="treeNode">Inner node of the tree.</param>
        public void Visit(SASOperatorTreeDecisionNode treeNode)
        {
            foreach (var value in refState.GetAllValues(treeNode.decisionVariable))
                treeNode.successorsByValues[value].Accept(this);
            treeNode.dontCareSuccessor.Accept(this);
        }

        /// <summary>
        /// Visits the leaf node of the SAS+ operator decision tree.
        /// </summary>
        /// <param name="treeNode">Leaf node of the tree.</param>
        public void Visit(SASOperatorTreeLeafNode treeNode)
        {
            foreach (var oper in treeNode.operatorsList)
            {
                if (oper.IsRelevant(refState) && mutexManager.CheckOperatorApplicability(oper))
                    retList.Add(new Successor(refState, oper));
            }
        }
    }

    /// <summary>
    /// SAS+ operator decision tree visitor for finding a random relevant applicable operator.
    /// </summary>
    public class SASOperatorRandomRelevantSelector : ISASOperatorTreeVisitor
    {
        /// <summary>
        /// Reference state.
        /// </summary>
        private SASState refState;

        /// <summary>
        /// Found random operator to be returned.
        /// </summary>
        private IOperator retOperator;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private Random randomizer;

        /// <summary>
        /// Mutex manager. Successors have to comply with mutex constraints in the SAS+ planning problem.
        /// </summary>
        private SASMutexManager mutexManager;

        /// <summary>
        /// Constructs the SAS+ operator decision tree visitor for finding a random applicable operator.
        /// </summary>
        public SASOperatorRandomRelevantSelector(SASMutexGroups mutexGroups)
        {
            randomizer = new Random();
            mutexManager = new SASMutexManager(mutexGroups);
        }

        /// <summary>
        /// Gets a random applicable operator in the specified SAS+ operator decision tree.
        /// </summary>
        /// <param name="referState">Reference state.</param>
        /// <param name="treeNode">Decision tree root node.</param>
        /// <returns>An applicable operator to the given state.</returns>
        public Successor GetRandomApplicableRelevantTransition(SASState referState, ISASOperatorTreeNode treeNode)
        {
            refState = referState;
            retOperator = null;
            mutexManager.SetReferenceStateLocks(refState);

            treeNode.Accept(this);

            if (retOperator != null)
                return new Successor(refState, retOperator);
            return null;
        }

        /// <summary>
        /// Visits the inner node of the SAS+ operator decision tree.
        /// </summary>
        /// <param name="treeNode">Inner node of the tree.</param>
        public void Visit(SASOperatorTreeDecisionNode treeNode)
        {
            if (retOperator != null) // result already found
                return;

            List<int> allValues = refState.GetAllValues(treeNode.decisionVariable);
            allValues.Add(-2); // special value for "dontCareSuccessor" subtree

            while (allValues.Count > 0)
            {
                int selectedIdx = randomizer.Next(allValues.Count);
                int selectedValue = allValues[selectedIdx];

                if (selectedValue != -2)
                    treeNode.successorsByValues[selectedValue].Accept(this);
                else
                    treeNode.dontCareSuccessor.Accept(this);

                if (retOperator != null) // result already found
                    return;

                allValues.RemoveAt(selectedIdx);
            }
        }

        /// <summary>
        /// Visits the leaf node of the SAS+ operator decision tree.
        /// </summary>
        /// <param name="treeNode">Leaf node of the tree.</param>
        public void Visit(SASOperatorTreeLeafNode treeNode)
        {
            if (retOperator != null) // result already found
                return;

            List<int> opIndices = new List<int>(treeNode.operatorsList.Count);
            for (int i = 0; i < treeNode.operatorsList.Count; ++i)
                opIndices[i] = i;

            while (opIndices.Count > 0)
            {
                int selectedIdx = randomizer.Next(opIndices.Count);
                SASOperator selectedOp = treeNode.operatorsList[opIndices[selectedIdx]];
                if (selectedOp.IsRelevant(refState) && mutexManager.CheckOperatorApplicability(selectedOp))
                {
                    retOperator = selectedOp;
                    return;
                }
                opIndices.RemoveAt(selectedIdx);
            }
        }
    }

}
