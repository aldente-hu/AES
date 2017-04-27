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

	#region WideScanViewModelクラス
	public class WideScanViewModel : ViewModelBase
	{

		// データをロードする前とした後を識別するフラグが，今のところ存在しない．
		// (_WideScanがnullということはありえない．)

		WideScan _wideScan = new WideScan();

		//public ScanParameter ScanParameter
		//{
		//	get
		//	{
		//		return _wideScan == null ? null : _wideScan.Parameter;
		//	}
		//}
		//ScanParameter _scanParameter;

		#region *FittingConditionプロパティ
		public FittingCondition FittingCondition
		{
			get
			{
				return _fittingCondition;
			}
		}
		FittingCondition _fittingCondition = new FittingCondition();
		#endregion

/*
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
			*/

			/*
		#region *ReferenceSpectraプロパティ
		public ObservableCollection<ReferenceSpectrum> ReferenceSpectra
		{
			get
			{
				return _referenceSpectra;
			}
		}
		ObservableCollection<ReferenceSpectrum> _referenceSpectra = new ObservableCollection<ReferenceSpectrum>();
		#endregion
			*/


		// 現在機能していません．
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

		#region *コンストラクタ(WideScanViewModel)
		public WideScanViewModel()
		{
			_exportCsvCommand = new DelegateCommand(ExportCsv_Executed, ExportCsv_CanExecute);
			_selectDestinationDirectoryCommand = new DelegateCommand(SelectDestinationDirectory_Executed);
			_addFixedSpectrumCommand = new DelegateCommand(AddFixedSpectrum_Executed);
			_addReferenceSpectrumCommand = new DelegateCommand(AddReferenceSpectrum_Executed);
			_fitCommand = new DelegateCommand(Fit_Executed, Fit_CanExecute);

			FittingCondition.ReferenceSpectra.CollectionChanged += ReferenceSpectra_CollectionChanged;
		}

		private void ReferenceSpectra_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			_fitCommand.RaiseCanExecuteChanged();
		}
		#endregion
		/*
		public static async Task<WideScanViewModel> GenerateAsync(string directory)
		{
			var model = new WideScanViewModel();
			model._wideScan = await WideScan.GenerateAsync(directory);
			return model;
		}
		*/
		//public async Task LoadFromAsync(string directory)
		//{
		//	await _wideScan.LoadFromAsync(directory);
		//	this.EnergyBegin = _wideScan.Parameter.Start;
		//	this.EnergyEnd = _wideScan.Parameter.Stop;
		//}

			
		public async Task LoadFromAsync(string directory)
		{
			await _wideScan.LoadFromAsync(directory);
			FittingCondition.RangeBegin = _wideScan.Parameter.Start;
			FittingCondition.RangeEnd = _wideScan.Parameter.Stop;
		}


		#region ExportCsv

		#region プロパティ

		public bool ExportCsvRaw
		{
			get
			{
				return _exportCsvMode.HasFlag(ExportCsvMode.Raw);
			}
			set
			{
				if (ExportCsvRaw != value)
				{
					_exportCsvMode ^= ExportCsvMode.Raw;	// XORをとる．
					NotifyPropertyChanged();
					ExportCsvCommand.RaiseCanExecuteChanged();
				}
			}
		}

		public bool ExportCsvDiff
		{
			get
			{
				return _exportCsvMode.HasFlag(ExportCsvMode.Diff);
			}
			set
			{
				if (ExportCsvDiff != value)
				{
					_exportCsvMode ^= ExportCsvMode.Diff;  // XORをとる．
					NotifyPropertyChanged();
					ExportCsvCommand.RaiseCanExecuteChanged();
				}
			}
		}

		ExportCsvMode _exportCsvMode = ExportCsvMode.None;

		#endregion

		#region ハンドラ

		public DelegateCommand ExportCsvCommand
		{
			get
			{
				return _exportCsvCommand;
			}
		}
		DelegateCommand _exportCsvCommand;

		async void ExportCsv_Executed(object parameter)
		{
			// こういうのもMessengerを使うべきか？
			var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "csvファイル(*.csv)|*.csv" };
			if (dialog.ShowDialog() == true)
			{
				var raw_file_name = dialog.FileName;

				if (ExportCsvRaw)
				{
					using (var writer = new System.IO.StreamWriter(raw_file_name, false))
					{
						await _wideScan.ExportCsvAsync(writer);
					}
				}
				if (ExportCsvDiff)
				{
					var diff_file_name = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(raw_file_name),
						System.IO.Path.GetFileNameWithoutExtension(raw_file_name) + "_diff" + System.IO.Path.GetExtension(raw_file_name)
					);
					using (var writer = new System.IO.StreamWriter(diff_file_name, false))
					{
						await _wideScan.Differentiate(3).ExportCsvAsync(writer);
					}
				}
			}
		}

		bool ExportCsv_CanExecute(object parameter)
		{
			return _exportCsvMode != ExportCsvMode.None;
		}

		#endregion

		#region ExportCsvMode列挙体
		[Flags]
		enum ExportCsvMode
		{
			/// <summary>
			/// 生データをCSVとして出力します．
			/// </summary>
			Raw = 0x01,
			/// <summary>
			/// 微分データをCSVとして出力します．
			/// </summary>
			Diff = 0x02,
			/// <summary>
			/// 何も出力しません．
			/// </summary>
			None = 0x0
		}
		#endregion

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
				this.FittingCondition.OutputDestination = System.IO.Path.GetDirectoryName(dialog.FileName);
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
				string dir = await SelectSpectrumAsync("追加する参照スペクトルを選んで下さい。");
				if (!string.IsNullOrEmpty(dir))
				{
					FittingCondition.ReferenceSpectra.Add(new ReferenceSpectrum { DirectoryName = dir });
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
			string dir = await SelectSpectrumAsync("追加する固定スペクトルを選んで下さい。");
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
			var d_data = _wideScan.GetRestrictedData(FittingCondition.RangeBegin, FittingCondition.RangeEnd).Differentiate(3);

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


			var references = await FittingCondition.ReferenceSpectra.ForEachAsync(sp => sp.GetDataAsync(d_data.Parameter, 3), 10);

			//var result = Fitting.WithConstant(d_data.Data, references);
			var result = Fitting.WithoutConstant(d_data.Data, references);

			// とりあえず簡単に結果を出力する．
			string destination = System.IO.Path.Combine(FittingCondition.OutputDestination, "result.txt");
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
			return (!string.IsNullOrEmpty(FittingCondition.OutputDestination) && (_wideScan != null));
		}


		#endregion


		async Task<string> SelectSpectrumAsync(string description)
		{
			var message = new SelectOpenFileMessage(this) { Message = description};
			message.Filter = new string[] { "id" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				var id_file = message.SelectedFile;
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

	}
	#endregion


	public class NotWideScanException : Exception
	{
		public NotWideScanException() : base() { }
		public NotWideScanException(string message) : base(message) { }

	}




}
