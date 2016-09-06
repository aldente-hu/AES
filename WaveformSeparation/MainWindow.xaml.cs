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
				using (var writer = new StreamWriter(@"B:\depth.csv"))
				{
					_depthProfileData.Spectra[(string)comboBoxElement.SelectedItem].Differentiate(3).ExportCsv(writer);
				}
				DisplayDepthChart(@"B:\depth.png", ChartFormat.Png);

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
				await new Gnuplot
				{
					Format = ChartFormat.Png,
					Width = 800,
					Height = 600,
					FontSize = 14,
					Destination = @"B:\depth.png"
				}.OutputPltFileAsync(dialog.FileName);
			}
		}

		#region DepthProfile関連

		Data.DepthProfile _depthProfileData;

		void DisplayDepthChart(string destination, ChartFormat format)
		{
			new Gnuplot
			{
				Format = format,
				Width = 800,
				Height = 600,
				FontSize = 14,
				Destination = destination
			}.Draw();
			imageChart.Source = new BitmapImage(new Uri(destination));

		}

		#endregion


	}
}
