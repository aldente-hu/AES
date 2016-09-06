using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.Data
{
	
	#region WideScanクラス
	public class WideScan
	{
		#region *Parameterプロパティ
		public ScanParameter Parameter
		{
			get
			{
				return _scanParameter;
			}
		}
		ScanParameter _scanParameter;
		#endregion


		#region *Dataプロパティ
		public EqualIntervalData Data
		{
			get
			{
				return _data;
			}
		}
		EqualIntervalData _data;
		#endregion

		private WideScan()
		{ }

		public WideScan(string directory)
		{
			// パラメータを読み込む。
			_scanParameter = LoadPara(directory);

			// データを読み込む。
			using (var reader = new BinaryReader(new FileStream(Path.Combine(directory , "data"), FileMode.Open, FileAccess.Read)))
			{
				_data = new EqualIntervalData(reader);
			}

		}

/*		public Spectrum Spectrum
		{
			get
			{
				return _spectrum;
			}
		}
		Spectrum _spectrum;
*/

		// WideScanのコンストラクタから呼び出すことを考慮して、asyncにはしていない。
		// う～ん、ScanParameterクラスのメソッドでもいいのかな？
		protected static ScanParameter LoadPara(string directory)
		{
			var parameter = new ScanParameter();
			using (var reader = new StreamReader(Path.Combine(directory, "para")))
			{
				while (reader.Peek() > -1)
				{
					var line = reader.ReadLine();
					var cols = line.Split(new string[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
					if (cols.Count() > 1)
					{
						switch (cols[0])
						{
							case "$AP_SPC_WSTART":
								parameter.Start = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_SPC_WSTOP":
								parameter.Stop = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_SPC_WSTEP":
								parameter.Step = Convert.ToDecimal(cols[1]);
								break;
							// とりあえず無視する。
							//case "$AP_SPC_WPOINTS":
							//	_scanParameter.noPoints = Convert.ToInt32(cols[1]);
							//	break;
						}
					}
				}
			}
			return parameter;
		}

		public WideScan Differentiate(int m)
		{
			return new WideScan {
				_scanParameter = this._scanParameter.GetDifferentiatedParameter(m),
				_data = this.Data.Differentiate(m, _scanParameter.Step)
			};
		}

		#region *csvとしてエクスポート(ExportCsv)
		public void ExportCsv(TextWriter writer)
		{
			for (int i = 0; i < Parameter.PointsCount; i++)
			{
				decimal x = Parameter.Start + i * Parameter.Step;
				writer.WriteLine($"{x},{Data.GetDataForCsv(i)}");
			}
		}
		#endregion

		/*
		struct ScanParameter
		{
			public SpectrumParameter SpectrumParameter
			{
				get
				{
					return new SpectrumParameter { Start = ScanStart, Step = ScanStep, Count = noPoints };
				}
			}

			public decimal ScanStart;
			public decimal ScanStop;
			public decimal ScanStep;
			public int noPoints;
		}
		*/
	}
	#endregion

	#region DepthProfileクラス
	public class DepthProfile
	{
/*
		public ICollection<string> Elements
		{
			get
			{
				return ROIParameters.Select(roi => roi.Name).ToArray();
			}
		}

		public int NoROI
		{
			get
			{
				return ROIParameters.Length;
			}
		}
		ROIParameter[] ROIParameters;
*/
		int _cycles;

		#region *Spectraプロパティ
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
		public DepthProfile(string directory)
		{
			// WideScanと異なり、スペクトルがたくさんある。

			// まずパラメータだけでROISpectraを生成する。
			var roi_spectra = ReadParaPeak(directory);

			// レイヤーごと→元素範囲ごとで格納されているが、
			// 元素範囲ごと→レイヤーごとの方が使いやすいと思うので、そのように変換する。

			for (int j = 0; j < roi_spectra.Length; j++)
			{
				roi_spectra[j].Data = new EqualIntervalData[_cycles];
			}

			// データを読み込む。
			using (var reader = new BinaryReader(new FileStream(Path.Combine(directory, "data.peak"), FileMode.Open, FileAccess.Read)))
			{
				// レイヤーごと
				for (int i = 0; i < _cycles; i++)
				{
					// 元素範囲ごと
					for (int j = 0; j < roi_spectra.Length; j++)
					{
						roi_spectra[j].Data[i] = new EqualIntervalData(reader, roi_spectra[j].Parameter.PointsCount);
					}
				}
			}

			_spectra = roi_spectra.ToDictionary(spec => spec.Name);
		}
		#endregion


		/*
		public void DrawChart(int roi)
		{
			
			_spectra[roi].Generate
		}
		*/
/*
		protected struct ROIParameter
		{
			public SpectrumParameter SpectrumParameter
			{
				get
				{
					return new SpectrumParameter { Start = ScanStart, Step = ScanStep, Count = noPoints };
				}
			}
			public string Name;
			public decimal ScanStart;
			public decimal ScanStop;
			public decimal ScanStep;
			public int noPoints;
		}
		*/

		// コンストラクタから呼び出すことを考慮して、asyncにはしていない。
		#region *パラメータを読み込む(ReadParaPeak)
		protected ROISpectra[] ReadParaPeak(string directory)
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
			ROISpectra[] roi_spectra;

			using (var reader = new StreamReader(Path.Combine(directory, "para.peak")))
			{
				while (reader.Peek() > -1)
				{
					var line = reader.ReadLine();
					var cols = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
					if (cols.Count() > 1)
					{
						switch (cols[0])
						{
							case "$AP_DEP_CYCLES":
								_cycles = Convert.ToInt32(cols[1]);

								break;
							case "$AP_DEP_ROI_NOFEXE":
								int count = Convert.ToInt32(cols[1]);
								roi_spectra = new ROISpectra[count];
								for (int i = 0; i < count; i++)
								{
									// ここで各要素を初期化しておく。
									roi_spectra[i] = new ROISpectra();
								}
								goto SCAN_ROI;	// switch内でループから脱出するための黒魔術。
						}
					}
				}
				throw new ApplicationException("para.peakファイルに $AP_DEP_ROI_NOFEXEキーがありませんでした。");

				// roi_parametersが初期化されていることを保証するために、ループを2つに分ける。
				SCAN_ROI:

				int ch;
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
