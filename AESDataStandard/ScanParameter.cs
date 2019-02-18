using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{
	// (0.3.0) 傾斜角($AP_STGTILT)を考慮．
	// (0.2.0)
	#region ScanParameterクラス
	/// <summary>
	/// スキャンのパラメータを保持します。
	/// </summary>
	public class ScanParameter
	{
		/// <summary>
		/// 装置のオージェ電子取り出し角です(deg単位)．
		/// </summary>
		public static double TakeoffAngle = 60;

		#region プロパティ

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

		#region *XShiftプロパティ
		/// <summary>
		/// X軸のシフト補正値を取得／設定します。
		/// </summary>
		public decimal XShift
		{
			get; set;
		}
		#endregion

		#region *Currentプロパティ
		/// <summary>
		/// 測定時の電流値(A単位)を取得／設定します。
		/// </summary>
		public decimal Current
		{
			get; set;
		}
		#endregion

		#region *Dwellプロパティ
		/// <summary>
		/// 測定時のDwell Time(s単位)を取得／設定します。
		/// </summary>
		public decimal Dwell
		{ get; set; }
		#endregion

		// (0.3.0)
		#region *Tiltプロパティ
		/// <summary>
		/// 試料の傾斜角を取得／設定します．
		/// </summary>
		public double Tilt
		{
			get; set;
		}
		#endregion

		// (0.3.0) Tilt(とTakeoffAngle)を考慮．
		#region *NormalizationGainプロパティ
		/// <summary>
		/// 正規化するために、データに乗じるゲイン係数を取得します。
		/// </summary>
		public decimal NormalizationGain
		{
			get
			{
				// JEOLのAP92(「AESのピーク分離解析とその応用」)では，[10kV，]10nA，(dwell)1s, (tilt)30degに正規化しているが，
				// ここでは100nA，100ms．0degに正規化する？
				return 1e-7M / Current * 0.1M / Dwell / (1 + Convert.ToDecimal(Math.Tan(Math.PI * TakeoffAngle / 180) * Math.Tan(Math.PI * this.Tilt / 180)));
			}
		}
		#endregion

		#endregion

		#region *コンストラクタ(ScanParameter)
		public ScanParameter()
		{
		}
		#endregion

		public static ScanParameter Generate(string directory)
		{
			var parameter = new ScanParameter();
			parameter.LoadFrom(directory);
			return parameter;
		}

		// (0.2.0)
		/// <summary>
		/// 指定したディレクトリのparaファイルを読み込み，Scanparameterオブジェクトを生成します．
		/// </summary>
		/// <param name="directory"></param>
		/// <returns></returns>
		public static async Task<ScanParameter> GenerateAsync(string directory)
		{
			var parameter = new ScanParameter();
			await parameter.LoadFromAsync(directory);
			return parameter;
		}

		public async Task LoadFromAsync(string directory)
		{
			using (var reader = new StreamReader(new FileStream(Path.Combine(directory, "para"), FileMode.Open, FileAccess.Read)))
			{
				while (reader.Peek() > -1)
				{
					var line = await reader.ReadLineAsync();
					var cols = line.Split(new string[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
					if (cols.Count() > 1)
					{
						switch (cols[0])
						{
							case "$AP_SPC_WSTART":
								Start = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_SPC_WSTOP":
								Stop = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_SPC_WSTEP":
								Step = Convert.ToDecimal(cols[1]);
								break;
							// とりあえず無視する。
							//case "$AP_SPC_WPOINTS":
							//	_scanParameter.noPoints = Convert.ToInt32(cols[1]);
							//	break;

							// 正規化に関しては、とりあえず電流とdwellだけ考慮する。Tiltや加速電圧はあとで考える。
							case "$AP_PCURRENT":
								Current = ScanParameter.ConvertPressure(cols[1]);
								break;
							case "$AP_SPC_WDWELL":
								Dwell = Convert.ToDecimal(cols[1]) * 1e-3M;
								break;
							case "$AP_SPC_W_XSHIFT":
								XShift = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_STGTILT":
								Tilt = Convert.ToDouble(cols[1]);
								break;


						}
					}
				}
			}

		}

		// とりあえず．
		public void LoadFrom(string directory)
		{
			using (var reader = new StreamReader(new FileStream(Path.Combine(directory, "para"), FileMode.Open, FileAccess.Read)))
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
								Start = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_SPC_WSTOP":
								Stop = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_SPC_WSTEP":
								Step = Convert.ToDecimal(cols[1]);
								break;
							// とりあえず無視する。
							//case "$AP_SPC_WPOINTS":
							//	_scanParameter.noPoints = Convert.ToInt32(cols[1]);
							//	break;

							// 正規化に関しては、とりあえず電流とdwellだけ考慮する。Tiltや加速電圧はあとで考える。
							case "$AP_PCURRENT":
								Current = ScanParameter.ConvertPressure(cols[1]);
								break;
							case "$AP_SPC_WDWELL":
								Dwell = Convert.ToDecimal(cols[1]) * 1e-3M;
								break;
							case "$AP_SPC_W_XSHIFT":
								XShift = Convert.ToDecimal(cols[1]);
								break;
							case "$AP_STGTILT":
								Tilt = Convert.ToDouble(cols[1]);
								break;
						}
					}
				}
			}

		}


		// (0.3.0) Tiltに対応．
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
				XShift = this.XShift,
				Tilt = this.Tilt
			};
		}

		// (0.3.0) Tiltに対応．
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
				XShift = this.XShift,
				Tilt = this.Tilt
			};
		}
		#endregion

		// (0.3.0) Tiltに対応．
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
				XShift = this.XShift,
				Tilt = this.Tilt
			};
		}

		#region *[static]圧力などを表す文字列を数値に変換(ConvertPressure)

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
			return ConvertPressure(cols[0], cols[1]);
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

		#endregion

	}
	#endregion



}
