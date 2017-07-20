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

					_cycles.Clear();
					for (int i = 0; i < CurrentROI.Data.Length; i++)
					{
						_cycles.Add(i);
					}
					NotifyPropertyChanged("Cycles");

					this.FittingCondition.Name = CurrentROI.Name;
					// 微分を考慮していない！
					this.FittingCondition.RangeBegin = CurrentROI.Parameter.Start;
					this.FittingCondition.RangeEnd = CurrentROI.Parameter.Stop;
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


		#region *Cyclesプロパティ
		/// <summary>
		/// 分析に含まれるLayerの一覧を取得します。設定は内部の_layersから行います。
		/// </summary>
		public IEnumerable<int> Cycles
		{
			get
			{
				return _cycles;
			}
		}
		List<int> _cycles = new List<int>();
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

		#region *コンストラクタ(DepthProfileViewModel)
		public DepthProfileViewModel()
		{
			_selectCsvDestinationCommand = new DelegateCommand(SelectCsvDestination_Executed);
			//_exportCsvCommand = new DelegateCommand(ExportCsv_Executed);
			_exportCsvCommand = new DelegateCommand(ExportCsv_Executed, ExportCsv_CanExecute);
			_selectChartDestinationCommand = new DelegateCommand(SelectChartDestination_Executed);
			_addReferenceSpectrumCommand = new DelegateCommand(AddReferenceSpectrum_Executed);
			_fitSpectrumCommand = new DelegateCommand(FitSpectrum_Executed);

			this.PropertyChanged += DepthProfileViewModel_PropertyChanged;
		}
		#endregion

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

		public async Task LoadFromAsync(string directory)
		{
			await _depthProfile.LoadFromAsync(directory);
			NotifyPropertyChanged("ROISpectraCollection");
		}


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
					FittingCondition.ReferenceSpectra.Add(new ReferenceSpectrum { DirectoryName = dir });
				}
				else
				{
					var error_message = new SimpleMessage(this) { Message = "WideScanじゃないとだめだよ！" };
					Messenger.Default.Send(this, error_message);
				}

			}

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
			var d_data = CurrentROI.Restrict(FittingCondition.RangeBegin, FittingCondition.RangeEnd)
						.Differentiate(3);



			IEnumerable<int> target_cycles;
			if (FitAll)
			{
				target_cycles = Cycles;
			}
			else
			{
				if (SelectedCycle.HasValue)
				{
					target_cycles = new int[] { SelectedCycle.Value };
				}
				else
				{
					throw new InvalidOperationException("Cycleを選択して下さい。");
				}
			}

			var tasks = new Dictionary<int, Task<Gnuplot>>();


			// ※フィッティングの計算と結果の出力をどう分けるか？
			//Parallel.ForEach(target_layers,
			//		(i) =>
			foreach (int i in target_cycles)
			{
				var task = FitOneCycle(i, FittingCondition.WithOffset, d_data.Data[i], d_data.Parameter);
				tasks.Add(i, task);
				//task.Start();
			}
			await Task.WhenAll(tasks.Values.ToArray());

			var charts = tasks.ToDictionary(pair => pair.Key, pair => pair.Value.Result);
			//Dictionary<int, Gnuplot> charts = new Dictionary<int, Gnuplot>();

			Range x_range = Range.Union(charts.Select(gnuplot => gnuplot.Value.XAxis.Range).ToArray());
			Range y_range = Range.Union(charts.Select(gnuplot => gnuplot.Value.YAxis.Range).ToArray());

			Parallel.ForEach(charts.Keys,
				async (i) =>
				{
					charts[i].SetXAxis(x_range);
					charts[i].SetYAxis(y_range);

					// pltファイルも出力してみる。
					using (var writer = new StreamWriter(GetCsvFileName(i) + ".plt"))
					{
						await charts[i].OutputPltFileAsync(writer);
					}
					// チャートを描画する。
					await charts[i].Draw();
				}
			);

		}

		// ※とりあえず、計算と出力をここでまとめて行う。将来的には分けたい？
		//async void FitOneLayer(int layer, EqualIntervalData data, ScanParameter originalParameter)
		async Task<Gnuplot> FitOneCycle(int cycle, bool with_offset, EqualIntervalData data, ScanParameter originalParameter)
		{
			// 固定参照スペクトルを取得する。
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

			/// フィッティング対象となるデータ。すなわち、もとのデータからFixされた分を差し引いたデータ。
			//var target_data = fixed_data.Count > 0 ? data.Substract(fixed_data) : data;
			// なんだけど、とりあえずはFixedを考慮しない。
			var target_data = data;

			FittingResult result;

			// A.最適なエネルギーシフト量を見つける場合
			if (!FittingCondition.FixEnergyShift)
			{
				#region エネルギーシフト量を決定する
				var gains = new Dictionary<decimal, Vector<double>>();
				Dictionary<decimal, decimal> residuals = new Dictionary<decimal, decimal>();
				for (int m = -6; m < 7; m++)
				{

					decimal shift = 0.5M * m; // とりあえず。

					var shifted_parameter = originalParameter.GetShiftedParameter(shift);


					// シフトされた参照スペクトルを読み込む。
					var standards = await LoadShiftedStandardsData(FittingCondition.ReferenceSpectra, shifted_parameter);
					//var standards = LoadShiftedStandardsData(ReferenceSpectra, originalParameter);

					// フィッティングを行い、
					Debug.WriteLine($"Cycle {cycle}");
					gains.Add(shift, GetOptimizedGains(with_offset, target_data, standards.ToArray()));
					for (int j = 0; j < FittingCondition.ReferenceSpectra.Count; j++)
					{
						Debug.WriteLine($"    {FittingCondition.ReferenceSpectra[j].Name} : {gains[shift][j]}");
					}
					Debug.WriteLine($"    Const : {gains[shift][FittingCondition.ReferenceSpectra.Count]}");

					// 残差を取得する。
					var residual = EqualIntervalData.GetTotalSquareResidual(target_data, gains[shift].ToArray(), standards.ToArray()); // 残差2乗和
					residuals.Add(shift, residual);
					Debug.WriteLine($"residual = {residual}");

				}

				// 最適なシフト値(仮)を決定。
				decimal best_shift = DecideBestShift(residuals);
				Debug.WriteLine($"最適なシフト値は {best_shift} だよ！");

				// その周辺を細かくスキャンする。
				for (int m = -4; m < 5; m++)
				{
					// シフト量を適当に設定する→mの最適値を求める→残差を求める
					decimal shift = best_shift + 0.1M * m;
					Debug.WriteLine($"shift = {shift}");
					if (!residuals.Keys.Contains(shift))
					{
						// ☆繰り返しなのでメソッド化したい。

						var shifted_parameter = originalParameter.GetShiftedParameter(shift);

						// シフトされた参照スペクトルを読み込む。
						var standards = await LoadShiftedStandardsData(FittingCondition.ReferenceSpectra, shifted_parameter);

						// フィッティングを行い、
						Debug.WriteLine($"Cycle {cycle}");
						gains.Add(shift, GetOptimizedGains(with_offset, target_data, standards.ToArray()));
						for (int j = 0; j < FittingCondition.ReferenceSpectra.Count; j++)
						{
							Debug.WriteLine($"    {FittingCondition.ReferenceSpectra[j].Name} : {gains[shift][j]}");
						}
						Debug.WriteLine($"    Const : {gains[shift][FittingCondition.ReferenceSpectra.Count]}");

						// 残差を取得する。
						var residual = EqualIntervalData.GetTotalSquareResidual(target_data, gains[shift].ToArray(), standards.ToArray()); // 残差2乗和
						residuals.Add(shift, residual);
						Debug.WriteLine($"residual = {residual}");

						// ☆ここまで。
					}
				}
				#endregion

				// 最適なシフト値を決定。
				best_shift = DecideBestShift(residuals);
				Debug.WriteLine($" {cycle} 本当に最適なシフト値は {best_shift} だよ！");



				// シフトされた参照スペクトルを読み込む。
				var best_shifted_parameter = originalParameter.GetShiftedParameter(best_shift);
				var best_standards = await LoadShiftedStandardsData(FittingCondition.ReferenceSpectra, best_shifted_parameter);
				var best_gains = gains[best_shift];

				result = new FittingResult { Shift = best_shift, Gains = best_gains.Select(d => Convert.ToDecimal(d)).ToArray(), Standards = best_standards };
			}
			else
			{
				// B.エネルギーシフト量を自分で与える場合

				var shifted_parameter = originalParameter.GetShiftedParameter(FittingCondition.FixedEnergyShift);

				// シフトされた参照スペクトルを読み込む。
				var standards = await LoadShiftedStandardsData(FittingCondition.ReferenceSpectra, shifted_parameter).ConfigureAwait(false);
				//var standards = LoadShiftedStandardsData(ReferenceSpectra, originalParameter);

				// フィッティングを行い、
				Debug.WriteLine($"Cycle {cycle}");
				var gains = GetOptimizedGains(with_offset, target_data, standards.ToArray());
				for (int j = 0; j < FittingCondition.ReferenceSpectra.Count; j++)
				{
					Debug.WriteLine($"    {FittingCondition.ReferenceSpectra[j].Name} : {gains[j]}");
				}
				Debug.WriteLine($"    Const : {gains[FittingCondition.ReferenceSpectra.Count]}");

				result = new FittingResult
				{
					Shift = FittingCondition.FixedEnergyShift,
					Standards = standards,
					Gains = gains.Select(d => Convert.ToDecimal(d)).ToArray()
				};
			}

			// 出力が少し後で行う！
			//await OutputFittedResult(layer, originalParameter, FittingCondition.ReferenceSpectra.Select(r => r.Name).ToList(),
			//						target_data, result);
			return await Fit(cycle, originalParameter, FittingCondition.ReferenceSpectra.Select(r => r.Name).ToList(),
									target_data, result).ConfigureAwait(false);


		}



		#region FittingResultクラス
		public class FittingResult
		{
			/// <summary>
			/// シフト値を取得／設定します。
			/// </summary>
			public decimal Shift { get; set; }
			public decimal[] Gains { get; set; }

			/// <summary>
			/// Gainを乗じる前の参照データ(Shiftは考慮済み)を取得／設定します。
			/// </summary>
			public List<List<decimal>> Standards { get; set; }

			public decimal GetGainedStandard(int standard, int position)
			{
				return Convert.ToDecimal(Gains[standard]) * Standards[standard][position];
			}
		}
		#endregion


		#region *フィットした結果をCSV形式で出力(OutputFittedCsv)
		private async Task OutputFittedCsv(StreamWriter writer, ScanParameter originalParameter, EqualIntervalData targetData, FittingResult result, bool outputConvolution)
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


		private async Task<Gnuplot> Fit(
			int cycle,
			ScanParameter originalParameter,
			IList<string> referenceNames,
			EqualIntervalData target_data,
			FittingResult result)
		{
			// フィッティングした結果をチャートにする？
			// ★とりあえずFixedなデータは表示しない。

			bool output_convolution = result.Standards.Count > 1;

			// それには、csvを出力する必要がある。
			//string fitted_csv_path = Path.Combine(FittingCondition.OutputDestination, $"{FittingCondition.Name}_{layer}.csv");
			using (var csv_writer = new StreamWriter(GetCsvFileName(cycle)))
			{
				await OutputFittedCsv(csv_writer, originalParameter, target_data, result, output_convolution).ConfigureAwait(false);
			}

			// チャート出力の準備？
			return ConfigureChart(cycle, result, referenceNames, output_convolution);

		}





		string GetCsvFileName(int cycle)
		{
			return Path.Combine(FittingCondition.OutputDestination, $"{FittingCondition.Name}_{cycle}.csv");
		}

		#region *チャートを設定(ConfigureChart)
		Gnuplot ConfigureChart(int cycle, FittingResult result, IList<string> referenceNames, bool outputConvolution)
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

			var chart_destination = Path.Combine(FittingCondition.OutputDestination, $"{FittingCondition.Name}_{cycle}{chart_ext}");

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

			var source_csv = GetCsvFileName(cycle);

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

			for (int j = 0; j < referenceNames.Count; j++)
			{

				gnuplot.DataSeries.Add(new LineChartSeries
				{
					SourceFile = source_csv,
					XColumn = 1,
					YColumn = j + 3,
					Title = $"{result.Gains[j].ToString("f3")} * {referenceNames[j]}",
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
					YColumn = referenceNames.Count + 3,
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


		/// <summary>
		/// シフトを考慮した標準試料データを読み込みます．
		/// </summary>
		/// <param name="references"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		async Task<List<List<decimal>>> LoadShiftedStandardsData(ICollection<ReferenceSpectrum> references, ScanParameter parameter)
		{
			List<List<decimal>> standards = new List<List<decimal>>();
			foreach (var item in references)
			{
				var ws = await WideScan.GenerateAsync(item.DirectoryName);
				standards.Add(
					ws.Differentiate(3)
						.GetInterpolatedData(parameter.Start, parameter.Step, parameter.PointsCount)
						.Select(d => d * ws.Parameter.NormalizationGain / parameter.NormalizationGain).ToList()
				);
			}
			return standards;
		}

		/// <summary>
		/// シフトを考慮した固定参照データを読み込みます．
		/// </summary>
		/// <param name="references"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		async Task<List<List<decimal>>> LoadShiftedFixedStandardsData(ICollection<FixedSpectrum> references, ScanParameter parameter)
		{
			List<List<decimal>> standards = new List<List<decimal>>();
			foreach (var item in references)
			{
				var ws = await WideScan.GenerateAsync(item.DirectoryName);
				standards.Add(
					ws.Differentiate(3)
						.GetInterpolatedData(parameter.Start - item.Shift, parameter.Step, parameter.PointsCount)
						.Select(d => d * ws.Parameter.NormalizationGain / parameter.NormalizationGain * item.Gain).ToList()
				);
			}
			return standards;
		}

		#region *最適なシフト量を決定(DecideBestShift)
		/// <summary>
		/// 残差データから，最適なシフト量を決定します．
		/// </summary>
		/// <param name="residuals"></param>
		/// <returns></returns>
		decimal DecideBestShift(Dictionary<decimal, decimal> residuals)
		{
			// ↓これでいいのかなぁ？
			// return residuals.First(r => r.Value == residuals.Values.Min()).Key;

			KeyValuePair<decimal, decimal>? best = null;
			foreach (var residual in residuals)
			{
				if (!best.HasValue || best.Value.Value > residual.Value)
				{
					best = residual;
				}
			}
			return best.Value.Key;
		}
		#endregion


		/// <summary>
		/// 残差2乗和を求めてそれを返します。
		/// </summary>
		/// <param name="data">測定データ．</param>
		/// <param name="reference">(一般には複数の)参照データ．</param>
		/// <returns></returns>
		decimal CulculateResidual(bool with_offset, IList<decimal> data, params IList<decimal>[] references)	// C# 6.0 で，params引数に配列型以外も使えるようになった．
		{
			// 2.最適値なゲイン係数(+オフセット値)を求める
			var gains = GetOptimizedGains(with_offset, data, references);

			// 3.残差を求める
			var residual = EqualIntervalData.GetTotalSquareResidual(data, gains.ToArray(), references); // 残差2乗和
			Debug.WriteLine($"residual = {residual}");

			return residual;
		}

/*
		/// <summary>
		/// 最適なゲイン係数を配列として返します。
		/// </summary>
		/// <param name="data">測定データ．</param>
		/// <param name="references">(一般には複数の)参照データ．</param>
		/// <returns></returns>
		public static Vector<double> GetOptimizedGains(IList<decimal> data, params IList<decimal>[] references)
		{
			int n = references.Length;

			// これdecimalではできないのかな？
			var a = DenseMatrix.Create(n, n, 0);
			var b = DenseVector.Create(n, 0);

			// a[p,q] = Σ (references[p][*] * references[q][*])
			// b[p] = Σ (references[p][*] * data[*])

			for (int i = 0; i < data.Count; i++)
			{
				for (int p = 0; p < n; p++)
				{
					for (int q = p; q < n; q++)
					{
						//Debug.WriteLine($"{data[i]},{reference[i]}");
						a[p, q] += Convert.ToDouble(references[p][i] * references[q][i]);
					}
					b[p] += Convert.ToDouble(references[p][i] * data[i]);
				}
			}

			for (int p = 0; p < n; p++)
			{
				for (int q = p + 1; q < n; q++)
				{
					a[q, p] += a[p, q];
				}
			}


			Vector<double> result = null;
			bool retry_flag = true;
			while (retry_flag)
			{
				retry_flag = false;
				result = a.Inverse() * b;

				// resultに負の値があったらやり直す。
				for (int i = 0; i < result.Count; i++)
				{
					if (result[i] < 0)
					{
						retry_flag = true;
						// i行とi列をゼロベクトルにする。
						for (int j = 0; j < a.ColumnCount; j++)
						{
							a[i, j] = 0;
							a[j, i] = 0;
						}
						a[i, i] = 1;
						b[i] = 0;
					}
				}
			}
			return result;

		}
*/
		/// <summary>
		/// 最適なゲイン係数＋オフセット定数を配列として返します。
		/// </summary>
		/// <param name="data">測定データ．</param>
		/// <param name="references">(一般には複数の)参照データ．</param>
		/// <returns></returns>
		public static Vector<double> GetOptimizedGains(bool with_offset, IList<decimal> data, params IList<decimal>[] references)
		{
			int n = references.Length;
			int m = n + 1;

			// これdecimalではできないのかな？
			var a = DenseMatrix.Create(m, m, 0);
			var b = DenseVector.Create(m, 0);

			// p<n, q<n とすると，
			// a[p,q] = Σ (references[p][*] * references[q][*])
			// a[p,n] = Σ references[p][*]
			// a[n,n] = data.Count
			// b[p] = Σ (references[p][*] * data[*])
			// b[n] = Σ data[*]

			for (int i = 0; i < data.Count; i++)
			{
				for (int p = 0; p < m; p++)
				{
					for (int q = p; q < m; q++)
					{
						//Debug.WriteLine($"{data[i]},{reference[i]}");
						if (q == n)
						{
							if (with_offset && p != n)
							{
								a[p, n] += Convert.ToDouble(references[p][i]);
							}
						}
						else
						{
							a[p, q] += Convert.ToDouble(references[p][i] * references[q][i]);
						}
					}
					if (with_offset && p == n)
					{
						b[p] += Convert.ToDouble(data[i]);
					}
					else
					{
						b[p] += Convert.ToDouble(references[p][i] * data[i]);
					}
				}
			}

			for (int p = 0; p < m; p++)
			{
				for (int q = p + 1; q < m; q++)
				{
					a[q, p] += a[p, q];
				}
			}
			a[n, n] = data.Count;	// 0ではないはずなので，with_offset = falseの場合でもaがregularにならずに困ることはない．

			Vector<double> result = null;
			bool retry_flag = true;
			while (retry_flag)
			{
				retry_flag = false;
				result = a.Inverse() * b;

				// 定数項以外にresultに負の値があったらやり直す。
				for (int i = 0; i < result.Count - 1; i++)
				{
					if (result[i] < 0)
					{
						retry_flag = true;
						// i行とi列をゼロベクトルにする。
						for (int j = 0; j < a.ColumnCount; j++)
						{
							a[i, j] = 0;
							a[j, i] = 0;
						}
						a[i, i] = 1;
						b[i] = 0;
					}
				}
			}
			return result;

		}

	}
	#endregion

}
