using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    public sealed class ArrayEqualityComparer : IEqualityComparer<int[]>
    {
        public static ArrayEqualityComparer comparer = new ArrayEqualityComparer();

        public bool Equals(int[] first, int[] second)
        {
            if (first == second)
            {
                return true;
            }
            if (first == null || second == null)
            {
                return false;
            }
            if (first.Length != second.Length)
            {
                return false;
            }
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(int[] array)
        {
            unchecked
            {
                if (array == null)
                {
                    return 0;
                }
                int hash = 17;
                foreach (int element in array)
                {
                    hash = hash * 31 + element;
                }
                return hash;
            }
        }
    }

    public sealed class ListArrayEqualityComparer : IEqualityComparer<List<int>[]>
    {
        public static ListArrayEqualityComparer comparer = new ListArrayEqualityComparer();

        public bool Equals(List<int>[] first, List<int>[] second)
        {
            if (first == second)
            {
                return true;
            }
            if (first == null || second == null)
            {
                return false;
            }
            if (first.Length != second.Length)
            {
                return false;
            }
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i].Count != second[i].Count) return false;
                for (int j = 0; j < first[i].Count; j++)
                    if (first[i][j] != second[i][j])
                        return false;
            }
            return true;
        }

        public int GetHashCode(List<int>[] array)
        {
            unchecked
            {
                if (array == null)
                {
                    return 0;
                }
                int hash = 17;
                foreach (List<int> element in array)
                {
                    int hash2 = 51;
                    foreach (var item in element)
                    {
                        hash2 = hash2 * 41 + item;
                    }
                    hash = hash * 31 + hash2;
                }
                return hash;
            }
        }
    }

    public sealed class ArrayEqualityComparer1 : IEqualityComparer<int[]>
    {
        public bool Equals(int[] first, int[] second)
        {
            if (first == second)
            {
                return true;
            }
            if (first == null || second == null)
            {
                return false;
            }
            if (first.Length != second.Length)
            {
                return false;
            }
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(int[] array)
        {
            unchecked
            {
                if (array == null)
                {
                    return 0;
                }
                int result = 0;
                int shift = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    shift = (shift + 11) % 21;
                    result ^= (array[i] + 1024) << shift;
                }
                return result;
            }
        }
    }

    public sealed class ArrayEqualityComparer2 : IEqualityComparer<int[]>
    {
        public bool Equals(int[] first, int[] second)
        {
            if (first == second)
            {
                return true;
            }
            if (first == null || second == null)
            {
                return false;
            }
            if (first.Length != second.Length)
            {
                return false;
            }
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(int[] array)
        {
            unchecked
            {
                if (array == null)
                {
                    return 0;
                }
                int result = array.Length;
                for (int i = 0; i < array.Length; i++)
                {
                    result += array[i];
                }
                return result;
            }
        }
    }

}
