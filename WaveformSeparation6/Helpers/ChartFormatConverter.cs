using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

// これもどこに置くべきなんだろう？

namespace HirosakiUniversity.Aldente.AES

{
	using Data.Standard;

	namespace WaveformSeparation.Net6.Helpers
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
				// ※こちらのtargetTypeは、もともとのデータタイプ(Convertメソッドでのvalueに対応するデータ型)。

				// true→falseの変化は無視する。
				if (value is bool || value is bool?)
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
				return (ChartFormat)Enum.Parse(typeof(ChartFormat), (string)parameter);
			}

		}
		#endregion

	}
}
