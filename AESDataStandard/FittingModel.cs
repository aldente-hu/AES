using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{

	// とりあえずWideScanのFittingをモデル化してみる。
	[Obsolete("0.2.0では使われていないようなので，その次のバージョンで削除します．")]
	#region FittingModelクラス
	public class FittingModel : INotifyPropertyChanged
	{

		#region *Nameプロパティ
		/// <summary>
		/// フィッティング系列の名前を取得／設定します。
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					value = string.Empty;
				}
				if (Name != value)
				{
					this._name = value;
					NotifyPropertyChanged("Name");
				}
			}
		}
		string _name = string.Empty;
		#endregion

		// 便宜的にここにおいておく。
		#region *OutputDestinationプロパティ
		/// <summary>
		/// 出力先を取得／設定します。
		/// </summary>
		public string OutputDestination
		{
			get
			{
				return _outputDestination;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					value = string.Empty;
				}
				if (OutputDestination != value)
				{
					this._outputDestination = value;
					NotifyPropertyChanged("OutputDestination");
				}
			}
		}
		string _outputDestination = string.Empty;
		#endregion

		public ObservableCollection<ReferenceSpectrum> ReferenceSpectra { get; } = new ObservableCollection<ReferenceSpectrum>();

		public ObservableCollection<FixedSpectrum> FixedSpectra { get; } = new ObservableCollection<FixedSpectrum>();

		#region *EnergyStopプロパティ
		public decimal EnergyStop
		{
			get
			{
				return _energyStop;
			}
			set
			{
				if (EnergyStop != value)
				{
					this._energyStop = value;
					NotifyPropertyChanged("EnergyStop");
				}
			}
		}
		decimal _energyStop = 200;
		#endregion


		#region *EnergyStartプロパティ
		public decimal EnergyStart
		{
			get
			{
				return _energyStart;
			}
			set
			{
				if (EnergyStart != value)
				{
					this._energyStart = value;
					NotifyPropertyChanged("EnergyStart");
				}
			}
		}
		decimal _energyStart = 50;
		#endregion


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
