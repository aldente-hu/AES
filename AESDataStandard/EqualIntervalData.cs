using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{
	using Helpers;

	#region EqualIntervalDataクラス
	/// <summary>
	/// 独立変数が等間隔なデータを扱うクラスです。
	/// </summary>
	public class EqualIntervalData : List<decimal>
	{

		#region *IsRawDataプロパティ
		/// <summary>
		/// 生データかどうかを示す値を取得します。※これ使ってるの？←一応，GetDataForCsvで使っています...
		/// </summary>
		public bool IsRawData { get; private set; } = false;
		#endregion

		#region *コンストラクタ(EqualIntervalData)
		public EqualIntervalData()
		{ }
		#endregion

		// (0.2.0)
		#region *生成(GenerateAsync)
		/// <summary>
		/// JEOL形式のスペクトルデータを読み込みます．
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static async Task<EqualIntervalData> GenerateAsync(BinaryReader reader)
		{
			var data = new EqualIntervalData();

			while (reader.PeekChar() > -1)
			{
				// エンディアンが逆なので、単純にreader.ReadInt32()とはいかない！
				int count = await reader.ReadInt32Async();
				data.Add(count);
			}
			data.IsRawData = true;

			return data;
		}


		// (0.2.0)
		public static async Task<EqualIntervalData> GenerateAsync(BinaryReader reader, int length)
		{
			var data = new EqualIntervalData();

			for (int i = 0; i < length; i++)
			{
				// エンディアンが逆なので、単純にreader.ReadInt32()とはいかない！
				int count = await reader.ReadInt32Async();
				data.Add(count);
			}
			data.IsRawData = true;

			return data;
		}

		public static EqualIntervalData Generate(BinaryReader reader)
		{
			var data = new EqualIntervalData();

			while (reader.PeekChar() > -1)
			{
				// エンディアンが逆なので、単純にreader.ReadInt32()とはいかない！
				int count = reader.ReadInt32Inverse();
				data.Add(count);
			}
			data.IsRawData = true;

			return data;
		}



		#endregion

		// (0.2.1)
		#region *[static][async]テキストファイルから生成(GeenrateFromText)
		public static async Task<EqualIntervalData> GenerateFromText(StreamReader reader, bool isRaw)
		{
			var data = new EqualIntervalData();

			while (!reader.EndOfStream)
			{
				var count = await reader.ReadLineAsync();
				if (isRaw)
				{
					data.Add(int.Parse(count));
				}
				else
				{
					data.Add(decimal.Parse(count));
				}
			}
			data.IsRawData = isRaw;
			return data;
		}
		#endregion

		#region *一部分を取得(GetSubData)
		/// <summary>
		/// 自身の一部範囲を含んだインスタンスを生成して返します．
		/// </summary>
		/// <param name="startIndex"></param>
		/// <param name="endIndex"></param>
		/// <returns></returns>
		public EqualIntervalData GetSubData(int startIndex, int endIndex)
		{
			if (startIndex >= 0 && endIndex < this.Count && startIndex <= endIndex)
			{
				var data = new EqualIntervalData();
				for (int i = startIndex; i <= endIndex; i++)
				{
					data.Add(this[i]);
				}
				data.IsRawData = true;
				return data;
			}
			else
			{
				throw new ArgumentException("もとのデータの範囲内で指定して下さい。");
			}
		}
		#endregion

		#region *差を取得(Substract)
		/// <summary>
		/// 各要素の差をとった新たなインスタンスを返します．(otherの方が要素が少ない場合，例外を発生するような気がします．)
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public EqualIntervalData Substract(IList<decimal> other)
		{
			var data = new EqualIntervalData();
			for (int i = 0; i < this.Count; i++)
			{
				data.Add(this[i] - other[i]);
			}
			data.IsRawData = false;
			return data;

		}
		#endregion


		#region *微分データを取得(Differentiate)
		/// <summary>
		/// 2次？のSavizky-Golay法によって微分した結果を返します。要素数が2mだけ少なくなります．
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

		// (0.2.1)
		#region *テキストとして出力(OutputText)
		/// <summary>
		/// 1行につき1つの要素をテキスト化して出力します．
		/// </summary>
		/// <param name="writer"></param>
		/// <returns></returns>
		public async Task OutputText(StreamWriter writer)
		{
			for (int i = 0; i < this.Count; i++)
			{
				await writer.WriteLineAsync(GetDataForCsv(i));
			}
		}
		#endregion

		// (0.2.1) とりあえず同期メソッドで。
		#region *バイナリとして出力(OutputBinary)
		/// <summary>
		/// JEOL形式のバイナリ(1データ32bit)として出力します．
		/// </summary>
		/// <param name="writer"></param>
		public void OutputBinary(BinaryWriter writer)
		{
			for (int i = 0; i < this.Count; i++)
			{
				int count = Convert.ToInt32(decimal.Truncate(this[i]));
				writer.Write((byte)(count >> 24));
				writer.Write((byte)(count >> 16));
				writer.Write((byte)(count >> 8));
				writer.Write((byte)(count % 256));
			}
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

		// (0.1.1)定数項が含まれる場合を考慮。
		public static decimal GetTotalSquareResidual(IList<decimal> data, double[] gains, params IList<decimal>[] references)
		{
			decimal residual = 0;

			for (int i = 0; i < data.Count; i++)
			{
				var diff = data[i];
				for (int j = 0; j < references.Length; j++)
				{
					diff -= (decimal)gains[j] * references[j][i];
				}
				// gainsが定数項を含む場合。
				if (gains.Length > references.Length)
				{
					diff -= (decimal)gains[references.Length];
				}
				residual += diff * diff;
			}
			return residual;
		}

		#endregion

	}
	#endregion

}
