using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DSoft.VersionChanger.Converters
{
    [ValueConversion(typeof(Version), typeof(string))]
    public class VersionToStringConverter : BaseConverter, IValueConverter
    {
        public VersionToStringConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var aVer = value as Version;

            return aVer.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
