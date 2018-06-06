using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.ExtensionMethods
{
	public static class ExtensionMethods
	{
		/// <summary>
		/// Returns a minimal element from given sequence together with its index. If there are more minimal elements, it returns index of the first one.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <returns></returns>
		public static (T element, int index) MinWithIndex<T>(this IEnumerable<T> sequence)
			where T : IComparable<T>
		{
			if (!sequence.Any())
				return (default, -1);

			T minElement = sequence.First();
			int minIndex = 0;
			int index = 0;
			foreach (var item in sequence.Skip(1))
			{
				index++;
				if (item.CompareTo(minElement) < 0)
				{
					minElement = item;
					minIndex = index;
				}
			}
			return (minElement, minIndex);
		}

		/// <summary>
		/// Returns a maximal element from given sequence together with its index. If there are more maximal elements, it returns index of the first one.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <returns></returns>
		public static (T element, int index) MaxWithIndex<T>(this IEnumerable<T> sequence)
			where T : IComparable<T>
		{
			if (!sequence.Any())
				return (default, -1);

			T maxElement = sequence.First();
			int maxIndex = 0;
			int index = 0;
			foreach (var item in sequence.Skip(1))
			{
				index++;
				if (item.CompareTo(maxElement) > 0)
				{
					maxElement = item;
					maxIndex = index;
				}
			}
			return (maxElement, maxIndex);
		}

		public static void AddRange<T>(this HashSet<T> source, IEnumerable<T> toAdd)
		{
			foreach (var item in toAdd)
			{
				source.Add(item);
			}
		}

		public static void AddRange<T>(this List<T> source, List<T> toAdd, bool reverse)
		{
			if (!reverse)
			foreach (var item in toAdd)
			{
				source.Add(item);
			}
			else
			{
				for (int i = toAdd.Count - 1; i >= 0; i--)
				{
					source.Add(toAdd[i]);
				}
			}
		}
	}
}
