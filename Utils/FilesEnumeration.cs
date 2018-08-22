using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PADDUtils
{
	public static class FileSystemUtils
	{
		/// <summary>
		/// Enumerates all problem files in given domains folder. If <paramref name="SASFiles"/> is set to true, it returns problems in SAS format, otherwise it returns problems in PDDL format (if they exist)
		/// </summary>
		/// <param name="domainFolder"></param>
		/// <param name="SASFiles"></param>
		/// <returns></returns>
		public static IEnumerable<string> enumerateProblemFiles(string domainFolder, bool SASFiles = true)
		{
			foreach (var item in Directory.EnumerateFiles(domainFolder))
			{
				if (Path.GetExtension(item) != ".sas")
					continue;
				yield return item;
			}
		}

		public static (string domainFile, string problemFile) getPDDLProblemPath(string sasProblemPath)
		{
			string fileName = Path.GetFileNameWithoutExtension(sasProblemPath);
			string pddlPath = Path.Combine(Path.GetDirectoryName(sasProblemPath), "pddl");
			string problemFilePath = Path.Combine(pddlPath, fileName + ".pddl");
			string domainFilePath = Path.Combine(pddlPath, "domain.pddl");
			return (domainFilePath, problemFilePath);
		}

		public static void createDirIfNonExisting(string directoryPath)
		{
			if (!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
		}
	}
}
