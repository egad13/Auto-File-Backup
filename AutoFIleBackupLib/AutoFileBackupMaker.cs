using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoFileBackupLib {

	/// <summary>
	/// A class that can watch a file for changes and make backups of that file in the same directory, with
	/// the last write time appended to the name.
	/// </summary>
	public class AutoFileBackupMaker {

		// ////////////////////////////////////////////////////////////////////
		// 
		// CONSTS & PROPERTIES
		// 
		// ////////////////////////////////////////////////////////////////////
		#region public props

		/// <summary> The default maximum number of backups. </summary>
		public const int DEFAULT_MAX_BACKUPS = 10;
		/// <summary> The default minimum number of minutes between backups. </summary>
		public const int DEFAULT_MINUTES = 10;

		/// <summary> Whether or not a file is currently being watched. </summary>
		public bool IsWatchingFile { get { return FileWatcher != null; } }

		/// <summary> The file currently being watched. </summary>
		public FileInfo CurrentFile { get; private set; }

		/// <summary>
		/// The maximum number of backups to make before starting to delete old ones. Default is 10. Will
		/// be 0 if no file is currently being watched.
		/// </summary>
		public int MaxBackups { get; private set; }

		/// <summary>
		/// The minimum number of minutes that has to pass before a new backup is created. Default is 10.
		/// Will be 0 if no file is currently being watched.
		/// </summary>
		public int MinutesBetweenBackups { get; private set; }

		#endregion
		#region private props

		/// <summary> The backups on hand for the file currently being watched. </summary>
		private Queue<FileInfo> Backups;
		/// <summary> The thing which watches the files... </summary>
		private FileSystemWatcher FileWatcher;
		/// <summary> Just helps us not do too much extra work after a file is restored from deletion on our watch. </summary>
		private bool JustRestoredFile;

		#endregion

		// ////////////////////////////////////////////////////////////////////
		// 
		// PUBLIC FUNCTIONS
		// 
		// ////////////////////////////////////////////////////////////////////
		#region public functions

		/// <summary>
		/// Start watching the given file and making backups when it changes.
		/// </summary>
		/// <param name="filepath">The path to the file to watch for changes.</param>
		/// <param name="maxBackups">
		/// The maximum number of backups to make before starting to delete old ones. Default is 10.
		/// </param>
		/// <param name="minutesBetweenBackups">
		/// The minimum number of minutes that must pass after the most recent backup before a new one is made.
		/// </param>
		public void StartWatchingFile(string filepath, int maxBackups = DEFAULT_MAX_BACKUPS, int minutesBetweenBackups = DEFAULT_MINUTES) {
			if (IsWatchingFile) {
				throw new Exception("Cannot start watching a new file; we're already watching one.");
			}

			// Validate the filepath
			if (!File.Exists(filepath)) {
				throw new Exception("Invalid file path. The file must exist to be watched.");
			}
			CurrentFile = new FileInfo(filepath);

			// Validate the max number of backups
			if (maxBackups <= 0) {
				throw new Exception("Max Backups must be a positive integer.");
			}
			MaxBackups = maxBackups;

			// Validate the number of minutes between backups
			if (minutesBetweenBackups < 0) {
				throw new Exception("Minutes Between Backups must be a positive integer.");
			}
			MinutesBetweenBackups = minutesBetweenBackups;

			Backups = new Queue<FileInfo>(MaxBackups);

			Console.WriteLine(
				 Console.Out.NewLine
				 + $"Watching File: {CurrentFile.Name}{Console.Out.NewLine}"
				 + $"Max Backups: {MaxBackups}{Console.Out.NewLine}"
				 + $"Minutes Between Backups: {MinutesBetweenBackups}{Console.Out.NewLine}"
			 );

			// Create a backup to start us off
			string newPath = MakeTimestampedFilepath(CurrentFile, CurrentFile.Name);
			File.Copy(CurrentFile.FullName, newPath, true);
			FileInfo newBackFi = new(newPath);
			Backups.Enqueue(newBackFi);
			Console.WriteLine($"New backup created: {newBackFi.Name}");

			// Begin watching file
			FileWatcher = new FileSystemWatcher() {
				Path = CurrentFile.DirectoryName,
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
			};
			FileWatcher.Changed += OnFileChanged;
			FileWatcher.Renamed += OnFileRenamed;
			FileWatcher.Deleted += OnFileDeleted;
			FileWatcher.Error += OnError;
			FileWatcher.EnableRaisingEvents = true;
		}

		/// <summary>
		/// Stops watching the current file and sets all properties to their defaults.
		/// </summary>
		public void StopWatchingFile() {
			if (FileWatcher != null) {
				FileWatcher.EnableRaisingEvents = false;
				FileWatcher.Dispose();
			}
			FileWatcher = null;

			CurrentFile = null;
			Backups = null;
			MaxBackups = 0;
			MinutesBetweenBackups = 0;
			JustRestoredFile = false;
		}

		#endregion

		// ////////////////////////////////////////////////////////////////////
		// 
		// PRIVATE FUNCTIONS
		// 
		// ////////////////////////////////////////////////////////////////////
		#region private functions

		/// <summary>
		/// Creates a path for a file with the given name, in the directory given by the FileInfo, with the
		/// LastWriteTime of the given FileInfo appended to the name.
		/// </summary>
		/// <param name="fi">The source for the directory and write time to use.</param>
		/// <param name="name">The</param>
		/// <returns>A timestamped filepath.</returns>
		private static string MakeTimestampedFilepath(FileInfo fi, string name) {
			return Path.Combine(
				fi.DirectoryName,
				name.Substring(0, name.Length - fi.Extension.Length)
					+ fi.LastWriteTime.ToString("__yyyy-MM-dd_HH-mm-ss")
					+ fi.Extension
			);
		}

		#endregion

		// ////////////////////////////////////////////////////////////////////
		// 
		// EVENT LISTENERS
		// 
		// ////////////////////////////////////////////////////////////////////
		#region event listeners

		/// <summary>
		/// Creates a copy of the watched file with the last write time appended to the name, and tracks it
		/// in the backup queue. If we exceed the maximum number of backups, delete the oldest backup.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private void OnFileChanged(object source, FileSystemEventArgs e) {
			if (JustRestoredFile || e.ChangeType != WatcherChangeTypes.Changed || e.FullPath != CurrentFile.FullName) {
				JustRestoredFile = false;
				return;
			}

			FileInfo newFi = new(e.FullPath);
			FileInfo recentFi = Backups.LastOrDefault(); // peek at the element we added last
			TimeSpan timeSinceLastBackup = newFi.LastWriteTime - recentFi.LastWriteTime;
			Console.WriteLine($"File changed. Minutes since last backup: {timeSinceLastBackup.TotalMinutes:0.##}");

			// If the queue is empty, or if more than (MinutesBetweenBackups) minutes have passed since last backup...
			if (recentFi == null || timeSinceLastBackup.TotalMinutes >= MinutesBetweenBackups) {

				// If we've reached max capacity on backups, delete the oldest backup.
				if (Backups.Count == MaxBackups) {
					FileInfo oldestFi = Backups.Dequeue();
					File.Delete(oldestFi.FullName);
				}

				// Create a new backup, add its info to the queue.
				string newPath = MakeTimestampedFilepath(newFi, newFi.Name);
				File.Copy(e.FullPath, newPath, true);
				FileInfo newBackFi = new(newPath);
				Backups.Enqueue(newBackFi);
				Console.Write($" New backup created: {newBackFi.Name}");
			}
			Console.WriteLine();
		}

		/// <summary> If the watched file's name is changed, update the tracked current file to reflect that. </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private void OnFileRenamed(object source, RenamedEventArgs e) {
			if (JustRestoredFile || e.ChangeType != WatcherChangeTypes.Renamed || e.OldFullPath != CurrentFile.FullName) {
				JustRestoredFile = false;
				return;
			}
			Console.WriteLine("File renamed.");
			CurrentFile = new FileInfo(e.FullPath);

			// Run through the queue and rename all tracked backups accordingly.
			for (int i = 0; i < Backups.Count; i++) {
				FileInfo fi = Backups.Dequeue();
				string newPath = MakeTimestampedFilepath(fi, e.Name);
				File.Move(fi.FullName, newPath);
				Backups.Enqueue(new FileInfo(newPath));
			}
		}

		/// <summary>
		/// If the watched file is deleted, start watching the most recent backup; if we have no backups, exit.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private void OnFileDeleted(object source, FileSystemEventArgs e) {
			if (e.ChangeType != WatcherChangeTypes.Deleted || e.FullPath != CurrentFile.FullName) {
				return;
			}

			FileInfo recentFi = Backups.LastOrDefault(); // peek at the element we added last

			JustRestoredFile = true;
			CurrentFile = new FileInfo(e.FullPath);
			File.Copy(recentFi.FullName, e.FullPath);
			Console.WriteLine($"File {e.Name} was deleted. Restored from most recent backup.");
		}

		/// <summary>
		/// Outputs the error that happened, but does nothing else.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private void OnError(object source, ErrorEventArgs e) {
			Exception ex = e.GetException();
			if (ex != null) {
				Console.Error.WriteLine(
					Console.Error.NewLine
					+ $"ERROR: {ex.Message}{Console.Error.NewLine}"
					+ $"Stack trace:{Console.Error.NewLine}{ex.StackTrace}"
					+ Console.Error.NewLine
				);
			}
		}

		#endregion
	}
}
