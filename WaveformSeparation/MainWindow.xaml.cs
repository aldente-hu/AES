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


namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{

	using HirosakiUniversity.Aldente.AES.Data.Portable;
	using Mvvm;

	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{

		MainWindowViewModel ViewModel
		{
			get
			{
				return (MainWindowViewModel)this.DataContext;
			}
		}

		public MainWindow()
		{
			InitializeComponent();

			Messenger.Default.Register<SelectSaveFileMessage>(ViewModel.DepthProfileData,
				(message => SelectSaveFile(message))
			);
			Messenger.Default.Register<SelectOpenFileMessage>(ViewModel.DepthProfileData,
				(message => SelectOpenFile(message)));

			// 同じメッセージなのに発信元によって処理を変えるのは面倒だね．
			Messenger.Default.Register<SelectOpenFileMessage>(ViewModel.WideScanData,
				(message => SelectOpenFile(message))
			);
			Messenger.Default.Register<SimpleMessage>(ViewModel.DepthProfileData,
				(message => MessageBox.Show(message.Message))
			);
			ViewModel.JampDataOpened += ViewModel_JampDataOpened;
		}

		#region *[static]保存先のファイルを選択(SelectSaveFile)
		static void SelectSaveFile(SelectSaveFileMessage message)
		{
			Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog { Title = message.Message };
			dialog.Filter = string.Join("|", message.Ext.Select(ext => $"*{ext}|*{ext}").ToArray());
			if (System.IO.Path.IsPathRooted(message.SelectedFile))
			{
				dialog.FileName = message.SelectedFile;
			}
			if (dialog.ShowDialog() == true)
			{
				message.SelectedFile = dialog.FileName;
			}
			else
			{
				message.SelectedFile = string.Empty;
			}
		}
		#endregion

		#region *[static]開くファイルを選択(SelectSaveFile)
		static void SelectOpenFile(SelectOpenFileMessage message)
		{
			// とりあえず1つだけ選択する．

			var dialog = new Microsoft.Win32.OpenFileDialog { Title = message.Message };
			dialog.Filter = string.Join("|", message.Filter.Select(filter => $"{filter}|{filter}").ToArray());
			if (System.IO.Path.IsPathRooted(message.SelectedFile))
			{
				dialog.FileName = message.SelectedFile;
			}
			if (dialog.ShowDialog() == true)
			{
				message.SelectedFile = dialog.FileName;
			}
			else
			{
				message.SelectedFile = string.Empty;
			}
		}
		#endregion

		// TabControl.SelectedItemイベントを使わずに行う方法がある？
		private void ViewModel_JampDataOpened(object sender, JampDataEventArgs e)
		{
			switch (e.DataType)
			{
				case DataType.WideScan:
					tabControlData.SelectedIndex = 0;
					break;
				case DataType.DepthProfile:
					tabControlData.SelectedIndex = 1;
					break;
			}
		}

		#region Wide関連

		/*
		private async void buttonOutputDepthCsv_Click(object sender, RoutedEventArgs e)
		{
			// CSVを出力。積分か微分かはソースによる。
			if (comboBoxElement.SelectedIndex >= 0)
			{
				var csv_destination = labelOutputDepthCsvDestination.Content.ToString();

				using (var writer = new StreamWriter(csv_destination))
				{
					if (e.Source == buttonOutputDepthCsv)
					{
						await _depthProfileData.Spectra[(string)comboBoxElement.SelectedItem].ExportCsvAsync(writer);
					}
					else if (e.Source == buttonOutputDepthDiffCsv)
					{
						await _depthProfileData.Spectra[(string)comboBoxElement.SelectedItem].Differentiate(3).ExportCsvAsync(writer);
					}
				}
			}
			else
			{
				MessageBox.Show("元素を選んでから来てください。");
			}
		}
		*/



		#endregion

		/*
		private async void buttonOutputDepth_Click(object sender, RoutedEventArgs e)
		{
			if (comboBoxElement.SelectedIndex >= 0)
			{
				var csv_destination = labelOutputDepthCsvDestination.Content.ToString();
				var chart_destination = labelOutputDepthChartDestination.Content.ToString();

				using (var writer = new StreamWriter(csv_destination))
				{
					await _depthProfileData.Spectra[(string)comboBoxElement.SelectedItem].Differentiate(3).ExportCsvAsync(writer);
				}
				DisplayDepthChart(csv_destination, chart_destination, ChartFormat.Png);

			}
			else
			{
				MessageBox.Show("元素を選んでから来てください。");
			}
		}
		*/
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



		#region DepthProfile関連

		//DepthProfile _depthProfileData;

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
			//imageChart.Source = new BitmapImage(new Uri(destination));

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

		/*

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


		}
*/



		#region 標準スペクトル(新)関連

		#region *ReferenceSpectraプロパティ
		public ObservableCollection<ReferenceSpectrum> ReferenceSpectra
		{ get
			{
				return _refSpectra;
			}
		}
		ObservableCollection<ReferenceSpectrum> _refSpectra = new ObservableCollection<ReferenceSpectrum>();
		#endregion

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

		#region *DepthProfileSettingプロパティ
		public DepthProfileSetting DepthProfileSetting
		{
			get
			{
				return _depthProfileSetting;
			}
		}
		DepthProfileSetting _depthProfileSetting = new DepthProfileSetting();
		#endregion

		#region *CurrentChartFormatプロパティ
		public ChartFormat CurrentChartFormat
		{
			get; set;
		}
		#endregion

		private void RadioButtonChart_Checked(object sender, RoutedEventArgs e)
		{
			if (e.OriginalSource == radioButtonPng)
			{
				CurrentChartFormat = ChartFormat.Png;
			}
			else if (e.OriginalSource == radioButtonSvg)
			{
				CurrentChartFormat = ChartFormat.Svg;
			}
		}



		private void buttonSelectDepthOutputDestination_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.SaveFileDialog { DefaultExt = ".csv" };
			if (dialog.ShowDialog() == true)
			{
				DepthProfileSetting.OutputDestination = Path.GetDirectoryName(dialog.FileName);
			}

		}

/*
		// とりあえずここに置いておく。
		public static RoutedCommand SeparateSpectrumCommand = new RoutedCommand();

		private async void SeparateSpectrum_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var d_data = _depthProfileData.Spectra[(string)comboBoxElement.SelectedItem]
											.Restrict(DepthProfileSetting.RangeStart, DepthProfileSetting.RangeStop)
											.Differentiate(3);

			// 固定参照スペクトルを取得する。
			List<decimal> fixed_data = new List<decimal>();
			if (FixedSpectra.Count > 0)
			{
				var v_data = await LoadShiftedFixedStandardsData(FixedSpectra, d_data.Parameter);
				for (int j = 0; j < v_data.First().Count; j++)
				{
					fixed_data.Add(v_data.Sum(one => one[j]));
				}
			}

			if (radioButtonFitAll.IsChecked == true)
			{
				// これをパラレルに行う。
				Parallel.For(0, d_data.Data.Length,
					i => FitOneLayer(i, d_data.Data[i], d_data.Parameter, ReferenceSpectra, fixed_data,
						_depthProfileSetting.OutputDestination,
						_depthProfileSetting.Name)
				);
			}
			else
			{
				int i = (int)comboBoxLayers.SelectedItem;
				FitOneLayer(i, d_data.Data[i], d_data.Parameter, ReferenceSpectra, fixed_data,
					_depthProfileSetting.OutputDestination,
					_depthProfileSetting.Name);
			}

		}
*/
/*
 * 
		// (0.0.3)定数項を考慮。
		private async void FitOneLayer(
					int layer,
					EqualIntervalData data,
					ScanParameter originalParameter,
					IList<ReferenceSpectrum> referenceSpectra,
					List<decimal> fixed_data,
					string outputDestination,
					string name)
		{
			/// フィッティング対象となるデータ。すなわち、もとのデータからFixされた分を差し引いたデータ。
			var target_data = fixed_data.Count > 0 ? data.Substract(fixed_data) : data;

			// A.最適なエネルギーシフト量を見つける場合

			#region エネルギーシフト量を決定する
			var gains = new Dictionary<decimal, Vector<double>>();
			Dictionary<decimal, decimal> residuals = new Dictionary<decimal, decimal>();
			for (int m = -6; m < 7; m++)
			{

				decimal shift = 0.5M * m; // とりあえず。

				var shifted_parameter = originalParameter.GetShiftedParameter(shift);


				// シフトされた参照スペクトルを読み込む。
				var standards = await LoadShiftedStandardsData(referenceSpectra, shifted_parameter);
				//var standards = LoadShiftedStandardsData(ReferenceSpectra, originalParameter);

				// フィッティングを行い、
				Debug.WriteLine($"Layer {layer}");
				gains.Add(shift, GetOptimizedGainsWithOffset(target_data, standards.ToArray()));
				for (int j = 0; j < referenceSpectra.Count; j++)
				{
					Debug.WriteLine($"    {referenceSpectra[j].Name} : {gains[shift][j]}");
				}

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
					var standards = await LoadShiftedStandardsData(referenceSpectra, shifted_parameter);

					// フィッティングを行い、
					Debug.WriteLine($"Layer {layer}");
					gains.Add(shift, GetOptimizedGains(target_data, standards.ToArray()));
					for (int j = 0; j < referenceSpectra.Count; j++)
					{
						Debug.WriteLine($"    {referenceSpectra[j].Name} : {gains[shift][j]}");
					}

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
			var best_standards = await LoadShiftedStandardsData(referenceSpectra, best_shifted_parameter);
			var best_gains = gains[best_shift];
			

			 
			// B.エネルギーシフト量を自分で与える場合

			//var best_shift = -0.5M;
			
			//var best_shifted_parameter = originalParameter.GetShiftedParameter(best_shift);
			//var best_standards = LoadShiftedStandardsData(referenceSpectra, best_shifted_parameter);
			//var best_gains = GetOptimizedGains(target_data, best_standards.ToArray());
			

			await OutputFittedResult(layer, originalParameter, referenceSpectra, outputDestination, name, target_data, best_shift, best_standards, best_gains);

		}
*/


		/*
		private async void buttonAddReference_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "idファイル(id)|id" };
			if (dialog.ShowDialog() == true)
			{
				var id_file = dialog.FileName;
				var dir = System.IO.Path.GetDirectoryName(id_file);

				if ((await IdFile.CheckTypeAsync(id_file)) == DataType.WideScan)
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
					//else if (sender == buttonAddWideReference)
					//{
					//	WideFittingModel.ReferenceSpectra.Add(new ReferenceSpectrum { DirectoryName = dir });
					//}
				}
				else
				{
					MessageBox.Show("WideScanじゃないとだめだよ！");
				}

			}
		}
		*/

		private void DeleteSpectrum_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (sender is ListBox)
			{
				((System.Collections.IList)((ListBox)sender).ItemsSource).Remove(e.Parameter);
			}

			/*
			// FixedSpectrumはReferenceSpectrumを継承しているので注意。
			if (e.Parameter is FixedSpectrum)
			{
				FixedSpectra.Remove((FixedSpectrum)e.Parameter);
			}
			else if (e.Parameter is ReferenceSpectrum)
			{
				ReferenceSpectra.Remove((ReferenceSpectrum)e.Parameter);
			}
			*/
		}

		/*
		private void comboBoxElement_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var name = (string)comboBoxElement.SelectedItem;
			DepthProfileSetting.Name = name;
			DepthProfileSetting.RangeStart = _depthProfileData.Spectra[name].Parameter.Start;
			DepthProfileSetting.RangeStop = _depthProfileData.Spectra[name].Parameter.Stop;
		}
		*/

		#endregion

	}
}
