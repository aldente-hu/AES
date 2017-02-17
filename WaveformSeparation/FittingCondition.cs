using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{

	// Fitting全般に使える条件をここに押し込む？

		// FittingModelとほとんど同じ？

	public class FittingCondition : INotifyPropertyChanged
	{

		#region *Nameプロパティ
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
		#endregion

		#region *RangeBeginプロパティ
		public decimal RangeBegin
		{
			get
			{
				return _rangeBegin;
			}
			set
			{
				if (_rangeBegin != value)
				{
					_rangeBegin = value;
					NotifyPropertyChanged("RangeBegin");
				}
			}
		}
		decimal _rangeBegin = 100;
		#endregion

		#region *RangeEndプロパティ
		public decimal RangeEnd
		{
			get
			{
				return _rangeEnd;
			}
			set
			{
				if (_rangeEnd != value)
				{
					_rangeEnd = value;
					NotifyPropertyChanged("RangeEnd");
				}
			}
		}
		decimal _rangeEnd = 200;
		#endregion

		#region *OutputDestinationプロパティ
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
		#endregion

		public ObservableCollection<ReferenceSpectrum> ReferenceSpectra
		{
			get
			{
				return _referenceSpectra;
			}
		}
		ObservableCollection<ReferenceSpectrum> _referenceSpectra = new ObservableCollection<ReferenceSpectrum>();

		// とりあえず実装しない．
		ObservableCollection<FixedSpectrum> _fixedSpectra = new ObservableCollection<FixedSpectrum>();


		#region INotifyPropertyChanged実装

		protected void NotifyPropertyChanged(string propertyName)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		#endregion

	}

}
