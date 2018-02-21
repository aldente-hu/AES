using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{ 

	public class ReferenceSpectrum : INotifyPropertyChanged
	{
		#region *DirectoryNameプロパティ
		public string DirectoryName
		{
			get
			{
				return _directoryName;
			}
			set
			{
				if (_directoryName != value)
				{
					_directoryName = value;
					NotifyPropertyChanged("DirectoryName");
				}
			}
		}
		string _directoryName = string.Empty;
		#endregion

		#region *Nameプロパティ
		public string Name
		{
			get
			{
				var cycles = DirectoryName.Split('\\');
				return cycles[cycles.Length - 1].Replace(".A", string.Empty);
			}
		}
		#endregion


		/// <summary>
		/// 与えられたパラメータによりシフトされたデータ列を返します．
		/// </summary>
		/// <param name="parameter">エネルギー軸の範囲の情報だけを用いています．</param>
		/// <param name="m"></param>
		/// <returns></returns>
		public async Task<IList<decimal>> GetDataAsync(ScanParameter parameter, int m, decimal shift = 0, decimal gain = 1)
		{
			if (m <= 0)
			{
				throw new ArgumentException("mには正の値を与えて下さい．");
			}
			var ws = await WideScan.GenerateAsync(this.DirectoryName);
			return ws.Differentiate(m)
					.GetInterpolatedData(parameter.Start - shift, parameter.Step, parameter.PointsCount)
					.Select(d => d * ws.Parameter.NormalizationGain / parameter.NormalizationGain * gain).ToList();
		}



		#region INotifyPropertyChanged実装

		protected void NotifyPropertyChanged(string propertyName)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		#endregion

	}



}
