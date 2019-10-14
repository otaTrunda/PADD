using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD_Support
{
	/// <summary>
	/// Performs looging about progress of computations. Allows several computers to log simultaneously.
	/// </summary>
	public class Logger : IDisposable
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
			return;
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
		/// Just to write integer, to be compatible with console.writeln(int x)
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

}
