using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProblemDownload
{
	class Program
	{
		static void Main(string[] args)
		{
			Downloader.download(17, @"C:\Users\Trunda_Otakar\Desktop\zenotravel");

			//Downloader.download(67, @"C:\Users\Trunda_Otakar\Desktop\depots");
			//Translator.translateDomain(@"C:\Users\Trunda_Otakar\Desktop\depots\domain.pddl");
		}
	}
}
