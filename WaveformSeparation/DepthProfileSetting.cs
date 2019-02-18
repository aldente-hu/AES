using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{

	#region DepthProfileSettingクラス
	[Obsolete("0.2.0では使われていないようなので，次のバージョンでは削除します．")]
	public class DepthProfileSetting : INotifyPropertyChanged
	{
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if (_name != value)
				{
					_name = value;
					NotifyPropertyChanged("Name");
				}
			}
		}
		string _name = string.Empty;

		public decimal RangeStart
		{
			get
			{
				return _rangeStart;
			}
			set
			{
				if (_rangeStart != value)
				{
					_rangeStart = value;
					NotifyPropertyChanged("RangeStart");
				}
			}
		}
		decimal _rangeStart = 100;

		public decimal RangeStop
		{
			get
			{
				return _rangeStop;
			}
			set
			{
				if (_rangeStop != value)
				{
					_rangeStop = value;
					NotifyPropertyChanged("RangeStop");
				}
			}
		}

		decimal _rangeStop = 200;

		/// <summary>
		/// データの出力先を取得／設定します。
		/// </summary>
		public string OutputDestination
		{
			get
			{
				return _outputDestination;
			}
			set
			{
				if (_outputDestination != value)
				{
					_outputDestination = value;
					NotifyPropertyChanged("OutputDestination");
				}
			}
		}
		string _outputDestination = string.Empty;

		#region INotifyPropertyChanged実装

		protected void NotifyPropertyChanged(string propertyName)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		#endregion

	}
	#endregion

}

