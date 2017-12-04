using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Class encapsulating a single successor - applied grounded operator and the actual successor state (which is generated on-demand).
    /// </summary>
    public class Successor
    {
        /// <summary>
        /// Reference to the original state.
        /// </summary>
        private IState refState;

        /// <summary>
        /// Grounded applied operator.
        /// </summary>
        private IOperator appliedOperator;

        /// <summary>
        /// Constructs the successor entity.
        /// </summary>
        /// <param name="refState">Reference to the original state.</param>
        /// <param name="appliedOperator">Grounded applied operator.</param>
        public Successor(IState refState, IOperator appliedOperator)
        {
            this.refState = refState;
            this.appliedOperator = appliedOperator;
        }

        /// <summary>
        /// Gets the applied operator.
        /// </summary>
        /// <returns>Applied operator.</returns>
        public IOperator GetOperator()
        {
            return appliedOperator;
        }

        /// <summary>
        /// Gets the successor state. Lazy evaluated.
        /// </summary>
        /// <returns>Successor state.</returns>
        public IState GetSuccessorState()
        {
            return appliedOperator.Apply(refState);
        }

        /// <summary>
        /// Constructs a hash code used in the dictionaries, maps etc.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return refState.GetHashCode() + 17 * appliedOperator.GetHashCode();
        }

        /// <summary>
        /// Checks the equality of objects.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Successor succ = obj as Successor;
            if (succ == null)
                return false;
            return refState.Equals(succ.refState) && appliedOperator.Equals(succ.appliedOperator);
        }
    }

    /// <summary>
    /// Class encapsulating a collection of successors.
    /// </summary>
    public class Successors : IEnumerable<Successor>
    {
        /// <summary>
        /// List of single successors.
        /// </summary>
        private IList<Successor> succList;

        /// <summary>
        /// Constructs the successors entity.
        /// </summary>
        /// <param name="succList">List of single successors.</param>
        public Successors(IList<Successor> succList)
        {
            this.succList = succList;
        }

        /// <summary>
        /// Gets the applied operator.
        /// </summary>
        /// <param name="idx">Successor index in the collection.</param>
        /// <returns>Applied operator.</returns>
        public IOperator GetAppliedOperator(int idx)
        {
            return succList[idx].GetOperator();
        }

        /// <summary>
        /// Gets the successor state. Lazy evaluated.
        /// </summary>
        /// <param name="idx">Successor index in the collection.</param>
        /// <returns>Successor state.</returns>
        public IState GetSuccessorState(int idx)
        {
            return succList[idx].GetSuccessorState();
        }

        /// <summary>
        /// Gets the successor at the specified index.
        /// </summary>
        /// <param name="successorIdx">Index of successor object.</param>
        /// <returns>Successor at the specified index.</returns>
        public Successor this[int successorIdx]
        {
            get { return succList[successorIdx]; }
        }

        /// <summary>
        /// Gets the number of successors in the colection.
        /// </summary>
        /// <returns>Number of successors.</returns>
        public int Count
        {
            get { return succList.Count; }
        }

        /// <summary>
        /// Gets enumerator for the collection of successors.
        /// </summary>
        /// <returns>Enumerator of successor entries.</returns>
        public IEnumerator<Successor> GetEnumerator()
        {
            for (int i = 0; i < succList.Count; ++i)
                yield return succList[i];
        }

        /// <summary>
        /// Gets enumerator for the collection of successors.
        /// </summary>
        /// <returns>Enumerator of successor entries.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Converts the successor collection into the list. Should be only used for internal tests (not efficient).
        /// </summary>
        /// <returns>List of successors.</returns>
        public IList<Successor> ToList()
        {
            return new List<Successor>(succList);
        }
    }

    /// <summary>
    /// Class encapsulating a single predecessor - applied grounded operator and the actual predecessor state (which has to be
    /// actually stored, because of ambiguity).
    /// </summary>
    public class Predecessor
    {
        /// <summary>
        /// Predecessor state.
        /// </summary>
        private IState predState;

        /// <summary>
        /// Applied operator.
        /// </summary>
        private IOperator appliedOperator;

        /// <summary>
        /// Constructs the predecessor entity.
        /// </summary>
        /// <param name="predState">Predecessor state.</param>
        /// <param name="appliedOperator">Applied operator.</param>
        public Predecessor(IState predState, IOperator appliedOperator)
        {
            this.predState = predState;
            this.appliedOperator = appliedOperator;
        }

        /// <summary>
        /// Gets the applied operator.
        /// </summary>
        /// <returns>Applied operator.</returns>
        public IOperator GetOperator()
        {
            return appliedOperator;
        }

        /// <summary>
        /// Gets the predecessor state.
        /// </summary>
        /// <returns>Predecessor state.</returns>
        public IState GetPredecessorState()
        {
            return predState;
        }

        /// <summary>
        /// Constructs a hash code used in the dictionaries, maps etc.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return predState.GetHashCode() + 17 * appliedOperator.GetHashCode();
        }

        /// <summary>
        /// Checks the equality of objects.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Predecessor pred = obj as Predecessor;
            if (pred == null)
                return false;
            return predState.Equals(pred.predState) && appliedOperator.Equals(pred.appliedOperator);
        }
    }

    /// <summary>
    /// Class encapsulating a collection of predecessors.
    /// </summary>
    public class Predecessors : IEnumerable<Predecessor>
    {
        /// <summary>
        /// List of single predecessors.
        /// </summary>
        private IList<Predecessor> predList;

        /// <summary>
        /// Constructs the predecessors entity.
        /// </summary>
        /// <param name="predList">List of single predecessors.</param>
        public Predecessors(IList<Predecessor> predList)
        {
            this.predList = predList;
        }

        /// <summary>
        /// Gets the applied operator.
        /// </summary>
        /// <param name="idx">Predecessor index in the collection.</param>
        /// <returns>Applied operator.</returns>
        public IOperator GetAppliedOperator(int idx)
        {
            return predList[idx].GetOperator();
        }

        /// <summary>
        /// Gets the predecessor state.
        /// </summary>
        /// <param name="idx">Predecessor index in the collection.</param>
        /// <returns>Predecessor state.</returns>
        public IState GetPredecessorState(int idx)
        {
            return predList[idx].GetPredecessorState();
        }

        /// <summary>
        /// Gets the predecessor at the specified index.
        /// </summary>
        /// <param name="predecessorIdx">Index of predeccesor object.</param>
        /// <returns>Predecessor at the specified index.</returns>
        public Predecessor this[int predecessorIdx]
        {
            get { return predList[predecessorIdx]; }
        }

        /// <summary>
        /// Gets the number of predecessors in the colection.
        /// </summary>
        /// <returns>Number of predecessors.</returns>
        public int Count
        {
            get { return predList.Count; }
        }

        /// <summary>
        /// Gets enumerator for the collection of predecessors.
        /// </summary>
        /// <returns>Enumerator of predecessor entries.</returns>
        public IEnumerator<Predecessor> GetEnumerator()
        {
            for (int i = 0; i < predList.Count; ++i)
                yield return predList[i];
        }

        /// <summary>
        /// Gets enumerator for the collection of predecessors.
        /// </summary>
        /// <returns>Enumerator of predecessor entries.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Converts the predecessor collection into the list. Should be only used for internal tests (not efficient).
        /// </summary>
        /// <returns>List of predecessors.</returns>
        public IList<Predecessor> ToList()
        {
            return new List<Predecessor>(predList);
        }
    }
}
