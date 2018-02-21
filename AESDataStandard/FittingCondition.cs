using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Xml.Linq;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{

	// Fitting全般に使える条件をここに押し込む？

		// FittingModelとほとんど同じ？

	public class FittingCondition : INotifyPropertyChanged
	{

		// (0.1.0)各プロファイルに依存する部分は，ここに押し込む．


		#region FittingProfile関連

		#region *FittingProfilesプロパティ
		public ObservableCollection<FittingProfile> FittingProfiles
		{
			get
			{
				return _fittingProfiles;
			}
		}
		ObservableCollection<FittingProfile> _fittingProfiles = new ObservableCollection<FittingProfile>();
		#endregion

		#region *CurrentFittingProfileプロパティ
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

		// WideScan向け．
		public void AddFittingProfile()
		{
			string base_name = "Profile";
			string name = base_name;
			int i = 0;
			while (FittingProfiles.Select(p => p.Name).Contains(name))
			{
				i++;
				name = $"{base_name}({i})";
			}
			FittingProfiles.Add(new FittingProfile()
			{
				Name = name,
				// 微分を考慮していない！
				RangeBegin = 480,
				RangeEnd = 525
			});
		}

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
			FittingProfiles.Add(new FittingProfile()
			{
				Name = name,
				// 微分を考慮していない！
				RangeBegin = currentROI.Parameter.Start,
				RangeEnd = currentROI.Parameter.Stop
			});
		}

		#endregion


		#region 出力関連

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

		#endregion


		#region Cycle関連

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

		#endregion


		#region 条件入出力関連

		public const string ELEMENT_NAME = "FittingCondition";
		const string OUTPUTDESTINATION_ATTRIBUTE = "OutputDestination";
		const string CHARTFORMAT_ATTRIBUTE = "ChartFormat";
		const string PROFILES_ELEMENT = "Profiles";

		public XDocument GenerateDocument()
		{
			return new XDocument(GenerateElement());
		}

		#region *XML要素を生成(GenerateElement)
		public XElement GenerateElement()
		{
			var element = new XElement(ELEMENT_NAME,
				new XAttribute(OUTPUTDESTINATION_ATTRIBUTE, this.OutputDestination),
				new XAttribute(CHARTFORMAT_ATTRIBUTE, ChartFormat.ToString())
			);

			var profiles_element = new XElement(PROFILES_ELEMENT);
			foreach (var profile in this.FittingProfiles)
			{
				profiles_element.Add(profile.GenerateElement());
			}
			element.Add(profiles_element);

			return element;
		}
		#endregion

		#region *条件をロード(LoadFrom)
		public void LoadFrom(StreamReader reader)
		{
			var doc = XDocument.Load(reader);
			var root = doc.Root;
			if (root.Name == ELEMENT_NAME)
			{
				this.OutputDestination = (string)root.Attribute(OUTPUTDESTINATION_ATTRIBUTE);
				var chart_format = (string)root.Attribute(CHARTFORMAT_ATTRIBUTE);
				if (!string.IsNullOrEmpty(chart_format))
				this.ChartFormat = (ChartFormat)Enum.Parse(typeof(ChartFormat), chart_format);
			}

			FittingProfiles.Clear();
			foreach (var profile in root.Element(PROFILES_ELEMENT).Elements(FittingProfile.ELEMENT_NAME))
			{
				this.FittingProfiles.Add(FittingProfile.LoadProfile(profile));
			}
		}
		#endregion

		#endregion


		#region INotifyPropertyChanged実装

		protected void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		#endregion

	}

}
