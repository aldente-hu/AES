using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
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

		/// <summary>
		/// 与えられたパラメータによりシフトされたデータ列を返します．
		/// </summary>
		/// <param name="parameter">エネルギー軸の範囲の情報だけを用いています．</param>
		/// <param name="m"></param>
		/// <returns></returns>
		public async Task<IList<decimal>> GetShiftedDataAsync(ScanParameter parameter, int m)
		{
			return await GetDataAsync(parameter, m, Shift, Gain);
		}

	}
	#endregion

}
