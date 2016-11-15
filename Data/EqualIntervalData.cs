using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.Data
{

	#region EqualIntervalDataクラス
	/// <summary>
	/// 独立変数が等間隔なデータを扱うクラスです。
	/// </summary>
	public class EqualIntervalData : List<decimal>
	{

		#region *IsRawDataプロパティ
		/// <summary>
		/// 生データかどうかを示す値を取得します。※これ使ってるの？
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

		public EqualIntervalData GetSubData(int startIndex, int endIndex)
		{
			if (startIndex >= 0 && endIndex < this.Count && startIndex <= endIndex)
			{
				var data = new EqualIntervalData();
				for (int i = startIndex; i <= endIndex; i++)
				{
					data.Add(this[i]);
				}
				data._isRawData = true;
				return data;
			}
			else
			{
				throw new ArgumentException("もとのデータの範囲内で指定して下さい。");
			}
		}

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

		public static decimal GetTotalSquareResidual(IList<decimal> data,  double[] gains, params IList<decimal>[] references)
		{
			decimal residual = 0;

			for (int i = 0; i < data.Count; i++)
			{
				var diff = data[i];
				for (int j = 0; j < gains.Length; j++)
				{
					diff -= (decimal)gains[j] * references[j][i];
				}
				residual += diff * diff;
			}
			return residual;
		}

		#endregion

	}
	#endregion

}
