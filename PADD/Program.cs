using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PADD
{
    class Program
    {
        public static Random r = new Random();
		public static Logger logger = new Logger();

		private static string SAS_allFolder = "./../tests/benchmarksSAS_ALL";
		private static string SAS_all_WithoutAxioms = "./../tests/benchmarksSAS_ALL_withoutAxioms";
		private static string mediumDomainsFolder = "./../tests/benchmarksSAS_ALL - medium";
        private static string mediumDomainsFolderFirstHalf = "./../tests/benchmarksSAS_ALL - medium1";
        private static string mediumDomainsFolderSecondHalf = "./../tests/benchmarksSAS_ALL - medium2";
        private static string small_and_mediumDomainsFolder = "./../tests/benchmarksSAS_ALL - small+medium";
        private static string freeLunchSmall = "../tests/FreeLunchBenchmarks - small";

        private static string testFilesFolder = "../tests/test";
		private static string test2FilesFolder = "../tests/test2";

		/// <summary>
		/// Main function for creating statistics of heuristic values compared to real goal-distances. I.e. for creating "dataToLearn.tsv" file by combining existing histograms.
		/// </summary>
		/// <param name="args"></param>
		static void Main3(string[] args)
		{
			/*
			FeaturesCalculator.countHistograms(small_and_mediumDomainsFolder);
			return;
			*/

			/*
			string folder = mediumDomainsFolderSecondHalf;
			int domainID = int.Parse(args[0]);
            if (Directory.EnumerateDirectories(folder).ToList().Count <= domainID)
                return;
            string domainFolder = Directory.EnumerateDirectories(folder).ToList()[domainID];

			int numberOfProblems = Directory.EnumerateFiles(domainFolder).Count();
            for (int i = 0; i < numberOfProblems; i++)
            {
                createHeuristic2distanceStatictics(domainFolder, i);
            }
			return;
			*/
			//processAllHistogramFolders(testFilesFolder);

			DateTime reWriteIfOlderThan = DateTime.Now;
			processAllHistogramFolders(SAS_all_WithoutAxioms, reWriteIfOlderThan);
		}

		/// <summary>
		/// This function combines all computed resultFiles into a single file.
		/// </summary>
		/// <param name="args"></param>
		static void Main9(string[] args)
		{
			string domainsFolder = SAS_all_WithoutAxioms;
			//string domainsFolder = "../tests/test2";
			gatherResultsFile(domainsFolder);
		}

		/// <summary>
		/// Main function for creating heuristic-distance histograms OR planners results statistics
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			/*
			string Domain = SAS_all_WithoutAxioms + "/logistics00";
			//string Domain = small_and_mediumDomainsFolder + "/tidybotFL";
			int problemNum = 22;
			createHeuristic2distanceStatictics(Domain, problemNum, computers: 1, reWriteIfOlderThan: DateTime.Now);
			//createPlannerResultsStatistics(Domain, problemNum, numberOfComputersUsed: 1, reWriteIfOlderThan: DateTime.Now, timeLimit: TimeSpan.FromMinutes(15));
			return;
			*/

			string domainsFolder = SAS_all_WithoutAxioms;
			//string domainsFolder = "./../tests/test2";

			DateTime reWriteIfOlderThan = new DateTime(2018, 3, 9);
			//DateTime reWriteIfOlderThan = DateTime.Now;

			int problemNumber = int.Parse(args[0]) - 1;
			int numberOfComputers = int.Parse(args[1]);
			//int problemNumber = 1;

			//foreach (var domain in Directory.EnumerateDirectories(domainsFolder))
			//foreach (var domain in Directory.EnumerateDirectories(domainsFolder).Reverse()) //reverse the order of domains in order to skip some problematic instances
			//var domainFolders = shuffleList(Directory.EnumerateDirectories(domainsFolder).ToList(), new Random(problemNumber));
			var domainFolders = shuffleList(Directory.EnumerateDirectories(domainsFolder).ToList(), new Random());

			foreach (var domain in domainFolders)
			{
				//createHeuristic2distanceStatictics(domain, problemNumber, numberOfComputers, reWrite);
				createPlannerResultsStatistics(domain, problemNumber, numberOfComputers, reWriteIfOlderThan, TimeSpan.FromMinutes(30));
			}
			return;
		}

		/// <summary>
		/// Main function for running the experiments.
		/// </summary>
		/// <param name="args"></param>
		[STAThread]
        static void Main5(string[] args)
        {
			//runPlanningExperiments(testFilesFolder, TimeSpan.FromMinutes(15), 3);
			//runPlanningExperiments(mediumDomainsFolderFirstHalf, TimeSpan.FromMinutes(15), int.Parse(args[0]));
			//runPlanningExperiments(mediumDomainsFolderSecondHalf, TimeSpan.FromMinutes(15), int.Parse(args[0]));

			runPlanningExperiments(SAS_all_WithoutAxioms, TimeSpan.FromMinutes(30), int.Parse(args[0]) - 1);

			//runPlanningExperiments(test2FilesFolder, TimeSpan.FromMinutes(30), int.Parse(args[0]) - 1);

			//runPlanningExperiments(test2FilesFolder, TimeSpan.FromMinutes(30), 30);

			return;
        }

		static void Main2(string[] args)
		{
			//runPlanningExperiments(test2FilesFolder, TimeSpan.FromMinutes(15), 3);
			//return;

			/*
			MultiPolynomialRegressionModel m = new MultiPolynomialRegressionModel(2);
			m.trainModel(new List<double[]>() { new double[] { 1 }, new double[] { 2 }, new double[] { 3 }, new double[] { 4 } }, new List<double>() { 3, 1, 2, 2 });
			while (true)
			{
				double input = double.Parse(Console.ReadLine());
				logger.Log("point: " + input + "\tval: " + m.eval(new double[] { input }));
			}
			*/

			//runPlanningExperiments(testFilesFolder, TimeSpan.FromMinutes(15), 3);

			//var predictions = FeaturesCalculator.generatePredictions(@"C:\Users\Trunda_Otakar\Documents\Visual Studio 2017\Projects\PADD\heuristicStats\blocks_probBLOCKS-4-0.txt");

			//FeaturesCalculator.processFolder(@"..\..\..\testing\results\NNvsFF_withRandomWalks\inputs");
			//return;

			//string dataFilePath = @"..\..\..\testing\results\NNvsFF_withoutRandomWalks\inputData\dataToLearn1.tsv",
			string dataFilePath = @"..\..\..\testing\results\NNvsFF_withRandomWalks\inputs\dataToLearn.tsv",
				resultNetworkFile = @"..\..\..\testing\results\NNvsFF_withRandomWalks\inputs\trainedNetwork.bin",
				trainingOutputsFile = @"..\..\..\testing\results\NNvsFF_withRandomWalks\inputs\trainingOutputs.txt";

			//BrightWireNN m = new BrightWireNN();
			BrightWireNN m = new FileBasedModel();

			m.train(dataFilePath);
			
			/*
			foreach (var item in m.evalOnFile(dataFilePath))
			{
				logger.Log(item);
			}
			*/

			//BrightWireNN.save(m, resultNetworkFile);
			//return;
			//m = BrightWireNN.load(resultNetworkFile);

			using (var writer = new StreamWriter(trainingOutputsFile))
			{
				foreach (var item in m.evalOnFile(dataFilePath))
				{
					writer.WriteLine(item);
				}
			}
		}

		/// <summary>
		/// Main function for counting number of computed histograms or searchFile results
		/// </summary>
		/// <param name="args"></param>
		static void Main4(string[] args)
		{
			string domainsFolder = SAS_all_WithoutAxioms;

			var resultsFilesPrefixes = Enum.GetNames(typeof(SearchAlgorithmType)).Select(p => p + "_").ToList();
			var counts = resultsFilesPrefixes.Select(pre => countResultsFiles(domainsFolder, pre));
			
			int totalCount = countAllProblemFiles(domainsFolder);

			foreach (var item in resultsFilesPrefixes.Zip(counts, (pref, count) => (pref, count)))
			{
				Console.WriteLine(item.pref + " results: " + item.count + " computed out of " + totalCount);
			}

			//countFF = countHistograms(domainsFolder);
			//Console.WriteLine("Histograms: " + countFF + " computed out of " + totalCount);
		}

		/// <summary>
		/// The function processes all result files found in subfolders of given folder and merges them into a single file.
		/// </summary>
		/// <param name="domainsFolder"></param>
		static void gatherResultsFile(string domainsFolder)
		{
			string combinedResultsFile = "allResults.tsv";

			var prefixes = Enum.GetNames(typeof(SearchAlgorithmType)).ToList();

			//List<string> resultsFilesPrefixes = new List<string>() { "HeurFF_", "HeurFile_" };
			List<string> resultsFilesPrefixes = prefixes.Select(p => p + "_").ToList();

			int numerOfParamsInEachFile = 6;
			using (var writer = new StreamWriter(combinedResultsFile))
			{
				writer.Write("domainFile\tproblemFile\t");
				foreach (var prefix in resultsFilesPrefixes)
				{
					writer.Write(prefix + "totalTime(sec)\t");
					writer.Write(prefix + "expandedNodes\t");
					writer.Write(prefix + "solved?\t");
					writer.Write(prefix + "planLength\t");
					writer.Write(prefix + "minHeurValue\t");
					writer.Write(prefix + "avgHeurValue\t");
				}
				writer.WriteLine();

				foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolder))
				{
					foreach (var problemFile in Directory.EnumerateFiles(domainFolder))
					{
						if (Path.GetExtension(problemFile) != ".sas")
							continue;   //not a planning problem file
						writer.Write(Path.GetFileName(domainFolder) + "\t");
						writer.Write(Path.GetFileNameWithoutExtension(problemFile) + "\t");
						foreach (var prefix in resultsFilesPrefixes)
						{
							string resultFile = Path.Combine(new FileInfo(problemFile).Directory.FullName, "results", prefix + Path.GetFileNameWithoutExtension(problemFile) + ".txt");
							if (!File.Exists(resultFile))
							{
								for (int i = 0; i < numerOfParamsInEachFile; i++)
								{
									writer.Write("\t");
								}
							}
							else
							{
								string[] splittedFileData = File.ReadAllText(resultFile).Split(new string[] { ";", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
								foreach (var item in splittedFileData.Skip(splittedFileData.Length - numerOfParamsInEachFile))
								{
									writer.Write(item + "\t");
								}
							}
						}
						writer.WriteLine();
					}
				}
			}
		}

		/// <summary>
		/// Returns a number that corresponds to total number of computed histograms for all domains in a given folder
		/// </summary>
		/// <param name="domainsFolder"></param>
		public static int countHistograms(string domainsFolder)
		{
			int totalCount = 0;
			foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolder))
			{
				foreach (var problemFile in Directory.EnumerateFiles(domainFolder))
				{
					if (Path.GetExtension(problemFile) != ".sas")
						continue;
					if (Program.IsHistogramComputed(domainFolder, problemFile))
						totalCount++;
				}
			}
			return totalCount;
		}

		public static int countAllProblemFiles(string domainsFolder)
		{
			int totalCount = 0;
			foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolder))
			{
				foreach (var problemFile in Directory.EnumerateFiles(domainFolder))
				{
					if (Path.GetExtension(problemFile) == ".sas")
						totalCount++;
				}
			}
			return totalCount;
		}

		public static int countResultsFiles(string domainsFolder, string filePrefix)
		{
			int totalCount = 0;
			foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolder))
			{
				foreach (var problemFile in Directory.EnumerateFiles(domainFolder))
				{
					if (Path.GetExtension(problemFile) != ".sas")
						continue;
					if (!Directory.Exists(Path.Combine(domainFolder, "results")))
						continue;
					string fileToFind = Path.Combine(domainFolder, "results", filePrefix + Path.GetFileName(Path.ChangeExtension(problemFile, ".txt")));
					if (Directory.EnumerateFiles(Path.Combine(domainFolder, "results")).Any(f => f == fileToFind))
						totalCount++;
				}
			}
			return totalCount;
		}

		/// <summary>
		/// This function copies results (as well as computed histograms) from one tests folder to another, such that the results are on the right place in the second folder.
		/// </summary>
		/// <param name="args"></param>
		public static void copyResultFiles(string folderWithResults, string folderToCopyResultsTo)
		{
			List<string> namesOfFoldersToCopy = new List<string>() { "results", "histograms" };

			var domainsWithResults = new HashSet<string>(Directory.EnumerateDirectories(folderWithResults).Select(dir => Path.GetFileName(dir)));

			foreach (var domainFolderName in Directory.EnumerateDirectories(folderToCopyResultsTo).Select(dir => Path.GetFileName(dir)))
			{
				if (domainsWithResults.Contains(domainFolderName))
				{
					var folderNamesInSource = Directory.EnumerateDirectories(Path.Combine(folderWithResults, domainFolderName)).Select(dir => Path.GetFileName(dir));
					foreach (var folderToCopy in namesOfFoldersToCopy)
					{
						if (folderNamesInSource.Contains(folderToCopy))
						{
							DirectoryCopy(Path.Combine(folderWithResults, domainFolderName, folderToCopy), Path.Combine(folderToCopyResultsTo, domainFolderName, folderToCopy), true);
						}
					}
				}
			}
		}

		private static void DirectoryCopy(string sourceDirPath, string destDirPath, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirPath);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirPath);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirPath))
			{
				Directory.CreateDirectory(destDirPath);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirPath, file.Name);
				file.CopyTo(temppath, false);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirPath, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}

		[STAThread]
        static void Main_OLD(string[] args)
        {
            //runPlanningExperiments(mediumDomainsFolder, TimeSpan.FromMinutes(15), int.Parse(args[0]));
            return;


            /*
            //Red_BlackDomain d = Red_BlackDomain.createFromFile(@"..\tests\floortile-seq-p01-001.sas");
            //Red_BlackDomain d = Red_BlackDomain.createFromFile(@"..\tests\nomystery-p01.sas");
            Red_BlackDomain d = Red_BlackDomain.createFromFile(@"..\tests\elevators-p01.sas");
            //Red_BlackDomain d = Red_BlackDomain.createFromFile(@"..\tests\parcprinter-p01.sas");
            //Red_BlackDomain d = Red_BlackDomain.createFromFile(@"..\tests\pegsol-p01.sas");
            //Red_BlackDomain d = Red_BlackDomain.createFromFile(@"..\tests\test.sas");

            d.makeAllAbstracted();
            //AStarSearch ast = new AStarSearch(d, new BlindHeuristic());
            //AStarSearch ast = new AStarSearch(d, new NotAccomplishedGoalCount(d));
            AStarSearch ast = new AStarSearch(d, new AbstractStateSizeHeuristic(d));
            */

            //runHeapTests(); return;


            //runPatternsTest(@"..\tests\nomystery-p01.sas");
            //runPatternsTest(@"..\tests\test.sas");
            //return;

            //List<IHeap<int, State>> datastructs = new List<IHeap<int, State>>();
            /*
            datastructs.Add(new Heaps.FibonacciHeap2<State>());
            
            datastructs.Add(new Heaps.BinomialHeap<State>());
            datastructs.Add(new Heaps.LeftistHeap<State>());
            datastructs.Add(new Heaps.RedBlackTreeHeap<State>());
            datastructs.Add(new Heaps.RegularBinaryHeap<State>());
            datastructs.Add(new Heaps.RegularTernaryHeap<State>());
            
            datastructs.Add(new Heaps.OrderedMutliDictionaryHeap<State>());
             
            datastructs.Add(new Heaps.FibonacciHeap2<State>());
            
            runHeapsTests(mediumDomainsFolder, datastructs, TimeSpan.FromMinutes(15)); return;

            /*
            string FilesToPrune = @"..\tests\benchmarksSAS_ALL - small";
            DeleteTooEasyTasks(FilesToPrune, TimeSpan.FromSeconds(5));
            return;
             */

            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\floortile-seq-p01-001.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\..\..\..\TSP\TSP\bin\Release\tempTSP2SAS.sas");
            //  SASDomain d = SASDomain.createFromFile(@"..\tests\nomystery-p01.sas");
            //PlanningProblem d = PDDLDomain.createFromFile(@"..\tests\example_domain1.pddl", @"..\tests\example_problem1.pddl");


            // rozsah: 16 hodnot pro predID, 64 hodnot pro 4 ruzne parametry, pro kazdy ze 4 parametru mame take isVar()
            //PDDLDesignatorAdv pred = new PDDLDesignatorAdv(3, new int[] { 5, 31, 9, 40 }, new bool[] { true, true, false, true });

            /*

            // 5bit predID + 3x 8bit paramID (+ 3x 1bit isVarID)
            PDDLDesignatorAdv_5_8_8_8 pred = new PDDLDesignatorAdv_5_8_8_8(3, new int[] { 50, 33, 4 }, new bool[] { true, true, false });
            int predID = pred.getPredID();
            int paramID1 = pred.getParam(0);
            int paramID2 = pred.getParam(1);
            int paramID3 = pred.getParam(2);
            //int paramID4 = pred.getParam(3);
            bool isVar1 = pred.isParamVar(0);
            bool isVar2 = pred.isParamVar(1);
            bool isVar3 = pred.isParamVar(2);
            //bool isVar4 = pred.isParamVar(3);

            pred.setParam(0, 21);
            pred.setParam(1, 13);
            pred.setParam(2, 0);
            //pred.setParam(3, 9);

            paramID1 = pred.getParam(0);
            paramID2 = pred.getParam(1);
            paramID3 = pred.getParam(2);
            //paramID4 = pred.getParam(3);

            */





            //long StopBytes = 0;
            //PDDLDesignator2 myFoo;

            //long StartBytes = System.GC.GetTotalMemory(true);
            //myFoo = new PDDLDesignator2();
            //StopBytes = System.GC.GetTotalMemory(true);
            //GC.KeepAlive(myFoo); // This ensure a reference to object keeps object in memory


            //long ccc = (StopBytes - StartBytes);
            //logger.Log("Byte size = {0}, bit size = {1}, ints = {2}", ccc, ccc*8, ccc/4);

            //PDDLDesignator predd = new PDDLDesignator();

            //long size2 = System.Runtime.InteropServices.Marshal.SizeOf(predd);



            //int aaa = 5;






            //PDDLDesignatorAdv_5_8_8_8 pred = new PDDLDesignatorAdv_5_8_8_8(3, new int[] { 50, 33, 4 }, new bool[] { true, true, false });
            //int aaa = 5;

            //long size = 0;
            //object o = new PDDLDesignatorSimple2();
            //using (Stream s = new MemoryStream())
            //{
            //    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //    formatter.Serialize(s, o);
            //    size = s.Length;
            //}

            //long size2 = System.Runtime.InteropServices.Marshal.SizeOf(aaa);

            //long size3 = sizeof(int);

            //PDDLDesignator pp;
            //pp.pred = 32;


            ////long dfdf = sizeof(PDDLExper);
            //long dfbdsb = System.Runtime.InteropServices.Marshal.SizeOf(pp);

            //PDDLDesignator pred = new PDDLDesignator(21, new int[] { 1 }, new bool[] { true });
            //int predID = pred.getPredID();
            //int paramID1 = pred.getParam(0);
            //int paramID2 = pred.getParam(1);
            //int paramID3 = pred.getParam(2);

            //bool isVar1 = pred.isParamVar(0);
            //bool isVar2 = pred.isParamVar(1);
            //bool isVar3 = pred.isParamVar(2);

            //int paramCount = pred.getParamCount();
            //bool isNull = pred.isNull();

            //PDDLOperatorSubstitution substit = new PDDLOperatorSubstitution(new int[] { 1, 2 });
            //pred = PDDLOperatorSubstitution.makeSubstituedDesignator(pred, substit);


            //paramID1 = pred.getParam(0);
            //paramID2 = pred.getParam(1);
            //paramID3 = pred.getParam(2);



            IPlanningProblem d = PDDLProblem.CreateFromFile(
                @"..\tests\01_IPC-1998_Gripper\domain.pddl",
                @"..\tests\01_IPC-1998_Gripper\prob01.pddl",
                new PDDLDesignatorFactory(),
                new PDDLStateFactory()
                );

            //var dict = d.getSuccessors(d.getInitialState(), 1);

            //int countA = 0;
            //int countB = 0;
            //for (int i = 0; i < 100; ++i)
            //{
            //    var succ = d.getRandomSuccessor(d.getInitialState());
            //    if (succ.Item2.GetHashCode() == 1122)
            //        ++countA;
            //    else
            //        ++countB;
            //    //System.Threading.Thread.Sleep(100);
            //}

            //var dict3 = d.getRandomSuccessor(d.getInitialState());
            //int stateHash2 = dict3.Item2.GetHashCode();

            //var dict4 = d.getRandomSuccessor(d.getInitialState());
            //var dict5 = d.getRandomSuccessor(d.getInitialState());
            //var dict6 = d.getRandomSuccessor(d.getInitialState());

            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\nomystery-p02.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\nomystery-p03.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\elevators-p01.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\parcprinter-p01.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\pegsol-p01.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\sokoban-p01.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\visitall-problem14.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\scanalyzer-p03.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\openstack-p02.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\test.sas");
            //PlanningProblem d = PlanningProblem.createFromFile(@"..\tests\benchmarksSAS_ALL - medium\airport\p36-airport5MUC-p2.sas");

            AStarSearch ast = new AStarSearch(d, null);
            //AStarSearch ast = new f_limitedAStarSearch(d, null);

            //ast.setHeuristic(new BlindHeuristic());
            ast.SetHeuristic(new NotAccomplishedGoalCount());
            //ast.setHeuristic(new FFHeuristic(d));
            //ast.setHeuristic(new PDBHeuristic(d));
            //ast.setHeuristic(new DeleteRelaxationHeuristic_Perfect(d));
            //ast.setHeuristic(new PlannigGraphLayersHeuristic(d));
            //ast.setHeuristic(new WeightedHeuristic(new FFHeuristic(d), 200));
            
            //HeuristicSearchEngine ast = new HillClimbingSearch(d, new FFHeuristic(d));
            //AStarSearch ast = new MCTSSolver(d, new FFHeuristic(d));
            //Heuristic h = new PDBHeuristic(d);

            //PlanningGraphComputation pgc = new PlanningGraphComputation(d);
            //pgc.computePlanningGraph(d.initialState);

            //KnowledgeHolder holder = KnowledgeHolder.compute(d);
            //holder.visualize();
            //return;

            //TreeVisualizerForm f = new TreeVisualizerForm(d, new FFHeuristic(d));
            //System.Windows.Forms.Application.Run(f);
            //return;
            ast.setHeapDatastructure(new Heaps.FibonacciHeap2<IState>());
            //ast.setHeapDatastructure(new Heaps.RedBlackTreeHeap<State>());
            //ast.setHeapDatastructure(new Heaps.SortedSetHeap<State>());
            //ast.setHeapDatastructure(new Heaps.SortedDictionaryHeap<State>());
            //ast.setHeapDatastructure(new Heaps.RegularBinaryHeap<State>());
            //ast.setHeapDatastructure(new Heaps.RegularTernaryHeap<State>());
            //ast.setHeapDatastructure(new Heaps.OrderedMutliDictionaryHeap<State>());
            //ast.setHeapDatastructure(new Heaps.MeasuredHeap<State>());
            //ast.setHeapDatastructure(new Heaps.SingleBucket<State>(32));

            ast.Search();
            string solutionStr = ast.GetSolution().ToString();

          //  if (ast.openNodes is Heaps.MeasuredHeap<State>)
          //      ((Heaps.MeasuredHeap<State>)ast.openNodes).printStats();
         }

        static void runHeapsTestsOLD(string domainsFolder, List<IHeap<double, IState>> dataStrucutures, TimeSpan timeLimit)
        {
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
                using (var writer = new StreamWriter(heapsResultsFolder + "\\" + ds.getName()+".txt"))
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

                            if (ast.openNodes is Heaps.MeasuredHeap<IState>)
                                ((Heaps.MeasuredHeap<IState>)ast.openNodes).setOutputFile(currentDirectory.Name + "_" + currentFile.Name);

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

		static void runPlanningExperiments(string domainsFolder, TimeSpan timeLimit, int param)
		{
			List<SearchResults> allResults = new List<SearchResults>();

			//try
			//{

			//the number of computes that participate on this job. Computation is distributed among them.
			int numberOfComputes = 2;

			//if set to true, problem file that don't have histogram computed will be skipped. Otherwise all problems will be processed.
			bool onlyWhenHistogramExists = false;

			SASProblem d;
			AStarSearch ast;
			//HillClimbingSearch ast;
			var directories = Directory.EnumerateDirectories(domainsFolder);
			allResults = new List<SearchResults>();
			foreach (var directory in directories)
			{
				var files = Directory.EnumerateFiles(directory).ToList();
				foreach (var SASFile in files)
				{
					int indexOfFile = files.IndexOf(SASFile);
					if (indexOfFile % numberOfComputes != param) //if (indexOfFile != param) 
						continue;
					if (onlyWhenHistogramExists && !IsHistogramComputed(directory, SASFile))
						continue;

					logger.Log(" ----- new problem ----- ");
					logger.Log(directory + "\\" + SASFile);

					d = SASProblem.CreateFromFile(SASFile);

					Heuristic h = new FFHeuristic(d);

					/*
					string histogramFolder = @"C:\Users\Ota\Documents\Visual Studio 2017\Projects\PADD\heuristicStats";
					var samples = HistogramVisualizer.Form1.getHistograms(histogramFolder);
					List<HistogramVisualizer.Histograms> selectedSamples = new List<HistogramVisualizer.Histograms>();
					foreach (var item in samples[Path.GetFileName(directory)].Values)
					{
						selectedSamples.AddRange(item);
					}
					*/

					//string trainedNetworkFile = Path.Combine("..", "trainedNetwork.bin");
					//Heuristic h = new NNHeuristic(d, trainedNetworkFile);

					//string dataFile = Path.Combine(directory, "histograms", "dataToLearn.tsv");
					//Heuristic h = new FileBasedHeuristic(d, dataFile, false);

					//Heuristic h = new FFHeuristic(d);
					//Heuristic h = new RegHeuristic(new FFHeuristic(d));
					//Heuristic h = new FileBasedHeuristic(d, dataFile, false);

					//h = getHeuristicByParam(param, d);
					//h = getHeuristicByParam(6, d);

					ast = new AStarSearch(d, h);
					//ast = new MultipleOpenListsAStar(d, h);
					//ast = new MultipleOpenListsAStar(d, new List<Heuristic>() { h, hNN });
					//ast = new IDAStarSearch(d, null);

					IHeap<double, IState> heapStructure = null;

					//heapStructure = getHeapByParam(param);

					//IHeap<double, IState> heapStructure = new Heaps.MeasuredHeap<IState>();
					heapStructure = new Heaps.RedBlackTreeHeap<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.FibonacciHeap1<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.FibonacciHeap2<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.RegularBinaryHeap<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.RegularTernaryHeap<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.BinomialHeap<IState>();
					//IHeap<double, IState> heapStructure = new Heaps.LeftistHeap<IState>();

					//ast.setHeapDatastructure(heapStructure);

					DirectoryInfo currentDirectory = new DirectoryInfo(directory);
					FileInfo currentFile = new FileInfo(SASFile);

					if (ast.openNodes is Heaps.MeasuredHeap<IState>)
						((Heaps.MeasuredHeap<IState>)ast.openNodes).setOutputFile(currentDirectory.Name + "_" + currentFile.Name);

					ast.timeLimit = timeLimit;
					ast.results.domainName = (Path.GetFileName(directory));
					ast.results.problemName = (Path.GetFileName(SASFile));
					ast.results.heuristicName = h.getDescription();
					ast.results.algorithm = ast.getDescription() + "+" + heapStructure.getName();

					ast.Search();
					ast.results.bestHeuristicValue = h.statistics.bestHeuristicValue;
					ast.results.avgHeuristicValue = h.statistics.getAverageHeurValue();

					//foreach (var item in ast.GetSolution().GetOperatorSeqIndices())
					//    Console.Write(item + " ");
					allResults.Add(ast.results);
					if (ast.openNodes is Heaps.MeasuredHeap<IState>)
						((Heaps.MeasuredHeap<IState>)ast.openNodes).clearStatistics();
					logger.Log();
				}
				logger.Log(" ----- new domain ----- ");
			}


			//			catch (Exception e)
			//			{
			//			    using (var writer = new System.IO.StreamWriter("results" + Environment.MachineName + ".txt"))
			//			        foreach (var item in allResults)
			//			        {
			//			            writer.WriteLine(item.ToString());
			//			        }
			//}
			using (var writer = new System.IO.StreamWriter("results" + Environment.MachineName + ".txt"))
				foreach (var item in allResults)
				{
					writer.WriteLine(item.ToString());
				}
		}

		/// <summary>
		/// Takes path to a SAS planning problem and determines whether histogram for this file is computed and stored in apropriate folder.
		/// I.e. it checks a folder ./histograms and searches for a file with the same name as the domain file (except the extension).
		/// </summary>
		/// <param name="folderPath"></param>
		/// <param name="fileName"></param>
		/// <returns>True if the histogram file is found.</returns>
		public static bool IsHistogramComputed(string folderPath, string fileName)
		{
			if (!Directory.Exists(Path.Combine(folderPath, "histograms")))
				return false;
			var histogramFiles = Directory.EnumerateFiles(Path.Combine(folderPath, "histograms"));
			if (histogramFiles.Any(t => Path.GetFileNameWithoutExtension(t) == Path.GetFileNameWithoutExtension(fileName)))
				return true;
			return false;
		}

		/// <summary>
		/// The method takes the given problem and enumerates its state-space.
		/// If rewrite is set to true, new histograms will be created even if they already exist, is it is set to false, domains, where there already are histograms, will be skipped.
		/// </summary>
		static void createHeuristic2distanceStatictics(string domainFolder, int param, int computers, DateTime reWriteIfOlderThan)
        {
			logger.Log("Param is " + param);
			var files = Directory.EnumerateFiles(domainFolder).ToList();
			int numberOfComputers = computers;
			foreach (var item in files)
			{
				if (numberOfComputers == 1 && files.IndexOf(item) != param)
				{
					logger.Log("Skipping file " + item + " whose index is " + files.IndexOf(item));
					continue;
				}

				if (numberOfComputers > 1 && files.IndexOf(item) % numberOfComputers != param)
				{
					logger.Log("Skipping file " + item + " whose index is " + files.IndexOf(item));
					continue;
				}

				string problemFile = item;
				logger.Log("Processing file " + item);

				var d = SASProblem.CreateFromFile(problemFile);
				List<Heuristic> heuristics = new List<Heuristic>() { new FFHeuristic(d) };


				string domainName = Path.GetFileName(domainFolder);
				string probleName = Path.GetFileName(item);

				/*
				List<int> solution = new List<int>() { 3, 5, 14, 13, 12, 10, 1, 16 };
				List<SASOperator> allOps = d.GetOperators();

				var s = d.GetInitialState();
				foreach (var opIndex in solution)
				{
					SASOperator op = allOps[opIndex];
					logger.Log();
					logger.Log(string.Join(" ", ((SASState)(s)).GetAllValues().Select(v => v >= 0 ? " " + v : v.ToString())));
					if (!op.IsApplicable(s))
						throw new Exception();
					s = op.Apply(s);
				}
				if (s.IsMeetingGoalConditions())
				{
					logger.Log("huraa");
				}
				*/

				string resultsPath = Path.Combine(domainFolder, "histograms");

				if (!Directory.Exists(resultsPath))
					Directory.CreateDirectory(resultsPath);

				string resultFile = Path.Combine(resultsPath, Path.ChangeExtension(probleName, "txt"));
				if(File.Exists(resultFile) && File.GetCreationTime(resultFile) >= reWriteIfOlderThan)
				{
					logger.Log("Skipping domain " + domainName + " , file " + probleName + " - histogram already exists and it's up to date (" + File.GetCreationTime(resultFile).ToLongDateString() + ")");
					continue;
				}

				logger.Log("Processing domain " + domainName + " , file " + probleName + (File.Exists(resultFile) ? "\tfile exists but it's old (" + File.GetCreationTime(resultFile).ToLongDateString() + ")" : ""));

				var histogram = StateSpaceHistogramCalculator.getHistogram(problemFile, heuristics);

				StateSpaceHistogramCalculator.writeHistograms(resultFile, histogram);
				logger.Log("Histogram successfully written to " + resultFile);
				//StateSpaceHistogramCalculator.writeHistograms(domainName + "_" + Path.ChangeExtension(probleName, "txt"), histogram);
			}
        }

		/// <summary>
		/// Runs planner on the set of problems creating a file with results. Separate results file is created for every input problem.
		/// </summary>
		/// <param name="domainFolder"></param>
		/// <param name="problemNumber"></param>
		/// <param name="reWrite"></param>
		/// <param name="timeLimit"></param>
		static void createPlannerResultsStatistics(string domainFolder, int problemNumber, int numberOfComputersUsed, DateTime reWriteIfOlderThan, TimeSpan timeLimit)
		{
			logger.Log("Param is " + problemNumber);
			var files = Directory.EnumerateFiles(domainFolder).ToList();
			int numberOfComputers = numberOfComputersUsed;

			var types = Enum.GetValues(typeof(SearchAlgorithmType));

			foreach (SearchAlgorithmType typeItem in types)
			{
				SearchAlgorithmType type = typeItem;
				//SearchAlgorithmType type = SearchAlgorithmType.heurFile_Median;

				Console.WriteLine("Number of computers: " + numberOfComputers);
				foreach (var item in files)
				{
#if DEBUG
					if (numberOfComputersUsed == 1 && files.IndexOf(item) != problemNumber)
						continue;
#endif

					if (numberOfComputersUsed > 1 && files.IndexOf(item) % numberOfComputers != problemNumber)
					{
						logger.Log("Skipping file " + item + " whose index is " + files.IndexOf(item));
						continue;
					}

					string problemFile = item;
					logger.Log("Processing file " + item);

					var d = SASProblem.CreateFromFile(problemFile);

					string domainName = Path.GetFileName(domainFolder);
					string probleName = Path.GetFileName(item);
					string resultsPath = Path.Combine(domainFolder, "results");

					if (!Directory.Exists(resultsPath))
						Directory.CreateDirectory(resultsPath);

					string resultFile = Path.Combine(resultsPath, type.ToString() + Path.GetFileNameWithoutExtension(probleName) + ".txt");

					if (File.Exists(resultFile) && File.GetCreationTime(resultFile) >= reWriteIfOlderThan)
					{
						logger.Log("Skipping domain " + domainName + " , file " + probleName + " - resultsFile already exists and is up to date (" + File.GetCreationTime(resultFile).ToLongDateString() + ")");
						continue;
					}

					logger.Log("Processing domain " + domainName + " , file " + probleName + (File.Exists(resultFile) ? "\tresultsFile exists but it's old (" + File.GetCreationTime(resultFile).ToLongDateString() + ")" : ""));

					string dataFile = "";
					switch (type)
					{
						case SearchAlgorithmType.doubleListFF_FileMean:
						case SearchAlgorithmType.heurFile_Mean:
							dataFile = Path.Combine(domainFolder, "histograms", "dataToLearn_Mean.tsv");
							break;
						case SearchAlgorithmType.doubleListFF_FileMedian:
						case SearchAlgorithmType.heurFile_Median:
							dataFile = Path.Combine(domainFolder, "histograms", "dataToLearn_Median.tsv");
							break;
					}

					Heuristic h = null;

					switch (type)
					{
						case SearchAlgorithmType.heurFF:
							h = new FFHeuristic(d);
							break;
						case SearchAlgorithmType.doubleListFF_FileMean:
						case SearchAlgorithmType.doubleListFF_FileMedian:
						case SearchAlgorithmType.heurFile_Mean:
						case SearchAlgorithmType.heurFile_Median:
							h = new FileBasedHeuristic(d, dataFile, false);
							break;
					}

					AStarSearch ast = null;

					switch (type)
					{
						case SearchAlgorithmType.heurFF:
						case SearchAlgorithmType.heurFile_Mean:
						case SearchAlgorithmType.heurFile_Median:
							ast = new AStarSearch(d, h);
							break;
						case SearchAlgorithmType.doubleListFF_FileMean:
						case SearchAlgorithmType.doubleListFF_FileMedian:
							ast = new MultipleOpenListsAStar(d, new List<Heuristic>() { h, new FFHeuristic(d) });
							break;
					}

					ast.timeLimit = timeLimit;
					ast.results.domainName = domainName;
					ast.results.problemName = probleName;
					ast.results.heuristicName = h.getDescription();
					ast.results.algorithm = ast.getDescription() + "+" + ast.openNodes.getName();

					ast.Search();
					ast.results.bestHeuristicValue = h.statistics.bestHeuristicValue;
					ast.results.avgHeuristicValue = h.statistics.getAverageHeurValue();

#if DEBUG
					bool printPlan = true;
					if (printPlan)
					{
						Console.WriteLine("Plan:");
						var state = d.GetInitialState();
						Console.WriteLine(state.ToString());
						foreach (var opIndex in ast.GetSolution().GetOperatorSeqIndices())
						{
							var op = d.GetOperators()[opIndex];
							state = op.Apply(state);
							Console.WriteLine(state.ToString() + "\toperator aplied: " + op.ToString());
						}
					}
#endif
					using (var writer = new System.IO.StreamWriter(resultFile))
						writer.WriteLine(ast.results.ToString());

					logger.Log("Results successfully written to " + resultFile);
				}
			}
		}

		/// <summary>
		/// Reads all folders in given folder, searches for folders named "histograms". Inside these folders it finds all histogram files and merges them into a "dataToLearn.tsv" file that can then be used for training a ML model.
		/// These files will be created in every respective directory. If the ".tsv" file already exists, it will be rewritten only if it is older than <paramref name="reWriteIfOlderThan"/>.
		/// </summary>
		static void processAllHistogramFolders(string domainsFolderPath, DateTime reWriteIfOlderThan)
		{
			foreach (var domainFolder in Directory.EnumerateDirectories(domainsFolderPath))
			{
				string histogramFolder = Path.Combine(domainFolder, "histograms");
				if (Directory.Exists(histogramFolder))
				{
					logger.Log("processing folder " + histogramFolder);
					foreach (StatesHistogramType item in Enum.GetValues(typeof(StatesHistogramType)))
					{
						FeaturesCalculator.processHistogramsFolder(histogramFolder, reWriteIfOlderThan, item);
					}
					logger.Log("done");
				}
			}
		}

        static Heuristic getHeuristicByParam(int param, SASProblem d)
        {
            Heuristic h = null;

            //Heuristic innerHeuristic = new FFHeuristic(d);
            Heuristic innerHeuristic = new WeightedHeuristic(new FFHeuristic(d), 2);

            switch (param)
            {
                case 1:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.05));
                    break;
                case 2:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.1));
                    break;
                case 3:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.15));
                    break;
                case 4:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.2));
                    break;
                case 5:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.3));
                    break;
                case 6:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalNormal, 0.4));
                    break;
                case 7:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.05));
                    break;
                case 8:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.1));
                    break;
                case 9:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.15));
                    break;
                case 10:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.2));
                    break;
                case 11:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.3));
                    break;
                case 12:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ProportionalUniform, 0.4));
                    break;
                case 13:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.constantUniform, 1));
                    break;
                case 14:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.constantUniform, 2));
                    break;
                case 15:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.constantUniform, 3));
                    break;
                case 16:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.constantUniform, 4));
                    break;
                case 17:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.constantUniform, 5));
                    break;
                case 18:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ConstantNormal, 1));
                    break;
                case 19:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ConstantNormal, 2));
                    break;
                case 20:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ConstantNormal, 3));
                    break;
                case 21:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ConstantNormal, 4));
                    break;
                case 22:
                    h = new NoisyHeuristic(innerHeuristic, NoiseGenerator.createInstance(NoiseGenerationType.ConstantNormal, 5));
                    break;
                default:
                    break;
            }
            return h;
        }

        static IHeap<double, IState> getHeapByParam(int param)
        {
            IHeap<double, IState> heapStructure = null;
            switch (param)
            {
                case 1:
                    heapStructure = new Heaps.RedBlackTreeHeap<IState>();
                    break;
                case 2:
                    heapStructure = new Heaps.FibonacciHeap1<IState>();
                    break;
                case 3:
                    heapStructure = new Heaps.FibonacciHeap2<IState>();
                    break;
                case 4:
                    heapStructure = new Heaps.RegularBinaryHeap<IState>();
                    break;
                case 5:
                    heapStructure = new Heaps.RegularTernaryHeap<IState>();
                    break;
                case 6:
                    heapStructure = new Heaps.BinomialHeap<IState>();
                    break;
                case 7:
                    heapStructure = new Heaps.LeftistHeap<IState>();
                    break;
                default:
                    break;
            }

            return heapStructure;
            //heapStructure = new Heaps.RedBlackTreeHeap<IState>();
            //IHeap<double, IState> heapStructure = new Heaps.FibonacciHeap1<IState>();
            //IHeap<double, IState> heapStructure = new Heaps.FibonacciHeap2<IState>();
            //IHeap<double, IState> heapStructure = new Heaps.RegularBinaryHeap<IState>();
            //IHeap<double, IState> heapStructure = new Heaps.RegularTernaryHeap<IState>();
            //IHeap<double, IState> heapStructure = new Heaps.BinomialHeap<IState>();
            //IHeap<double, IState> heapStructure = new Heaps.LeftistHeap<IState>();
        }

        /// <summary>
        /// Recursively enumerates given directory and for every sas+ task it finds, it runs an A* search with given time limit. If the task is not solved in the time limit, the file is removed. If all files from the directory are removed, the directory is removed as well.
        /// </summary>
        /// <param name="domainsFolder"></param>
        /// <param name="timeLimit"></param>
        static void DeleteTooComplexTasks(string domainsFolder, TimeSpan timeLimit)
        {
            SASProblem d;
            AStarSearch ast;
            //HillClimbingSearch ast;
            var directories = Directory.EnumerateDirectories(domainsFolder);
            foreach (var directory in directories)
            {
                bool isSomethingSolvedInThisDirectory = false,
                    previousFileNotSolved = false,
                    removingAllFiles = false;
                var files = Directory.EnumerateFiles(directory);
                foreach (var item in files)
                {
                    logger.Log(" ----- new problem ----- ");
                    logger.Log(directory + "\\" + item);
                    if (removingAllFiles)
                    {
                        logger.Log("deleting file " + item);
                        File.Delete(item);
                        continue;
                    }

                    d = SASProblem.CreateFromFile(item);
                    //ast = new AStarSearch(d, new FFHeuristic(d));
                    ast = new AStarSearch(d, null);
                    ast.SetHeuristic(new FFHeuristic(d));
                    ast.setHeapDatastructure(new Heaps.RedBlackTreeHeap<IState>());
                    ast.timeLimit = timeLimit;
                    ast.Search();
                    if (ast.GetSearchStatus() != SearchStatus.SolutionFound && ast.GetSearchStatus() != SearchStatus.NoSolutionExist)
                    {
                        //task not solved. it will be removed
                        logger.Log("deleting file " + item);
                        File.Delete(item);
                        if (previousFileNotSolved)
                        {
                            //if two successive tasks are not solved within the timelimit, we assume the following tasks wont be solved either and remove them all
                            //removingAllFiles = true;
                        }
                        previousFileNotSolved = true;
                    }
                    else
                    {
                        isSomethingSolvedInThisDirectory = true;
                        previousFileNotSolved = false;
                    }
                    logger.Log();
                }
                if (!isSomethingSolvedInThisDirectory)
                {
                    //no task has been solved - removing empty folder
                    logger.Log("deleting directory " + directory);
                    Directory.Delete(directory);
                }
                logger.Log(" ----- new domain ----- ");
            }
            
        }

        static void DeleteTooEasyTasks(string domainsFolder, TimeSpan timeLimit)
        {
            SASProblem d;
            AStarSearch ast;
            //HillClimbingSearch ast;
            var directories = Directory.EnumerateDirectories(domainsFolder);
            foreach (var directory in directories)
            {
                bool isSomethingSolvedInThisDirectory = false,
                    previousFileNotSolved = false,
                    removingAllFiles = false;
                var files = Directory.EnumerateFiles(directory);
                foreach (var item in files)
                {
                    logger.Log(" ----- new problem ----- ");
                    logger.Log(directory + "\\" + item);
                    if (removingAllFiles)
                    {
                        logger.Log("deleting file " + item);
                        File.Delete(item);
                        continue;
                    }

                    d = SASProblem.CreateFromFile(item);
                    //ast = new AStarSearch(d, new FFHeuristic(d));
                    ast = new AStarSearch(d, null);
                    ast.SetHeuristic(new FFHeuristic(d));
                    ast.setHeapDatastructure(new Heaps.RedBlackTreeHeap<IState>());
                    ast.timeLimit = timeLimit;
                    ast.Search();
                    if (ast.GetSearchStatus() == SearchStatus.SolutionFound || ast.GetSearchStatus() == SearchStatus.NoSolutionExist)
                    {
                        //task solved. it will be removed
                        logger.Log("deleting file " + item);
                        File.Delete(item);
                        if (previousFileNotSolved)
                        {
                            //if two successive tasks are not solved within the timelimit, we assume the following tasks wont be solved either and remove them all
                            //removingAllFiles = true;
                        }
                        previousFileNotSolved = true;
                    }
                    else
                    {
                        isSomethingSolvedInThisDirectory = true;
                        previousFileNotSolved = false;
                    }
                    logger.Log();
                }
                if (!isSomethingSolvedInThisDirectory)
                {
                    //no task has been solved - removing empty folder
                    logger.Log("deleting directory " + directory);
                    Directory.Delete(directory);
                }
                logger.Log(" ----- new domain ----- ");
            }

        }

		static void DeleteProblemsWithAxiomRules(string domainsFolder)
		{
			SASProblem d;

			var directories = Directory.EnumerateDirectories(domainsFolder);
			foreach (var directory in directories)
			{
				bool isSomethingSolvedInThisDirectory = false,
					previousFileNotSolved = false,
					removingAllFiles = false;
				var files = Directory.EnumerateFiles(directory);
				foreach (var item in files)
				{
					logger.Log(" ----- new problem ----- ");
					logger.Log(directory + "\\" + item);
					if (removingAllFiles)
					{
						logger.Log("deleting file " + item);
						File.Delete(item);
						continue;
					}

					d = SASProblem.CreateFromFile(item);

					if (d.GetAxiomRules().Count > 0)
					{
						//task has axioms. it will be removed
						logger.Log("deleting file " + item);
						File.Delete(item);
						if (previousFileNotSolved)
						{
							//if two successive tasks are not solved within the timelimit, we assume the following tasks wont be solved either and remove them all
							//removingAllFiles = true;
						}
						previousFileNotSolved = true;
					}
					else
					{
						isSomethingSolvedInThisDirectory = true;
						previousFileNotSolved = false;
					}
					logger.Log();
				}
				if (!isSomethingSolvedInThisDirectory)
				{
					//no task remains here - removing empty folder
					logger.Log("deleting directory " + directory);
					Directory.Delete(directory);
				}
				logger.Log(" ----- new domain ----- ");
			}

		}

		static List<T> shuffleList<T>(List<T> inputList, Random r)
		{
			List<int> allIndices = new List<int>(inputList.Count);
			foreach (var item in Enumerable.Range(0, inputList.Count))
				allIndices.Add(item);
			
			List<T> result = new List<T>(inputList.Count);
			while(allIndices.Count > 0)
			{
				int rand = r.Next(allIndices.Count);
				int selected = allIndices[rand];
				allIndices.RemoveAt(rand);
				result.Add(inputList[selected]);
			}

			return result;
		}

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
            if (maxValue < 0) maxValue = size / 4;
            List<IHeap<double, int>> testSubjects = new List<IHeap<double, int>>();
            List<string> names = new List<string>();
            testSubjects.Add(new Heaps.RegularBinaryHeap<int>());
            names.Add("Regular Heap");
            testSubjects.Add(new Heaps.LeftistHeap<int>());
            names.Add("Leftist Heap");
            testSubjects.Add(new Heaps.BinomialHeap<int>());
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
            logger.Log("Generating test input");
            Random r = new Random();
            List<int> result = new List<int>();
            result.Add(0);
            for (int i = 1; i < size; i++)
            {
                int item = result[i-1] + r.Next(5);
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
            logger.Log("Items added in " + (end-start).TotalSeconds + " seconds.");
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
    }

	/// <summary>
	/// Performs looging about progress of computations. Allows several computers to log simultaneously.
	/// </summary>
	public class Logger :IDisposable
	{
		string logFolder = Path.Combine(".", "Logs");
		bool quiet = false;

		string logBuffer;

		StreamWriter writter;

		public void Log(string MSG)
		{
			if (quiet)
				return;

			Console.WriteLine(MSG);
			string LogFileFullPath_Name = Path.Combine(logFolder, "log_" + Environment.MachineName + ".txt");
			if (!Directory.Exists(logFolder))
				Directory.CreateDirectory(logFolder);
			if (!File.Exists(LogFileFullPath_Name))
				File.Create(LogFileFullPath_Name);

			if (writter == null)
			{
				writter = new StreamWriter(LogFileFullPath_Name);
				writter.AutoFlush = true;
			}

			writter.WriteLine(MSG);
			/*
			logBuffer += (MSG + "\n");

			try
			{
				using (var writer = new StreamWriter(LogFileFullPath_Name, true))
				{
					writer.WriteLine(logBuffer);
				}
			}
			catch(Exception)
			{
				//file is currently in use. buffer will be written later.
				return;
			}
			//if the writing succeded, the buffer must empty.
			logBuffer = "";
			*/
		}

		/// <summary>
		/// Just to write empty line, to be compatible with console.writeln()
		/// </summary>
		public void Log()
		{
			Log("\n");
		}

		/// <summary>
		/// Just to write integer, to be compatible with console.writeln()
		/// </summary>
		public void Log(int x)
		{
			Log(x.ToString());
		}

		public void Dispose()
		{
			if (writter != null && writter.BaseStream.CanWrite)
			{
				writter.Flush();
				writter.Close();
				writter = null;
			}
		}

		public Logger()
		{

		}

		~Logger()
		{
			if (writter != null && writter.BaseStream.CanWrite)
			{
				writter.Flush();
				writter.Close();
				writter = null;
			}
		}

	}

	public enum SearchAlgorithmType
	{
		heurFF,
		heurFile_Median,
		heurFile_Mean,
		doubleListFF_FileMedian,
		doubleListFF_FileMean,
	}

	public enum StatesHistogramType
	{
		heurFF_Median,
		heurFF_Mean,
	}

}
