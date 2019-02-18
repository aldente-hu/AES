using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Linq;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{

	#region WideScanFittingDataクラス
	public class WideScanFittingData : FittingData
	{

		#region *WideScanプロパティ
		public WideScan WideScan { get; } = new WideScan();
		#endregion


		// コンストラクタでやることは特にない？
		int _differentialWindow = 3;	// とりあえず．


		public async Task LoadFromAsync(string directory)
		{
			await WideScan.LoadFromAsync(directory);

		}


		public async Task ExportCsv(string destination, ExportCsvFlag flag)
		{
			if ((flag & ExportCsvFlag.Diff) == ExportCsvFlag.Diff)
			{
				var diff_file_name = Path.Combine(Path.GetDirectoryName(destination),
					Path.GetFileNameWithoutExtension(destination) + "_diff" + Path.GetExtension(destination)
				);
				using (var writer = new StreamWriter(diff_file_name, false))
				{
					await WideScan.Differentiate(_differentialWindow).ExportCsvAsync(writer);
				}
			}
			else
			{
				using (var writer = new StreamWriter(destination, false))
				{
					await WideScan.ExportCsvAsync(writer);
				}
			}
		}



		#region *1つのProfileに対してフィッティングを行う(FitSingleProfile)
		public async Task FitSingleProfile(FittingProfile profile)
		{
			var d_data = WideScan.GetRestrictedData(profile.RangeBegin, profile.RangeEnd).Differentiate(_differentialWindow);

			var fitting_results = new FittingProfile.FittingResult();

			// キーはサイクル数．
			EqualIntervalData target_data = new EqualIntervalData();

			// 1.まず，フィッティングの計算を行う．
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
				target_data = d_data.Data;

				//fitting_tasks.Add(i, profile.FitOneCycle(i, target_data[i], d_data.Parameter));
				fitting_results = profile.FitOneCycle(-1, target_data, d_data.Parameter);
			}

			// 2.その後に，チャート出力を行う？

			Gnuplot charts = await Output(d_data.Parameter, profile, target_data, fitting_results);


			// pltファイルも出力してみる。
			using (var writer = new StreamWriter(GetCsvFileName(profile.Name, -1) + ".plt"))
			{
				await charts.OutputPltFileAsync(writer);
			}
			// チャートを描画する。
			await charts.Draw();

		}
		#endregion

		// (0.1.0)メソッド名をFitからOutputに変更．というか，これどこにおけばいいのかな？
		private async Task<Gnuplot> Output(
			ScanParameter originalParameter,
			FittingProfile profile,
			EqualIntervalData target_data,
			FittingProfile.FittingResult result)
		{
			// フィッティングした結果をチャートにする？
			// ★とりあえずFixedなデータは表示しない。

			bool output_convolution = result.Standards.Count > 1;

			// それには、csvを出力する必要がある。
			//string fitted_csv_path = Path.Combine(FittingCondition.OutputDestination, $"{FittingCondition.Name}_{layer}.csv");
			using (var csv_writer = new StreamWriter(GetCsvFileName(profile.Name)))
			{
				await OutputFittedCsv(csv_writer, originalParameter, target_data, result, output_convolution).ConfigureAwait(false);
			}

			// チャート出力の準備？
			return ConfigureChart(result, profile, output_convolution);

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

	}
	#endregion

	[Flags]
	public enum ExportCsvFlag
	{
		/// <summary>
		/// 生スペクトルを出力します．
		/// </summary>
		Raw = 0x01,
		/// <summary>
		/// 微分スペクトルを出力します．
		/// </summary>
		Diff = 0x02
	}
}
