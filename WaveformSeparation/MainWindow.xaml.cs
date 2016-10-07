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
			var m = GetOptimizedGain(data, reference);
			Debug.WriteLine($"m = {m}");

			// 3.残差を求める
			var residual = GetResidual(data, reference, m); // 残差2乗和
			Debug.WriteLine($"residual = {residual}");

			return residual;
		}


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

		/// <summary>
		/// 最適なゲイン係数を2要素の配列として返します。前者がreference1の係数、後者がreference2の係数です。
		/// </summary>
		/// <param name="data"></param>
		/// <param name="reference1"></param>
		/// <param name="reference2"></param>
		/// <returns></returns>
		public static decimal[] GetOptimizedGains(IList<decimal> data, IList<decimal> reference1, IList<decimal> reference2)
		{

			decimal r1r1 = 0;
			decimal r1r2 = 0;
			decimal r2r2 = 0;
			decimal r1d = 0;
			decimal r2d = 0;


			for (int i = 0; i < data.Count; i++)
			{
				//Debug.WriteLine($"{data[i]},{reference[i]}");
				r1r1 += reference1[i] * reference1[i];
				r1r2 += reference1[i] * reference2[i];
				r2r2 += reference2[i] * reference2[i];
				r1d += reference1[i] * data[i];
				r2d += reference2[i] * data[i];
			}
			return new decimal[] {
				(r2r2 * r1d - r1r2 * r2d) / (r1r1 * r2r2 - r1r2 * r1r2),
				(r1r1 * r2d - r1r2 * r1d) / (r1r1 * r2r2 - r1r2 * r1r2)
			};
		}

		public static decimal GetResidual(IList<decimal> data, IList<decimal> reference, decimal gain)
		{
			decimal residual = 0;

			for (int i = 0; i < data.Count; i++)
			{
				var diff = (data[i] - gain * reference[i]);
				residual += diff * diff;
			}
			return residual;
		}

		private void buttonInvestigateSpectrum_Click(object sender, RoutedEventArgs e)
		{
			// ついでにLayer1のフィッティングを行う。

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
						csv_writer.WriteLine($"{data.Parameter.Start + j * data.Parameter.Step},{layer_data[j].ToString("f3")},{(gains[0]*reference_zro2[j]).ToString("f3")},{(gains[1]*reference_zr[j]).ToString("f3")}");
					}
				}

			}

		}

	}
}
