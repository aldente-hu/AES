using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.Data
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
		/// レイヤーごとのデータを取得／設定します。
		/// </summary>
		public EqualIntervalData[] Data { get; set; }

		public ROISpectra()
		{
			Parameter = new ScanParameter();

		}


		public ROISpectra Differentiate(int m)
		{
			return new ROISpectra
			{
				Name = this.Name,
				Parameter = this.Parameter.GetDifferentiatedParameter(m),
				Data = this.Data.Select(data => data.Differentiate(m, Parameter.Step)).ToArray()
			};
		}

		public ROISpectra Shift(decimal pitch)
		{
			return new ROISpectra
			{
				Name = this.Name,
				Data = this.Data,
				Parameter = this.Parameter.GetShiftedParameter(pitch)
			};
		}

		#region *csvとしてエクスポート(ExportCsv)
		public void ExportCsv(TextWriter writer)
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
				writer.WriteLine(string.Join(",", cols.ToArray()));
			}
		}
		#endregion


		public void DrawChart()
		{

		}

	}
	#endregion

}
