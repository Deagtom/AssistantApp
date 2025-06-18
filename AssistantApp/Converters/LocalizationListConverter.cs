using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AssistantApp.Converters
{
    public class LocalizationListConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var raw = values[0] as string;
            var lang = values[1] as string;
            if (string.IsNullOrWhiteSpace(raw) || lang == null) return raw;

            var items = raw.Split(',')
                           .Select(s => s.Trim())
                           .Select(key =>
                           {
                               if (LocalizationMultiConverter.TryTranslate(key, lang, out var text))
                                   return text;
                               return key;
                           });

            return string.Join(", ", items);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
