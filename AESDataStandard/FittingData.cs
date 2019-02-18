using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{

	public class FittingData : INotifyPropertyChanged
	{

		#region *FittingConditionプロパティ
		public FittingCondition FittingCondition { get; } = new FittingCondition();
		#endregion


		#region *指定したROIをフィッティング対象に追加(AddFittingProfile)
		// 指定したROIを，フィッティング対象に追加する．
		public void AddFittingProfile(ROISpectra roi)
		{
			FittingCondition.AddFittingProfile(roi);
		}
		#endregion

		#region *指定したROIをフィッティング対象から除外(RemoveFittingProfile)
		// 指定したプロファイルを削除する？
		public void RemoveFittingProfile(FittingProfile profile)
		{
			FittingCondition.FittingProfiles.Remove(profile);
		}
		#endregion

		#region *グラフの出力先を設定(SetChartDestination)
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
		#endregion


		#region 測定条件関連

		// DepthProfileFittingDataからのコピペ．

		#region *フィッティング条件をロード(LoadFittingCondition)
		public void LoadFittingCondition(string fileName)
		{
			// ロードする．
			using (StreamReader reader = new StreamReader(fileName))
			{
				FittingCondition.LoadFrom(reader);
			}
		}
		#endregion

		#region *フィッティング条件をセーブ(SaveFittingCondition)
		public void SaveFittingCondition(string fileName)
		{
			using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
			{
				FittingCondition.GenerateDocument().Save(writer);
			}
		}
		#endregion

		#endregion


		#region *参照スペクトルを追加(AddReferenceSpectrumAsync)
		/// <summary>
		/// ファイルからスペクトルを読み込み，指定されたプロファイルの参照データとします．
		/// </summary>
		/// <param name="idFileName"></param>
		/// <param name="profile"></param>
		/// <returns></returns>
		public async Task AddReferenceSpectrumAsync(string idFileName, FittingProfile profile)
		{
			var dir = Path.GetDirectoryName(idFileName);

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
		#endregion


		#region *チャートを設定(ConfigureChart)
		/// <summary>
		/// 具体的なチャートの設定を行います．
		/// </summary>
		/// <param name="cycle"></param>
		/// <param name="result"></param>
		/// <param name="profile"></param>
		/// <param name="outputConvolution"></param>
		/// <returns></returns>
		public Gnuplot ConfigureChart(FittingProfile.FittingResult result, FittingProfile profile, bool outputConvolution, int cycle = -1)
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

			var chart_destination = Path.Combine(FittingCondition.OutputDestination, $"{profile.Name}{(cycle >= 0 ? string.Format("_{0}", cycle) : string.Empty)}{chart_ext}");

			#region チャート設定
			var gnuplot = new Gnuplot
			{
				Format = FittingCondition.ChartFormat,
				Width = 800,
				Height = 600,
				FontScale = 1.6,
				Destination = chart_destination,
				XTitle = "Kinetic Energy / eV",
				YTitle = "dN(E)/dE",
				Title = (cycle >= 0 ? $"Cycle {cycle} , " : string.Empty) + $"Shift {result.Shift} eV"
			};

			var source_csv = GetCsvFileName(profile.Name, cycle);

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




		// とりあえず．
		protected virtual string GetCsvFileName(string name, int cycle = -1)
		{
			return Path.Combine(FittingCondition.OutputDestination, cycle >= 0 ? $"{name}_{cycle}.csv" : $"{name}.csv");
		}


		#region INotifyPropertyChanged実装
		protected void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		#endregion


	}
}

