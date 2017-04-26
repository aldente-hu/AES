using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{

	#region ChartFormatConverterクラス
	public class ChartFormatConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var format = ConvertParameter(parameter);
			if (targetType == typeof(bool) || targetType == typeof(bool?))
			{
				return format.Equals(value);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// true→falseの変化は無視する。
			if (targetType == typeof(bool) || targetType == typeof(bool?))
			{
				if (value.Equals(true))
				{
					return ConvertParameter(parameter);
				}
				else
				{
					return System.Windows.DependencyProperty.UnsetValue;
				}
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		ChartFormat ConvertParameter(object parameter)
		{
			return (ChartFormat)Enum.Parse(typeof(ChartFormat), parameter as string);
		}

	}
	#endregion

}
