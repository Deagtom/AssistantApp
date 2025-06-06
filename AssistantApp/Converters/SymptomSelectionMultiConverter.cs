using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using AssistantApp.Models;

namespace AssistantApp.Converters
{
    public class SymptomSelectionMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var selected = values[0] as ObservableCollection<Symptom>;
            var current = values[1] as Symptom;
            if (selected == null || current == null)
                return false;
            return selected.Any(s => s.Id == current.Id);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}