using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{

	#region FixedSpectrumクラス
	public class FixedSpectrum : ReferenceSpectrum
	{
		public decimal Gain
		{
			get
			{
				return _gain;
			}
			set
			{
				if (_gain != value)
				{
					_gain = value;
					NotifyPropertyChanged("Gain");
				}
			}
		}
		decimal _gain = 0.1M;

		public decimal Shift
		{
			get
			{
				return _shift;
			}
			set
			{
				if (_shift != value)
				{
					_shift = value;
					NotifyPropertyChanged("Shift");
				}
			}
		}
		decimal _shift = 0;
	}
	#endregion

}
