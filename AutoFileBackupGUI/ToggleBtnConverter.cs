using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoFileBackupGUI {
	/// <summary>
	/// For changing the text of a ToggleButton by listening to it's IsChecked property.
	/// </summary>
	public class ToggleBtnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

			if ((bool)value == false) {
				return "Start Watching File";
			}
			else {
				return "Stop Watching File";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
