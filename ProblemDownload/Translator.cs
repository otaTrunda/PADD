using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace ProblemDownload
{
	/// <summary>
	/// Translates given .pddl file to .sas file using Fast-Downwards's translator (in python)
	/// </summary>
	class Translator
	{
		static string pathToTranslator = @"..\..\..\PADD\bin\tests\Translator\translate.py";

		public static void translate(string PDDLdomainFile, string PDDLproblemFile, string outputSASProblemFile)
		{
			Process p = new Process();
			p.StartInfo.Arguments = "\"" + Path.GetFullPath(pathToTranslator) + "\" \"" + Path.GetFullPath(PDDLdomainFile) + "\" \"" + Path.GetFullPath(PDDLproblemFile) + "\"";
			p.StartInfo.FileName = "python";
			p.Start();
			p.WaitForExit();

			if (File.Exists("output.sas"))
			{
				File.Move("output.sas", Path.GetFullPath(outputSASProblemFile));
			}
		}

		public static void translateDomain(string PDDLdomainFile)
		{
			string outputFolderName = "SAS";
			string outputFolder = Path.Combine(Path.GetDirectoryName(PDDLdomainFile), outputFolderName);
			if (!Directory.Exists(outputFolder))
				Directory.CreateDirectory(outputFolder);
			foreach (var item in Directory.EnumerateFiles(Path.GetDirectoryName(PDDLdomainFile)))
			{
				if (Path.GetFileName(item) == Path.GetFileName(PDDLdomainFile))
					continue;
				if (Path.GetExtension(item) == ".pddl")
				{
					translate(PDDLdomainFile, item, Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(item) + ".sas"));
				}
			}
		}
	}
}
