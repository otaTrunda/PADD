using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ProblemDownload
{
	class Program
	{
		static void Main(string[] args)
		{
			removeTextFromFiles(" - block", @"C:\Users\Trunda_Otakar\Desktop\blocks\");

			//Downloader.download(112, @"C:\Users\Trunda_Otakar\Desktop\blocks");
			Translator.translateDomain(@"C:\Users\Trunda_Otakar\Desktop\blocks\domain.pddl");

			//Downloader.download(17, @"C:\Users\Trunda_Otakar\Desktop\zenotravel");

			//Downloader.download(67, @"C:\Users\Trunda_Otakar\Desktop\depots");
			//Translator.translateDomain(@"C:\Users\Trunda_Otakar\Desktop\depots\domain.pddl");
		}

		public static void removeTextFromFiles(string text, string folder)
		{
			foreach (var item in Directory.EnumerateFiles(folder))
			{
				if (item.Contains("domain"))
					continue;
				string content = File.ReadAllText(item);
				content = content.Replace(text, "");
				File.WriteAllText(item, content);
			}
		}
	}
}
