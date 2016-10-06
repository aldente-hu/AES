﻿using System;
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
			using (var reader = new BinaryReader(new FileStream(Path.Combine(directory, "data"), FileMode.Open, FileAccess.Read)))
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

		/// <summary>
		/// 前後のデータから線形補間した値を返します。
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		public decimal GetInterpolatedDataAt(decimal x)
		{
			if (x >= Parameter.Start && x <= Parameter.Stop)
			{
				var index = (x - Parameter.Start) / Parameter.Step;
				int i = Convert.ToInt32(Decimal.Floor(index));
				decimal d = index - i;

				return (1 - d) * Data[i] + d * Data[i + 1];
			}
			else
			{
				throw new ArgumentException();
			}
		}

		/// <summary>
		/// 指定した範囲の、線形補間されたデータを返します。
		/// </summary>
		/// <param name="start"></param>
		/// <param name="step"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public List<decimal> GetInterpolatedData(decimal start, decimal step, int count)
		{
			List<decimal> data = new List<decimal>();
			for (int i = 0; i < count; i++)
			{
				data.Add(GetInterpolatedDataAt(start + step * i));
			}
			return data;
		}



		public WideScan Differentiate(int m)
		{
			return new WideScan
			{
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

}
