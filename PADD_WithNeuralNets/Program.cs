using PAD.Planner.Heuristics;
using PADD;
using PADD_Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD_WithNeuralNets
{
	class Program
	{
		public static string SAS_allFolder = PADD_Support.SupportMethods.SAS_allFolder;
		public static string SAS_all_WithoutAxioms = PADD_Support.SupportMethods.SAS_all_WithoutAxioms;
		public static string mediumDomainsFolder = PADD_Support.SupportMethods.mediumDomainsFolder;
		public static string mediumDomainsFolderFirstHalf = PADD_Support.SupportMethods.mediumDomainsFolderFirstHalf;
		public static string mediumDomainsFolderSecondHalf = PADD_Support.SupportMethods.mediumDomainsFolderSecondHalf;
		public static string small_and_mediumDomainsFolder = PADD_Support.SupportMethods.small_and_mediumDomainsFolder;
		public static string freeLunchSmall = PADD_Support.SupportMethods.freeLunchSmall;

		public static string testFilesFolder = PADD_Support.SupportMethods.testFilesFolder;
		public static string test2FilesFolder = PADD_Support.SupportMethods.test2FilesFolder;

		static void OldMain(string[] args)
		{
			//printTestErrors("zenotravel", true); return;
			//storeTrainingSamples("zenotravel", int.Parse(args[0]), int.Parse(args[1]), true); return;
			//storeTrainingSamples("blocks", int.Parse(args[0]), int.Parse(args[1]), true); return;

			//filterUniqueTrainigSamples("zenotravel", int.Parse(args[0]), int.Parse(args[1]), true);

			//Subgraphs_FeatureGenerator.test(); return;

			//visualizeKnowledgeGraphs(Path.Combine(SAS_all_WithoutAxioms, "zenotravel", "pddl", "pfile1.pddl")); return;

			//runNetworks("blocks", 0, 1, false, 30, "FastForward", HeuristicType.FF); return;

			SupportMethods.testNeuralNetHeuristic(Path.Combine(SAS_all_WithoutAxioms, "zenotravel", "pfile8.sas"), @"B:\SAS_Data\zenotravel\uniqueSamplesFULL\c_1000000\4_ff,FULL_good6\trainedNet.bin", @"B:\SAS_Data\zenotravel\featuresGen\generator_4_FULL.bin", 10, "zenotravel", false); return;

			//runNetworks("zenotravel", int.Parse(args[0]), int.Parse(args[1]), storeAdditionalSamples: false, architecture: args[2], timeMinutes: args.Length > 3 ? int.Parse(args[3]) : 30); return;

			//runNetworks("blocks", int.Parse(args[0]), int.Parse(args[1]), storeAdditionalSamples: false, architecture: args[2], timeMinutes: args.Length > 3 ? int.Parse(args[3]) : 30); return;
			//printTestErrors("zenotravel"); return;

			/*
			//zeno
			WeightedSumHeuristic.minweight = 0.5;
			WeightedSumHeuristic.noiseMax = 0.1;
			WeightedSumHeuristic.weightMax = 20;
			
			runNetworks("zenotravel", int.Parse(args[0]), int.Parse(args[1]), storeAdditionalSamples: false, architecture: args[2], timeMinutes: args.Length > 3 ? int.Parse(args[3]) : 30, type: HeuristicType.domainSolver_NN);

			WeightedSumHeuristic.noiseMax = 0.02;
			WeightedSumHeuristic.weightMax = 30;
			runNetworks("zenotravel", int.Parse(args[0]), int.Parse(args[1]), storeAdditionalSamples: false, architecture: args[2], timeMinutes: args.Length > 3 ? int.Parse(args[3]) : 30, type: HeuristicType.domainSolver_NN);

			
			WeightedSumHeuristic.noiseMax = 0.002;
			WeightedSumHeuristic.weightMax = 50;
			runNetworks("zenotravel", int.Parse(args[0]), int.Parse(args[1]), storeAdditionalSamples: false, architecture: args[2], timeMinutes: args.Length > 3 ? int.Parse(args[3]) : 30, type: HeuristicType.domainSolver_NN);
			
			WeightedSumHeuristic.noiseMax = 0.0002;
			WeightedSumHeuristic.weightMax = 80;
			runNetworks("zenotravel", int.Parse(args[0]), int.Parse(args[1]), storeAdditionalSamples: false, architecture: args[2], timeMinutes: args.Length > 3 ? int.Parse(args[3]) : 30, type: HeuristicType.domainSolver_NN);
			return;
			*/

			/*
			//blocks
			WeightedSumHeuristic.minweight = 0;
			WeightedSumHeuristic.noiseMax = 0.5;
			WeightedSumHeuristic.weightMax = 5;
			runNetworks("blocks", int.Parse(args[0]), int.Parse(args[1]), storeAdditionalSamples: false, architecture: args[2], timeMinutes: args.Length > 3 ? int.Parse(args[3]) : 30, type: HeuristicType.domainSolver_NN);
			
			WeightedSumHeuristic.minweight = 0.3;
			WeightedSumHeuristic.noiseMax = 0.1;
			WeightedSumHeuristic.weightMax = 10;
			runNetworks("blocks", int.Parse(args[0]), int.Parse(args[1]), storeAdditionalSamples: false, architecture: args[2], timeMinutes: args.Length > 3 ? int.Parse(args[3]) : 30, type: HeuristicType.domainSolver_NN);

			WeightedSumHeuristic.noiseMax = 0.01;
			WeightedSumHeuristic.weightMax = 30;
			runNetworks("blocks", int.Parse(args[0]), int.Parse(args[1]), storeAdditionalSamples: false, architecture: args[2], timeMinutes: args.Length > 3 ? int.Parse(args[3]) : 30, type: HeuristicType.domainSolver_NN);

			WeightedSumHeuristic.noiseMax = 0.001;
			WeightedSumHeuristic.weightMax = 60;
			runNetworks("blocks", int.Parse(args[0]), int.Parse(args[1]), storeAdditionalSamples: false, architecture: args[2], timeMinutes: args.Length > 3 ? int.Parse(args[3]) : 30, type: HeuristicType.domainSolver_NN); return;
			*/


			//filterUniqueTrainigSamples("zenotravel"); return;
			//filterUniqueTrainigSamples("blocks", int.Parse(args[0]), int.Parse(args[1])); return;

			//createSamplesByHistogram("zenotravel", 1000000); return;
			//createSamplesByHistogram("blocks", 2000000); return;

			//storeStatesHistogram("zenotravel"); return;
			//storeStatesHistogram("blocks"); return;

			//storeStatesForTraining("zenotravel", DomainType.Zeno);
			//storeStatesForTraining("blocks", DomainType.Blocks);
			//storeStatesForTraining("visitall", DomainType.VisitAll); return;

			//storeBackwardsStatesForTraining("zenotravel"); return;
			//storeBackwardsStatesForTraining("blocks"); return;

			//storeUniqueStatesForTraining("zenotravel"); return;
			//storeUniqueStatesForTraining("blocks"); return;

			//trainAndSaveFeaturesGenerator("zenotravel", new List<int>() { 2, 3, 4 }); return;
			//trainAndSaveFeaturesGenerator("blocks", new List<int>() { 2, 3, 4 }); return;

			//storeTrainingSamples("blocks", int.Parse(args[0]), int.Parse(args[1])); return;
			//storeTrainingSamples("zenotravel", int.Parse(args[0]), int.Parse(args[1])); return;

			//var res = new Helper().ReGenerateSamples(Utils.Serialization.Deserialize<List<NeuralNetTrainer.TrainingSample>>(@"B:\iterativeTraining Blocks\4FCQ\currentSamples.bin")).ToList();
			//Utils.Serialization.Serialize(res, @"B:\iterativeTraining Blocks\4FCQ\re -generatedSamples.bin");

			//var samples = Utils.Serialization.Deserialize<List<NeuralNetTrainer.TrainingSample>>(@"B:\iterativeTraining Blocks\4FCQ\currentSamples.bin");
			//foreach (var item in samples)
			//{
			//	var ffHeurVal = (float)computeFFHeuristic(item.userData);
			//	item.inputs = item.inputs.Concat(new float[] { ffHeurVal }).ToArray();
			//}
			//Utils.Serialization.Serialize(samples, @"B:\iterativeTraining Blocks\4FCQ\re -generatedSamples.bin");

			if (args.Length > 0 && args[0] == "iterative")
			{
				SupportMethods.iterativeLearning(int.Parse(args[1]), int.Parse(args[2]));
				return;
			}

			if (args.Length > 0 && args[0] == "iterativeB")
			{
				SupportMethods.iterativeLearning(int.Parse(args[1]), int.Parse(args[2]), isBlocks: true);
				return;
			}
			//var res = new Helper().ReGenerateSamples(Utils.Serialization.Deserialize<List<NeuralNetTrainer.TrainingSample>>(@"B:\iterativeTraining\4FCQ\initialSamples.bin")).ToList();
			//var res = new Helper().ReGenerateSamples(Utils.Serialization.Deserialize<List<NeuralNetTrainer.TrainingSample>>(@"B:\iterativeTraining\4FCQ\currentSamples - Copy.bin")).ToList();
			//Utils.Serialization.Serialize(res, "re-generatedSamples.bin");
			//Console.WriteLine("all DONE at " + DateTime.Now.ToString()); return;
			PADD_Support.SupportMethods.visualizeKnowledgeGraphs(Path.Combine(SAS_all_WithoutAxioms, "zenotravel", "pddl", "pfile2.pddl"));

			//testZenoSolver();
			//solveDomain(Path.Combine(SAS_all_WithoutAxioms, "zenotravel"), new DomainDependentSolvers.Zenotravel.ZenotravelSolver());
			//solveDomain(Path.Combine(SAS_all_WithoutAxioms, "visitall"), new DomainDependentSolvers.VisitAll.VisitAllGreedySolver());
			//solveDomain(Path.Combine(SAS_all_WithoutAxioms, "blocks"), new DomainDependentSolvers.BlocksWorld.BlocksWorldSolver(), submitPlans: true);
			//return;


			//testFFNetHeuristic(8, HeuristicType.net); return;
			//foreach (var item in Enum.GetValues(typeof(HeuristicType)))
			{
				for (int i = 1; i < 20; i++)
				{
					SupportMethods.testFFNetHeuristic(i, /*(HeuristicType)item);*/ HeuristicType.net, MaxTime_Minutes: 10);
				}
			}
			return;

			if (args.Length == 1 && args[0] == "combineResults")
			{
				PADD_Support.SupportMethods.CombineResultFiles();
			}
			if (args.Length == 1 && args[0] == "combineHistograms")
			{
				SupportMethods.CombineHistogramFiles();
			}
			if (args.Length == 1 && args[0] == "count")
			{
				PADD_Support.SupportMethods.CountHistogramsAndResultFiles();
			}
			if (args.Length == 1 && args[0] == "visualizeGraphs")
			{
				//PADD_Support.SupportMethods.visualizeKnowledgeGraphs(Path.Combine(SAS_all_WithoutAxioms, "gripper", "prob10.sas"));
				//PADD_Support.SupportMethods.visualizeKnowledgeGraphs(Path.Combine(SAS_all_WithoutAxioms, "visitall", "problem12.sas"));
				//PADD_Support.SupportMethods.visualizeKnowledgeGraphs(Path.Combine(SAS_all_WithoutAxioms, "blocks", "probBLOCKS-4-1.sas"));
				//PADD_Support.SupportMethods.visualizeKnowledgeGraphs(Path.Combine(SAS_all_WithoutAxioms, "zenotravel", "pfile3.sas"));
				PADD_Support.SupportMethods.visualizeKnowledgeGraphs(Path.Combine(SAS_all_WithoutAxioms, "zenotravel", "pddl", "pfile1.pddl"));
				//PADD_Support.SupportMethods.visualizeKnowledgeGraphs(Path.Combine(SAS_all_WithoutAxioms, "blocks", "pddl", "probBLOCKS-7-1.pddl"));
				//PADD_Support.SupportMethods.visualizeKnowledgeGraphs(Path.Combine(SAS_all_WithoutAxioms, "gripper", "pddl", "prob10.pddl"));
			}

			if (args.Length == 1 && args[0] == "createDB")
			{
				//PADD_Support.SupportMethods.createStatesDB(Path.Combine(SAS_all_WithoutAxioms, "gripper", "prob10.sas"), new GripperSolver());
				//PADD_Support.SupportMethods.createStatesDB(Path.Combine(SAS_all_WithoutAxioms, "visitall", "problem16.sas"), new VisitAllSolver());
				//PADD_Support.SupportMethods.createStatesDB(Path.Combine(SAS_all_WithoutAxioms, "blocks", "probBLOCKS-7-1.sas"), new DomainDependentSolvers.BlocksWorld.BlocksWorldSolver());
				//PADD_Support.SupportMethods.createStatesDB(Path.Combine(SAS_all_WithoutAxioms, "zenotravel", "pfile3.sas"), new DomainDependentSolvers.Zenotravel.ZenotravelSolver());

				//PADD_Support.SupportMethods.createStatesDBForDomain(Path.Combine(SAS_all_WithoutAxioms, "zenotravel"), Path.Combine(SAS_all_WithoutAxioms, "zenotravel", "trainingSamples"), new DomainDependentSolvers.Zenotravel.ZenotravelSolver(), 10000);
				PADD_Support.SupportMethods.createStatesDBForDomain(Path.Combine(SAS_all_WithoutAxioms, "zenotravel"), "B:\\trainingSamplesLarge", new PADD.DomainDependentSolvers.Zenotravel.ZenotravelSolver(), 1000000);
			}

			if (args.Length == 3 && args[0] == "createHistograms_Results")
			{
				SupportMethods.CreateHistograms_Results(args.Skip(1).ToArray());
			}
		}

		static void Main(string[] args)
		{
			//SupportMethods.printTestErrors("zenotravel", true); return;
			//RunFakeNetworks(args);
			SupportMethods.storeStatesForTraining("blocks", PADD.DomainDependentSolvers.DomainType.Blocks);
		}
		
	}
}
