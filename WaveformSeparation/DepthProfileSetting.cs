using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{

	#region DepthProfileSettingクラス
	public class DepthProfileSetting : INotifyPropertyChanged
	{
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

