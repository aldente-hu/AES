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

					_layers.Clear();
					for (int i = 0; i < CurrentROI.Data.Length; i++)
					{
						_layers.Add(i);
					}
					NotifyPropertyChanged("Layers");

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


		#region *Layersプロパティ
		/// <summary>
		/// 分析に含まれるLayerの一覧を取得します。設定は内部の_layersから行います。
		/// </summary>
		public IEnumerable<int> Layers
		{
			get
			{
				return _layers;
			}
		}
		List<int> _layers = new List<int>();
		#endregion

		// この2つはFittingConditionに入れた方がいいのかな？
		#region *SelectedLayerプロパティ
		public int? SelectedLayer
		{
			get
			{
				return _selectedLayer;
			}
			set
			{
				if (SelectedLayer != value)
				{
					_selectedLayer = value;
					NotifyPropertyChanged();
				}
			}
		}
		int? _selectedLayer = null;
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

		private void DepthProfileViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Layers":
					if (this.SelectedLayer.HasValue && !this.Layers.Contains(this.SelectedLayer.Value))
					{
						this.SelectedLayer = null;
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

		// parameterで数値が渡されれば、1サイクルに対して解析を行う。
		// さもなければ、全サイクルに対して解析を行う。
		async void FitSpectrum_Executed(object parameter)
		{
			var d_data = CurrentROI.Restrict(FittingCondition.RangeBegin, FittingCondition.RangeEnd)
						.Differentiate(3);



			IEnumerable<int> target_layers;
			if (FitAll)
			{
				target_layers = Layers;
			}
			else
			{
				if (SelectedLayer.HasValue)
				{
					target_layers = new int[] { SelectedLayer.Value };
				}
				else
				{
					throw new InvalidOperationException("Layerを選択して下さい。");
				}
			}

			// ※フィッティングの計算と結果の出力をどう分けるか？
			Parallel.ForEach(target_layers,
					i => FitOneLayer(i, d_data.Data[i], d_data.Parameter)
			);


			// これ以降は，WideScanのFit_Executeをコピペしただけ．


			// 参照スペクトルを取得する．

			// 参照スペクトルのデータを，測定データの範囲に制限し，ピッチも測定データに合わせる．
			// →と考えたが，これはいずれもシフト値によって変わることに注意！

			// リファレンスをどう用意するか？
			//var references = await FittingCondition.ReferenceSpectra.ForEachAsync(sp => sp.GetDataAsync(d_data.Parameter, 3), 10);

			//var result = Fitting.WithConstant(d_data.Data[cycle], references);

			// とりあえず簡単に結果を出力する．
			/*
			string destination = Path.Combine(Path.GetDirectoryName(FittingCondition.OutputDestination), "result.txt");
			using (var writer = new StreamWriter(destination, false))
			{
				for (int i = 0; i < result.Factors.Count; i++)
				{
					writer.WriteLine($"Factor{i}  :  {result.Factors[i]}");
				}
				writer.WriteLine($"residual  :  {result.Residual}");
			}
			*/

			//FitOneLayer(0, d_data.Data, d_data.Parameter, WideFittingModel.ReferenceSpectra, fixed_data,
			//	WideFittingModel.OutputDestination,
			//	"Wide");

		}

		// ※とりあえず、計算と出力をここでまとめて行う。将来的には分けたい？
		async void FitOneLayer(int layer, EqualIntervalData data, ScanParameter originalParameter)
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

			// A.最適なエネルギーシフト量を見つける場合

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
				Debug.WriteLine($"Layer {layer}");
				gains.Add(shift, GetOptimizedGainsWithOffset(target_data, standards.ToArray()));
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
					Debug.WriteLine($"Layer {layer}");
					gains.Add(shift, GetOptimizedGainsWithOffset(target_data, standards.ToArray()));
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
			Debug.WriteLine($" {layer} 本当に最適なシフト値は {best_shift} だよ！");


			// シフトされた参照スペクトルを読み込む。
			var best_shifted_parameter = originalParameter.GetShiftedParameter(best_shift);
			var best_standards = await LoadShiftedStandardsData(FittingCondition.ReferenceSpectra, best_shifted_parameter);
			var best_gains = gains[best_shift];

			var result = new FittingResult { Shift = best_shift, Gains = best_gains.Select(d => Convert.ToDecimal(d)).ToArray(), Standards = best_standards };

			/*
			 * 
			// B.エネルギーシフト量を自分で与える場合

			var best_shift = -0.5M;
			
			var best_shifted_parameter = originalParameter.GetShiftedParameter(best_shift);
			var best_standards = LoadShiftedStandardsData(referenceSpectra, best_shifted_parameter);
			var best_gains = GetOptimizedGains(target_data, best_standards.ToArray());
			*/

			await OutputFittedResult(layer, originalParameter, FittingCondition.ReferenceSpectra.Select(r => r.Name).ToList(),
									target_data, result);


		}

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

		private async Task OutputFittedResult(
					int layer,
					ScanParameter originalParameter,
					IList<string> referenceNames,
					EqualIntervalData target_data,
					FittingResult result)
		{
			// フィッティングした結果をチャートにする？
			// ★とりあえずFixedなデータは表示しない。

			bool output_convolution = result.Standards.Count > 1;

			// それには、csvを出力する必要がある。
			string fitted_csv_path = Path.Combine(FittingCondition.OutputDestination, $"{FittingCondition.Name}_{layer}.csv");
			using (var csv_writer = new StreamWriter(fitted_csv_path))
			{
				await OutputFittedCsv(csv_writer, originalParameter, target_data, result, output_convolution);
			}

			// チャート出力？

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

			var chart_destination = Path.Combine(FittingCondition.OutputDestination, $"{FittingCondition.Name}_{layer}{chart_ext}");

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
				Title = $"Cycle {layer} , Shift {result.Shift} eV"
			};

			gnuplot.DataSeries.Add(new LineChartSeries
			{
				SourceFile = fitted_csv_path,
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
					SourceFile = fitted_csv_path,
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

			if (output_convolution)
			{
				gnuplot.DataSeries.Add(new LineChartSeries
				{
					SourceFile = fitted_csv_path,
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

			// pltファイルも出力してみる。
			using (var writer = new StreamWriter(chart_destination + ".plt"))
			{
				await gnuplot.OutputPltFileAsync(writer);
			}
			// チャートを描画する。
			await gnuplot.Draw();
		}


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

		/// <summary>
		/// 残差2乗和を求めてそれを返します。
		/// </summary>
		/// <param name="data"></param>
		/// <param name="reference"></param>
		/// <returns></returns>
		decimal CulculateResidual(IList<decimal> data, IList<decimal> reference)
		{
			// 2.mの最適値を求める
			var m = Convert.ToDecimal(GetOptimizedGains(data, reference)[0]);
			Debug.WriteLine($"m = {m}");
			// たとえば負の値になる要素があった場合とか、そのまま使っていいのかなぁ？

			// 3.残差を求める
			var residual = EqualIntervalData.GetTotalSquareResidual(data, reference, m); // 残差2乗和
			Debug.WriteLine($"residual = {residual}");

			return residual;
		}

		/// <summary>
		/// 残差2乗和を求めてそれを返します。
		/// </summary>
		/// <param name="data"></param>
		/// <param name="reference"></param>
		/// <returns></returns>
		decimal CulculateResidual(IList<decimal> data, params IList<decimal>[] references)
		{
			// 2.mの最適値を求める
			var gains = GetOptimizedGains(data, references);
			//Debug.WriteLine($"m = {m}");

			// 3.残差を求める
			var residual = EqualIntervalData.GetTotalSquareResidual(data, gains.ToArray(), references); // 残差2乗和
			Debug.WriteLine($"residual = {residual}");

			return residual;
		}



		/*
		public static decimal GetOptimizedGain(IList<decimal> data, IList<decimal> reference)
		{
			decimal numerator = 0;  // 分子
			decimal denominator = 0;	// 分母

			for(int i=0; i < data.Count; i++)
			{
				//Debug.WriteLine($"{data[i]},{reference[i]}");
				numerator += data[i] * reference[i];
				denominator += reference[i] * reference[i];
			}
			return numerator / denominator;
		}
		*/

		/// <summary>
		/// 最適なゲイン係数を配列として返します。
		/// </summary>
		/// <param name="data"></param>
		/// <param name="reference1"></param>
		/// <param name="reference2"></param>
		/// <returns></returns>
		public static Vector<double> GetOptimizedGains(IList<decimal> data, params IList<decimal>[] references)
		{
			int n = references.Length;

			// これdecimalではできないのかな？
			var a = DenseMatrix.Create(n, n, 0);
			var b = DenseVector.Create(n, 0);

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

		/// <summary>
		/// 最適なゲイン係数＋オフセット定数を配列として返します。
		/// </summary>
		/// <param name="data"></param>
		/// <param name="reference1"></param>
		/// <param name="reference2"></param>
		/// <returns></returns>
		public static Vector<double> GetOptimizedGainsWithOffset(IList<decimal> data, params IList<decimal>[] references)
		{
			int n = references.Length;
			int m = n + 1;

			// これdecimalではできないのかな？
			var a = DenseMatrix.Create(m, m, 0);
			var b = DenseVector.Create(m, 0);

			for (int i = 0; i < data.Count; i++)
			{
				for (int p = 0; p < m; p++)
				{
					for (int q = p; q < m; q++)
					{
						//Debug.WriteLine($"{data[i]},{reference[i]}");
						if (q == n)
						{
							if (p != n)
							{
								a[p, n] += Convert.ToDouble(references[p][i]);
							}
						}
						else
						{
							a[p, q] += Convert.ToDouble(references[p][i] * references[q][i]);
						}
					}
					if (p == n)
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
			a[n, n] = data.Count;

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

}
