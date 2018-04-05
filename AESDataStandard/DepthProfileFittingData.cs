using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{

	#region DepthProfileFittingDataクラス
	public class DepthProfileFittingData : FittingData
	{

		#region *DepthProfileプロパティ
		public DepthProfile DepthProfile { get; } = new DepthProfile();
		#endregion

		#region *ROISpectraCollectionプロパティ
		public List<ROISpectra> ROISpectraCollection
		{
			get
			{
				if (DepthProfile.Spectra != null)
				{
					return DepthProfile.Spectra.Values.ToList();
				}
				else
				{
					return null;
				}
			}
		}
		#endregion


		#region *コンストラクタ(DepthProfileViewModel)

		/// <summary>
		/// コンストラクタでは何も行っていません．
		/// </summary>
		public DepthProfileFittingData()
		{
		}

		#endregion

		#region *測定データをロード(LoadFromAsync)
		/// <summary>
		/// 指定されたディレクトリにあるDepthProfileの測定データをロードします．
		/// </summary>
		/// <param name="directory"></param>
		/// <returns></returns>
		public async Task LoadFromAsync(string directory)
		{
			await DepthProfile.LoadFromAsync(directory);
			// ここでCyclesを指定する？
			FittingCondition.Cycles = DepthProfile.Cycles;
			//OutputCondition.Cycles = _depthProfile.Cycles;
			NotifyPropertyChanged("ROISpectraCollection");
		}
		#endregion

		#region 単純出力関連

		#region *Csvとして出力(ExportCsvAsync)

		/// <summary>
		/// 指定したROIについてのスペクトルデータを出力します．diffRangeに正の値を与えると，微分スペクトルを出力します．
		/// </summary>
		/// <param name="roi"></param>
		/// <param name="destination"></param>
		/// <param name="diffRange"></param>
		/// <returns></returns>
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

		// とりあえず分けた．

		/// <summary>
		/// profileで指定した範囲に最適なROIを探し出します．
		/// </summary>
		/// <param name="profile"></param>
		protected ROISpectra FindROI(FittingProfile profile)
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
			return suitable_roi_list.OrderBy(roi => roi.Parameter.Step).First();
		}


		// (0.1.0)1つのProfileについてのみフィッティングを行う．
		#region *1つのProfileに対してフィッティングを行う(FitSingleProfile)
		public async Task FitSingleProfile(FittingProfile profile)
		{
			// profileがBaseROIを持つのではなく，ここでBaseとなるROIを決定するようにしてみた．

			// ★一応，範囲から推測するのをベースにするけど，
			// ★ROIの名前からでも指定できるようにするのがいいのでは？

			// ProfileのRangeを包含するROIを探す．
			var baseROI = FindROI(profile);

			var d_data = baseROI.Restrict(profile.RangeBegin, profile.RangeEnd)
						.Differentiate(3);

			//var fitting_tasks = new Dictionary<int, Task<FittingProfile.FittingResult>>();
			var fitting_results = new Dictionary<int, FittingProfile.FittingResult>();

			// キーはサイクル数．
			Dictionary<int, EqualIntervalData> target_data = new Dictionary<int, EqualIntervalData>();

			// 1.まず，フィッティングの計算を行う．
			foreach (int i in FittingCondition.TargetCycles)
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

				fitting_results.Add(i, profile.FitOneCycle(i, target_data[i], d_data.Parameter));
			}

			// 2.その後に，チャート出力を行う？

			//Dictionary<int, Gnuplot> charts = new Dictionary<int, Gnuplot>();
			Dictionary<int, Task<Gnuplot>> gnuplot_tasks = new Dictionary<int, Task<Gnuplot>>();
			foreach (int i in FittingCondition.TargetCycles)
			{
				gnuplot_tasks[i] = Output(i, d_data.Parameter, profile, target_data[i], fitting_results[i]);
			}
			await Task.WhenAll(gnuplot_tasks.Values.ToArray());

			Dictionary<int, Gnuplot> charts = new Dictionary<int, Gnuplot>();
			foreach (var task in gnuplot_tasks)
			{
				charts[task.Key] = task.Value.Result;
			}

			// 全てのchartで共通の軸範囲を使用する．
			Range x_range = Range.Union(charts.Select(gnuplot => gnuplot.Value.XAxis.Range).ToArray());
			Range y_range = Range.Union(charts.Select(gnuplot => gnuplot.Value.YAxis.Range).ToArray());
			//Range x_range = Range.Union(charts.Select(gnuplot => gnuplot.Value.Result.XAxis.Range).ToArray());
			//Range y_range = Range.Union(charts.Select(gnuplot => gnuplot.Value.Result.YAxis.Range).ToArray());

			Parallel.ForEach(charts.Keys,
				async (i) =>
				{
					charts[i].DefineXAxis(x_range);
					charts[i].DefineYAxis(y_range);
					//charts[i].Result.SetXAxis(x_range);
					//charts[i].Result.SetYAxis(y_range);

					// pltファイルも出力してみる。
					using (var writer = new StreamWriter(GetCsvFileName(i, profile.Name) + ".plt"))
					{
						await charts[i].OutputPltFileAsync(writer);
						//await charts[i].Result.OutputPltFileAsync(writer);
					}
					// チャートを描画する。
					await charts[i].Draw();
					//await charts[i].Result.Draw();
				}
			);

		}
		#endregion

		// (0.1.0)メソッド名をFitからOutputに変更．というか，これどこにおけばいいのかな？
		#region *CSVを出力して，グラフ描画の準備を行う(Output)
		/// <summary>
		/// フィッティングした結果から，チャートの出力を設定します．
		/// </summary>
		/// <param name="cycle"></param>
		/// <param name="originalParameter"></param>
		/// <param name="profile"></param>
		/// <param name="target_data"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		private async Task<Gnuplot> Output(
			int cycle,
			ScanParameter originalParameter,
			FittingProfile profile,
			EqualIntervalData target_data,
			FittingProfile.FittingResult result)
		{
			// フィッティングした結果をチャートにする？
			// ★とりあえずFixedなデータは表示しない。
			Trace.WriteLine($"Let's start outputting! cycle{cycle} {DateTime.Now}     [{Thread.CurrentThread.ManagedThreadId}]");
			bool output_convolution = result.Standards.Count > 1;

			// それには、csvを出力する必要がある。
			using (var csv_writer = new StreamWriter(GetCsvFileName(cycle, profile.Name)))
			{
				await OutputFittedCsv(csv_writer, originalParameter, target_data, result, output_convolution);
			}
			Trace.WriteLine($"CSV output Completed! cycle{cycle} {DateTime.Now}     [{Thread.CurrentThread.ManagedThreadId}]");

			// チャート出力の準備？
			return ConfigureChart(result, profile, output_convolution, cycle);

			#region こうすると，1024バイトほど書き込んだところで落ちる．
			// どう違うのかはよくわかっていない．
			/*
			Gnuplot chartConfiguration;
			// chartConfigurationを構成して返す処理とcsv出力処理が入り交じっているので注意．(こういう書き方しか思いつかなかった．)
			using (var csv_writer = new StreamWriter(GetCsvFileName(cycle, profile.Name)))
			{
				//var outputCsvTask = OutputFittedCsv(csv_writer, originalParameter, target_data, result, output_convolution);
				//chartConfiguration = ConfigureChart(cycle, result, profile, output_convolution);
				//await outputCsvTask;
				await OutputFittedCsv(csv_writer, originalParameter, target_data, result, output_convolution);
			}
			return chartConfiguration;
			*/
			#endregion

		}
		#endregion

		/// <summary>
		/// チャート出力用のcsvファイルの出力先を取得します．
		/// </summary>
		/// <param name="cycle">サイクル数</param>
		/// <param name="name">プロファイル名</param>
		/// <returns></returns>
		string GetCsvFileName(int cycle, string name)
		{
			return Path.Combine(FittingCondition.OutputDestination, $"{name}_{cycle}.csv");
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


