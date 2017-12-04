using System;

namespace shuffler
{
	class Program
	{
		static void Main(string[] args)
		{
			string[] sides = new string[] { "U", "R", "F", "L", "B", "D" };
			string[] modifiers = new string[] { "", "'", "2" };
			Random rand = new Random();
			for (int j = 0; j < 2000; j++) {
				for (int i = 0; i < 90; i++)
					Console.Write($"{sides[rand.Next(sides.Length)]}{modifiers[rand.Next(modifiers.Length)]} ");
				Console.WriteLine();
			}
		}
	}
}
