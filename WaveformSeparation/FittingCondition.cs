using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using HirosakiUniversity.Aldente.AES.Data.Portable;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{

	// Fitting全般に使える条件をここに押し込む？

		// FittingModelとほとんど同じ？

	public class FittingCondition : INotifyPropertyChanged
	{

		// (0.1.0)各プロファイルに依存する部分は，ここに押し込む．

		#region FittingProfilesプロパティ
		public ObservableCollection<FittingProfile> FittingProfiles
		{
			get
			{
				return _fittingProfiles;
			}
		}
		ObservableCollection<FittingProfile> _fittingProfiles = new ObservableCollection<FittingProfile>();
		#endregion

		#region CurrentFittingProfileプロパティ
		public FittingProfile CurrentFittingProfile
		{
			get
			{
				return _currentFittingProfile;
			}
			set
			{
				if (CurrentFittingProfile != value)
				{
					this._currentFittingProfile = value;
					NotifyPropertyChanged();
				}
			}
		}
		FittingProfile _currentFittingProfile = null;
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


		// (0.1.0)
		#region *Cyclesプロパティ
		public int Cycles
		{
			get
			{
				return _cycleList.Count;
			}
			set
			{
				if (value <= 0)
				{
					throw new ArgumentOutOfRangeException("Cyclesには正の値を指定して下さい．");
				}
				if (Cycles != value)
				{
					NotifyPropertyChanged();
					for (int i = 0; i < value; i++)
					{
						_cycleList.Add(i);
					}
					NotifyPropertyChanged("CycleList");
				}
			}
		}
		#endregion

		// (0.1.0)
		#region *CycleListプロパティ
		public List<int> CycleList
		{
			get
			{
				return _cycleList;
			}
		}
		List<int> _cycleList = new List<int>();
		#endregion

		// この2つはFittingConditionに入れた方がいいのかな？
		#region *SelectedCycleプロパティ
		public int? SelectedCycle
		{
			get
			{
				return _selectedCycle;
			}
			set
			{
				if (SelectedCycle != value)
				{
					_selectedCycle = value;
					NotifyPropertyChanged();
				}
			}
		}
		int? _selectedCycle = null;
		#endregion

		#region *FitAllプロパティ
		public bool FitAll
		{
			get
			{
				return _fitAll;
			}
			set
			{
				if (FitAll != value)
				{
					_fitAll = value;
					NotifyPropertyChanged();
				}
			}
		}
		bool _fitAll = true;
		#endregion


		public void AddFittingProfile(ROISpectra currentROI)
		{
			string base_name = currentROI.Name;
			string name = base_name;
			int i = 0;
			while (FittingProfiles.Select(p => p.Name).Contains(name))
			{
				i++;
				name = $"{base_name}({i})";
			}
			FittingProfiles.Add(new FittingProfile
			{
				Name = name,
				// 微分を考慮していない！
				RangeBegin = currentROI.Parameter.Start,
				RangeEnd = currentROI.Parameter.Stop
			});
		}




		#region INotifyPropertyChanged実装

		protected void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		#endregion

	}

}
