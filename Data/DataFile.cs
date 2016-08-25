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
		ScanParameter _scanParameter = new ScanParameter();

		public WideScan(string directory)
		{
			ReadPara(directory);

			using (var reader = new BinaryReader(new FileStream(Path.Combine(directory , "data"), FileMode.Open, FileAccess.Read)))
			{
				_spectrum = new Spectrum(reader, _scanParameter.SpectrumParameter);
			}

		}

		public Spectrum Spectrum
		{
			get
			{
				return _spectrum;
			}
		}
		Spectrum _spectrum;


		// WideScanのコンストラクタから呼び出すことを考慮して、asyncにはしていない。
		protected void ReadPara(string directory)
		{
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
								_scanParameter.ScanStart = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_SPC_WSTOP":
								_scanParameter.ScanStop = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_SPC_WSTEP":
								_scanParameter.ScanStep = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_SPC_WPOINTS":
								_scanParameter.noPoints = Convert.ToInt32(cols[1]);
								break;
						}
					}
				}
			}
		}

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
	}
	#endregion
	
	// このレイヤーはいるのかな？直接DepthSpectraを使ってもよい？
	#region DepthProfileクラス
	public class DepthProfile
	{
		public int NoROI
		{
			get
			{
				return ROIParameters.Length;
			}
		}
		ROIParameter[] ROIParameters;

		int _cycles;

		DepthSpectra[] _spectra;

		public DepthProfile(string directory)
		{
			// WideScanと異なり、スペクトルがたくさんある。


			ReadParaPeak(directory);

			// レイヤーごと→元素範囲ごとで格納されているが、
			// 元素範囲ごと→レイヤーごとの方が使いやすいと思うので、そのように変換する。
			_spectra = new DepthSpectra[NoROI];
			for (int j=0; j<NoROI; j++)
			{
				_spectra[j] = new DepthSpectra(ROIParameters[j].SpectrumParameter);
			}
			

			using (var reader = new BinaryReader(new FileStream(Path.Combine(directory, "data.peak"), FileMode.Open, FileAccess.Read)))
			{
				// レイヤーごと
				for (int i = 0; i < _cycles; i++)
				{
					// 元素範囲ごと
					for (int j = 0; j < NoROI; j++)
					{
						_spectra[j].Data.Add(new EqualIntervalData(reader, ROIParameters[j].noPoints));
					}
				}
			}
		}

		public void ExportCsv(int roi, string destination, bool diff)
		{
			var data = diff ? _spectra[roi].Differentiate(3) : _spectra[roi];
			data.ExportCsv(destination);
		}


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

		// コンストラクタから呼び出すことを考慮して、asyncにはしていない。
		#region *パラメータを読み込む(ReadParaPeak)
		protected void ReadParaPeak(string directory)
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
			//ROIParameter[] roi_parameters;

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
								ROIParameters = new ROIParameter[6];
								for (int i = 0; i < count; i++)
								{
									ROIParameters[i] = new ROIParameter();
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
								ROIParameters[ch].Name = cols[2];
								break;
							case "$AP_DEP_ROI_START":
								ch = Convert.ToInt32(cols[1]) - 1;
								ROIParameters[ch].ScanStart = Convert.ToDecimal(cols[2]);
								break;
							case "$AP_DEP_ROI_STOP":
								ch = Convert.ToInt32(cols[1]) - 1;
								ROIParameters[ch].ScanStop = Convert.ToDecimal(cols[2]);
								break;
							case "$AP_DEP_ROI_STEP":
								ch = Convert.ToInt32(cols[1]) - 1;
								ROIParameters[ch].ScanStep = Convert.ToDecimal(cols[2]);
								break;
							case "$AP_DEP_ROI_POINTS":
								ch = Convert.ToInt32(cols[1]) - 1;
								ROIParameters[ch].noPoints = Convert.ToInt32(cols[2]);
								break;
						}
					}
				}
			}
		}
		#endregion

	}
	#endregion




}
