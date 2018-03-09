using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wintellect;
using UAM.Kora;

namespace PADD
{
    public interface IHeap<Key, Value> where Key : IComparable
    {
        void insert(Key k, Value v);
        Value getMin();
        Key getMinKey();
        Value removeMin();
        bool remove(Value v);
        bool change(Value v, Key newKey);
        int size();

        void clear();

        string getName();

		IEnumerable<(Key k, Value v)> getAllElements();
    }
}

namespace PADD.Heaps
{
    public class RegularBinaryHeap<Value> : IHeap<double, Value>
    {
        private class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }
            public int index { get; set; }

            public TreeNode(TheValue value, Key k, int index)
            {
                this.val = value;
                this.key = k;
                this.index = index;
            }

            public override string ToString()
            {
                return "Key :" + key + " index:" + index;
            }
        }

        private IList<TreeNode<double, Value>> tree;

        private bool isRoot(TreeNode<double, Value> t)
        {
            return t.index == 0;
        }

        private bool isLeaf(TreeNode<double, Value> t)
        {
            return getLeftSuccesor(t) == null;
        }

        private TreeNode<double, Value> getPredecessor(TreeNode<double, Value> t)
        {
            return t.index == 0 ? null : tree[(t.index - 1) / 2];
        }

        private TreeNode<double, Value> getLeftSuccesor(TreeNode<double, Value> t)
        {
            int index = t.index * 2 + 1;
            return tree.Count > index ? tree[index] : null;
        }

        private TreeNode<double, Value> getRightSuccesor(TreeNode<double, Value> t)
        {
            int index = t.index * 2 + 2;
            return tree.Count > index ? tree[index] : null;
        }

        public bool isEmpty()
        {
            return tree.Count == 0;
        }

        private void checkUp(TreeNode<double, Value> node)
        {
            TreeNode<double, Value> current = node,
                predecessor = getPredecessor(current);
            while (!isRoot(current) && current.key < predecessor.key)
            {
                swap(current, predecessor);
                predecessor = getPredecessor(current);
            }
        }

        private void swap(TreeNode<double, Value> current, TreeNode<double, Value> predecessor)
        {
            TreeNode<double, Value> stored = tree[current.index];

            tree[current.index] = tree[predecessor.index];
            tree[predecessor.index] = stored;

            int storedIndex = current.index;
            current.index = predecessor.index;
            predecessor.index = storedIndex;
        }

        private void checkDown(TreeNode<double, Value> node)
        {
            TreeNode<double, Value> current = node,
                succesor = null,
                succesorLeft = getLeftSuccesor(current),
                succesorRight = getRightSuccesor(current);

            if (succesorLeft != null)
            {
                if (succesorRight == null)
                    succesor = succesorLeft;
                else
                    succesor = (succesorLeft.key < succesorRight.key ? succesorLeft : succesorRight);

                while (succesor.key < current.key && !isLeaf(current))
                {
                    swap(current, succesor);

                    succesorLeft = getLeftSuccesor(current);
                    succesorRight = getRightSuccesor(current);
                    if (succesorLeft != null)
                    {
                        if (succesorRight == null)
                            succesor = succesorLeft;
                        else
                            succesor = (succesorLeft.key < succesorRight.key ? succesorLeft : succesorRight);
                    }
                }
            }
        } 

        public RegularBinaryHeap()
        {
            this.tree = new List<TreeNode<double, Value>>();
        }

        #region Heap<int,Value> Members

        public void insert(double k, Value v)
        {
            TreeNode<double, Value> newNode = new TreeNode<double, Value>(v, k, tree.Count);
            tree.Add(newNode);
            checkUp(newNode);
        }

        public Value getMin()
        {
            return (tree.Count > 0 ? tree[0].val : default(Value));
        }

        public Value removeMin()
        {
            Value result = tree[0].val;
            swap(tree[0], tree[tree.Count - 1]);
            tree.RemoveAt(tree.Count - 1);
            if (!isEmpty())
                checkDown(tree[0]);
            return result;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, double newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return tree.Count;
        }

        public double getMinKey()
        {
            return (tree.Count > 0 ? tree[0].key : -1);
        }

        public string getName()
        {
            return "Regular binary heap";
        }
        public void clear()
        {
            this.tree = new List<TreeNode<double, Value>>();
        }

		public IEnumerable<(double k, Value v)> getAllElements()
		{
			throw new NotImplementedException();
		}

		#endregion
	}

    public class RegularTernaryHeap<Value> : IHeap<double, Value>
    {
        private class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }
            public int index { get; set; }

            public TreeNode(TheValue value, Key k, int index)
            {
                this.val = value;
                this.key = k;
                this.index = index;
            }

            public override string ToString()
            {
                return "Key :" + key + " index:" + index;
            }
        }

        private IList<TreeNode<double, Value>> tree;

        private bool isRoot(TreeNode<double, Value> t)
        {
            return t.index == 0;
        }

        private bool isLeaf(TreeNode<double, Value> t)
        {
            return getLeftSuccesor(t) == null;
        }

        private TreeNode<double, Value> getPredecessor(TreeNode<double, Value> t)
        {
            return t.index == 0 ? null : tree[(t.index - 1) / 3];
        }

        private TreeNode<double, Value> getLeftSuccesor(TreeNode<double, Value> t)
        {
            int index = t.index * 3 + 1;
            return tree.Count > index ? tree[index] : null;
        }

        private TreeNode<double, Value> getCentralSuccesor(TreeNode<double, Value> t)
        {
            int index = t.index * 3 + 2;
            return tree.Count > index ? tree[index] : null;
        }

        private TreeNode<double, Value> getRightSuccesor(TreeNode<double, Value> t)
        {
            int index = t.index * 3 + 3;
            return tree.Count > index ? tree[index] : null;
        }

        public bool isEmpty()
        {
            return tree.Count == 0;
        }

        private void checkUp(TreeNode<double, Value> node)
        {
            TreeNode<double, Value> current = node,
                predecessor = getPredecessor(current);
            while (!isRoot(current) && current.key < predecessor.key)
            {
                swap(current, predecessor);
                predecessor = getPredecessor(current);
            }
        }

        private void swap(TreeNode<double, Value> current, TreeNode<double, Value> predecessor)
        {
            TreeNode<double, Value> stored = tree[current.index];

            tree[current.index] = tree[predecessor.index];
            tree[predecessor.index] = stored;

            int storedIndex = current.index;
            current.index = predecessor.index;
            predecessor.index = storedIndex;
        }

        private TreeNode<double, Value> getSmallestSuccesor(TreeNode<double, Value> current)
        {
            TreeNode<double, Value> result = null,
                left = getLeftSuccesor(current);
            if (left == null)
                return null;

            TreeNode<double, Value> central = getCentralSuccesor(current);
            if (central == null)
                return left;

            result = central.key < left.key ? central : left;

            TreeNode<double, Value> right = getRightSuccesor(current);
            if (right == null)
                return result;

            result = result.key < right.key ? result : right;
            return result;
        }

        private void checkDown(TreeNode<double, Value> node)
        {
            while (!isLeaf(node))
            {
                swap(node, getSmallestSuccesor(node));
            }
        }

        public RegularTernaryHeap()
        {
            this.tree = new List<TreeNode<double, Value>>();
        }

        #region Heap<int,Value> Members

        public void insert(double k, Value v)
        {
            TreeNode<double, Value> newNode = new TreeNode<double, Value>(v, k, tree.Count);
            tree.Add(newNode);
            checkUp(newNode);
        }

        public Value getMin()
        {
            return (tree.Count > 0 ? tree[0].val : default(Value));
        }

        public Value removeMin()
        {
            Value result = tree[0].val;
            swap(tree[0], tree[tree.Count - 1]);
            tree.RemoveAt(tree.Count - 1);
            if (!isEmpty())
                checkDown(tree[0]);
            return result;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, double newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return tree.Count;
        }

        public double getMinKey()
        {
            return (tree.Count > 0 ? tree[0].key : -1);
        }

        public string getName()
        {
            return "Regular ternary heap";
        }
        public void clear()
        {
            this.tree = new List<TreeNode<double, Value>>();
        }

		public IEnumerable<(double k, Value v)> getAllElements()
		{
			throw new NotImplementedException();
		}

		#endregion
	}

    public class LeftistHeap<Value> : IHeap<double, Value>
    {
        private class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }
            public int npl { get; set; }
            public TreeNode<Key, TheValue> ancestor,
                leftSuccesor,
                rightSuccesor;

            public bool isRoot()
            {
                return this.ancestor == null;
            }

            public bool isLeaf()
            {
                return leftSuccesor == null;
            }

            public TreeNode(TheValue value, Key k, int npl)
            {
                this.val = value;
                this.key = k;
                this.npl = npl;
            }

            public override string ToString()
            {
                return "Key: " + key + " npl: " + npl;
            }
        }

        private TreeNode<double, Value> root;
        private int count;

        private int getNpl(TreeNode<double, Value> node)
        {
            if (node == null)
                return -1;
            return node.npl;
        }

        private TreeNode<double, Value> merge(TreeNode<double, Value> first, TreeNode<double, Value> second)
        {
            if (first == null)
                return second;
            if (second == null)
                return first;
            if (first.key > second.key)
                return merge(second, first);

            TreeNode<double, Value> newRight = merge(first.rightSuccesor, second);
            first.rightSuccesor = newRight;
            newRight.ancestor = first;
            if (getNpl(first.rightSuccesor) > getNpl(first.leftSuccesor))
            {
                TreeNode<double, Value> stored = first.leftSuccesor;
                first.leftSuccesor = first.rightSuccesor;
                first.rightSuccesor = stored;
            }
            first.npl = getNpl(first.rightSuccesor) + 1;

            return first;
        }

        #region IHeap<int,Value> Members

        public void insert(double k, Value v)
        {
            TreeNode<double, Value> newNode = new TreeNode<double, Value>(v, k, 0);
            root = merge(root, newNode);
            count++;
        }

        public Value getMin()
        {
            return root.val;
        }

        public Value removeMin()
        {
            Value result = root.val;
            root = merge(root.leftSuccesor, root.rightSuccesor);
            count--;
            return result;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, double newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return count;
        }

        public double getMinKey()
        {
            return root.key;
        }
        public string getName()
        {
            return "Leftist heap";
        }
        public void clear()
        {
            this.root = null;
            this.count = 0;
        }

		public IEnumerable<(double k, Value v)> getAllElements()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Constructors

		public LeftistHeap()
        {
            this.root = null;
            this.count = 0;
        }

        private LeftistHeap(TreeNode<double, Value> root, int size)
        {
            this.root = root;
            this.count = size;
        }

        #endregion Constructors

    }

    public class BinomialHeap<Value> : IHeap<double, Value>
    {
        private class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }
            public int rank;
            public TreeNode<Key, TheValue> ancestor;
            public List<TreeNode<Key, TheValue>> succesors;

            public bool isRoot()
            {
                return this.ancestor == null;
            }

            public bool isLeaf()
            {
                return succesors.Count == 0;
            }

            public TreeNode(TheValue value, Key k, int rank)
            {
                this.val = value;
                this.key = k;
                this.rank = rank;
                this.succesors = new List<BinomialHeap<Value>.TreeNode<Key, TheValue>>();
            }

            public override string ToString()
            {
                return "Key: " + key;
            }
        }

        private int count = 0;

        private TreeNode<double, Value> join(TreeNode<double, Value> first, TreeNode<double, Value> second)
        {
            if (first.key > second.key)
                return join(second, first);
            second.ancestor = first;
            first.succesors.Add(second);
            first.rank += 1;
            return first;
        }

        private LinkedList<TreeNode<double, Value>> trees;

        private LinkedList<TreeNode<double, Value>> merge(LinkedList<TreeNode<double, Value>> first,
            LinkedList<TreeNode<double, Value>> second)
        {
            first.AddLast(second.First);
            return first;
        }

        private LinkedList<TreeNode<double, Value>> repair(List<TreeNode<double, Value>>[] list)
        {
            LinkedList<TreeNode<double, Value>> result = new LinkedList<BinomialHeap<Value>.TreeNode<double, Value>>();
            for (int i = 0; i < list.Length; i++)
            {
                while(list[i].Count > 1)
                {
                    TreeNode<double, Value> first = list[i][0];
                    TreeNode<double, Value> second = list[i][1];
                    list[i].RemoveAt(1);
                    list[i].RemoveAt(0);
                    list[i + 1].Add(join(first, second));
                }
                if (list[i].Count > 0)
                    result.AddLast(new LinkedListNode<TreeNode<double, Value>>(list[i][0]));
            }
            return result;
        }

        #region Constructors

        public BinomialHeap()
        {
            this.count = 0;
            this.trees = new LinkedList<BinomialHeap<Value>.TreeNode<double, Value>>();
        }

        #endregion Constructors

        #region IHeap<double,Value> Members

        public void insert(double k, Value v)
        {
            LinkedListNode<TreeNode<double, Value>> newnode =
                new LinkedListNode<TreeNode<double, Value>>(new TreeNode<double, Value>(v, k, 0));
            trees.AddFirst(newnode);
            count++;
        }

        public Value getMin()
        {
            if (trees.First == null)
                return default(Value);
            TreeNode<double, Value> min = trees.First.Value;
            List<TreeNode<double, Value>>[] byRank =
                new List<BinomialHeap<Value>.TreeNode<double, Value>>[(int)Math.Log(count, 2) + 1];
            byRank.Initialize();
            foreach (TreeNode<double, Value> item in trees)
            {
                if (item.key < min.key)
                    min = item;
                byRank[item.rank].Add(item);
            }
            trees = repair(byRank);

            return min.val;
        }

        public Value removeMin()
        {
            if (trees.First == null)
                return default(Value);
            TreeNode<double, Value> min = trees.First.Value;
            List<TreeNode<double, Value>>[] byRank =
                new List<BinomialHeap<Value>.TreeNode<double, Value>>[(int)Math.Log(count, 2) + 1];
            for (int i = 0; i < byRank.Length; i++)
            {
                byRank[i] = new List<TreeNode<double, Value>>();
            }
            foreach (TreeNode<double, Value> item in trees)
            {
                if (item.key < min.key)
                    min = item;
                byRank[item.rank].Add(item);
            }
            byRank[min.rank].Remove(min);
            foreach (TreeNode<double, Value> item in min.succesors)
            {
                byRank[item.rank].Add(item);
            }
            trees = repair(byRank);
            count--;
            return min.val;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, double newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return count;
        }

        public double getMinKey()
        {
            return trees.Min(a => a.key);
        }
        public string getName()
        {
            return "Binomial heap";
        }
        public void clear()
        {
            this.count = 0;
            this.trees = new LinkedList<BinomialHeap<Value>.TreeNode<double, Value>>();
        }

		public IEnumerable<(double k, Value v)> getAllElements()
		{
			throw new NotImplementedException();
		}

		#endregion

	}

    public class SortedSetHeap<Value> : IHeap<double, Value>
    {
        private static uint IDHolder = 0;
        protected struct Pair : IComparable
        {
            public double key;
            public uint ID;

            public Pair(double key, uint ID)
            {
                this.key = key;
                this.ID = ID;
            }

            public int CompareTo(object obj)
            {
                if (obj is Pair)
                {
                    Pair p = (Pair)obj;
                    return this.key - p.key == 0d ? (int)(this.ID - p.ID) : (int)(this.key - p.key);
                }
                return 0;
            }
        }

        private readonly SortedSet<KeyValuePair<Pair, Value>> _sortedSet;

        #region IHeap<int,Value> Members

        void IHeap<double, Value>.insert(double k, Value v)
        {
            _sortedSet.Add(new KeyValuePair<Pair, Value>(new Pair(k, IDHolder++), v));
        }

        Value IHeap<double, Value>.getMin()
        {
            return _sortedSet.Min.Value;
        }

        double IHeap<double, Value>.getMinKey()
        {
            return _sortedSet.Min.Key.key;
        }

        Value IHeap<double, Value>.removeMin()
        {
            var min = _sortedSet.Min;
            _sortedSet.Remove(min);
            return min.Value;
        }

        bool IHeap<double, Value>.remove(Value v)
        {
            throw new NotImplementedException();
        }

        bool IHeap<double, Value>.change(Value v, double newKey)
        {
            throw new NotImplementedException();
        }

        int IHeap<double, Value>.size()
        {
            return _sortedSet.Count;
        }

        void IHeap<double, Value>.clear()
        {
            _sortedSet.Clear();
        }

        string IHeap<double, Value>.getName()
        {
            return "Sorted Set Heap";
        }

		public IEnumerable<(double k, Value v)> getAllElements()
		{
			throw new NotImplementedException();
		}

		#endregion

		public SortedSetHeap()
        {
            _sortedSet = new SortedSet<KeyValuePair<Pair, Value>>(new KeyValueComparer<Value>());
        }

        protected class KeyValueComparer<V> : IComparer<KeyValuePair<Pair, V>>
        {
            public int Compare(KeyValuePair<Pair, V> x, KeyValuePair<Pair, V> y)
            {
                var res = (int)(x.Key.key - y.Key.key);
                return res == 0 ? (int)(x.Key.ID - y.Key.ID) : res;
            }
        }
    }

    public class SortedDictionaryHeap<Value> : IHeap<int, Value>
    {
        private static uint IDHolder = 0;
        private struct Pair
        {
            public int key;
            public uint ID;

            public Pair(int key, uint ID)
            {
                this.key = key;
                this.ID = ID;
            }
        }

        private SortedDictionary<Pair, Value> items;

        #region IHeap<int,Value> Members

        public void insert(int k, Value v)
        {
            items.Add(new Pair(k, IDHolder++), v);
        }

        public Value getMin()
        {
            foreach (var item in items.Keys)
            {
                return items[item];
            }
            return default(Value);
        }

        public Value removeMin2()
        {
            foreach (var item in items.Keys)
            {
                Value result = items[item];
                items.Remove(item);
                return result;
            }
            return default(Value);
        }

        public Value removeMin()
        {
            var result = items.First();
            items.Remove(result.Key);
            return result.Value;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, int newKey)
        {
            if (remove(v))
            {
                insert(newKey, v);
                return true;
            }
            insert(newKey, v);
            return false;
        }

        public int size()
        {
            return items.Count;
        }

        public int getMinKey()
        {
            foreach (var item in items.Keys)
            {
                return item.key;
            }
            return -1;
        }
        public string getName()
        {
            return "Sorted Dictionary heap";
        }
        public void clear()
        {
            items.Clear();
        }

		public IEnumerable<(int k, Value v)> getAllElements()
		{
			throw new NotImplementedException();
		}

		#endregion

		public SortedDictionaryHeap()
        {
            items = new SortedDictionary<Pair, Value>(new PairComparer());
        }

        private class PairComparer : IComparer<Pair>
        {
            public int Compare(Pair x, Pair y)
            {
                var res = x.key - y.key;
                return res == 0 ? (int)(x.ID - y.ID) : res;
            }
        }
    }

    public class SingleBucket<Value> : IHeap<int, Value>
    {
        protected class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }

            public TreeNode(TheValue value, Key k)
            {
                this.val = value;
                this.key = k;
            }

            public override string ToString()
            {
                return "Key :" + key + " value:" + val.ToString();
            }
        }

        protected int initialSize;

        protected List<TreeNode<int,Value>>[] buckets;
        
        /// <summary>
        /// Size of the hash array. Range of the elements. MUST BE GREATER THAN DIFFERENCE BETWEEN THE HIGHEST POSSIBLE KEY AND THE LOWEST POSSIBLE KEY !
        /// </summary>
        protected int C;

        /// <summary>
        /// The minimum key stored in the structure 
        /// </summary>
        protected int minKey;

        /// <summary>
        /// Position of the minimum element
        /// </summary>
        protected int minPos;

        /// <summary>
        /// Number of elements in the structure
        /// </summary>
        protected int n;

        protected void reHashWithLargerSize()
        {
            var oldBuckets = this.buckets;
            this.C = C * 2;
            if (C >= 32000000)
                throw new OutOfMemoryException();
            this.n = 0;
            this.minPos = -1;
            this.minKey = int.MaxValue;
            this.buckets = new List<TreeNode<int, Value>>[C + 1];
            for (int i = 0; i < C + 1; i++)
            {
                this.buckets[i] = new List<TreeNode<int, Value>>();
            }
            foreach (var bucket in oldBuckets)
            {
                foreach (var item in bucket)
                {
                    insert(item);
                }
            }
        }

        protected void insertTo(int bucket, int key, Value val)
        {
            buckets[bucket].Add(new TreeNode<int, Value>(val, key));
        }

        protected void insertTo(int bucket, TreeNode<int, Value> node)
        {
            buckets[bucket].Add(node);
        }

        protected void insert(TreeNode<int, Value> node)
        {
            this.n++;
            int pos = node.key % (C + 1);
            if (node.key < minKey)
            {
                minPos = pos;
                minKey = node.key;
            }
            insertTo(pos, node);
        }

        public void insert(int k, Value v)
        {
            while (n > 0 && k > minKey + C)
            {
                Console.WriteLine("Insuficient heap limit. Rebuilding the heap.");
                reHashWithLargerSize();
            }       

            this.n++;
            int pos = k % (C+1);
            if (k < minKey)
            {
                minPos = pos;
                minKey = k;
            }
            insertTo(pos, k, v);
        }

        public Value getMin()
        {
            foreach (var item in buckets[minPos])
            {
                if (item.key == minKey)
                    return item.val;
            }
            return default(Value);
        }

        public int getMinKey()
        {
            return minKey;
        }

        public Value removeMin()
        {
            /*
            int minKeyTest = getMinKey_TotalSearch();
            if (minKey != minKeyTest)
            {
                throw new Exception();
            }
            */

            Value x = default(Value);
            for (int i = 0; i < buckets[minPos].Count; i++)
            {
                if (buckets[minPos][i].key == minKey)
                {
                    x = buckets[minPos][i].val;
                    buckets[minPos].RemoveAt(i);
                    break;
                }
            }
            n = n - 1;
            if (n > 0)
            {
                while(/*buckets[minPos] = null ||*/ buckets[minPos].Count == 0)
                    minPos = (minPos + 1) % (C + 1);
                minKey = buckets[minPos].Min(a => a.key);
            }
            else minKey = int.MaxValue;
            return x;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return n;
        }
        
        public string getName()
        {
            return "Single bucket heap";
        }
        
        public void clear()
        {
            this.C = initialSize;
            this.n = 0;
            this.minPos = -1;
            this.minKey = int.MaxValue;
            this.buckets = new List<TreeNode<int, Value>>[C + 1];
            for (int i = 0; i < C + 1; i++)
            {
                this.buckets[i] = new List<TreeNode<int, Value>>();
            }
        }

        public SingleBucket(int C)
        {
            this.initialSize = C;
            this.n = 0;
            this.C = C;
            this.minPos = -1;
            this.minKey = int.MaxValue;
            this.buckets = new List<TreeNode<int, Value>>[C + 1];
            for (int i = 0; i < C+1; i++)
            {
                this.buckets[i] = new List<TreeNode<int, Value>>();
            }
        }
    
        /// <summary>
        /// Returns minimal key stored in the structure, using total search - very ineffective, used only for testing correctness
        /// </summary>
        /// <returns></returns>
        protected int getMinKey_TotalSearch()
        {
            int min = int.MaxValue;
            for (int i = 0; i < buckets.Length; i++)
            {
                foreach (var item in buckets[i])
                {
                    if (item.key < min)
                        min = item.key;
                }
            }
            return min;
        }

		public IEnumerable<(int k, Value v)> getAllElements()
		{
			throw new NotImplementedException();
		}
	}

    public class RedBlackTreeHeap<Value> : IHeap<double, Value>
    {
        Wintellect.PowerCollections.OrderedBag<Wintellect.PowerCollections.Pair<double, Value>> structure;

        public void insert(double k, Value v)
        {
            structure.Add(new Wintellect.PowerCollections.Pair<double, Value>(k, v));
        }

        public Value getMin()
        {
            throw new NotImplementedException();
        }

        public double getMinKey()
        {
			return structure.First().First;
        }

        public Value removeMin()
        {
            return structure.RemoveFirst().Second;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, double newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return structure.Count;
        }
        public string getName()
        {
            return "Wintelect Red-Black Tree Heap";
        }
        public void clear()
        {
            this.structure.Clear();
        }

		public IEnumerable<(double k, Value v)> getAllElements()
		{
			foreach (var item in structure)
			{
				yield return (item.First, item.Second);
			}
		}

		public RedBlackTreeHeap()
        {
            this.structure = new Wintellect.PowerCollections.OrderedBag<Wintellect.PowerCollections.Pair<double, Value>>((a, b) => (int)(a.First - b.First));
        }
    }

    public class FibonacciHeap1<TValue> : IHeap<double, TValue>
    {
        private List<Node> _root = new List<Node>();
        int _count;
        Node _min;

        public void insert(double k, TValue v)
        {
            Insert(new Node
            {
                Key = k,
                Value = v
            });
        }

        public KeyValuePair<double, TValue> Pop()
        {
            if (_min == null)
                throw new InvalidOperationException();
            var min = ExtractMin();
            return new KeyValuePair<double, TValue>(min.Key, min.Value);
        }

        void Insert(Node node)
        {
            _count++;
            _root.Add(node);
            if (_min == null)
            {
                _min = node;
            }
            else if (node.Key < _min.Key)
            {
                _min = node;
            }
        }

        Node ExtractMin()
        {
            var result = _min;
            if (result == null)
                return null;
            foreach (var child in result.Children)
            {
                child.Parent = null;
                _root.Add(child);
            }
            _root.Remove(result);
            if (_root.Count == 0)
            {
                _min = null;
            }
            else
            {
                _min = _root[0];
                Consolidate();
            }
            _count--;
            return result;
        }

        void Consolidate()
        {
            var a = new Node[UpperBound()];
            for (int i = 0; i < _root.Count; i++)
            {
                var x = _root[i];
                var d = x.Children.Count;
                while (true)
                {
                    var y = a[d];
                    if (y == null)
                        break;
                    if (x.Key > y.Key)
                    {
                        var t = x;
                        x = y;
                        y = t;
                    }
                    _root.Remove(y);
                    i--;
                    x.AddChild(y);
                    y.Mark = false;
                    a[d] = null;
                    d++;
                }
                a[d] = x;
            }
            _min = null;
            for (int i = 0; i < a.Length; i++)
            {
                var n = a[i];
                if (n == null)
                    continue;
                if (_min == null)
                {
                    _root.Clear();
                    _min = n;
                }
                else
                {
                    if (n.Key < _min.Key)
                    {
                        _min = n;
                    }
                }
                _root.Add(n);
            }
        }

        int UpperBound()
        {
            return (int)Math.Floor(Math.Log(_count, (1.0 + Math.Sqrt(5)) / 2.0)) + 1;
        }

        class Node
        {
            public double Key;
            public TValue Value;
            public Node Parent;
            public List<Node> Children = new List<Node>();
            public bool Mark;

            public void AddChild(Node child)
            {
                child.Parent = this;
                Children.Add(child);
            }

            public override string ToString()
            {
                return string.Format("({0},{1})", Key, Value);
            }
        }

        public double getMinKey()
        {
            if (_min == null)
                throw new InvalidOperationException();
            return _min.Key;
        }

        public TValue getMin()
        {
            if (_min == null)
                throw new InvalidOperationException();
            return _min.Value;
        }

        public TValue removeMin()
        {
            if (_min == null)
                throw new InvalidOperationException();
            var min = ExtractMin();
            return min.Value;
        }

        public bool remove(TValue v)
        {
            throw new NotImplementedException();
        }

        public bool change(TValue v, double newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return this._count;
        }

        public void clear()
        {
            _root = new List<Node>();
            _count = 0;
            _min = null;              
        }

        public string getName()
        {
            return "Fibbonaci heap-1";
        }

		public IEnumerable<(double k, TValue v)> getAllElements()
		{
			foreach (var r in _root)
			{
				foreach (var item in enumerateChildrenRecur(r))
				{
					yield return item;
				}
			} 
		}

		private IEnumerable<(double k, TValue v)> enumerateChildrenRecur(Node n)
		{
			yield return (n.Key, n.Value);
			foreach (var ch in n.Children)
			{
				foreach (var item in enumerateChildrenRecur(ch))
				{
					yield return item;
				}
			}
		}
	}

    public class RadixHeap<Value> : IHeap<int, Value>
    {
        protected class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }

            public TreeNode(TheValue value, Key k)
            {
                this.val = value;
                this.key = k;
            }

            public override string ToString()
            {
                return "Key :" + key + " value:" + val.ToString();
            }
        }

        protected List<TreeNode<int, Value>>[] buckets;

        protected int[] bounds;

        /// <summary>
        /// Range of the elements. MUST BE GREATER THAN DIFFERENCE BETWEEN THE HIGHEST POSSIBLE KEY AND THE LOWEST POSSIBLE KEY !
        /// </summary>
        protected int C;

        /// <summary>
        /// Size of the hash array. Depends on C
        /// </summary>
        protected int B;

        /// <summary>
        /// Number of elements in the structure
        /// </summary>
        protected int n;

        void IHeap<int, Value>.insert(int k, Value v)
        {
            int i = B - 1;
            while (bounds[i] > k) i--;
            buckets[i].Add(new TreeNode<int, Value>(v, k));
            n++;
        }

        Value IHeap<int, Value>.getMin()
        {
            throw new NotImplementedException();
        }

        int IHeap<int, Value>.getMinKey()
        {
            throw new NotImplementedException();
        }

        Value IHeap<int, Value>.removeMin()
        {
            int i = 0, j = 0;
            while (buckets[i].Count == 0)
                i++;
            int minkey = buckets[i][0].key, minIndex = 0;
            for (j = 1; j < buckets[i].Count; j++)
            {
                if (buckets[i][j].key < minkey)
                {
                    minkey = buckets[i][j].key;
                    minIndex = j;
                }
            }
            Value minValue = buckets[i][minIndex].val;
            buckets[i].RemoveAt(minIndex);
            n--;
            if (n == 0) return minValue;
            while (buckets[i].Count == 0)
                i++;
            if (i > 0)
            {
                int k = buckets[i].Min(a => a.key);
                bounds[0] = k;
                bounds[1] = k + 1;
                for (j = 2; j < i + 1; j++)
                    bounds[j] = min(bounds[j - 1] + 1 << (j - 2), bounds[i + 1]);
                while (buckets[i].Count > 0)
                {
                    j = 0;
                    var el = buckets[i][buckets[i].Count - 1];
                    buckets[i].RemoveAt(buckets[i].Count - 1);
                    while (el.key > bounds[j + 1])
                        j++;
                    buckets[j].Add(el);
                }
            }
            return minValue;
        }
        public string getName()
        {
            return "Radix heap";
        }

        public void clear()
        {
            this.B = (int)Math.Ceiling(Math.Log(C + 1, 2) + 2);
            this.n = 0;
            this.buckets = new List<TreeNode<int, Value>>[B];
            this.bounds = new int[B];
            for (int i = 0; i < B; i++)
                buckets[i] = new List<TreeNode<int, Value>>();
            bounds[0] = 0;
            bounds[1] = 1;
            int exp = 1;
            for (int i = 2; i < B; i++)
            {
                bounds[i] = bounds[i - 1] + exp;
                exp *= 2;
            }
        }

        private int min(int a, int b)
        {
            return a < b ? a : b;
        }

        bool IHeap<int, Value>.remove(Value v)
        {
            throw new NotImplementedException();
        }

        bool IHeap<int, Value>.change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        int IHeap<int, Value>.size()
        {
            return n;
        }

		public IEnumerable<(int k, Value v)> getAllElements()
		{
			throw new NotImplementedException();
		}

		public RadixHeap(int range)
        {
            this.C = range;
            this.B = (int)Math.Ceiling(Math.Log(C + 1, 2) + 2);
            this.n = 0;
            this.buckets = new List<TreeNode<int, Value>>[B];
            this.bounds = new int[B];
            for (int i = 0; i < B; i++)
                buckets[i] = new List<TreeNode<int, Value>>();
            bounds[0] = 0;
            bounds[1] = 1;
            int exp = 1;
            for (int i = 2; i < B; i++)
            {
                bounds[i] = bounds[i - 1] + exp;
                exp *= 2;
            }

        }

    }

        /*
    public class VanEmdeBoasHeap<Value> : IHeap<int, Value>
    {
        private static uint IDHolder = 0;
        protected struct Pair : IComparable
        {
            public int key;
            public uint ID;

            public Pair(int key, uint ID)
            {
                this.key = key;
                this.ID = ID;
            }

            public int CompareTo(object obj)
            {
                if (obj is Pair)
                {
                    Pair p = (Pair)obj;
                    return this.key - p.key == 0 ? (int)(this.ID - p.ID) : this.key - p.key;
                }
                return 0;
            }
        }

        protected class KeyValueComparer<V> : IComparer<KeyValuePair<Pair, V>>
        {
            public int Compare(KeyValuePair<Pair, V> x, KeyValuePair<Pair, V> y)
            {
                var res = x.Key.key - y.Key.key;
                return res == 0 ? (int)(x.Key.ID - y.Key.ID) : res;
            }
        }

        private VEBTree<KeyValuePair<Pair, Value>> baseDS;

        #region IHeap<int,Value> Members

        public void insert(int k, Value v)
        {
            baseDS.Add((uint)k, v);
        }

        public Value getMin()
        {
            return baseDS.First().Value.Value;
        }

        public int getMinKey()
        {
            return (int)baseDS.First().Value.Key;
        }

        public Value removeMin()
        {
            var val = getMin();
            baseDS.Remove((uint)getMinKey());
            return val;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return baseDS.Count();
        }

        public void clear()
        {
            baseDS.Clear();
        }

        public string getName()
        {
            return "Van Emde Boas Heap";
        }

        #endregion

        public VanEmdeBoasHeap()
        {
            this.baseDS = new VEBTree<Value>(8);
        }
    }
        */

    public class FibonacciHeap2<T> : IHeap<double, T>
    {
        public class FibonacciHeapNode<R>
        {
            public FibonacciHeapNode(R data, double key)
            {
                Right = this;
                Left = this;
                Data = data;
                Key = key;
            }

            public R Data { get; private set; }
            public FibonacciHeapNode<R> Child { get; set; }
            public FibonacciHeapNode<R> Left { get; set; }
            public FibonacciHeapNode<R> Parent { get; set; }
            public FibonacciHeapNode<R> Right { get; set; }
            public bool Mark { get; set; }
            public double Key { get; set; }
            public int Degree { get; set; }
        }        
 
        private static readonly double _oneOverLogPhi = 1.0/Math.Log((1.0 + Math.Sqrt(5.0))/2.0);
        private FibonacciHeapNode<T> _minNode;
        private int _nNodes;

        public bool IsEmpty()
        {
            return _minNode == null;
        }

        public void Clear()
        {
            _minNode = null;
            _nNodes = 0;
        }

        /// <summary>
        /// Decreses the key of a node.
        /// O(1) amortized.
        /// </summary>
        public void DecreaseKey(FibonacciHeapNode<T> x, int k)
        {
            if (k > x.Key)
            {
                throw new ArgumentException("decreaseKey() got larger key value");
            }

            x.Key = k;

            FibonacciHeapNode<T> y = x.Parent;

            if ((y != null) && (x.Key < y.Key))
            {
                Cut(x, y);
                CascadingCut(y);
            }

            if (x.Key < _minNode.Key)
            {
                _minNode = x;
            }
        }

        /// <summary>
        /// Deletes a node from the heap.
        /// O(log n)
        /// </summary>
        public void Delete(FibonacciHeapNode<T> x)
        {
            // make newParent as small as possible
            DecreaseKey(x, int.MinValue);

            // remove the smallest, which decreases n also
            RemoveMin();
        }

        /// <summary>
        /// Inserts a new node with its key.
        /// O(1)
        /// </summary>
        public void Insert(FibonacciHeapNode<T> node, double key)
        {
            node.Key = key;

            // concatenate node into min list
            if (_minNode != null)
            {
                node.Left = _minNode;
                node.Right = _minNode.Right;
                _minNode.Right = node;
                node.Right.Left = node;

                if (key < _minNode.Key)
                {
                    _minNode = node;
                }
            }
            else
            {
                _minNode = node;
            }

            _nNodes++;
        }

        /// <summary>
        /// Returns the smalles node of the heap.
        /// O(1)
        /// </summary>
        /// <returns></returns>
        public FibonacciHeapNode<T> Min()
        {
            return _minNode;
        }

        /// <summary>
        /// Removes the smalles node of the heap.
        /// O(log n) amortized
        /// </summary>
        /// <returns></returns>
        public FibonacciHeapNode<T> RemoveMin()
        {
            FibonacciHeapNode<T> minNode = _minNode;

            if (minNode != null)
            {
                int numKids = minNode.Degree;
                FibonacciHeapNode<T> oldMinChild = minNode.Child;

                // for each child of minNode do...
                while (numKids > 0)
                {
                    FibonacciHeapNode<T> tempRight = oldMinChild.Right;

                    // remove oldMinChild from child list
                    oldMinChild.Left.Right = oldMinChild.Right;
                    oldMinChild.Right.Left = oldMinChild.Left;

                    // add oldMinChild to root list of heap
                    oldMinChild.Left = _minNode;
                    oldMinChild.Right = _minNode.Right;
                    _minNode.Right = oldMinChild;
                    oldMinChild.Right.Left = oldMinChild;

                    // set parent[oldMinChild] to null
                    oldMinChild.Parent = null;
                    oldMinChild = tempRight;
                    numKids--;
                }

                // remove minNode from root list of heap
                minNode.Left.Right = minNode.Right;
                minNode.Right.Left = minNode.Left;

                if (minNode == minNode.Right)
                {
                    _minNode = null;
                }
                else
                {
                    _minNode = minNode.Right;
                    Consolidate();
                }

                // decrement size of heap
                _nNodes--;
            }

            return minNode;
        }

        /// <summary>
        /// The number of nodes. O(1)
        /// </summary>
        /// <returns></returns>
        public int Size()
        {
            return _nNodes;
        }

        /// <summary>
        /// Joins two heaps. O(1)
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <returns></returns>
        public static FibonacciHeap2<T> Union(FibonacciHeap2<T> h1, FibonacciHeap2<T> h2)
        {
            var h = new FibonacciHeap2<T>();

            if ((h1 != null) && (h2 != null))
            {
                h._minNode = h1._minNode;

                if (h._minNode != null)
                {
                    if (h2._minNode != null)
                    {
                        h._minNode.Right.Left = h2._minNode.Left;
                        h2._minNode.Left.Right = h._minNode.Right;
                        h._minNode.Right = h2._minNode;
                        h2._minNode.Left = h._minNode;

                        if (h2._minNode.Key < h1._minNode.Key)
                        {
                            h._minNode = h2._minNode;
                        }
                    }
                }
                else
                {
                    h._minNode = h2._minNode;
                }

                h._nNodes = h1._nNodes + h2._nNodes;
            }

            return h;
        }

        /// <summary>
        /// Performs a cascading cut operation. This cuts newChild from its parent and then
        /// does the same for its parent, and so on up the tree.
        /// </summary>
        protected void CascadingCut(FibonacciHeapNode<T> y)
        {
            FibonacciHeapNode<T> z = y.Parent;

            // if there's a parent...
            if (z != null)
            {
                // if newChild is unmarked, set it marked
                if (!y.Mark)
                {
                    y.Mark = true;
                }
                else
                {
                    // it's marked, cut it from parent
                    Cut(y, z);

                    // cut its parent as well
                    CascadingCut(z);
                }
            }
        }

        protected void Consolidate()
        {
            int arraySize = ((int) Math.Floor(Math.Log(_nNodes)*_oneOverLogPhi)) + 1;

            var array = new List<FibonacciHeapNode<T>>(arraySize);

            // Initialize degree array
            for (var i = 0; i < arraySize; i++)
            {
                array.Add(null);
            }

            // Find the number of root nodes.
            var numRoots = 0;
            FibonacciHeapNode<T> x = _minNode;

            if (x != null)
            {
                numRoots++;
                x = x.Right;

                while (x != _minNode)
                {
                    numRoots++;
                    x = x.Right;
                }
            }

            // For each node in root list do...
            while (numRoots > 0)
            {
                // Access this node's degree..
                int d = x.Degree;
                FibonacciHeapNode<T> next = x.Right;

                // ..and see if there's another of the same degree.
                for (;;)
                {
                    FibonacciHeapNode<T> y = array[d];
                    if (y == null)
                    {
                        // Nope.
                        break;
                    }

                    // There is, make one of the nodes a child of the other.
                    // Do this based on the key value.
                    if (x.Key > y.Key)
                    {
                        FibonacciHeapNode<T> temp = y;
                        y = x;
                        x = temp;
                    }

                    // FibonacciHeapNode<T> newChild disappears from root list.
                    Link(y, x);

                    // We've handled this degree, go to next one.
                    array[d] = null;
                    d++;
                }

                // Save this node for later when we might encounter another
                // of the same degree.
                array[d] = x;

                // Move forward through list.
                x = next;
                numRoots--;
            }

            // Set min to null (effectively losing the root list) and
            // reconstruct the root list from the array entries in array[].
            _minNode = null;

            for (var i = 0; i < arraySize; i++)
            {
                FibonacciHeapNode<T> y = array[i];
                if (y == null)
                {
                    continue;
                }

                // We've got a live one, add it to root list.
                if (_minNode != null)
                {
                    // First remove node from root list.
                    y.Left.Right = y.Right;
                    y.Right.Left = y.Left;

                    // Now add to root list, again.
                    y.Left = _minNode;
                    y.Right = _minNode.Right;
                    _minNode.Right = y;
                    y.Right.Left = y;

                    // Check if this is a new min.
                    if (y.Key < _minNode.Key)
                    {
                        _minNode = y;
                    }
                }
                else
                {
                    _minNode = y;
                }
            }
        }

        /// <summary>
        /// The reverse of the link operation: removes newParent from the child list of newChild.
        /// This method assumes that min is non-null.
        /// Running time: O(1)
        /// </summary>
        protected void Cut(FibonacciHeapNode<T> x, FibonacciHeapNode<T> y)
        {
            // remove newParent from childlist of newChild and decrement degree[newChild]
            x.Left.Right = x.Right;
            x.Right.Left = x.Left;
            y.Degree--;

            // reset newChild.child if necessary
            if (y.Child == x)
            {
                y.Child = x.Right;
            }

            if (y.Degree == 0)
            {
                y.Child = null;
            }

            // add newParent to root list of heap
            x.Left = _minNode;
            x.Right = _minNode.Right;
            _minNode.Right = x;
            x.Right.Left = x;

            // set parent[newParent] to nil
            x.Parent = null;

            // set mark[newParent] to false
            x.Mark = false;
        }

        /// <summary>
        /// Makes newChild a child of Node newParent.
        /// O(1)
        /// </summary>
        protected void Link(FibonacciHeapNode<T> newChild, FibonacciHeapNode<T> newParent)
        {
            // remove newChild from root list of heap
            newChild.Left.Right = newChild.Right;
            newChild.Right.Left = newChild.Left;

            // make newChild a child of newParent
            newChild.Parent = newParent;

            if (newParent.Child == null)
            {
                newParent.Child = newChild;
                newChild.Right = newChild;
                newChild.Left = newChild;
            }
            else
            {
                newChild.Left = newParent.Child;
                newChild.Right = newParent.Child.Right;
                newParent.Child.Right = newChild;
                newChild.Right.Left = newChild;
            }

            // increase degree[newParent]
            newParent.Degree++;

            // set mark[newChild] false
            newChild.Mark = false;
        }

        void IHeap<double, T>.insert(double k, T v)
        {
            Insert(new FibonacciHeapNode<T>(v, k), k);
        }

        T IHeap<double, T>.getMin()
        {
            return Min().Data;
        }

        double IHeap<double, T>.getMinKey()
        {
            return Min().Key;
        }

        T IHeap<double, T>.removeMin()
        {
            return RemoveMin().Data;
        }

        bool IHeap<double, T>.remove(T v)
        {
            throw new NotImplementedException();
        }

        bool IHeap<double, T>.change(T v, double newKey)
        {
            throw new NotImplementedException();
        }

        int IHeap<double, T>.size()
        {
            return Size();
        }

        void IHeap<double, T>.clear()
        {
            Clear();
        }

        string IHeap<double, T>.getName()
        {
            return "Fibonacci heap by sqeezy";
        }

		public IEnumerable<(double k, T v)> getAllElements()
		{
			throw new NotImplementedException();
		}
	}
  
    public class OrderedMutliDictionaryHeap<Value> : IHeap<int, Value>
    {
        Wintellect.PowerCollections.OrderedMultiDictionary<int, Value> structure;

        public void insert(int k, Value v)
        {
            structure.Add(k, v);
        }

        public Value getMin()
        {
            throw new NotImplementedException();
        }

        public int getMinKey()
        {
            throw new NotImplementedException();
        }

        public Value removeMin()
        {
            return structure.RemoveMin();

            /*
            var t = structure.First();
            var minKey = t.Key;
            var minValue = t.Value.First();
            ((IList<Value>)structure[minKey]).RemoveAt(structure[minKey].Count - 1);
            return minValue;
             */
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            //return structure.Count;
            return structure.Keys.Count;
        }

        public string getName()
        {
            return "Wintelect Ordered Multidictionary Heap";
        }

        public void clear()
        {
            structure.Clear();
        }

		public IEnumerable<(int k, Value v)> getAllElements()
		{
			throw new NotImplementedException();
		}

		public OrderedMutliDictionaryHeap()
        {
            this.structure = new Wintellect.PowerCollections.OrderedMultiDictionary<int, Value>(true, (a, b) => a - b, (a,b) => 0);
        }
    }

    public class MeasuredHeap<Value> : IHeap<int, Value>
    {
        private string outputFile;
        private string outputFolder = @".\..\tests\heapUsage";
        private System.IO.StreamWriter writer;

        public void setOutputFile(string output)
        {
            if (!System.IO.Directory.Exists(outputFolder))
                System.IO.Directory.CreateDirectory(outputFolder);
            this.outputFile = output;
            string file = outputFolder + "\\" + outputFile.Substring(0, outputFile.Length - 3) + "txt";
            this.writer = new System.IO.StreamWriter(file);
        }

        private struct heapNode<Value> : IComparable<heapNode<Value>>
        {
            public int key;
            public Value val;
            public int addOrder, timeStamp;

            int IComparable<heapNode<Value>>.CompareTo(heapNode<Value> other)
            {
                return this.key - other.key;
            }

            public heapNode(int key, Value v)
            {
                this.key = key;
                this.val = v;
                this.timeStamp = 0;
                this.addOrder = 0;
            }
        }

        private class HeapStatistics<Value>
        {
            private int currentTime = 0;

            public int highestAddedElement,
                lowestAddedElement,
                highestExtractedElement,
                oldestExtractedElement,
                numberOfInserts,
                numberOfExtracts,
                maxNumberOfElements,
                currentHeapSize;

            public long extractedElementsAgeTotalSum = 0;
            public Dictionary<int, int> gapsCountsBySize = new Dictionary<int, int>();

            public List<int> elementsAges;
            public HashSet<int> keysUsed;

            public void clear()
            {
                this.highestAddedElement = int.MinValue;
                this.highestExtractedElement = int.MinValue;
                this.lowestAddedElement = int.MaxValue;
                this.maxNumberOfElements = 0;
                this.numberOfExtracts = 0;
                this.numberOfInserts = 0;
                this.currentHeapSize = 0;
                extractedElementsAgeTotalSum = 0;
                oldestExtractedElement = 0;

                currentTime = 0;
                elementsAges.Clear();
                keysUsed.Clear();
            }

            private void computeGaps()
            {
                this.gapsCountsBySize = new Dictionary<int, int>();
                var keys = keysUsed.ToList();
                keys.Sort();
                for (int i = 1; i < keys.Count; i++)
                {
                    int gapSize = keys[i] - keys[i - 1] - 1;
                    if (gapSize == 0)
                        continue;
                    if (!gapsCountsBySize.ContainsKey(gapSize))
                        gapsCountsBySize.Add(gapSize, 0);
                    gapsCountsBySize[gapSize]++;
                }
            }

            public HeapStatistics()
            {
                this.elementsAges = new List<int>();
                this.keysUsed = new HashSet<int>();
                clear();
            }

            public void updateStatsInsert(heapNode<Value> addedElement)
            {
                this.currentTime++;
                this.currentHeapSize++;
                this.numberOfInserts++;
                if (highestAddedElement < addedElement.key)
                    highestAddedElement = addedElement.key;
                if (lowestAddedElement > addedElement.key)
                    lowestAddedElement = addedElement.key;
                if (maxNumberOfElements < currentHeapSize)
                    maxNumberOfElements = currentHeapSize;
                keysUsed.Add(addedElement.key);
            }

            public void updateStatsExtract(heapNode<Value> extractedElement)
            {
                this.currentTime++;
                this.currentHeapSize--;
                this.numberOfExtracts++;
                if (highestExtractedElement < extractedElement.key)
                    highestExtractedElement = extractedElement.key;
                int elementAge = this.currentTime - extractedElement.timeStamp;
                if (oldestExtractedElement < elementAge)
                    oldestExtractedElement = elementAge;

                elementsAges.Add(elementAge);
                extractedElementsAgeTotalSum += elementAge;
            }

            public void printStats()
            {
                Console.WriteLine("\t --- Printing heap usage statistics ---");
                Console.WriteLine("\t\tCurrent heap size:\t\t" + this.currentHeapSize);
                Console.WriteLine("\t\tTotal elements added:\t\t" + this.numberOfInserts);
                Console.WriteLine("\t\tTotal elements extracted:\t" + this.numberOfExtracts);
                Console.WriteLine("\t\tMax heap size\t\t\t" + this.maxNumberOfElements);

                Console.WriteLine("\t\tHighest added element:\t\t" + this.highestAddedElement);
                Console.WriteLine("\t\tLowest added element:\t\t" + this.lowestAddedElement);
                Console.WriteLine("\t\tHighest extracted element:\t" + this.highestExtractedElement);

                Console.WriteLine("\t\tElements range:\t\t\t" + (this.highestAddedElement - lowestAddedElement));
                Console.WriteLine("\t\tTotal keys used:\t\t" + this.keysUsed.Count);
                Console.WriteLine("\t\tNumber of elements / range:\t" + (this.maxNumberOfElements / ((double)(this.highestAddedElement - lowestAddedElement))));
                Console.WriteLine("\t\tNumber of elements / keys used:\t" + (this.maxNumberOfElements / ((double)(this.keysUsed.Count))));

                Console.WriteLine("\t\tGaps sizes:");
                computeGaps();
                foreach (var gapSize in gapsCountsBySize.Keys)
                {
                    Console.WriteLine("\t\t\tSize: " + gapSize + " Count: " + gapsCountsBySize[gapSize]);
                }

                Console.WriteLine("\t\tAdditions / extractions:\t" + (this.numberOfInserts / ((double)(this.numberOfExtracts))));

                Console.WriteLine("\t\tAverage element's age (extracted only):\t" + extractedElementsAgeTotalSum / elementsAges.Count);
            }

        }

        private List<heapNode<Value>> values;
        private IHeap<double, heapNode<Value>> basicHeap;
        private int time = 0;

        private HeapStatistics<Value> stats;

        ~MeasuredHeap()
        {
            this.writer.Close();
        }

        public MeasuredHeap()
        {
            this.time = 0;
            this.values = new List<heapNode<Value>>();
            this.stats = new HeapStatistics<Value>();
            basicHeap = new Heaps.RegularBinaryHeap<heapNode<Value>>();
            clearStatistics();
        }

        public void clearStatistics()
        {
            if (writer != null)
                writer.Close();
            stats.clear();
        }

        public void printStats()
        {
            stats.printStats();
            long totalAgesSum = 0;
            int oldesElement = 0,
                numberOfElementsGreaterThanExtractionLimit = 0;
            while (basicHeap.size() > 0)
            {
                var element = basicHeap.removeMin();
                var age = time - element.timeStamp;
                if (oldesElement < age)
                    oldesElement = age;
                totalAgesSum += age;
                if (element.key > stats.highestExtractedElement)
                    numberOfElementsGreaterThanExtractionLimit++;
            }
            totalAgesSum += stats.extractedElementsAgeTotalSum;
            Console.WriteLine("\t\tAverage element's age (all):\t" + totalAgesSum / (stats.elementsAges.Count + stats.currentHeapSize));

            Console.WriteLine("\t\tOldest extracted:\t\t" + stats.oldestExtractedElement);
            Console.WriteLine("\t\tOldest not extracted:\t\t" + oldesElement);

            Console.WriteLine("\t\tElements beyond extraction limit:\t" + numberOfElementsGreaterThanExtractionLimit);
        }

        #region IHeap<int,Value> Members

        void IHeap<int, Value>.insert(int k, Value v)
        {
            writer.WriteLine("i\t" + k);
            var newElement = new heapNode<Value>(k, v);
            /*
            int rank = 0;
            if (values.Count > 0)
                rank = values.BinarySearch(newElement);
            if (rank < 0)
                rank *= -1;
            if (rank > values.Count)
                rank = values.Count;
            newElement.addOrder = rank;
             */
            newElement.timeStamp = time++;
            //values.Insert(rank, newElement);
            basicHeap.insert(newElement.key, newElement);
            stats.updateStatsInsert(newElement);
        }

        Value IHeap<int, Value>.getMin()
        {
            throw new NotImplementedException();
        }

        int IHeap<int, Value>.getMinKey()
        {
            throw new NotImplementedException();
        }

        Value IHeap<int, Value>.removeMin()
        {
            /*
            var result = values[0];
            values.RemoveAt(0);
             */
            var result = basicHeap.removeMin();
            stats.updateStatsExtract(result);
            writer.WriteLine("r\t" + result.key);
            return result.val;
        }

        bool IHeap<int, Value>.remove(Value v)
        {
            throw new NotImplementedException();
        }

        bool IHeap<int, Value>.change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        int IHeap<int, Value>.size()
        {
            return basicHeap.size();
            //return values.Count;
        }

        void IHeap<int, Value>.clear()
        {
            this.stats.clear();
            this.values.Clear();
            this.time = 0;
        }

        string IHeap<int, Value>.getName()
        {
            return "Measured heap - NOT TO BE USED IN PRACTICE";
        }

		public IEnumerable<(int k, Value v)> getAllElements()
		{
			throw new NotImplementedException();
		}

		#endregion
	}

        
}
