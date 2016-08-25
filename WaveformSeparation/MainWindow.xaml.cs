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
using System.Windows.Shapes;

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


		private static void TestOpenWideScan(string sourceDir, bool output)
		{
			var data = new Data.WideScan(sourceDir);
			//MessageBox.Show($"最小値は {data.Spectrum.Values.Min()}、最大値は {data.Spectrum.Values.Max()} でした！");

			// 微分もしてみよう。
			var diff = data.Spectrum.Differentiate(3);
			MessageBox.Show($"最小値は {diff.Data.Min()}、最大値は {diff.Data.Max()} でした！");

			if (output)
			{
				using (StreamWriter writer = new StreamWriter(@"B:\Zr_diff_a.csv", false))
				{
					for (int i = 0; i < diff.Length; i++)
					{
						writer.WriteLine($"{diff.Start + i * diff.Step},{diff.Data[i]}");
					}
				}
			}

		}

		private void TestOpenDepthProfile(string sourceDir, bool output)
		{
			var data = new Data.DepthProfile(sourceDir);
			MessageBox.Show($"I have {data.NoROI} elements.");

			if (output)
			{
				string destination = @"B:\depth-0.csv";
				data.ExportCsv(0, destination, false);

				// 微分スペクトルも出力してみよう。
				string destination_diff = @"B:\depth-0-diff.csv";
				data.ExportCsv(0, destination_diff, true);
			}
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
						TestOpenWideScan(dir, false);
						break;
					case Data.DataType.DepthProfile:
						TestOpenDepthProfile(dir, true);
						break;
				}

			}
		}

	}
}
