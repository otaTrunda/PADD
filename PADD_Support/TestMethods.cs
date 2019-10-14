using PADD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD_Support
{
	static class TestMethods
	{
		static Random r = new Random(123);

		#region Patterns methods

		static void testPatterns(bool[] isSelected, int selectedCount, int position, int limit, SASProblem d, System.IO.StreamWriter writer)
		{
			if (selectedCount == limit)
			{
				if (TestResult.currentID < TestResult.IDStart)
				{
					TestResult.currentID++;
					return;
				}

				HashSet<int> pattern = new HashSet<int>();
				bool intersectsWithGoal = false;
				for (int i = 0; i < isSelected.Length; i++)
				{
					if (isSelected[i])
					{
						pattern.Add(i);
						if (d.GetGoalConditions().IsVariableAffected(i))
							intersectsWithGoal = true;
					}
				}
				if (!intersectsWithGoal)
				{
					TestResult.currentID++;
					return;
				}

				PDBHeuristic h = new PDBHeuristic(d);
				DateTime buildingStarted = DateTime.Now;
				h.initializePatterns(pattern);
				DateTime buildingEnded = DateTime.Now;
				AStarSearch ast = new AStarSearch(d, h);
				DateTime searchStarted = DateTime.Now;
				ast.Search();
				DateTime searchEnded = DateTime.Now;
				writer.WriteLine(TestResult.currentID + "\t" + pattern.Count + "\t" + String.Format("{0:0.##}", (buildingEnded - buildingStarted).TotalSeconds) +
					"\t" + String.Format("{0:0.##}", (searchEnded - searchStarted).TotalSeconds) + "\t" + h.statistics.heuristicCalls);
				//res.Add(new TestResult(d, pattern, (buildingEnded - buildingStarted).TotalSeconds, (searchEnded - searchStarted).TotalSeconds, nodes));
				TestResult.currentID++;

				return;
			}
			if (selectedCount < limit - (isSelected.Length - position))
				return;

			if (position >= isSelected.Length)
				return;

			isSelected[position] = true;
			testPatterns(isSelected, selectedCount + 1, position + 1, limit, d, writer);
			isSelected[position] = false;
			testPatterns(isSelected, selectedCount, position + 1, limit, d, writer);
		}

		static void runPatternsTest(string domainFile)
		{
			SASProblem d = SASProblem.CreateFromFile(domainFile);
			//AStarSearch ast;
			PDBHeuristic h = new PDBHeuristic(d);

			bool[] isSeleceted = new bool[d.GetVariablesCount()];
			//List<TestResult> res = new List<TestResult>(); 
			if (System.IO.File.Exists("idStart.txt"))
			{
				using (var reader = new System.IO.StreamReader("idStart.txt"))
				{
					TestResult.IDStart = int.Parse(reader.ReadLine());
				}
			}
			else
			{
				using (var writer = new System.IO.StreamWriter("results.txt", true))
				{
					writer.WriteLine("ID\tSize\tCreate\tSearch\tNodes");
				}
			}

			using (var writer = new System.IO.StreamWriter("results.txt", true))
			{
				writer.AutoFlush = true;
				for (int i = 0; i <= d.GetVariablesCount(); i++)
				{
					testPatterns(isSeleceted, 0, 0, i, d, writer);
				}
				/*
                //Writing the results

                logger.Log("<---- \tResults\t ---->");
                logger.Log();
                logger.Log("ID\tSize\tCreation\tSearch\tNodes");
                for (int i = 0; i < res.Count; i++)
                {
                    logger.Log(i + "\t" + res[i].pattern.Count + "\t" + res[i].creation + "\t" + res[i].search + "\t" + res[i].nodes);
                }
                 */
			}
		}

		#endregion Patterns methods

		#region Heap test methods
		private static void runHeapTests()
		{
			Logger logger = new Logger();
			logger.Log("\nTest number 0");
			heapTests(2000, 10);
			logger.Log("\nTest number 1");
			heapTests(int.MaxValue / 20000, 2);
			logger.Log("\nTest number 2");
			heapTests(int.MaxValue / 2000, 2);
			logger.Log("\nTest number 3");
			heapTests(int.MaxValue / 2000, 10);
			logger.Log("\nTest number 4");
			heapTests(int.MaxValue / 2000, 50);
			logger.Log("\nTest number 5");
			heapTests(int.MaxValue / 200, 2);
			logger.Log("\nTest number 6");
			heapTests(int.MaxValue / 200, 10);
			logger.Log("\nTest number 7");
			heapTests(int.MaxValue / 200, 50);
			logger.Log("\nTest number 8");
			heapTests(int.MaxValue / 100, 10);
			logger.Log("\nTest number 9");
			heapTests(int.MaxValue / 100, 20);
			logger.Log("\nTest number 10");
			heapTests(int.MaxValue / 100, 50);
			logger.Log("\nTest number 11");
			heapTests(int.MaxValue / 100, 100);
		}

		private static void heapTests(int size, int removeInterval, int maxValue = -1)
		{
			Logger logger = new Logger();
			if (maxValue < 0) maxValue = size / 4;
			List<IHeap<double, int>> testSubjects = new List<IHeap<double, int>>();
			List<string> names = new List<string>();
			testSubjects.Add(new PADD.Heaps.RegularBinaryHeap<int>());
			names.Add("Regular Heap");
			testSubjects.Add(new PADD.Heaps.LeftistHeap<int>());
			names.Add("Leftist Heap");
			testSubjects.Add(new PADD.Heaps.BinomialHeap<int>());
			names.Add("Binomial Heap");
			//testSubjects.Add(new SortedListHeap<int>());
			//names.Add("SortedList Heap");
			//testSubjects.Add(new Heaps.SingleBucket<int>(maxValue));
			//names.Add("Single Bucket Heap");
			//testSubjects.Add(new Heaps.RadixHeap<int>(maxValue));
			//names.Add("Radix Heap");

			List<List<int>> removedValues = new List<List<int>>();
			List<TimeSpan> results = new List<TimeSpan>();
			List<int> input = generateNonDecreasingTestInput(size, maxValue);

			for (int j = 0; j < testSubjects.Count; j++)
			{
				logger.Log("testing the " + names[j]);
				IHeap<double, int> heap = testSubjects[j];
				//removedValues.Add(new List<int>());
				//int index = removedValues.Count -1;
				DateTime start = DateTime.Now;

				for (int i = 0; i < size; i++)
				{
					heap.insert(input[i], input[i]);
					if (i % removeInterval == 0)
					{
						heap.removeMin();
						if ((DateTime.Now - start).TotalSeconds > 120)
						{
							logger.Log(names[j] + " time limit exceeded.");
							break;
						}
					}
				}
				DateTime end = DateTime.Now;
				logger.Log("Test finished.");
				results.Add(end - start);
				testSubjects[j] = null;
				GC.Collect();
			}

			/*
            for (int i = 0; i < removedValues[0].Count; i++)
            {
                for (int j = 0; j < removedValues.Count; j++)
                {
                    if (removedValues[j][i] != removedValues[0][i])
                    {
                        logger.Log("chyba");
                    }
                }
            }
             */
			for (int i = 0; i < testSubjects.Count; i++)
			{
				logger.Log(names[i] + " " + results[i].TotalSeconds + " seconds.");
			}

		}

		private static List<int> generateTestInput(int size, int maxValue)
		{
			Logger logger = new Logger();
			HashSet<int> a = new HashSet<int>();
			logger.Log("Generating test input");
			Random r = new Random();
			List<int> result = new List<int>();
			for (int i = 0; i < size; i++)
			{
				int item = r.Next(maxValue);
				/*while (a.Contains(item))
                    item = r.Next(maxValue);*/
				result.Add(item);
				a.Add(item);
			}
			logger.Log("Done");
			return result;
		}
		private static List<int> generateNonDecreasingTestInput(int size, int maxValue)
		{
			Logger logger = new Logger();
			logger.Log("Generating test input");
			Random r = new Random();
			List<int> result = new List<int>();
			result.Add(0);
			for (int i = 1; i < size; i++)
			{
				int item = result[i - 1] + r.Next(5);
				/*while (a.Contains(item))
                    item = r.Next(maxValue);*/
				result.Add(item);
			}
			logger.Log("Done");
			return result;
		}


		#endregion

		#region HashMaps test methods
		private static void runHashSetTests()
		{
			testHashSet(int.MaxValue / 20000);
			testHashSet(int.MaxValue / 2000);
			testHashSet(int.MaxValue / 200);
			testHashSet(int.MaxValue / 150);
			testHashSet(int.MaxValue / 150);
		}

		private static void testHashSet(int size)
		{
			Logger logger = new Logger();
			TimeSpan totalDict, totalHash;
			logger.Log("\n -------- Running a new test --------");
			GC.Collect();
			List<int> inputToAdd = generateTestInput(size, size / 10),
				inputToQuerry = generateTestInput(size, size / 10);
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			HashSet<int> hashSet = new HashSet<int>();

			logger.Log("Testing dictionary");
			logger.Log("Adding items");
			DateTime start = DateTime.Now;
			for (int i = 0; i < size; i++)
			{
				dictionary.Add(inputToAdd[i], inputToAdd[i]);
			}
			DateTime end = DateTime.Now;
			logger.Log("Items added in " + (end - start).TotalSeconds + " seconds.");
			logger.Log("Querrying items");
			int found = 0;
			DateTime start2 = DateTime.Now;
			for (int i = 0; i < size; i++)
			{
				if (dictionary.ContainsKey(inputToQuerry[i]))
					found++;
			}
			DateTime end2 = DateTime.Now;
			logger.Log("Querrying finished in " + (end2 - start2).TotalSeconds + "seconds.");
			totalDict = ((end2 - start2) + (end - start));
			logger.Log("Total time " + totalDict.TotalSeconds + "seconds.");
			dictionary.Clear();
			GC.Collect();

			logger.Log("\nTesting HashSet");
			logger.Log("Adding items");
			start = DateTime.Now;
			for (int i = 0; i < size; i++)
			{
				hashSet.Add(inputToAdd[i]);
			}
			end = DateTime.Now;
			logger.Log("Items added in " + (end - start).TotalSeconds + " seconds.");
			logger.Log("Querrying items");
			found = 0;
			start2 = DateTime.Now;
			for (int i = 0; i < size; i++)
			{
				if (hashSet.Contains(inputToQuerry[i]))
					found++;
			}
			end2 = DateTime.Now;
			logger.Log("Querrying finished in " + (end2 - start2).TotalSeconds + "seconds.");
			totalHash = ((end2 - start2) + (end - start));
			logger.Log("Total time " + totalHash.TotalSeconds + "seconds.");
			logger.Log("Results: \nDictionary: " + totalDict.TotalSeconds + "\nHashSet: " + totalHash.TotalSeconds);
		}

		#endregion

		#region EqualityComparer test methods

		private static void testArrayEquality()
		{
			Logger logger = new Logger();
			HashSet<int[]> hash = new HashSet<int[]>(new ArrayEqualityComparer());
			int[] a = { 0, 0 }, b = { 0, 0 };
			logger.Log(a.GetHashCode());
			logger.Log(b.GetHashCode());
			if (a.Equals(b))
				logger.Log("equal");
			else logger.Log("not equal");
			hash.Add(a);
			if (hash.Contains(a))
			{
				logger.Log("t");
			}
			else logger.Log("f");
			if (hash.Contains(b))
			{
				logger.Log("t");
			}
			else logger.Log("f");
		}

		private static List<int[]> generateInputs(int size, int arraySize, int[] valuesRange)
		{
			Logger logger = new Logger();
			logger.Log("Generating inputs");
			List<int[]> result = new List<int[]>();
			for (int i = 0; i < size; i++)
			{
				int[] item = generateInput(arraySize, valuesRange);
				result.Add(item);
			}
			logger.Log("Done");
			return result;
		}

		private static int[] generateInput(int arraySize, int[] valuesRange)
		{

			int[] result = new int[arraySize];
			for (int i = 0; i < arraySize; i++)
			{
				result[i] = r.Next(valuesRange[i]);
			}

			return result;
		}

		private static TimeSpan testComparer(List<int[]> inputsToAdd, List<int[]> inputsToTest, IEqualityComparer<int[]> testSubject, string name)
		{
			Logger logger = new Logger();
			GC.Collect();
			logger.Log("\nTesting " + name);
			logger.Log("Adding items");
			HashSet<int[]> h = new HashSet<int[]>();
			DateTime startAdding = DateTime.Now;
			for (int i = 0; i < inputsToAdd.Count; i++)
			{
				if (!h.Contains(inputsToAdd[i]))
					h.Add(inputsToAdd[i]);
			}
			DateTime endAdding = DateTime.Now;
			logger.Log("Done");

			logger.Log("Finding items");
			DateTime startFinding = DateTime.Now;
			for (int i = 0; i < inputsToTest.Count; i++)
			{
				if (h.Contains(inputsToTest[i]))
					h.Remove(inputsToTest[i]);
			}
			DateTime endFinding = DateTime.Now;
			logger.Log("Done");
			logger.Log("Time to add: " + (endAdding - startAdding).TotalSeconds + " seconds");
			logger.Log("Time to find: " + (endFinding - startFinding).TotalSeconds + " seconds");
			logger.Log("Total time: " + ((endAdding - startAdding) + (endFinding - startFinding)).TotalSeconds + " seconds");
			return (endAdding - startAdding) + (endFinding - startFinding);
		}

		private static void runTestOnComparers(int size, int arraySize, bool standard)
		{
			Logger logger = new Logger();
			logger.Log("\nNew test started ------------");

			int[] valuesRange = new int[arraySize];
			if (standard)
				for (int i = 0; i < arraySize; i++)
					valuesRange[i] = i;
			else
			{
				for (int i = 0; i < arraySize / 3; i++)
				{
					valuesRange[i] = 2;
				}
				for (int i = arraySize / 3; i < 2 * arraySize / 3; i++)
				{
					valuesRange[i] = r.Next(i / 2);
				}
				for (int i = 2 * arraySize / 3; i < arraySize; i++)
				{
					valuesRange[i] = 50;
				}
			}

			List<int[]> toAdd = generateInputs(size, arraySize, valuesRange),
				toFind = generateInputs(0, arraySize, valuesRange);
			TimeSpan t0 = testComparer(toAdd, toFind, new ArrayEqualityComparer(), "comparer 0");
			TimeSpan t1 = testComparer(toAdd, toFind, new ArrayEqualityComparer1(), "comparer 1");
			TimeSpan t2 = testComparer(toAdd, toFind, new ArrayEqualityComparer2(), "comparer 2");
			logger.Log("Results: ");
			logger.Log("comparer 0: " + t0.TotalSeconds + " seconds");
			logger.Log("comparer 1: " + t1.TotalSeconds + " seconds");
			logger.Log("comparer 2: " + t2.TotalSeconds + " seconds");
		}

		private static void testComparers()
		{
			runTestOnComparers(1000000, 20, true);
			runTestOnComparers(1000000, 30, true);
			runTestOnComparers(1000000, 40, true);
			runTestOnComparers(1000000, 50, true);
			runTestOnComparers(1000000, 60, true);
			runTestOnComparers(1000000, 70, true);

			runTestOnComparers(1000000, 20, false);
			runTestOnComparers(1000000, 30, false);
			runTestOnComparers(1000000, 40, false);
			runTestOnComparers(1000000, 50, false);
			runTestOnComparers(1000000, 60, false);
			runTestOnComparers(1000000, 70, false);
		}

		#endregion


		static void runHeapsTestsOLD(string domainsFolder, List<IHeap<double, IState>> dataStrucutures, TimeSpan timeLimit)
		{
			Logger logger = new Logger();
			List<List<string>> results = new List<List<string>>();
			results.Add(new List<string>());
			results[0].Add("");
			foreach (var directory in Directory.EnumerateDirectories(domainsFolder))
			{
				foreach (var file in Directory.EnumerateFiles(directory))
				{
					results[0].Add(file);
				}
			}

			foreach (var ds in dataStrucutures)
			{
				results.Add(new List<string>());
				results[results.Count - 1].Add(ds.getName());
				SASProblem d;
				AStarSearch ast;
				//HillClimbingSearch ast;
				foreach (var directory in Directory.EnumerateDirectories(domainsFolder))
				{
					foreach (var item in Directory.EnumerateFiles(directory))
					{
						logger.Log(" ----- new problem ----- ");
						ds.clear();
						d = SASProblem.CreateFromFile(item);
						//ast = new AStarSearch(d, new FFHeuristic(d));
						ast = new AStarSearch(d, new FFHeuristic(d));
						ast.setHeapDatastructure(ds);
						ast.Search();
						logger.Log();
						results[results.Count - 1].Add(ast.searchTime.TotalSeconds.ToString());
					}
					logger.Log(" ----- new domain ----- ");
				}
			}
			foreach (var row in results)
			{
				foreach (var item in row)
				{
					Console.Write(item + "\t");
				}
				logger.Log();
			}
		}

		static void runHeapsTests(string domainsFolder, List<IHeap<double, IState>> dataStrucutures, TimeSpan timeLimit)
		{
			Logger logger = new Logger();
			string heapsResultsFolder = @".\..\tests\heapsResults";
			SASProblem d;
			AStarSearch ast;
			//HillClimbingSearch ast;
			foreach (var ds in dataStrucutures)
			{
				if (!Directory.Exists(heapsResultsFolder))
				{
					Directory.CreateDirectory(heapsResultsFolder);
				}
				using (var writer = new StreamWriter(heapsResultsFolder + "\\" + ds.getName() + ".txt"))
				{
					var directories = Directory.EnumerateDirectories(domainsFolder);
					foreach (var directory in directories)
					{
						var files = Directory.EnumerateFiles(directory);
						foreach (var item in files)
						{
							logger.Log(" ----- new problem ----- ");
							logger.Log(directory + "\\" + item);

							d = SASProblem.CreateFromFile(item);
							//ast = new AStarSearch(d, new FFHeuristic(d));
							ast = new AStarSearch(d, null);
							ast.SetHeuristic(new FFHeuristic(d));

							DirectoryInfo currentDirectory = new DirectoryInfo(directory);
							FileInfo currentFile = new FileInfo(item);

							ds.clear();
							ast.setHeapDatastructure(ds);

							if (ast.openNodes is PADD.Heaps.MeasuredHeap<IState>)
								((PADD.Heaps.MeasuredHeap<IState>)ast.openNodes).setOutputFile(currentDirectory.Name + "_" + currentFile.Name);

							ast.timeLimit = timeLimit;
							ast.Search();
							writer.WriteLine(currentDirectory.Name + "_" + currentFile.Name + "\t" + ast.searchTime.TotalSeconds + "\t" + ast.GetSearchStatus());
							writer.Flush();

							logger.Log();
						}
						logger.Log(" ----- new domain ----- ");
					}
				}
			}
		}


	}
}
