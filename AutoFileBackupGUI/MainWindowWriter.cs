using System.IO;
using System.Text;

namespace AutoFileBackupGUI {
	/// <summary>
	/// A class for redirecting console output to the MainWindow class' AddMessage function, which can then
	/// do whatever it wants with the message.
	/// </summary>
	class MainWindowWriter : TextWriter {

		public override Encoding Encoding {
			get { return Encoding.ASCII; }
		}

		public override void Write(char value) {
			MainWindow.Instance.AddMessage(value.ToString());
		}

		public override void Write(string? value) {
			if (value != null) {
				MainWindow.Instance.AddMessage(value);
			}
		}

	}
}
