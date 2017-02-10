using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;


namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{
	using Mvvm;
	using Data.Portable;
	using Helpers;

	public class WideScanViewModel : ViewModelBase
	{

		WideScan _wideScan = new WideScan();

		public ScanParameter ScanParameter
		{
			get
			{
				return _wideScan == null ? null : _wideScan.Parameter;
			}
		}
		//ScanParameter _scanParameter;


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

		#region *EnergyBeginプロパティ
		public decimal EnergyBegin
		{
			get
			{
				return _energyBegin;
			}
			set
			{
				if (EnergyBegin != value)
				{
					_energyBegin = value;
					NotifyPropertyChanged();
				}
			}
		}
		decimal _energyBegin = 100;
		#endregion

		#region *EnergyEndプロパティ
		public decimal EnergyEnd
		{
			get
			{
				return _energyEnd;
			}
			set
			{
				if (EnergyEnd != value)
				{
					_energyEnd = value;
					NotifyPropertyChanged();
				}
			}
		}
		decimal _energyEnd = 200;
		#endregion

		#region *DestinationDirectoryプロパティ
		public string DestinationDirectory
		{
			get
			{
				return _destinationDirectory;
			}
			set
			{
				if (DestinationDirectory != value)
				{
					_destinationDirectory = value;
					NotifyPropertyChanged();
				}
			}
		}
		string _destinationDirectory;
		#endregion


		public ObservableCollection<ReferenceSpectrum> ReferenceSpectra
		{
			get
			{
				return _referenceSpectra;
			}
		}
		ObservableCollection<ReferenceSpectrum> _referenceSpectra = new ObservableCollection<ReferenceSpectrum>();

		public ObservableCollection<FixedSpectrum> FixedSpectra
		{
			get
			{
				return _fixedSpectra;
			}
		}
		ObservableCollection<FixedSpectrum> _fixedSpectra = new ObservableCollection<FixedSpectrum>();


		public WideScanViewModel()
		{
			_selectDestinationDirectoryCommand = new DelegateCommand(SelectDestinationDirectory_Executed);
			_addFixedSpectrumCommand = new DelegateCommand(AddFixedSpectrum_Executed);
			_addReferenceSpectrumCommand = new DelegateCommand(AddReferenceSpectrum_Executed);
			_fitCommand = new DelegateCommand(Fit_Executed, Fit_CanExecute);

			_referenceSpectra.CollectionChanged += ReferenceSpectra_CollectionChanged;
		}

		private void ReferenceSpectra_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			_fitCommand.RaiseCanExecuteChanged();
		}

		public static async Task<WideScanViewModel> GenerateAsync(string directory)
		{
			var model = new WideScanViewModel();
			model._wideScan = await WideScan.GenerateAsync(directory);
			return model;
		}

		public async Task LoadFromAsync(string directory)
		{
			await _wideScan.LoadFromAsync(directory);
			this.EnergyBegin = _wideScan.Parameter.Start;
			this.EnergyEnd = _wideScan.Parameter.Stop;
		}

		#region Load
/*
		public DelegateCommand LoadCommand
		{
			get
			{
				return _loadCommand;
			}
		}
		DelegateCommand _loadCommand;

		async void Load_Executed(object parameter)
		{
			var directory = (string)parameter;
			_wideScan = await WideScan.GenerateAsync(directory);

			NotifyPropertyChanged("ScanParameter");
		}
*/
		#endregion

		#region SelectDestinationDirectory

		public DelegateCommand SelectDestinationDirectoryCommand
		{
			get
			{
				return _selectDestinationDirectoryCommand;
			}
		}
		DelegateCommand _selectDestinationDirectoryCommand;

		void SelectDestinationDirectory_Executed(object parameter)
		{
			// フォルダダイアログの方が望ましい．
			var dialog = new Microsoft.Win32.SaveFileDialog { DefaultExt = ".csv" };

			// WPFでは同期的に実行するしかない？
			if (dialog.ShowDialog() == true)
			{
				this.DestinationDirectory = System.IO.Path.GetDirectoryName(dialog.FileName);
			}
		}

		#endregion

		#region AddReferenceSpectrum

		public DelegateCommand AddReferenceSpectrumCommand
		{
			get
			{
				return _addReferenceSpectrumCommand;
			}
		}
		DelegateCommand _addReferenceSpectrumCommand;

		async void AddReferenceSpectrum_Executed(object parameter)
		{
			try
			{
				string dir = await SelectSpectrumAsync();
				if (!string.IsNullOrEmpty(dir))
				{
					ReferenceSpectra.Add(new ReferenceSpectrum { DirectoryName = dir });
				}
			}
			catch (NotWideScanException)
			{
				var msg = new SimpleMessage(this) { Message = "ここにはWideScanのデータしか使えません．" };
				Messenger.Default.Send(this, msg);
			}
		}

		#endregion

		#region AddFixedSpectrum

		public DelegateCommand AddFixedSpectrumCommand
		{
			get
			{
				return _addFixedSpectrumCommand;
			}
		}
		DelegateCommand _addFixedSpectrumCommand;

		async void AddFixedSpectrum_Executed(object parameter)
		{
			string dir = await SelectSpectrumAsync();
			if (!string.IsNullOrEmpty(dir))
			{
				FixedSpectra.Add(new FixedSpectrum { DirectoryName = dir });
			}
		}

		#endregion

		#region Fit

		public DelegateCommand FitCommand
		{
			get
			{
				return _fitCommand;
			}
		}
		DelegateCommand _fitCommand;

		public async void Fit_Executed(object parameter)
		{
			var d_data = _wideScan.GetRestrictedData(EnergyBegin, EnergyEnd).Differentiate(3);

			// 固定参照スペクトルを取得する。
			List<decimal> fixed_data = new List<decimal>();
			if (FixedSpectra.Count > 0)
			{
				var v_data = await FixedSpectra.ForEachAsync(
					async sp => await sp.GetShiftedDataAsync(d_data.Parameter, 3), 10);			
				
				for (int j = 0; j < v_data.First().Count; j++)
				{
					fixed_data.Add(v_data.Sum(one => one[j]));
				}
			}

			// 参照スペクトルを取得する．

			// 参照スペクトルのデータを，測定データの範囲に制限し，ピッチも測定データに合わせる．
			// →と考えたが，これはいずれもシフト値によって変わることに注意！

			// リファレンスをどう用意するか？


			var references = await ReferenceSpectra.ForEachAsync(sp => sp.GetDataAsync(d_data.Parameter, 3), 10);

			var result = Fitting.WithConstant(d_data.Data, references);

			// とりあえず簡単に結果を出力する．
			string destination = System.IO.Path.Combine(DestinationDirectory, "result.txt");
			using (var writer = new System.IO.StreamWriter(destination, false))
			{
				for (int i = 0; i < result.Factors.Count; i++)
				{
					writer.WriteLine($"Factor{i}  :  {result.Factors[i]}");
				}
				writer.WriteLine($"residual  :  {result.Residual}");
			}

			//FitOneLayer(0, d_data.Data, d_data.Parameter, WideFittingModel.ReferenceSpectra, fixed_data,
			//	WideFittingModel.OutputDestination,
			//	"Wide");

		}

		public bool Fit_CanExecute(object parameter)
		{
			return (!string.IsNullOrEmpty(DestinationDirectory) && (_wideScan != null));
		}


		#endregion


		static async Task<string> SelectSpectrumAsync()
		{
			var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "idファイル(id)|id" };
			if (dialog.ShowDialog() == true)
			{
				string id_file = dialog.FileName;
				var dir = System.IO.Path.GetDirectoryName(id_file);
				if ((await IdFile.CheckTypeAsync(id_file)) == DataType.WideScan)
				{
					return dir;
				}
				else
				{
					// ※ViewModelからUIにメッセージを送る仕組みはどうする？
					// WideScanじゃないよ！
					throw new NotWideScanException("WideScanのデータしか使えませんよ！");
				}
			}
			else
			{
				// 単なるキャンセル．
				return string.Empty;
			}
		}

		// とりあえずのメソッド．
		public void ExportCsv(System.IO.StreamWriter writer)
		{
			_wideScan.ExportCsv(writer);
		}

	}

	public class NotWideScanException : Exception
	{
		public NotWideScanException() : base() { }
		public NotWideScanException(string message) : base(message) { }

	}

}
