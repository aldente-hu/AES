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
		/// X軸のシフト補正値を取得／設定します。
		/// </summary>
		public decimal XShift
		{
			get; set;
		}

		/// <summary>
		/// 測定時の電流値(A単位)を取得／設定します。
		/// </summary>
		public decimal Current
		{
			get; set;
		}

		/// <summary>
		/// 測定時のDwell Time(s単位)を取得／設定します。
		/// </summary>
		public decimal Dwell
		{ get; set; }

		/// <summary>
		/// 正規化するために、データに乗じるゲイン係数を取得／設定します。
		/// </summary>
		public decimal NormalizationGain
		{
			get
			{
				return 1e-7M / Current * 0.1M / Dwell;
			}
		}


		public ScanParameter()
		{
			//NormalizationGain = 1;
		}

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
				Step = this.Step,
				Current = this.Current,
				Dwell = this.Dwell,
				XShift = this.XShift
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
				Step = this.Step,
				Current = this.Current,
				Dwell = this.Dwell,
				XShift = this.XShift
			};
		}
		#endregion

		/// <summary>
		/// 範囲を制限したパラメータを返します。
		/// </summary>
		/// <param name="start">制限された範囲の開始インデックス。</param>
		/// <param name="stop">制限された範囲の終了インデックス。</param>
		/// <returns></returns>
		public ScanParameter ShrinkRange(int start, int stop)
		{
			if (start > stop)
			{
				throw new ArgumentException("stopはstartより大きくして下さい。");
			}
			return new ScanParameter
			{
				Start = this.Start + start * this.Step,
				Stop = this.Start + stop * this.Step,
				Step = this.Step,
				Current = this.Current,
				Dwell = this.Dwell,
				XShift = this.XShift
			};
		}


		/// <summary>
		/// 圧力や電流を表す文字列を、数値に換算します。(※メソッド名は後で再考。)
		/// </summary>
		/// <param name="pressure"></param>
		/// <returns></returns>
		public static decimal ConvertPressure(string pressure)
		{
			var cols = pressure.Split(' ');
			if (cols.Length < 2)
			{
				throw new ArgumentException("pressure引数が圧力を表す文字列になっていません。", "pressure");
			}
			return Convert.ToDecimal(cols[0]) * (decimal)Math.Pow(0.1, Convert.ToInt32(cols[1]));
		}

		/// <summary>
		/// 圧力や電流を表す文字列を、数値に換算します。(※メソッド名は後で再考。)
		/// </summary>
		/// <param name="pressure"></param>
		/// <returns></returns>
		public static decimal ConvertPressure(string pressure, string ex)
		{
			return Convert.ToDecimal(pressure) * (decimal)Math.Pow(0.1, Convert.ToInt32(ex));
		}

	}
	#endregion

}
