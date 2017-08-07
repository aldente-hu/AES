using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{
	using Mvvm;
	using Data.Portable;
	using Helpers;

	#region DepthProfileViewModelクラス
	public class DepthProfileViewModel : ViewModelBase
	{

		#region *ROISpectraCollectionプロパティ
		public List<ROISpectra> ROISpectraCollection
		{
			get
			{
				if (_depthProfile.Spectra != null)
				{
					return _depthProfile.Spectra.Values.ToList();
				}
				else
				{
					return null;
				}
			}
		}
		//List<ROISpectra> _roiSpectraCollection = new List<ROISpectra>();
		DepthProfile _depthProfile = new DepthProfile();
		#endregion

		#region *CurrentROIプロパティ
		public ROISpectra CurrentROI
		{
			get
			{
				return _currentROI;
			}
			set
			{
				if (CurrentROI != value)
				{
					_currentROI = value;
					NotifyPropertyChanged();
				}
			}
		}
		ROISpectra _currentROI = null;
		#endregion

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



		#region *コンストラクタ(DepthProfileViewModel)
		public DepthProfileViewModel()
		{

			_addFittingProfileCommand = new DelegateCommand(AddFittingProfile_Executed);
			_selectCsvDestinationCommand = new DelegateCommand(SelectCsvDestination_Executed);
			_exportCsvCommand = new DelegateCommand(ExportCsv_Executed, ExportCsv_CanExecute);
			_selectChartDestinationCommand = new DelegateCommand(SelectChartDestination_Executed);
			_removeProfileCommand = new DelegateCommand(RemoveProfile_Executed, RemoveProfile_CanExecute);
			_fitSpectrumCommand = new DelegateCommand(FitSpectrum_Executed, FitSpectrum_CanExecute);
			_addReferenceSpectrumCommand = new DelegateCommand(AddReferenceSpectrum_Executed, AddReferenceSpectrum_CanExecute);

			_loadConditionCommand = new DelegateCommand(LoadCondition_Executed);
			_saveConditionCommand = new DelegateCommand(SaveCondition_Executed);

			//this.PropertyChanged += DepthProfileViewModel_PropertyChanged;
			FittingCondition.PropertyChanged += FittingCondition_PropertyChanged;
			FittingCondition.FittingProfiles.CollectionChanged += FittingProfiles_CollectionChanged;
		}

		private void FittingProfiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			_fitSpectrumCommand.RaiseCanExecuteChanged();
		}

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

		/*
		private void DepthProfileViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Cycles":
					if (this.SelectedCycle.HasValue && !this.Cycles.Contains(this.SelectedCycle.Value))
					{
						this.SelectedCycle = null;
					}
					break;
			}
		}
		*/


		public async Task LoadFromAsync(string directory)
		{
			await _depthProfile.LoadFromAsync(directory);
			// ここでCyclesを指定する？
			FittingCondition.Cycles = _depthProfile.Cycles;
			NotifyPropertyChanged("ROISpectraCollection");
		}

		#region AddFittingProfileCommand

		void AddFittingProfile_Executed(object parameter)
		{
			FittingCondition.AddFittingProfile(CurrentROI);
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

		#region ExportCsv

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
			var diff = parameter is bool && (bool)parameter;
			if (!string.IsNullOrEmpty(this.ExportCsvDestination))
			{
				using (var writer = new StreamWriter(this.ExportCsvDestination, false))
				{
					if (diff)
					{
						await CurrentROI.Differentiate(3).ExportCsvAsync(writer);
					}
					else
					{
						await CurrentROI.ExportCsvAsync(writer);
					}
				}
			}
		}

		bool ExportCsv_CanExecute(object parameter)
		{
			return !string.IsNullOrEmpty(this.ExportCsvDestination);
		}

		#endregion

		#region *ExportCsvDestinationプロパティ
		public string ExportCsvDestination
		{
			get
			{
				return _exportCsvDestination;
			}
			set
			{
				if (ExportCsvDestination != value)
				{
					_exportCsvDestination = value;
					NotifyPropertyChanged();
					_exportCsvCommand.RaiseCanExecuteChanged();
				}
			}
		}
		string _exportCsvDestination;
		#endregion


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

		#region SelectCsvDestination

		public DelegateCommand SelectCsvDestinationCommand
		{
			get
			{
				return _selectCsvDestinationCommand;
			}
		}
		DelegateCommand _selectCsvDestinationCommand;

		void SelectCsvDestination_Executed(object parameter)
		{
			var message = new SelectSaveFileMessage(this) { Message = "csvファイルの出力先を選んで下さい．" };
			message.Ext = new string[] { ".csv" };
			Messenger.Default.Send(this, message);
			if (string.IsNullOrEmpty(message.SelectedFile))
			{
				this.ExportCsvDestination = string.Empty;
			}
			else
			{
				this.ExportCsvDestination = message.SelectedFile;
			}
		}


		#endregion

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
				//this.ChartDestination = string.Empty;
				FittingCondition.OutputDestination = string.Empty;
			}
			else
			{
				//this.ChartDestination = message.SelectedFile;
				FittingCondition.OutputDestination = Path.GetDirectoryName(message.SelectedFile);
			}

		}

		#endregion


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


		// (0.1.0)1つのProfileについてのみフィッティングを行う．
		async Task FitSingleProfile(FittingProfile profile)
		{
			// profileがBaseROIを持つのではなく，ここでBaseとなるROIを決定するようにしてみた．

			// ★一応，範囲から推測するのをベースにするけど，
			// ★ROIの名前からでも指定できるようにするのがいいのでは？

			// ProfileのRangeを包含するROIを探す．
			var suitable_roi_list = ROISpectraCollection.Where(roi => roi.Parameter.Start <= profile.RangeBegin && roi.Parameter.Stop >= profile.RangeEnd);
			if (suitable_roi_list.Count() == 0)
			{
				var message = $"{profile.Name}に対応する測定データがありませんでした。エネルギー範囲を確認して下さい。 {profile.RangeBegin} - {profile.RangeEnd}";
				// 終了．並列実行することがあるので，例外を発生させる．
				throw new MyException(message);
			}
			var baseROI = suitable_roi_list.OrderBy(roi => roi.Parameter.Step).First();

			var d_data = baseROI.Restrict(profile.RangeBegin, profile.RangeEnd)
						.Differentiate(3);



			IEnumerable<int> target_cycles;
			if (FittingCondition.FitAll)
			{
				target_cycles = FittingCondition.CycleList;
			}
			else
			{
				if (FittingCondition.SelectedCycle.HasValue)
				{
					target_cycles = new int[] { FittingCondition.SelectedCycle.Value };
				}
				else
				{
					throw new InvalidOperationException("Cycleを選択して下さい。");
				}
			}



			var fitting_tasks = new Dictionary<int, Task<FittingProfile.FittingResult>>();

			// キーはサイクル数．
			Dictionary<int, EqualIntervalData> target_data = new Dictionary<int, EqualIntervalData>();

			// 1.まず，フィッティングの計算を行う．
			foreach (int i in target_cycles)
			{
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
				target_data[i] = d_data.Data[i];

				fitting_tasks.Add(i, profile.FitOneCycle(i, target_data[i], d_data.Parameter));
				//task.Start();
			}
			await Task.WhenAll(fitting_tasks.Values.ToArray());


			// 2.その後に，チャート出力を行う．
			var outputting_tasks = new Dictionary<int, Task<Gnuplot>>();
			foreach (int i in target_cycles)
			{
				outputting_tasks.Add(i, Output(i, d_data.Parameter, profile, target_data[i], fitting_tasks[i].Result));
				//task.Start();
			}
			await Task.WhenAll(outputting_tasks.Values.ToArray());


			var charts = outputting_tasks.ToDictionary(pair => pair.Key, pair => pair.Value.Result);

			Range x_range = Range.Union(charts.Select(gnuplot => gnuplot.Value.XAxis.Range).ToArray());
			Range y_range = Range.Union(charts.Select(gnuplot => gnuplot.Value.YAxis.Range).ToArray());

			Parallel.ForEach(charts.Keys,
				async (i) =>
				{
					charts[i].SetXAxis(x_range);
					charts[i].SetYAxis(y_range);

					// pltファイルも出力してみる。
					using (var writer = new StreamWriter(GetCsvFileName(i, profile.Name) + ".plt"))
					{
						await charts[i].OutputPltFileAsync(writer);
					}
					// チャートを描画する。
					await charts[i].Draw();
				}
			);

		}



		// (0.1.0)メソッド名をFitからOutputに変更．というか，これどこにおけばいいのかな？
		private async Task<Gnuplot> Output(
			int cycle,
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
			using (var csv_writer = new StreamWriter(GetCsvFileName(cycle, profile.Name)))
			{
				await OutputFittedCsv(csv_writer, originalParameter, target_data, result, output_convolution).ConfigureAwait(false);
			}

			// チャート出力の準備？
			return ConfigureChart(cycle, result, profile, output_convolution);

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



		string GetCsvFileName(int cycle, string name)
		{
			return Path.Combine(FittingCondition.OutputDestination, $"{name}_{cycle}.csv");
		}

		#region *チャートを設定(ConfigureChart)
		Gnuplot ConfigureChart(int cycle, FittingProfile.FittingResult result, FittingProfile profile, bool outputConvolution)
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

			var chart_destination = Path.Combine(FittingCondition.OutputDestination, $"{profile.Name}_{cycle}{chart_ext}");

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
				Title = $"Cycle {cycle} , Shift {result.Shift} eV"
			};

			var source_csv = GetCsvFileName(cycle, profile.Name);

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



	}
	#endregion


	// とりあえず用意しておく．
	#region MyExceptionクラス
	public class MyException : System.Exception
	{
		public MyException() : base() { }
		public MyException(string message) : base(message) { }
	}
	#endregion

}


