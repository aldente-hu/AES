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

		#region *コンストラクタ(MainWindow)
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
		#endregion

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

		#region *[static]開くファイルを選択(SelectOpenFile)
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


		#endregion

	}
}
