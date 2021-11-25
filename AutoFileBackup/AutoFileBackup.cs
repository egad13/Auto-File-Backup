using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFileBackupLib;

namespace AutoFileBackup {
	class AutoFileBackup {

		/// <summary> List of args that should be interpreted as a "Show me the help text" command. </summary>
		private static readonly string[] HelpArgs = new string[] { "h", "help", "-h", "--help", };

		/// <summary> List of args that can precede a maximum number of backups. </summary>
		private static readonly string[] MaxBackupsArgs = new string[] { "-m", "--max" };

		/// <summary> List of args that can precede a minimum number of minutes between backups. </summary>
		private static readonly string[] MinutesBetweenArgs = new string[] { "-t", "--time" };

		static int Main(string[] args) {

			// Just show usage and exit, if that was requested 
			if (args.Length == 0 || args.Intersect(HelpArgs).Any()) {
				Console.Write(HelpText());
				return 0;
			}

			int exitCode = 0;
			int maxBack = AutoFileBackupMaker.DEFAULT_MAX_BACKUPS,
				minBwn = AutoFileBackupMaker.DEFAULT_MINUTES;

			// Parse Args
			#region parsing args
			if (!File.Exists(args[0])) {
				Console.Error.WriteLine("Invalid file path. The first argument must be a valid path to an existing file.");
				exitCode = 1;
			}

			for (int i = 1; i < args.Length; i++) {
				if (MaxBackupsArgs.Contains(args[i])) {
					if (i + 1 >= args.Length) {
						Console.Error.WriteLine("ERROR: Could not parse max_backups; too few arguments.");
						exitCode = 1;
					}
					else if (int.TryParse(args[i + 1], out maxBack) == false) {
						Console.Error.WriteLine("Could not parse max_backups; value is not an integer.");
						exitCode = 1;
					}
				}
				if (MinutesBetweenArgs.Contains(args[i])) {
					if (i + 1 >= args.Length) {
						Console.WriteLine("Could not parse minutes_between_backups; too few arguments.");
						exitCode = 1;
					}
					else if (int.TryParse(args[i + 1], out minBwn) == false) {
						Console.Error.WriteLine("Could not parse minutes_between_backups; value is not an integer.");
						exitCode = 1;
					}
				}
			}
			#endregion

			// Exit with usage text if any errors occured while parsing args
			if (exitCode != 0) {
				Console.Write(HelpText());
				return exitCode;
			}

			// Otherwise, try to watch the file.
			try {
				AutoFileBackupMaker auto = new();
				auto.StartWatchingFile(args[0], maxBack, minBwn);
				Console.WriteLine(Console.Out.NewLine + "Press enter to stop watching file." + Console.Out.NewLine);
				Console.ReadLine();
				auto.StopWatchingFile();
			}
			catch (Exception e) {
				Console.Error.WriteLine(
					Console.Error.NewLine
					+ $"ERROR: {e.Message}{Console.Error.NewLine}"
#if DEBUG
					+ $"Stack trace:{Console.Error.NewLine}{e.StackTrace}{Console.Error.NewLine}"
#endif
				);
				exitCode = 1;
			}

			Console.WriteLine("Exiting" + Console.Out.NewLine);
			return exitCode;
		}

		/// <returns>The usage information for this console application.</returns>
		private static string HelpText() {
			string n = Console.Out.NewLine;
			return
				$"{n}Usage:{n}{n}"
				+ $"AutoFileBackup.exe {{h | help | -h | --help}}{n}"
				+ $"\tPrints this help text.{n}"
				+ $"AutoFileBackup.exe file_path [-m max_backups] [-t minutes_between_backups]{n}"
				+ $"\tfile_path: The file to watch for changes.{n}"
				+ $"\t-m, --max: Optional. Default is 10. The maximum number of backups to create - older{n}"
				+ $"\t\tbackups made during this run of the program will be deleted.{n}"
				+ $"\t-t, --time: Optional. Default is 10. The minimum amount of time in minutes that has{n}"
				+ $"\t\tto have passed between the most recent backup and the current file in order{n}"
				+ $"\t\tfor a new backup to be created.{n}{n}";
		}
	}
}
