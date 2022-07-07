using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation.Net6
{
	public class DecimalConverter : System.Windows.Data.IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			decimal d_value = (decimal)value;
			if (targetType == typeof(string))
			{
				var param = parameter.ToString();
				return string.IsNullOrEmpty(param) ? d_value.ToString() : d_value.ToString(param);
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType == typeof(string))
			{
				string s_value = (string)value;
				return decimal.Parse(s_value);
			}
			else
			{
				throw new InvalidOperationException();
			}

		}
	}
}
