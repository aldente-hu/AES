using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Diagnostics;

using System.IO;
using System.Collections.ObjectModel;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}


		private void buttonTest_Click(object sender, RoutedEventArgs e)
		{
			OpenJampData();
		}

		public void OpenJampData()
		{
			var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "idファイル(id)|id" };
			if (dialog.ShowDialog() == true)
			{
				var id_file = dialog.FileName;
				var dir = System.IO.Path.GetDirectoryName(id_file);

				switch(Data.IdFile.CheckType(id_file))
				{
					case Data.DataType.WideScan:
						tabControlData.SelectedIndex = 0;
						//TestOpenWideScan(dir, false);
						_wideScanData = new Data.WideScan(dir);
						break;
					case Data.DataType.DepthProfile:
						tabControlData.SelectedIndex = 1;
						_depthProfileData = new Data.DepthProfile(dir);

						// リストボックスを設定する。
						foreach (var element in _depthProfileData.Spectra.Keys)
						{
							comboBoxElement.Items.Add(element);
						}
						int n = _depthProfileData.Spectra.First().Value.Data.Length;
						for(int i = 0; i<n; i++)
						{
							comboBoxLayers.Items.Add(i);
						}

						//TestOpenDepthProfile(dir, true);
						break;
				}

			}
		}


		#region Wide関連

		Data.WideScan _wideScanData;

		private void buttonWideOutput_Click(object sender, RoutedEventArgs e)
		{
			if (checkBoxRaw.IsChecked.Value || checkBoxDiff.IsChecked.Value)
			{
				var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "csvファイル(*.csv)|*.csv" };
				if (dialog.ShowDialog() == true)
				{
					var raw_file_name = dialog.FileName;
					if (checkBoxRaw.IsChecked == true)
					{
						using (var writer = new StreamWriter(raw_file_name, false))
						{
							_wideScanData.ExportCsv(writer);
						}
					}
					if (checkBoxDiff.IsChecked == true)
					{
						var diff_file_name = Path.Combine(Path.GetDirectoryName(raw_file_name),
							Path.GetFileNameWithoutExtension(raw_file_name) + "_diff" + Path.GetExtension(raw_file_name)
						);
						using (var writer = new StreamWriter(diff_file_name, false))
						{
							_wideScanData.Differentiate(3).ExportCsv(writer);
						}
					}
				}
			}else
			{
				MessageBox.Show("チェックボックスにチェックがないから、何も出力しないよ！");
			}
		}


		#endregion



		private void buttonOutputDepth_Click(object sender, RoutedEventArgs e)
		{
			if (comboBoxElement.SelectedIndex >= 0)
			{
				var csv_destination = labelOutputDepthCsvDestination.Content.ToString();
				var chart_destination = labelOutputDepthChartDestination.Content.ToString();

				using (var writer = new StreamWriter(csv_destination))
				{
					_depthProfileData.Spectra[(string)comboBoxElement.SelectedItem].Differentiate(3).ExportCsv(writer);
				}
				DisplayDepthChart(csv_destination, chart_destination, ChartFormat.Png);

			}
			else
			{
				MessageBox.Show("元素を選んでから来てください。");
			}
		}

		private async void buttonOutputPlt_Click(object sender, RoutedEventArgs e)
		{
			// Pltファイルを出力する。
			var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "pltファイル(*.plt)|*.plt" };
			if (dialog.ShowDialog() == true)
			{
				var csv_destination = labelOutputDepthCsvDestination.Content.ToString();
				var chart_destination = labelOutputDepthChartDestination.Content.ToString();

				using (var writer = new StreamWriter(dialog.FileName))
				{
					await WritePltCommands(writer, csv_destination, chart_destination, ChartFormat.Png);
				}
			}
		}

		private void buttonSelectOutputDepthCsvDestination_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "CSVファイル(*.csv)|*.csv" };
			if (dialog.ShowDialog() == true)
			{
				labelOutputDepthCsvDestination.Content = dialog.FileName;
			}
		}

		private void buttonSelectOutputDepthChartDestination_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "PNGファイル(*.png)|*.png" };
			if (dialog.ShowDialog() == true)
			{
				labelOutputDepthChartDestination.Content = dialog.FileName;
			}
		}


		#region DepthProfile関連

		Data.DepthProfile _depthProfileData;

		#region *DepthProfileのチャートを表示(DisplayDepthChart)
		async void DisplayDepthChart(string source, string destination, ChartFormat format)
		{
			var gnuplot = new Gnuplot
			{
				Format = format,
				Width = 800,
				Height = 600,
				FontSize = 20,
				Destination = destination,
				XTitle = "K.E. / eV",
				YTitle = "Intensity"
			};

			gnuplot.DataSeries.Add(new LineChartSeries
			{
				SourceFile = source,
				XColumn = 1,
				YColumn = 2,
				Title="Layer 0",
				Style = new LineChartSeriesStyle(LineChartStyle.Lines)
				{
					Style = new LinePointStyle
					{
						LineColor = "#FF0000",
						LineWidth = 3,
					}
				}
			});
			gnuplot.DataSeries.Add(new LineChartSeries
			{
				SourceFile = source,
				XColumn = 1,
				YColumn = 3,
				Title = "Layer 1",
				Style = new LineChartSeriesStyle(LineChartStyle.Lines)
				{
					Style = new LinePointStyle
					{
						LineColor = "#CC0000",
						LineWidth = 2,
					}
				}
			});
			gnuplot.DataSeries.Add(new LineChartSeries
			{
				SourceFile = source,
				XColumn = 1,
				YColumn = 4,
				Title = "Layer 2",
				Style = new LineChartSeriesStyle(LineChartStyle.Lines)
				{
					Style = new LinePointStyle
					{
						LineColor = "#AA0000"
					}
				}
			});


			await gnuplot.Draw();
			imageChart.Source = new BitmapImage(new Uri(destination));

		}
		#endregion

		#region *gnuplotのコマンドを出力(WritePltCommand)
		async Task WritePltCommands(TextWriter writer, string source, string chart_destination, ChartFormat format)
		{
			var gnuplot = new Gnuplot
			{
				Format = format,
				Width = 800,
				Height = 600,
				FontSize = 14,
				Destination = chart_destination
			};

			gnuplot.DataSeries.Add(new LineChartSeries
			{
				SourceFile = source,
				XColumn = 1,
				YColumn = 2,
				Style = new LineChartSeriesStyle(LineChartStyle.Lines)
				{
					Style = new LinePointStyle
					{
						LineColor = "#FF0000"
					}
				}
			});

			gnuplot.DataSeries.Add(new LineChartSeries
			{
				SourceFile = source,
				XColumn = 1,
				YColumn = 3,
				Style = new LineChartSeriesStyle(LineChartStyle.Lines)
				{
					Style = new LinePointStyle
					{
						LineColor = "#CC0000"
					}
				}
			});

			await gnuplot.OutputPltFileAsync(writer);

		}
		#endregion

		#endregion

		private void buttonSelectStandardSpectrum_Click(object sender, RoutedEventArgs e)
		{
			// このボタンから選択するのがいいのかどうかはわからない。





		}


		private void buttonPeakShift_Click(object sender, RoutedEventArgs e)
		{
			// 参照スペクトルを読み込む。
			var zro2_standard_dir = labelStandardSpectrum.Content.ToString();
			var zro2_standard = new Data.WideScan(zro2_standard_dir).Differentiate(3);

			// シフト量を求めてみる。
			Dictionary<decimal, decimal> residuals = new Dictionary<decimal, decimal>();
			var data = _depthProfileData.Spectra[(string)comboBoxElement.SelectedItem].Differentiate(3);
			for (int i = -19; i < 20; i++)
			{
				// シフト量を適当に設定する→mの最適値を求める→残差を求める
				decimal shift = 0.5M * i;
				Debug.WriteLine($"shift = {shift}");

				var spec = data.Shift(shift);
				var reference = zro2_standard.GetInterpolatedData(spec.Parameter.Start, spec.Parameter.Step, spec.Parameter.PointsCount);

				residuals.Add(shift, CulculateResidual(spec.Data[0], reference));
			}

			// 最適なシフト値(仮)を決定。
			decimal best_shift = DecideBestShift(residuals);
			MessageBox.Show($"最適なシフト値は {best_shift} だよ！");

			// その周辺を細かくスキャンする。
			for (int i = -4; i < 5; i++)
			{
				// シフト量を適当に設定する→mの最適値を求める→残差を求める
				decimal shift = best_shift + 0.1M * i;
				Debug.WriteLine($"shift = {shift}");
				if (!residuals.Keys.Contains(shift))
				{
					var spec = data.Shift(shift);
					var reference = zro2_standard.GetInterpolatedData(spec.Parameter.Start, spec.Parameter.Step, spec.Parameter.PointsCount);

					residuals.Add(shift, CulculateResidual(spec.Data[0], reference));
				}
			}

			// 最適なシフト値を決定。
			best_shift = DecideBestShift(residuals);
			MessageBox.Show($"本当に最適なシフト値は {best_shift} だよ！");
			sliderEnergyShift.Value = Convert.ToDouble(best_shift);

			// シフト後のcsvを出力しておく？
			/*
			using (var writer = new StreamWriter(@"B:\shifted_tanuki.csv"))
			{
				data.Shift(best_shift).ExportCsv(writer);
			}
			*/

		}

		decimal DecideBestShift(Dictionary<decimal, decimal> residuals)
		{
			// ↓これでいいのかなぁ？
			// return residuals.First(r => r.Value == residuals.Values.Min()).Key;

			KeyValuePair<decimal, decimal>? best = null;
			foreach(var residual in residuals)
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
			var residual = Data.EqualIntervalData.GetTotalSquareResidual(data, reference, m); // 残差2乗和
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
			var residual = Data.EqualIntervalData.GetTotalSquareResidual(data, gains.ToArray(), references); // 残差2乗和
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
					a[q, p] += a[p,q];
				}
			}

			var a_inv = a.Inverse();



			var result = a_inv * b;
			bool retry_flag = true;
			while (retry_flag)
			{
				retry_flag = false;
				result = a_inv * b;

				// resultに負の値があったらやり直す。
				for (int i = 0; i < result.Count; i++)
				{
					if (result[i] < 0)
					{
						retry_flag = true;
						// i行をゼロベクトルにする。
						for (int j = 0; j < a_inv.ColumnCount; j++)
						{
							a_inv[i, j] = 0;
						}
					}
				}
			}
			return result;

		}
		
		private async void buttonInvestigateSpectrum_Click(object sender, RoutedEventArgs e)
		{

			// 参照スペクトルを読み込む。
			var zro2_standard = new Data.WideScan(labelStandardSpectrum.Content.ToString()).Differentiate(3);
			var zr_standard = new Data.WideScan(labelStandardSpectrum2.Content.ToString()).Differentiate(3);

			var data = _depthProfileData.Spectra[(string)comboBoxElement.SelectedItem].Differentiate(3).Shift(Convert.ToDecimal(sliderEnergyShift.Value));

			var reference_zro2 = zro2_standard.GetInterpolatedData(data.Parameter.Start, data.Parameter.Step, data.Parameter.PointsCount);
			var reference_zr = zr_standard.GetInterpolatedData(data.Parameter.Start, data.Parameter.Step, data.Parameter.PointsCount);

			for (int i = 0; i < data.Data.Count(); i++)
			{
				var layer_data = data.Data[i];
				Debug.WriteLine($"Layer {i}");
				var gains = GetOptimizedGains(layer_data, reference_zro2, reference_zr);
				Debug.WriteLine($"ZrO2 : {gains[0]},   Zr : {gains[1]}");


				// フィッティングした結果をチャートにする？


				// それには、csvを出力する必要がある。
				string fitted_csv_path = $@"B:\fitted_{i}.csv";
				using (var csv_writer = new StreamWriter(fitted_csv_path))
				{
					for (int j = 0; j < data.Parameter.PointsCount; j++)
					{
						csv_writer.WriteLine(
							string.Join(",",
								new string[] {
									(data.Parameter.Start + j * data.Parameter.Step).ToString("f2"),
									layer_data[j].ToString("f3"),
									(Convert.ToDecimal(gains[0])*reference_zro2[j]).ToString("f3"),
									(Convert.ToDecimal(gains[1])*reference_zr[j]).ToString("f3")
								}
							)
						);
					}
				}

				// チャート出力
				var chart_destination = $@"B:\tanuki_{i}.png";
				#region チャート設定
				var gnuplot = new Gnuplot
				{
					Format = ChartFormat.Png,
					Width = 800,
					Height = 600,
					FontSize = 20,
					Destination = chart_destination,
					XTitle = "K.E. / eV",
					YTitle = "Intensity",
					Title = $"Layer {i}"
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
				gnuplot.DataSeries.Add(new LineChartSeries
				{
					SourceFile = fitted_csv_path,
					XColumn = 1,
					YColumn = 3,
					Title = $"{gains[0].ToString("f3")} * ZrO2",
					Style = new LineChartSeriesStyle(LineChartStyle.Lines)
					{
						Style = new LinePointStyle
						{
							LineColor = "#33CC33",
							LineWidth = 2,
						}
					}
				});
				gnuplot.DataSeries.Add(new LineChartSeries
				{
					SourceFile = fitted_csv_path,
					XColumn = 1,
					YColumn = 4,
					Title = $"{gains[1].ToString("f3")} * Zr",
					Style = new LineChartSeriesStyle(LineChartStyle.Lines)
					{
						Style = new LinePointStyle
						{
							LineColor = "#3333CC",
							LineWidth = 2
						}
					}
				});

				#endregion
				await gnuplot.Draw();

			}

		}



		#region 標準スペクトル(新)関連

		public ObservableCollection<ReferenceSpectrum> ReferenceSpectra
		{ get
			{
				return _refSpectra;
			}
		}
		ObservableCollection<ReferenceSpectrum> _refSpectra = new ObservableCollection<ReferenceSpectrum>();

		public ObservableCollection<FixedSpectrum> FixedSpectra
		{
			get
			{
				return _fixedSpectra;
			}
		}
		ObservableCollection<FixedSpectrum> _fixedSpectra = new ObservableCollection<FixedSpectrum>();

		public DepthProfileSetting DepthProfileSetting
		{
			get
			{
				return _depthProfileSetting;
			}
		}
		DepthProfileSetting _depthProfileSetting = new DepthProfileSetting();

		private void buttonSelectDepthOutputDestination_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.SaveFileDialog { DefaultExt = ".csv" };
			if (dialog.ShowDialog() == true)
			{
				DepthProfileSetting.OutputDestination = Path.GetDirectoryName(dialog.FileName);
			}

		}


		// とりあえずここに置いておく。
		public static RoutedCommand SeparateSpectrumCommand = new RoutedCommand();

		private void SeparateSpectrum_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var d_data = _depthProfileData.Spectra[(string)comboBoxElement.SelectedItem]
											.Restrict(DepthProfileSetting.RangeStart, DepthProfileSetting.RangeStop)
											.Differentiate(3);

			// 固定参照スペクトルを取得する。
			List<decimal> fixed_data = new List<decimal>();
			if (FixedSpectra.Count > 0)
			{
				var v_data = LoadShiftedFixedStandardsData(FixedSpectra, d_data.Parameter);
				for (int j = 0; j < v_data.First().Count; j++)
				{
					fixed_data.Add(v_data.Sum(one => one[j]));
				}
			}

			if (radioButtonFitAll.IsChecked == true)
			{
				// これをパラレルに行う。
				Parallel.For(0, d_data.Data.Length,
					i => FitOneLayer(i, d_data.Data[i], d_data.Parameter, fixed_data)
				);
			}
			else
			{
				int i = (int)comboBoxLayers.SelectedItem;
				FitOneLayer(i, d_data.Data[i], d_data.Parameter, fixed_data);
			}

		}

		List<List<decimal>> LoadShiftedStandardsData(ICollection<ReferenceSpectrum> references, Data.ScanParameter parameter)
		{
			List<List<decimal>> standards = new List<List<decimal>>();
			foreach (var item in references)
			{
				var ws = new Data.WideScan(item.DirectoryName);
				standards.Add(
					ws.Differentiate(3)
						.GetInterpolatedData(parameter.Start, parameter.Step, parameter.PointsCount)
						.Select(d => d * ws.Parameter.NormalizationGain / parameter.NormalizationGain).ToList()
				);
			}
			return standards;
		}

		List<List<decimal>> LoadShiftedFixedStandardsData(ICollection<FixedSpectrum> references, Data.ScanParameter parameter)
		{
			List<List<decimal>> standards = new List<List<decimal>>();
			foreach (var item in references)
			{
				var ws = new Data.WideScan(item.DirectoryName);
				standards.Add(
					ws.Differentiate(3)
						.GetInterpolatedData(parameter.Start - item.Shift, parameter.Step, parameter.PointsCount)
						.Select(d => d * ws.Parameter.NormalizationGain / parameter.NormalizationGain * item.Gain).ToList()
				);
			}
			return standards;
		}

		private async void FitOneLayer(int layer, Data.EqualIntervalData data, Data.ScanParameter originalParameter, List<decimal> fixed_data)
		{

			var target_data = fixed_data.Count > 0 ? data.Substract(fixed_data) : data;
			

			var gains = new Dictionary<decimal, Vector<double>>();
			Dictionary<decimal, decimal> residuals = new Dictionary<decimal, decimal>();
			for (int m = -6; m < 7; m++)
			{

				decimal shift = 0.5M * m; // とりあえず。

				var shifted_parameter = originalParameter.GetShiftedParameter(shift);


				// シフトされた参照スペクトルを読み込む。
				var standards = LoadShiftedStandardsData(ReferenceSpectra, shifted_parameter);

				// フィッティングを行い、
				Debug.WriteLine($"Layer {layer}");
				gains.Add(shift, GetOptimizedGains(target_data, standards.ToArray()));
				for (int j = 0; j < gains[shift].Count; j++)
				{
					Debug.WriteLine($"    {ReferenceSpectra[j].Name} : {gains[shift][j]}");
				}

				// 残差を取得する。
				var residual = Data.EqualIntervalData.GetTotalSquareResidual(target_data, gains[shift].ToArray(), standards.ToArray()); // 残差2乗和
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
					var standards = LoadShiftedStandardsData(ReferenceSpectra, shifted_parameter);

					// フィッティングを行い、
					Debug.WriteLine($"Layer {layer}");
					gains.Add(shift, GetOptimizedGains(target_data, standards.ToArray()));
					for (int j = 0; j < gains[shift].Count; j++)
					{
						Debug.WriteLine($"    {ReferenceSpectra[j].Name} : {gains[shift][j]}");
					}

					// 残差を取得する。
					var residual = Data.EqualIntervalData.GetTotalSquareResidual(target_data, gains[shift].ToArray(), standards.ToArray()); // 残差2乗和
					residuals.Add(shift, residual);
					Debug.WriteLine($"residual = {residual}");

					// ☆ここまで。
				}
			}


			// 最適なシフト値を決定。
			best_shift = DecideBestShift(residuals);
			Debug.WriteLine($" {layer} 本当に最適なシフト値は {best_shift} だよ！");
			var best_gains = gains[best_shift];


			// シフトされた参照スペクトルを読み込む。
			var best_shifted_parameter = originalParameter.GetShiftedParameter(best_shift);
			var best_standards = LoadShiftedStandardsData(ReferenceSpectra, best_shifted_parameter);


			// フィッティングした結果をチャートにする？
			// ★とりあえずFixedなデータは表示しない。

			// それには、csvを出力する必要がある。
			string fitted_csv_path = Path.Combine(DepthProfileSetting.OutputDestination, $"{DepthProfileSetting.Name}_{layer}.csv");
			using (var csv_writer = new StreamWriter(fitted_csv_path))
			{
				for (int k = 0; k < originalParameter.PointsCount; k++)
				{
					List<string> cols = new List<string>();
					cols.Add((originalParameter.Start + k * originalParameter.Step + best_shift).ToString("f2"));
					cols.Add(target_data[k].ToString("f3"));
					for (int j = 0; j < best_gains.Count; j++)
					{
						cols.Add((Convert.ToDecimal(best_gains[j]) * best_standards[j][k]).ToString("f3"));
					}
					csv_writer.WriteLine(string.Join(",", cols));
				}
			}

			// チャート出力？
			var chart_destination = Path.Combine(DepthProfileSetting.OutputDestination, $"{DepthProfileSetting.Name}_{layer}.png");
			#region チャート設定
			var gnuplot = new Gnuplot
			{
				Format = ChartFormat.Png,
				Width = 800,
				Height = 600,
				FontSize = 20,
				Destination = chart_destination,
				XTitle = "Energy / eV",
				YTitle = "Intensity",
				Title = $"Layer {layer}"
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

			for (int j = 0; j < best_gains.Count; j++)
			{

				gnuplot.DataSeries.Add(new LineChartSeries
				{
					SourceFile = fitted_csv_path,
					XColumn = 1,
					YColumn = j + 3,
					Title = $"{best_gains[j].ToString("f3")} * {ReferenceSpectra[j].Name}",
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
			#endregion
			await gnuplot.Draw();
			
		}

		private void buttonAddReference_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "idファイル(id)|id" };
			if (dialog.ShowDialog() == true)
			{
				var id_file = dialog.FileName;
				var dir = System.IO.Path.GetDirectoryName(id_file);

				if (Data.IdFile.CheckType(id_file) == Data.DataType.WideScan)
				{
					// OK
					if (sender == buttonAddReference)
					{
						_refSpectra.Add(new ReferenceSpectrum { DirectoryName = dir });
					}
					else if (sender == buttonAddFixedSpectrum)
					{
						_fixedSpectra.Add(new FixedSpectrum { DirectoryName = dir });
					}
				}
				else
				{
					MessageBox.Show("WideScanじゃないとだめだよ！");
				}

			}
		}

		private void DeleteSpectrum_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// FixedSpectrumはReferenceSpectrumを継承しているので注意。
			if (e.Parameter is FixedSpectrum)
			{
				FixedSpectra.Remove((FixedSpectrum)e.Parameter);
			}
			else if (e.Parameter is ReferenceSpectrum)
			{
				ReferenceSpectra.Remove((ReferenceSpectrum)e.Parameter);
			}
		}


		private void comboBoxElement_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var name = (string)comboBoxElement.SelectedItem;
			DepthProfileSetting.Name = name;
			DepthProfileSetting.RangeStart = _depthProfileData.Spectra[name].Parameter.Start;
			DepthProfileSetting.RangeStop = _depthProfileData.Spectra[name].Parameter.Stop;
		}

		#endregion
	}
}
