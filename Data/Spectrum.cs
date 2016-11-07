using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.Data
{

	#region ScanParameterクラス
	/// <summary>
	/// スキャンのパラメータを保持します。
	/// </summary>
	public class ScanParameter
	{
		#region *Startプロパティ
		/// <summary>
		/// スキャンの開始位置を取得／設定します。
		/// </summary>
		public decimal Start
		{
			get; set;
		}
		#endregion

		#region *Stopプロパティ
		/// <summary>
		/// スキャンの終了位置を取得／設定します。
		/// </summary>
		public decimal Stop
		{
			get; set;
		}
		#endregion


		#region *Stepプロパティ
		/// <summary>
		/// スキャンの間隔を取得／設定します。
		/// </summary>
		public decimal Step
		{
			get; set;
		}
		#endregion

		#region *PointsCountプロパティ
		/// <summary>
		/// 測定点数を取得します。
		/// </summary>
		public int PointsCount
		{
			get
			{
				// 普通は割り切れるはず。そうならなければユーザの責任。
				return Convert.ToInt32(decimal.Floor((Stop - Start) / Step)) + 1;
			}
		}
		#endregion

		/// <summary>
		/// シフトされたスペクトルのパラメータを取得します。
		/// </summary>
		/// <param name="pitch"></param>
		/// <returns></returns>
		public ScanParameter GetShiftedParameter(decimal pitch)
		{
			return new ScanParameter
			{
				Start = this.Start + pitch,
				Stop = this.Stop + pitch,
				Step = this.Step
			};
		}

		#region *微分スペクトルの範囲を取得(GetDiffentiatedParameter)
		/// <summary>
		/// 微分スペクトルの範囲を取得します。
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public ScanParameter GetDifferentiatedParameter(int m)
		{
			return new ScanParameter
			{
				Start = this.Start + m * this.Step,
				Stop = this.Stop - m * this.Step,
				Step = this.Step
			};
		}
		#endregion

	}
	#endregion


	#region EqualIntervalDataクラス
	/// <summary>
	/// 独立変数が等間隔なデータを扱うクラスです。
	/// </summary>
	public class EqualIntervalData : List<decimal>
	{

		#region *IsRawDataプロパティ
		/// <summary>
		/// 生データかどうかを示す値を取得します。
		/// </summary>
		public bool IsRawData
		{
			get
			{
				return _isRawData;
			}
		}
		bool _isRawData = false;
		#endregion

		#region *コンストラクタ(EqualIntervalData)
		public EqualIntervalData()
		{ }

		/// <summary>
		/// readerから、1データを4バイトのビッグエンディアンな整数型として読み込みます。
		/// </summary>
		/// <param name="reader"></param>
		public EqualIntervalData(BinaryReader reader)
		{
			while (reader.PeekChar() > -1)
			{
				// エンディアンが逆なので、単純にreader.ReadInt32()とはいかない！
				int count = 0;
				for (int j = 0; j < 4; j++)
				{
					count += reader.ReadByte() * (1 << (24 - 8 * j));
				}
				this.Add(count);
			}
			this._isRawData = true;
		}

		/// <summary>
		/// readerから、1データを4バイトのビッグエンディアンな整数型として読み込みます。
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="reader"></param>
		/// <param name="length">読み込むデータの数です。</param>
		public EqualIntervalData(BinaryReader reader, int length)
		{
			for (int i = 0; i < length; i++)
			{
				// エンディアンが逆なので、単純にreader.ReadInt32()とはいかない！
				int count = 0;
				for (int j = 0; j < 4; j++)
				{
					count += reader.ReadByte() * (1 << (24 - 8 * j));
				}
				this.Add(count);
			}
			this._isRawData = true;
		}
		#endregion

		#region *微分データを取得(Differentiate)
		/// <summary>
		/// 2次？のSavizky-Golay法によって微分した結果を返します。
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public EqualIntervalData Differentiate(int m, decimal step)
		{
			var data = new EqualIntervalData();

			for (int i = m; i < this.Count - m; i++)
			{
				decimal count = 0;
				for (int j = 1; j <= m; j++)
				{
					count += this[i + j] * j;
					count -= this[i - j] * j;
				}
				data.Add(3 * count / (m * (m + 1) * (2 * m + 1) * step));
			}
			return data;
		}
		#endregion

		#region *データのCSV出力用文字列を取得(GetDataForCsv)
		/// <summary>
		/// CSV出力用の文字列を取得します。
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public string GetDataForCsv(int i)
		{
			// 生データであればカウント値を整数表示、
			// 処理後のデータであれば、小数第2位まで表示。
			return IsRawData ? this[i].ToString() : this[i].ToString("f2");
		}
		#endregion


		#region *平方残差の合計を取得(GetTotalSquareResidual)
		/// <summary>
		/// 自身とreferenceの平方残差の合計を取得します。
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="gain"></param>
		/// <returns></returns>
		public decimal GetTotalSquareResidual(IList<decimal> reference, decimal gain)
		{
			return GetTotalSquareResidual(this, reference, gain);
		}

		/// <summary>
		/// 自身とreferenceの平方残差の合計を取得します。
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="gain"></param>
		/// <returns></returns>
		public static decimal GetTotalSquareResidual(IList<decimal> data, IList<decimal> reference, decimal gain)
		{
			decimal residual = 0;

			for (int i = 0; i < data.Count; i++)
			{
				var diff = (data[i] - gain * reference[i]);
				residual += diff * diff;
			}
			return residual;
		}
		#endregion

	}
	#endregion

	/*
		#region ROISpectrumクラス
		public class ROISpectrum
		{
			public string Name { get; set; }

			public ScanParameter Parameter { get; set; }

			public EqualIntervalData Data { get; set; }
		}
		#endregion
	*/


	#region ROISpectraクラス
	/// <summary>
	/// 1つのROIについての複数層のスペクトルデータを格納します。
	/// </summary>
	public class ROISpectra
	{
		#region *Nameプロパティ
		/// <summary>
		/// ROI系列の名前を取得／設定します。(※通常は元素名です。)
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



	#region 以下のクラスはいったん廃止する。

	/*


		#region [abstract]SpectrumBaseクラス
		public abstract class SpectrumBase : GeneralHelper.PltFileGeneratorBase
		{

			#region パラメータ関連プロパティ

			#region *Parameterプロパティ
			public SpectrumParameter Parameter
			{
				get { return _parameter; }
			}
			SpectrumParameter _parameter;
			#endregion

			#region *Startプロパティ
			public decimal Start
			{
				get
				{
					return _parameter.Start;
				}
			}
			#endregion

			#region *Stopプロパティ
			public decimal Stop
			{
				get
				{
					return _parameter.Stop;
				}
			}
			#endregion

			#region *Stepプロパティ
			public decimal Step
			{
				get
				{
					return _parameter.Step;
				}
			}
			#endregion

			#region *Lengthプロパティ
			public int Length
			{
				get
				{
					return _parameter.Count;
				}
			}
			#endregion

			#endregion

			#region *コンストラクタ(SpectrumBase)
			public SpectrumBase(SpectrumParameter parameter)
			{
				_parameter = parameter;
			}
			#endregion

			#region *[abstract]csvとしてエクスポート(ExportCsv)
			public void ExportCsv(string destination)
			{
				using (StreamWriter writer = new StreamWriter(destination, false))
				{
					ExportCsv(writer);
				}
			}

			public abstract void ExportCsv(TextWriter writer);
			#endregion

		}
		#endregion

		#region Spectrumクラス
		public class Spectrum : SpectrumBase
		{

			public EqualIntervalData Data
			{
				get
				{
					return _data;
				}
			}
			EqualIntervalData _data;


			#region *コンストラクタ(Spectrum)
			public Spectrum(SpectrumParameter parameter) : base(parameter)
			{
				_data = new EqualIntervalData();
			}

			public Spectrum(EqualIntervalData data, SpectrumParameter parameter) : base(parameter)
			{
				_data = data;
			}

			public Spectrum(BinaryReader reader, SpectrumParameter parameter) : base(parameter)
			{
				_data = new EqualIntervalData(reader, parameter.Count);
			}
			#endregion


			#region *微分スペクトルを取得(Differentiate)
			/// <summary>
			/// Savitzky-Golay法による1次微分スペクトルを返します。両端は削っています。
			/// </summary>
			/// <returns></returns>
			public Spectrum Differentiate(int m)
			{
				var spectrum = new Spectrum (
					this.Data.Differentiate(m),
					new SpectrumParameter { Start = this.Start + m * this.Step, Step = this.Step, Count = this.Length - 2 * m }
				);
				return spectrum;
			}
			#endregion

			#region *csvとしてエクスポート(ExportCsv)
			public override void ExportCsv(TextWriter writer)
			{
				for (int i = 0; i < Length; i++)
				{
					writer.WriteLine($"{Start + i * Step},{Data[i].ToString("f2")}");
				}
			}
			#endregion


			public override void Generate(StreamWriter writer, DateTime time)
			{
				throw new NotImplementedException();
			}

		}
		#endregion



		#region SpectrumParameter構造体
		public struct SpectrumParameter
		{
			public decimal Start { get; set; }
			public decimal Step { get; set; }
			public int Count { get; set; }

			public decimal Stop
			{
				get { return Start + Step * (Count - 1); }
			}
		}
		#endregion


		#region DepthSpectraクラス
		public class DepthSpectra : SpectrumBase
		{
			// 複数のEqualIntervalDataと共通のParameter。1元素に対するスペクトル群。

			#region *Dataプロパティ
			public List<EqualIntervalData> Data
			{
				get
				{
					return _data;
				}
			}
			List<EqualIntervalData> _data = new List<EqualIntervalData>();
			#endregion

			#region *Nameプロパティ
			public string Name
			{
				get { return _name; }
				set { _name = value; }
			}
			string _name = string.Empty;
			#endregion

			public DepthSpectra(SpectrumParameter parameter) : base(parameter)
			{
			}

			public DepthSpectra Differentiate(int m)
			{
				var spectrum = new DepthSpectra(
					new SpectrumParameter { Start = this.Start + m * this.Step, Step = this.Step, Count = this.Length - 2 * m }
				);

				foreach (var data in Data)
				{
					spectrum.Data.Add(data.Differentiate(m));
				}
				return spectrum;

			}


			public override void ExportCsv(TextWriter writer)
			{
				for (int i = 0; i < Parameter.Count; i++)
				{
					List<string> cols = new List<string>();

					decimal x = Parameter.Start + i * Parameter.Step;
					cols.Add(x.ToString());
					foreach (var data in Data)
					{
						cols.Add(data[i].ToString("f2"));
					}
					writer.WriteLine(string.Join(",", cols.ToArray()));
				}

			}


			public override void Generate(StreamWriter writer, DateTime time)
			{
				writer.WriteLine("set terminal png size 800,600");
				writer.WriteLine("cd '.'");
				writer.WriteLine("set output 'test-chart.png'");  // ※とりあえず決め打ち．
				writer.WriteLine("set encoding utf8");

				writer.WriteLine("set title 'Depth'"); // # 出力フォーマットによっては全角の"＋"は使わない方がよい．

				// 各軸の設定
				writer.WriteLine("set xlabel 'K.E. / eV'");
				writer.WriteLine("set xrange[400 : 440 ]");
				writer.WriteLine("set xtics border mirror norotate 400,5,440");

				writer.WriteLine("set ylabel 'Intensity'");
				writer.WriteLine("set yrange[-300 : 300 ]");
				writer.WriteLine("set ytics border - 300,100,300");

				// レイアウトの設定

				// プロットするデータの設定
				writer.WriteLine("set datafile separator ','");
				writer.WriteLine("plot 'depth-0-diff.csv' using 1:2 w lines lc rgbcolor '#FF0000', \\");
				writer.WriteLine(" 'depth-0-diff.csv' using 1:3 w lines lc rgbcolor '#FF3300', \\");
				writer.WriteLine(" 'depth-0-diff.csv' using 1:4 w lines lc rgbcolor '#FF6600'");

				writer.WriteLine("set output");
				writer.WriteLine("exit");

			}


		}
		#endregion

		*/
	#endregion

}
