using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;

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
			_selectChartDestinationCommand = new DelegateCommand(SelectChartDestination_Executed);
			_addFittingProfileCommand = new DelegateCommand(AddFittingProfile_Executed);
			_removeProfileCommand = new DelegateCommand(RemoveProfile_Executed, RemoveProfile_CanExecute);
			_fitSpectrumCommand = new DelegateCommand(FitSpectrum_Executed, FitSpectrum_CanExecute);

			_loadConditionCommand = new DelegateCommand(LoadCondition_Executed);
			_saveConditionCommand = new DelegateCommand(SaveCondition_Executed);

			FittingCondition.FittingProfiles.CollectionChanged += FittingProfiles_CollectionChanged;
			FittingCondition.PropertyChanged += FittingCondition_PropertyChanged;

		}

		// DepthProfileViewModelからのコピペ．
		private void FittingProfiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			FitSpectrumCommand.RaiseCanExecuteChanged();
		}

		// DepthProfileViewModelからのコピペ．
		private void FittingCondition_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "CurrentFittingProfile":
					_removeProfileCommand.RaiseCanExecuteChanged();
					_addReferenceSpectrumCommand.RaiseCanExecuteChanged();
					break;
			}
		}

		#endregion

		public async Task LoadFromAsync(string directory)
		{
			await _wideScan.LoadFromAsync(directory);
			// これどうする？(0.1.0)
			//FittingCondition.RangeBegin = _wideScan.Parameter.Start;
			//FittingCondition.RangeEnd = _wideScan.Parameter.Stop;
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



		#region AddFittingProfileCommand

		void AddFittingProfile_Executed(object parameter)
		{
			FittingCondition.AddFittingProfile();
		}


		public DelegateCommand AddFittingProfileCommand
		{
			get
			{
				return _addFittingProfileCommand;
			}
		}
		DelegateCommand _addFittingProfileCommand;




		#endregion


		// DepthProfileViewModelからのコピペ．
		#region LoadCondition

		public DelegateCommand LoadConditionCommand
		{
			get
			{
				return _loadConditionCommand;
			}
		}
		DelegateCommand _loadConditionCommand;

		void LoadCondition_Executed(object parameter)
		{
			var message = new SelectOpenFileMessage(this) { Message = "ロードするプロファイルを選択して下さい。" };
			message.Filter = new string[] { "*.fcd", "*" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				// ロードする．
				using (StreamReader reader = new StreamReader(message.SelectedFile))
				{
					FittingCondition.LoadFrom(reader);
				}
			}
		}

		#endregion

		// DepthProfileViewModelからのコピペ．
		#region SaveCondition

		public DelegateCommand SaveConditionCommand
		{
			get
			{
				return _saveConditionCommand;
			}
		}
		DelegateCommand _saveConditionCommand;

		void SaveCondition_Executed(object parameter)
		{
			var message = new SelectSaveFileMessage(this) { Message = "プロファイルの出力先を選んで下さい。" };
			message.Ext = new string[] { ".fcd" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				using (var writer = new StreamWriter(message.SelectedFile, false, Encoding.UTF8))
				{
					FittingCondition.GenerateDocument().Save(writer);
				}
			}

		}

		#endregion


		// DepthProfileViewModelからのコピペ．
		#region SelectChartDestination

		public DelegateCommand SelectChartDestinationCommand
		{
			get
			{
				return _selectChartDestinationCommand;
			}
		}
		DelegateCommand _selectChartDestinationCommand;

		void SelectChartDestination_Executed(object parameter)
		{
			// ディレクトリを選ぶのか？
			// とりあえずファイル選択にしておく．
			// →ファイル選択と見せかけて、ディレクトリ選択にする。

			var message = new SelectSaveFileMessage(this) { Message = "pngファイルの出力先を選んで下さい．" };
			message.Ext = new string[] { ".png" };
			Messenger.Default.Send(this, message);
			if (string.IsNullOrEmpty(message.SelectedFile))
			{
				FittingCondition.OutputDestination = string.Empty;
			}
			else
			{
				FittingCondition.OutputDestination = Path.GetDirectoryName(message.SelectedFile);
			}

		}

		#endregion


		// DepthProfileViewModelからのコピペ．
		#region RemoveProfile

		public DelegateCommand RemoveProfileCommand
		{
			get
			{
				return _removeProfileCommand;
			}
		}
		DelegateCommand _removeProfileCommand;

		void RemoveProfile_Executed(object parameter)
		{
			var profile = FittingCondition.CurrentFittingProfile;
			if (profile != null)
			{
				FittingCondition.FittingProfiles.Remove(profile);
			}
		}

		bool RemoveProfile_CanExecute(object parameter)
		{
			return FittingCondition.CurrentFittingProfile != null;
		}

		#endregion


		// DepthProfileViewModelからのコピペ．
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
			var message = new SelectOpenFileMessage(this) { Message = "参照スペクトルを選んで下さい．" };
			// 拡張子ではなくファイル名が指定されている場合ってどうするの？
			message.Filter = new string[] { "id" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				var id_file = message.SelectedFile;
				var dir = System.IO.Path.GetDirectoryName(id_file);

				if ((await IdFile.CheckTypeAsync(id_file)) == DataType.WideScan)
				{
					// OK
					FittingCondition.CurrentFittingProfile.ReferenceSpectra.Add(new ReferenceSpectrum { DirectoryName = dir });
				}
				else
				{
					var error_message = new SimpleMessage(this) { Message = "WideScanじゃないとだめだよ！" };
					Messenger.Default.Send(this, error_message);
				}

			}

		}

		bool AddReferenceSpectrum_CanExecute(object parameter)
		{
			return FittingCondition.CurrentFittingProfile != null;
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


		#region FitSpectrum


		public DelegateCommand FitSpectrumCommand
		{
			get
			{
				return _fitSpectrumCommand;
			}
		}
		DelegateCommand _fitSpectrumCommand;

		// (0.0.6)バグを修正．
		// parameterで数値が渡されれば、1サイクルに対して解析を行う。
		// さもなければ、全サイクルに対して解析を行う。
		async void FitSpectrum_Executed(object parameter)
		{
			// parameterがnullであれば，全てのprofileに対してフィッティングを行う．
			// parameterにprofileが与えられていれば，それに対してフィッティングを行う．

			// ★BaseROIを決める段階と，実際のフィッティングを行う段階を分離した方がいいのでは？

			var fitting_task = parameter is FittingProfile ?
						Task.WhenAll(FitSingleProfile((FittingProfile)parameter)) :
						Task.WhenAll(FittingCondition.FittingProfiles.Select(p => FitSingleProfile(p)));

			try
			{
				await fitting_task;
			}
			catch (Exception) { }

			if (fitting_task.Exception is AggregateException)
			{
				var message = string.Join("\n", fitting_task.Exception.InnerExceptions.Select(ex => ex.Message));
				Messenger.Default.Send(this, new SimpleMessage(this) { Message = message });
				return;
			}
		}

		bool FitSpectrum_CanExecute(object parameter)
		{
			if (parameter is FittingProfile)
			{
				return true;
			}
			else
			{
				return FittingCondition.FittingProfiles.Count > 0;
			}
		}
		// (参考として旧実装をおいておきます．)
		public bool Fit_CanExecute(object parameter)
		{
			return (!string.IsNullOrEmpty(FittingCondition.OutputDestination) && (_wideScan != null));
		}


		// (0.1.0)1つのProfileについてのみフィッティングを行う．
		async Task FitSingleProfile(FittingProfile profile)
		{
			var d_data = _wideScan.GetRestrictedData(FittingCondition.CurrentFittingProfile.RangeBegin, FittingCondition.CurrentFittingProfile.RangeEnd).Differentiate(3);





			// 1.まず，フィッティングの計算を行う．
				#region  固定参照スペクトルを取得する。(一時的にコメントアウト中)
				/*
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
				*/
				#endregion

				/// フィッティング対象となるデータ。すなわち、もとのデータからFixされた分を差し引いたデータ。
				//var target_data = fixed_data.Count > 0 ? data.Substract(fixed_data) : data;
				// なんだけど、とりあえずはFixedを考慮しない。
				var target_data = d_data.Data;

				var result = await profile.FitOneCycle(99, target_data, d_data.Parameter);


			// 2.その後に，チャート出力を行う．
			var chart = await Output(d_data.Parameter, profile, target_data, result);
			//task.Start();


			// pltファイルも出力してみる。
			using (var writer = new StreamWriter(GetCsvFileName(profile.Name) + ".plt"))
			{
				await chart.OutputPltFileAsync(writer);
			}
			// チャートを描画する。
			await chart.Draw();

		}

		// (0.1.0)メソッド名をFitからOutputに変更．というか，これどこにおけばいいのかな？
		private async Task<Gnuplot> Output(
			ScanParameter originalParameter,
			FittingProfile profile,
			EqualIntervalData target_data,
			FittingProfile.FittingResult result)
		{
			// フィッティングした結果をチャートにする？
			// ★とりあえずFixedなデータは表示しない。

			bool output_convolution = result.Standards.Count > 1;

			// それには、csvを出力する必要がある。
			//string fitted_csv_path = Path.Combine(FittingCondition.OutputDestination, $"{FittingCondition.Name}_{layer}.csv");
			using (var csv_writer = new StreamWriter(GetCsvFileName(profile.Name)))
			{
				await OutputFittedCsv(csv_writer, originalParameter, target_data, result, output_convolution).ConfigureAwait(false);
			}

			// チャート出力の準備？
			return ConfigureChart(result, profile, output_convolution);

		}


		#region *フィットした結果をCSV形式で出力(OutputFittedCsv)
		private async Task OutputFittedCsv(StreamWriter writer, ScanParameter originalParameter, EqualIntervalData targetData, FittingProfile.FittingResult result, bool outputConvolution)
		{
			for (int k = 0; k < originalParameter.PointsCount; k++)
			{
				List<string> cols = new List<string>();

				// 0列: エネルギー値
				cols.Add((originalParameter.Start + k * originalParameter.Step + result.Shift).ToString("f2"));

				// 1列: 測定データ
				cols.Add(targetData[k].ToString("f3"));

				// 2～N列: 各成分(標準データ×ゲイン)
				decimal conv = 0;
				for (int j = 0; j < result.Standards.Count; j++)
				{
					var intensity = result.GetGainedStandard(j, k);
					conv += intensity;
					cols.Add(intensity.ToString("f3"));
				}

				// 最終列: 成分の総和
				if (outputConvolution)
				{
					cols.Add(conv.ToString("f3"));
				}

				await writer.WriteLineAsync(string.Join(",", cols));
			}

		}
		#endregion

		#endregion

		string GetCsvFileName(string name)
		{
			return Path.Combine(FittingCondition.OutputDestination, $"{name}.csv");
		}

		#region *チャートを設定(ConfigureChart)
		Gnuplot ConfigureChart(FittingProfile.FittingResult result, FittingProfile profile, bool outputConvolution)
		{
			string chart_ext = string.Empty;
			switch (FittingCondition.ChartFormat)
			{
				case ChartFormat.Png:
					chart_ext = ".png";
					break;
				case ChartFormat.Svg:
					chart_ext = ".svg";
					break;
			}

			var chart_destination = Path.Combine(FittingCondition.OutputDestination, $"{profile.Name}{chart_ext}");

			#region チャート設定
			var gnuplot = new Gnuplot
			{
				Format = FittingCondition.ChartFormat,
				Width = 800,
				Height = 600,
				FontSize = 20,
				Destination = chart_destination,
				XTitle = "Kinetic Energy / eV",
				YTitle = "dN(E)/dE",
				Title = $"Shift {result.Shift} eV"
			};

			var source_csv = GetCsvFileName(profile.Name);

			gnuplot.DataSeries.Add(new LineChartSeries
			{
				SourceFile = source_csv,
				XColumn = 1,
				YColumn = 2,
				Title = "data",
				Style = new LineChartSeriesStyle(LineChartStyle.Lines)
				{
					Style = new LinePointStyle
					{
						LineColor = "#FF0000",
						LineWidth = 3,
					}
				}
			});

			var reference_names = profile.ReferenceSpectra.Select(r => r.Name).ToList();
			for (int j = 0; j < reference_names.Count; j++)
			{

				gnuplot.DataSeries.Add(new LineChartSeries
				{
					SourceFile = source_csv,
					XColumn = 1,
					YColumn = j + 3,
					Title = $"{result.Gains[j].ToString("f3")} * {reference_names[j]}",
					Style = new LineChartSeriesStyle(LineChartStyle.Lines)
					{
						Style = new LinePointStyle
						{
							LineColorIndex = j,
							LineWidth = 2,
						}
					}
				});
			}

			if (outputConvolution)
			{
				gnuplot.DataSeries.Add(new LineChartSeries
				{
					SourceFile = source_csv,
					XColumn = 1,
					YColumn = reference_names.Count + 3,
					Title = "Convolution",
					Style = new LineChartSeriesStyle(LineChartStyle.Lines)
					{
						Style = new LinePointStyle
						{
							LineColor = "#0000FF",
							LineWidth = 3,
						}
					}
				});
			}
			#endregion

			gnuplot.PreConfigureAxis();

			return gnuplot;
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
