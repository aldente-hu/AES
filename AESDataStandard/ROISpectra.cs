using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{

	#region ROISpectraクラス
	/// <summary>
	/// 1つのROIについての複数層のスペクトルデータを格納します。
	/// </summary>
	public class ROISpectra
	{
		#region *Nameプロパティ
		/// <summary>
		/// ROI系列の名前を取得／設定します。(※通常は元素名ですが、そうでない場合もあります。重なっている場合とか。)
		/// </summary>
		public string Name { get; set; }
		#endregion

		public ScanParameter Parameter { get; set; }

		/// <summary>
		/// サイクルごとのデータを取得／設定します。
		/// </summary>
		public EqualIntervalData[] Data { get; set; }

		public ROISpectra()
		{
			Parameter = new ScanParameter();
		}


		/// <summary>
		/// データを微分したROISpectraを返します。
		/// </summary>
		/// <param name="pitch"></param>
		/// <returns></returns>
		public ROISpectra Differentiate(int m)
		{
			return new ROISpectra
			{
				Name = this.Name,
				Parameter = this.Parameter.GetDifferentiatedParameter(m),
				Data = this.Data.Select(data => data.Differentiate(m, Parameter.Step)).ToArray()
			};
		}

		/// <summary>
		/// 指定された値だけエネルギー値をシフトしたROISpectraを返します。
		/// </summary>
		/// <param name="pitch"></param>
		/// <returns></returns>
		public ROISpectra Shift(decimal pitch)
		{
			return new ROISpectra
			{
				Name = this.Name,
				Data = this.Data,
				Parameter = this.Parameter.GetShiftedParameter(pitch)
			};
		}

		/// <summary>
		/// エネルギー値を指定した範囲に制限したROISpectraを返します。
		/// </summary>
		/// <param name="pitch"></param>
		/// <returns></returns>
		public ROISpectra Restrict(decimal start, decimal stop)
		{
			var original_start = Parameter.Start;
			var original_stop = Parameter.Stop;
			var step = Parameter.Step;

			if (start >= original_start && stop <= original_stop && start <= stop)
			{
				var start_index = Convert.ToInt32(Decimal.Ceiling((start - original_start) / step));
				var stop_index = Convert.ToInt32(Decimal.Floor((stop - original_start) / step));

				return new ROISpectra
				{
					Name = this.Name,
					Data = this.Data.Select(data => data.GetSubData(start_index, stop_index)).ToArray(),
					Parameter = new ScanParameter
					{
						Start = original_start + start_index * step,
						Stop = original_start + stop_index * step,
						Step = step,
						Current = Parameter.Current,
						Dwell = Parameter.Dwell,
						Tilt = Parameter.Tilt
					}
				};
			}
			else
			{
				throw new ArgumentException("もとのスペクトルの範囲内でないといけません。");
			}
		}


		#region *csvとしてエクスポート(ExportCsv)
		public async Task ExportCsvAsync(TextWriter writer)
		{
			for (int i = 0; i < Parameter.PointsCount; i++)
			{
				List<string> cols = new List<string>();

				decimal x = Parameter.Start + i * Parameter.Step;
				cols.Add(x.ToString());
				foreach (var data in Data)
				{
					cols.Add(data.GetDataForCsv(i));
				}
				await writer.WriteLineAsync(string.Join(",", cols.ToArray()));
			}
		}
		#endregion

	}
	#endregion
}
