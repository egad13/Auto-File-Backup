using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System;
using System.Diagnostics;
using AutoFileBackupLib;
using Avalonia.Interactivity;

namespace AutoFileBackupGUI {
	public partial class MainWindow : Window {

		public static MainWindow Instance;

		// Form Controls
		private Button? ChooseFile;
		private TextBox? DisplayFile;
		private TextBox? MaxBackups;
		private TextBox? MinutesBetween;
		private ToggleButton? OnOffSwitch;
		public TextBox? Output;

		// Filewatcher
		private AutoFileBackupMaker BackupMaker;

		public MainWindow() {
			InitializeComponent();
			#if DEBUG
            this.AttachDevTools();
			#endif

			Instance = this;

			//OnClickCommand = ReactiveCommand.Create(() => { /* do something */ });
		}
		//public ReactiveCommand OnClickCommand { get; }

		private void InitializeComponent() {
			// Initialize and find controls
			AvaloniaXamlLoader.Load(this);
			Output = this.FindControl<TextBox>("Output");
			MaxBackups = this.FindControl<TextBox>("MaxBackups");
			MinutesBetween = this.FindControl<TextBox>("MinutesBetween");
			OnOffSwitch = this.FindControl<ToggleButton>("OnOffSwitch");
			ChooseFile = this.FindControl<Button>("ChooseFile");
			DisplayFile = this.FindControl<TextBox>("DisplayFile");

			// Set watermark text for input textboxes
			MaxBackups.Watermark = AutoFileBackupMaker.DEFAULT_MAX_BACKUPS.ToString();
			MinutesBetween.Watermark = AutoFileBackupMaker.DEFAULT_MINUTES.ToString();

			// Set console to output to the output textbox
			MainWindowWriter outputWriter = new();
			Console.SetOut(outputWriter);
			Console.SetError(outputWriter);

			// Set toggle button to start and stop the file watcher as needed.
			OnOffSwitch.Checked += OnOffSwitch_Checked;
			OnOffSwitch.Unchecked += OnOffSwitch_Unchecked;
			BackupMaker = new();

			// Set choose file button to.... let you choose a file.
			ChooseFile.Click += ChooseFile_Click;
		}

		/// <summary>
		/// Appends the incoming text to the window's Output box and scrolls the box to the bottom.<br/>
		/// <br/>
		/// Basically just a workaround to let the console output be redirected to the Output textbox in the
		/// window, regardless of what thread the call to Console.Write is coming from.
		/// </summary>
		/// <param name="text">The message to add to the Output box.</param>
		public void AddMessage(string text) {
			Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
				Output.Text += text;
				Output.CaretIndex = Output.Text.Length;
			});
		}

		/**/
		private async void ChooseFile_Click(object? sender, RoutedEventArgs e) {
			try {
				OpenFileDialog dialog = new();
				var result = await dialog.ShowAsync((Window)this);
				DisplayFile.Text = result[0];
			}
			catch (Exception ex) {
				Console.Error.WriteLine(
					Console.Error.NewLine
					+ $"ERROR: {ex.Message}{Console.Error.NewLine}"
					#if DEBUG
					+ $"Stack trace:{Console.Error.NewLine}{ex.StackTrace}{Console.Error.NewLine}"
					#endif
				);
			}
		}/**/

		/// <summary>
		/// Event handler for the toggle-on event of the main toggle button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnOffSwitch_Checked(object? sender, RoutedEventArgs e) {
			int maxBack = AutoFileBackupMaker.DEFAULT_MAX_BACKUPS, minBwn = AutoFileBackupMaker.DEFAULT_MINUTES;

			bool error = false;
			if (MaxBackups != null && MaxBackups.Text.Length > 0) {
				if (int.TryParse(MaxBackups.Text, out maxBack) == false) {
					Console.Error.WriteLine("ERROR: Max Backups must be an integer.");
					error = true;
				}
			}
			if (MinutesBetween != null && MinutesBetween.Text.Length > 0) {
				if (int.TryParse(MinutesBetween.Text, out minBwn) == false) {
					Console.Error.WriteLine("ERROR: Minutes Between Backups must be an integer.");
					error = true;
				}
			}
			if (error) {
				return;
			}

			try {
				BackupMaker.StartWatchingFile(DisplayFile.Text, maxBack, minBwn);
			}
			catch (Exception ex) {
				Console.Error.WriteLine(
					Console.Error.NewLine
					+ $"ERROR: {ex.Message}{Console.Error.NewLine}"
					#if DEBUG
					+ $"Stack trace:{Console.Error.NewLine}{ex.StackTrace}{Console.Error.NewLine}"
					#endif
				);
				if (OnOffSwitch != null) {
					OnOffSwitch.IsChecked = false;
				}
			}
		}

		/// <summary>
		/// Event handler for the toggle-off event of the main toggle button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnOffSwitch_Unchecked(object? sender, RoutedEventArgs e) {
			try {
				if (BackupMaker.IsWatchingFile) {
					BackupMaker.StopWatchingFile();
					Console.WriteLine(Console.Out.NewLine + "Stopped watching file.");
				}
			}
			catch (Exception ex) {
				Console.Error.WriteLine(
					Console.Error.NewLine
					+ $"ERROR: {ex.Message}{Console.Error.NewLine}"
					#if DEBUG
					+ $"Stack trace:{Console.Error.NewLine}{ex.StackTrace}{Console.Error.NewLine}"
					#endif
				);
			}
		}

	}
}
