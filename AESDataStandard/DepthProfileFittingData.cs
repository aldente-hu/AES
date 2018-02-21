using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{

	#region DepthProfileFittingDataクラス
	public class DepthProfileFittingData : INotifyPropertyChanged
	{

		#region *DepthProfileプロパティ
		public DepthProfile DepthProfile
		{
			get
			{
				return _depthProfile;
			}
		}
		DepthProfile _depthProfile = new DepthProfile();
		#endregion

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
		public DepthProfileFittingData()
		{
			// コマンドハンドラの設定
			/*
			_selectSimpleCsvDestinationCommand = new DelegateCommand(SelectSimpleCsvDestination_Executed);
			_exportCsvCommand = new DelegateCommand(ExportCsv_Executed, ExportCsv_CanExecute);

			_addFittingProfileCommand = new DelegateCommand(AddFittingProfile_Executed);
			_selectCsvDestinationCommand = new DelegateCommand(SelectCsvDestination_Executed);
			_selectChartDestinationCommand = new DelegateCommand(SelectChartDestination_Executed);
			_removeProfileCommand = new DelegateCommand(RemoveProfile_Executed, RemoveProfile_CanExecute);
			_fitSpectrumCommand = new DelegateCommand(FitSpectrum_Executed, FitSpectrum_CanExecute);
			_addReferenceSpectrumCommand = new DelegateCommand(AddReferenceSpectrum_Executed, AddReferenceSpectrum_CanExecute);

			_loadConditionCommand = new DelegateCommand(LoadCondition_Executed);
			_saveConditionCommand = new DelegateCommand(SaveCondition_Executed);
			*/
			//this.PropertyChanged += DepthProfileViewModel_PropertyChanged;

			//FittingCondition.PropertyChanged += FittingCondition_PropertyChanged;
			//FittingCondition.FittingProfiles.CollectionChanged += FittingProfiles_CollectionChanged;
		}

		#endregion


		public async Task LoadFromAsync(string directory)
		{
			await _depthProfile.LoadFromAsync(directory);
			// ここでCyclesを指定する？
			FittingCondition.Cycles = _depthProfile.Cycles;
			//OutputCondition.Cycles = _depthProfile.Cycles;
			NotifyPropertyChanged("ROISpectraCollection");
		}


		#region 単純出力関連


		#region ExportCsv


		public async Task ExportCsvAsync(ROISpectra roi, string destination, int diffRange = 0)
		{
			if (diffRange < 0)
			{
				throw new ArgumentException("diffRangeには0か正の値を指定して下さい．", "diffRange");
			}

			using (var writer = new StreamWriter(destination, false))
			{
				if (diffRange > 0)
				{
					await roi.Differentiate(diffRange).ExportCsvAsync(writer);
				}
				else
				{
					await roi.ExportCsvAsync(writer);
				}
			}

		}

		#endregion


		#endregion



		// 指定したROIを，フィッティング対象に追加する．
		public void AddFittingProfile(ROISpectra roi)
		{
			FittingCondition.AddFittingProfile(roi);
		}

		// 指定したプロファイルを削除する？
		public void RemoveFittingProfile(FittingProfile profile)
		{
			FittingCondition.FittingProfiles.Remove(profile);
		}


		// フィッティング条件をロードする．
		public void LoadFittingCondition(string fileName)
		{
			// ロードする．
			using (StreamReader reader = new StreamReader(fileName))
			{
				FittingCondition.LoadFrom(reader);
			}
		}



		// フィッティング条件をセーブする．
		public void SaveFittingCondition(string fileName)
		{
			using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
			{
				FittingCondition.GenerateDocument().Save(writer);
			}

		}




		/// <summary>
		/// グラフの出力先を指定します．ファイル名で指定することもできますが，記録されるのはディレクトリ名だけです．
		/// </summary>
		/// <param name="destination"></param>
		public void SetChartDestination(string destination)
		{
			if (string.IsNullOrEmpty(destination))
			{
				FittingCondition.OutputDestination = string.Empty;
				return;
			}

			if (Path.IsPathRooted(destination))
			{
				FittingCondition.OutputDestination
					= Path.GetFileName(destination) == string.Empty ? destination : Path.GetDirectoryName(destination);
			}
			else
			{
				throw new ArgumentException("destinationには絶対パスを指定して下さい．");
			}
		}


		/// <summary>
		/// ファイルからスペクトルを読み込み，指定されたプロファイルの参照データとします．
		/// </summary>
		/// <param name="idFileName"></param>
		/// <param name="profile"></param>
		/// <returns></returns>
		public async Task AddReferenceSpectrumAsync(string idFileName, FittingProfile profile)
		{
			var dir = System.IO.Path.GetDirectoryName(idFileName);

			if ((await IdFile.CheckTypeAsync(idFileName)) == DataType.WideScan)
			{
				// OK
				profile.ReferenceSpectra.Add(new ReferenceSpectrum { DirectoryName = dir });
			}
			else
			{
				//var error_message = new SimpleMessage(this) { Message = "WideScanじゃないとだめだよ！" };
				//Messenger.Default.Send(this, error_message);
			}

		}



		#region FitSpectrum


		public async Task FitAsync()
		{
			var fitting_task = Task.WhenAll(FittingCondition.FittingProfiles.Select(p => FitSingleProfile(p)));

			try
			{
				await fitting_task;
			}
			catch (Exception) { }

			if (fitting_task.Exception is AggregateException)
			{
				//var message = string.Join("\n", fitting_task.Exception.InnerExceptions.Select(ex => ex.Message));
				//Messenger.Default.Send(this, new SimpleMessage(this) { Message = message });
				return;
			}

		}

		// (0.1.0)1つのProfileについてのみフィッティングを行う．
		#region *1つのProfileに対してフィッティングを行う(FitSingleProfile)
		public async Task FitSingleProfile(FittingProfile profile)
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
		#endregion

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


		#region INotifyPropertyChanged実装
		protected void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
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


