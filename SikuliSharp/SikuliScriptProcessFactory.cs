using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SikuliSharp
{
	public interface ISikuliScriptProcessFactory
	{
		ISikuliVersion GetSikuliVersion();
		Process Start(ISikuliVersion version);
	}

	public class SikuliScriptProcessFactory : ISikuliScriptProcessFactory
	{
		public ISikuliVersion GetSikuliVersion()
		{
			var sikuliHome = MakeEmptyNull(Environment.GetEnvironmentVariable("SIKULI_HOME"));
			if (sikuliHome == null) throw new Exception("Environment variable SIKULI_HOME not set. Please verify that Sikuli is installed (sikuli-script.jar must be present) and create a SIKULI_HOME environment variable. You may need to restart your command prompt or IDE.");

			sikuliHome = Environment.ExpandEnvironmentVariables(sikuliHome);
			var jarFiles = Directory.GetFiles(sikuliHome, "*.jar");
			var jarFilenames = jarFiles.Select(path => Path.GetFileName(path)).ToArray();

			// Version 101
			if (jarFilenames.Contains("sikuli-script.jar"))
				return new Sikuli101Version(Path.Combine(sikuliHome, "sikuli-script.jar"));
			if (jarFilenames.Contains("sikulix.jar"))
				return new Sikuli110Version(Path.Combine(sikuliHome, "sikulix.jar"));
			if (jarFilenames.Contains("sikulixapi.jar") && jarFilenames.Contains("jython-standalone-2.7.1.jar"))
				return new Sikuli114Version(Path.Combine(sikuliHome, "sikulixapi.jar"), Path.Combine(sikuliHome, "jython-standalone-2.7.1.jar"));
			throw new NotSupportedException(string.Format("Could not find a known Sikuli version in SIKULI_HOME: \"{0}\"", sikuliHome));
		}

		public Process Start(ISikuliVersion version)
		{
			var javaPath = GuessJavaPath();

#if (DEBUG)
			Debug.WriteLine("Launching Sikuli: \"" + javaPath + "\" " + version.Arguments);
#endif

			var process = new Process
			{
				StartInfo =
				{
					FileName = javaPath,
					Arguments = version.Arguments,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true
				}
			};

			process.Start();

			return process;
		}

		public static string GuessJavaPath()
		{
			var javaHome = MakeEmptyNull(Environment.GetEnvironmentVariable("JAVA_HOME"));

			if (String.IsNullOrEmpty(javaHome))
				throw new Exception("Java path not found. Is it installed? If yes, set the JAVA_HOME environment variable.");

			var javaPath = Path.Combine(Environment.GetEnvironmentVariable("JAVA_HOME"), "bin", $"java{(Environment.OSVersion.Platform is PlatformID.Win32NT ? ".exe" : "")}");

			if (!File.Exists(javaPath))
				throw new Exception(string.Format("Java executable not found in expected folder: {0}. If you have multiple Java installations, you may want to set the JAVA_HOME environment variable.", javaPath));

			return javaPath;
		}

		private static string MakeEmptyNull(string value)
		{
			return value == "" ? null : value;
		}
	}
}
