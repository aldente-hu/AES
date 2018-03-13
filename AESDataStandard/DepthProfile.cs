using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{
	// (0.2.0)
	#region DepthProfileクラス
	public class DepthProfile
	{

		#region *Cyclesプロパティ
		/// <summary>
		/// DepthProfileを測定したサイクル数を取得します．
		/// </summary>
		public int Cycles
		{
			get
			{
				return _cycles;
			}
		}
		int _cycles;
		#endregion

		// Dictionaryがいいのか，Listがいいのか...

		#region *Spectraプロパティ
		/// <summary>
		/// 測定対象となったROIの一覧を取得します．
		/// </summary>
		public Dictionary<string, ROISpectra> Spectra
		{
			get
			{
				return _spectra;
			}
		}
		Dictionary<string, ROISpectra> _spectra;
		#endregion

		#region *コンストラクタ(DepthProfile)

		public DepthProfile()
		{ }
		#endregion

		public async Task LoadFromAsync(string directory)
		{
			// まずパラメータだけでROISpectraを生成する。
			var roi_spectra = await ReadParaPeakAsync(directory);

			// サイクルごと→元素範囲ごとで格納されているが、
			// 元素範囲ごと→サイクルごとの方が使いやすいと思うので、そのように変換する。

			for (int j = 0; j < roi_spectra.Length; j++)
			{
				roi_spectra[j].Data = new EqualIntervalData[_cycles];
			}

			// データを読み込む。
			using (var reader = new BinaryReader(new FileStream(Path.Combine(directory, "data.peak"), FileMode.Open, FileAccess.Read)))
			{
				// サイクルごと
				for (int i = 0; i < _cycles; i++)
				{
					// 元素範囲ごと
					for (int j = 0; j < roi_spectra.Length; j++)
					{
						roi_spectra[j].Data[i] = await EqualIntervalData.GenerateAsync(reader, roi_spectra[j].Parameter.PointsCount);
					}
				}
			}

			_spectra = roi_spectra.ToDictionary(spec => spec.Name);

		}

		// (0.3.0)Tiltを考慮．
		#region *パラメータを読み込む(ReadParaPeakAsync)
		/// <summary>
		/// para.peakファイルを読み込みます。
		/// </summary>
		/// <param name="directory"></param>
		/// <returns></returns>
		protected async Task<ROISpectra[]> ReadParaPeakAsync(string directory)
		{
			// $AP_DEP_ROI_NOFEXE  6

			/*
			$AP_DEP_ROI_EXEMOD  1 1
			$AP_DEP_ROI_NAME  1 Ti
			$AP_DEP_ROI_START  1 404.00
			$AP_DEP_ROI_STOP  1 431.00
			$AP_DEP_ROI_STEP  1 0.50
			$AP_DEP_ROI_POINTS  1 55
			$AP_DEP_ROI_DWELL  1 200
			$AP_DEP_ROI_SWEEPS  1 5
			$AP_DEP_ROI_ACQRSF  1 0.414
			*/

			//ROIParameters[0] = new ROIParameter();
			ROISpectra[] roi_spectra = null;
			decimal? current = null;
			double? tilt = null;

			using (var reader = new StreamReader(new FileStream(Path.Combine(directory, "para"), FileMode.Open, FileAccess.Read)))
			{

				while (reader.Peek() > -1)
				{
					var line = await reader.ReadLineAsync();
					var cols = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
					if (cols.Count() > 1)
					{
						switch (cols[0])
						{
							case "$AP_DEP_CYCLES":
								_cycles = Convert.ToInt32(cols[1]);
								break;
							case "$AP_PCURRENT":
								current = ScanParameter.ConvertPressure(cols[1], cols[2]);
								break;
							case "$AP_STGTILT":
								tilt = Convert.ToDouble(cols[1]);
								break;
							case "$AP_DEP_ROI_NOFEXE":
								int count = Convert.ToInt32(cols[1]);
								roi_spectra = new ROISpectra[count];
								for (int i = 0; i < count; i++)
								{
									// ここで各要素を初期化しておく。
									roi_spectra[i] = new ROISpectra();
								}
								break;
								//goto SCAN_ROI;  // switch内でループから脱出するための黒魔術。
						}
					}
				}
			}

			if (roi_spectra == null)
			{
				throw new Exception("para.peakファイルに $AP_DEP_ROI_NOFEXEキーがありませんでした。");
			}
				// roi_parametersが初期化されていることを保証するために、ループを2つに分ける。
				//SCAN_ROI:

			int ch;
			using (var reader = new StreamReader(new FileStream(Path.Combine(directory, "para.peak"), FileMode.Open, FileAccess.Read)))
			{
				while (reader.Peek() > -1)
				{
					var line = reader.ReadLine();
					var cols = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
					if (cols.Count() > 1)
					{
						switch (cols[0])
						{

							case "$AP_DEP_ROI_NAME":
								ch = Convert.ToInt32(cols[1]) - 1;
								roi_spectra[ch].Name = cols[2];
								break;
							case "$AP_DEP_ROI_START":
								ch = Convert.ToInt32(cols[1]) - 1;
								roi_spectra[ch].Parameter.Start = Convert.ToDecimal(cols[2]);
								break;
							case "$AP_DEP_ROI_STOP":
								ch = Convert.ToInt32(cols[1]) - 1;
								roi_spectra[ch].Parameter.Stop = Convert.ToDecimal(cols[2]);
								break;
							case "$AP_DEP_ROI_STEP":
								ch = Convert.ToInt32(cols[1]) - 1;
								roi_spectra[ch].Parameter.Step = Convert.ToDecimal(cols[2]);
								break;
							//case "$AP_DEP_ROI_POINTS":
							//	ch = Convert.ToInt32(cols[1]) - 1;
							//	ROIParameters[ch].noPoints = Convert.ToInt32(cols[2]);
							//	break;
							case "$AP_DEP_ROI_DWELL":
								ch = Convert.ToInt32(cols[1]) - 1;
								roi_spectra[ch].Parameter.Dwell = Convert.ToInt32(cols[2]) * 1e-3M;
								// ここでcurrentを設定する。
								roi_spectra[ch].Parameter.Current = current.Value;  // HasValueでないことは想定していない。
								// ※AP_STGTILTがAP_DEP_ROI_NOFEXEより後に出てくるので，ここでは値が設定されていない！
								roi_spectra[ch].Parameter.Tilt = tilt.Value;  // HasValueでないことは想定していない。
								break;
						}
					}
				}
			}

			return roi_spectra;
		}
		#endregion

	}
	#endregion

}
