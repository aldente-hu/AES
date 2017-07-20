using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

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

		#region *WithOffsetプロパティ
		/// <summary>
		/// フィッティングの際に定数項を考慮するか否かの値を取得／設定します．
		/// </summary>
		public bool WithOffset
		{
			get
			{
				return _with_offset;
			}
			set
			{
				if (WithOffset != value)
				{
					_with_offset = value;
					NotifyPropertyChanged("WithOffset");
				}
			}
		}
		bool _with_offset = true;
		#endregion

		#region エネルギーシフト関連

		#region *FixedEnergyShiftValueプロパティ
		/// <summary>
		/// 固定されたエネルギーシフト値を取得／設定します。
		/// </summary>
		public bool FixEnergyShift
		{
			get
			{
				return _fixEnergyShift;
			}
			set
			{
				if (FixEnergyShift != value)
				{
					_fixEnergyShift = value;
					NotifyPropertyChanged();
				}
			}
		}
		bool _fixEnergyShift = false;
		#endregion

		#region *FixedEnergyShiftプロパティ
		/// <summary>
		/// 固定されたエネルギーシフト値を取得／設定します。このプロパティは、FixEnergyShiftプロパティがtrueの場合にのみ有効です。
		/// </summary>
		public decimal FixedEnergyShift
		{
			get
			{
				return _fixedEnergyShift;
			}
			set
			{
				if (FixedEnergyShift != value)
				{
					_fixedEnergyShift = value;
					NotifyPropertyChanged();
				}
			}
		}
		decimal _fixedEnergyShift = 0.0M;
		#endregion

		#endregion


		// このあたりの出力系プロパティは他のところに置いたほうがいいかもしれない。

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

		#region *ChartFormatプロパティ
		public ChartFormat ChartFormat
		{
			get
			{
				return _chartFormat;
			}
			set
			{
				if (this.ChartFormat != value)
				{
					_chartFormat = value;
					NotifyPropertyChanged();
				}
			}
		}
		ChartFormat _chartFormat = ChartFormat.Svg;
		#endregion

		#region *ReferenceSpectraプロパティ
		/// <summary>
		/// 参照スペクトルのコレクションを取得します．
		/// </summary>
		public ObservableCollection<ReferenceSpectrum> ReferenceSpectra
		{
			get
			{
				return _referenceSpectra;
			}
		}
		ObservableCollection<ReferenceSpectrum> _referenceSpectra = new ObservableCollection<ReferenceSpectrum>();
		#endregion



		// これいるのかな？と思うけど、とりあえず実装しておく。
		#region *FixedSpectraプロパティ
		public ObservableCollection<FixedSpectrum> FixedSpectra
		{
			get
			{
				return _fixedSpectra;
			}
		}
		ObservableCollection<FixedSpectrum> _fixedSpectra = new ObservableCollection<FixedSpectrum>();
		#endregion


		// 固定参照スペクトルを取得する。
		//List<decimal> fixed_data = new List<decimal>();
		//if (FixedSpectra.Count > 0)
		//{
		//	var v_data = await LoadShiftedFixedStandardsData(FixedSpectra, d_data.Parameter);
		//	for (int j = 0; j < v_data.First().Count; j++)
		//	{
		//		fixed_data.Add(v_data.Sum(one => one[j]));
		//	}
		//}


		#region INotifyPropertyChanged実装

		protected void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		#endregion

	}

}
