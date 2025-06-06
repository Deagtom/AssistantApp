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
            var selectedSymptoms = values[0] as ObservableCollection<Symptom>;
            var currentSymptom = values[1] as Symptom;
            if (selectedSymptoms == null || currentSymptom == null)
                return false;
            return selectedSymptoms.Any(s => s.Id == currentSymptom.Id);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}