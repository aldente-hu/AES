﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

// 一応ここにおいておくが，プロジェクトとしては別のところに分離した方がいいような気がする．

namespace HirosakiUniversity.Aldente.AES.Data.Standard
{

	// (0.3.0) Gnuplot5.2に対応？
	#region Gnuplotクラス
	public class Gnuplot
	{

		#region プロパティ

		/// <summary>
		/// 出力するグラフ画像の形式を取得／設定します。
		/// </summary>
		public ChartFormat Format
		{
			get; set;
		}

		/// <summary>
		/// グラフ画像の横幅を取得／設定します。
		/// </summary>
		public int Width
		{ get; set; }

		/// <summary>
		/// グラフ画像の縦幅を取得／設定します。
		/// </summary>
		public int Height
		{ get; set; }

		/// <summary>
		/// (obsolete)フォントサイズを取得／設定するものでした。現在は無視されます。
		/// </summary>
		[Obsolete("Gnuplot5.2では（多くのターミナルで）使えなくなりました．この値はもはや無視されます。FontScaleプロパティを使って下さい．")]
		public int FontSize
		{ get; set; }

		/// <summary>
		/// フォントスケールを取得／設定します．既定のフォントサイズに対する倍率になります．
		/// </summary>
		public double FontScale
		{ get; set; }


		/// <summary>
		/// グラフ画像の出力先を取得／設定します。
		/// </summary>
		public string Destination
		{ get; set; }

		#region *DataFileSeparatorプロパティ
		public string DataFileSeparator
		{
			get
			{
				return _dataFileSeparator;
			}
			set
			{
				_dataFileSeparator = value;
			}
		}
		string _dataFileSeparator = ",";
		#endregion

		/// <summary>
		/// グラフのタイトルを取得／設定します。
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// X軸のタイトルを取得／設定します。
		/// </summary>
		public string XTitle { get; set; }

		/// <summary>
		/// Y軸のタイトルを取得／設定します。
		/// </summary>
		public string YTitle { get; set; }

		// (0.0.5)
		/// <summary>
		/// X軸の設定を取得／設定します。
		/// </summary>
		public ChartAxisSettings XAxis { get; set; }

		// (0.0.5)
		/// <summary>
		/// Y軸の設定を取得／設定します。
		/// </summary>
		public ChartAxisSettings YAxis { get; set; }

		/// <summary>
		/// データ系列のリストを取得します。
		/// </summary>
		public DataSeries DataSeries
		{
			get
			{
				return _series;
			}
		}
		private DataSeries _series = new DataSeries();

		#endregion

		public static string BinaryPath = @"C:\Program Files\gnuplot\bin\gnuplot.exe";



		// コマンド群を生成する部分と、それをwriterに流し込む部分を分けた方がよい。

		#region *チャートを描画(Draw)
		public async Task Draw()
		{

			using (var process = new Process())
			{
				process.StartInfo.FileName = BinaryPath;
				//process.StartInfo.Arguments = pltFile;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;  // これを設定しないと，CreateNoWindowは無視される．
				process.StartInfo.RedirectStandardInput = true;
				process.Start();

				//	GenerateCommandSquence().ForEach(
				//		async line => await process.StandardInput.WriteLineAsync(line)
				//	);
				// ※↑は不可。(StandardInputへの同時アクセスが起こる。)
				var commands = GenerateCommandSequence();
				foreach (var line in commands)
				{
					await process.StandardInput.WriteLineAsync(line);
				}

			}

		}
		#endregion

		#region *Pltファイルを出力(OutputPltFileAsync)
		public async Task OutputPltFileAsync(TextWriter writer)
		{
			var commands = GenerateCommandSequence();
			foreach (var line in commands)
			{
				await writer.WriteLineAsync(line);
			}
		}
		#endregion

		#region AESチャート用メソッド？

		// ※ここのregionのメソッドは、別クラスにした方がいいでしょう。

		// 仮の軸設定を行う。
		public void PreConfigureAxis()
		{
			var range = DataSeries.Scan();
			this.XAxis = new ChartAxisSettings
			{
				Range = range.XRange
			};
			this.YAxis = new ChartAxisSettings
			{
				Range = range.YRange
			};
		}


		#endregion

		#region *X軸範囲を決定(DefineXAxis)
		/// <summary>
		/// 与えられたデータ範囲から，X軸の範囲などを決定します．
		/// </summary>
		/// <param name="range"></param>
		public void DefineXAxis(Range range)
		{
			decimal x_interval = DefineInterval(range.Width);
			var x_min = decimal.Floor(range.Start / x_interval) * x_interval;
			var x_max = decimal.Ceiling(range.Stop / x_interval) * x_interval;
			XAxis = new ChartAxisSettings
			{
				Range = new Range(x_min, x_max),
				Tics = new AxisTicsSettings { Start = x_min, Stop = x_max, Increase = x_interval, Mirror = true }
			};
		}
		#endregion

		#region *Y軸範囲を決定(DefineYAxis)
		/// <summary>
		/// 与えられたデータ範囲から，Y軸の範囲などを決定します．
		/// </summary>
		/// <param name="range"></param>
		public void DefineYAxis(Range range)
		{
			decimal y_interval = DefineInterval(range.Width);
			var y_min = decimal.Floor(range.Start / y_interval) * y_interval;
			var y_max = decimal.Ceiling(range.Stop / y_interval) * y_interval;

			YAxis = new ChartAxisSettings
			{
				Range = new Range(y_min, y_max),
				Tics = new AxisTicsSettings { Start = y_min, Stop = y_max, Increase = y_interval, Rotate = false }
			};

		}
		#endregion


		#region *一連のコマンド列を生成(GenerateCommandSequence)
		public List<string> GenerateCommandSequence()
		{
			List<string> commands = new List<string>();

			switch (Format)
			{
				case ChartFormat.Svg:
					commands.Add($"set terminal svg enhanced size {Width},{Height} fontscale {FontScale}");
					break;
				case ChartFormat.Png:
					commands.Add($"set terminal png size {Width},{Height}");
					break;
			}


			commands.Add($"set output '{Destination}'");
			if (!string.IsNullOrEmpty(Title))
			{
				commands.Add($"set title '{Title}'");
			}


			// データをあらかじめなぞってみる。
			// ※これを外部から行い、x_axisなどを外部から与えるようにしたい。

			if (XAxis == null || YAxis == null)
			{
				var range = DataSeries.Scan();

				if (XAxis == null)
				{
					DefineXAxis(range.XRange);
				}
				if (YAxis == null)
				{
					DefineYAxis(range.YRange);
				}

				if (!string.IsNullOrEmpty(XTitle))
				{
					XAxis.Title = XTitle;
				}
				if (!string.IsNullOrEmpty(YTitle))
				{
					YAxis.Title = YTitle;
				}
			}

			XAxis.GetCommands("x").ForEach(c => commands.Add(c));
			YAxis.GetCommands("y").ForEach(c => commands.Add(c));

			commands.Add($"set datafile separator '{DataFileSeparator}'");

			commands.Add($"plot {string.Join(",", DataSeries)}");
			commands.Add("set output");

			return commands;
		}
		#endregion

		#region *[static]rangeからintervalを決定(DefineInterval)
		static decimal DefineInterval(decimal range)
		{
			// とりあえずあまり小さいのは考えない。
			if (range < 7) return 1;
			if (range < 15) return 2;
			if (range < 36) return 5;
			return 10 * DefineInterval(range / 10);
		}
		#endregion

	}
	#endregion

	#region DataSeriesクラス
	public class DataSeries : List<LineChartSeries>
	{

		#region *DataFileSeparatorプロパティ
		public string DataFileSeparator
		{
			get
			{
				return _dataFileSeparator;
			}
			set
			{
				_dataFileSeparator = value;
			}
		}
		string _dataFileSeparator = ",";
		#endregion

		#region *データ範囲を取得する(Scan)
		/// <summary>
		/// データをプローブして，XYの範囲を返します．※Y2軸には未対応です。
		/// </summary>
		/// <returns></returns>
		public ScannedRange Scan()
		{
			var source_columns = new Dictionary<string, XYColumns>();
			foreach (var series in this)
			{
				if (!source_columns.ContainsKey(series.SourceFile))
				{
					source_columns[series.SourceFile] = new XYColumns();
				}
				source_columns[series.SourceFile].XColumns.Add(series.XColumn - 1);
				source_columns[series.SourceFile].YColumns.Add(series.YColumn - 1);
			}

			// ☆とりあえず、ここで同期実行にしておく。
			var ranges = source_columns.Select(
				source => DetectRange(source)
			);

			return ScannedRange.Union(ranges.ToArray());
		}

		ScannedRange DetectRange(KeyValuePair<string, XYColumns> source)
		{
			Dictionary<int, ProbeCsvResult> results;
			using (var reader = new StreamReader(source.Key))
			{
				results = ProbeCsv(reader, source.Value.Columns);
			}

			var x_results = results.Where(r => source.Value.XColumns.Contains(r.Key));
			var x_min = x_results.Select(r => r.Value.Minimum).Min();
			var x_max = x_results.Select(r => r.Value.Maximum).Max();

			var y_results = results.Where(r => source.Value.YColumns.Contains(r.Key));
			var y_min = y_results.Select(r => r.Value.Minimum).Min();
			var y_max = y_results.Select(r => r.Value.Maximum).Max();

			return new ScannedRange
			{
				XRange = new Range(x_min.Value, x_max.Value),
				YRange = new Range(y_min.Value, y_max.Value)
			};

		}
		#endregion

		public Dictionary<int, ProbeCsvResult> ProbeCsv(TextReader reader, IEnumerable<int> cols)
		{
			var separators = new string[] { DataFileSeparator };

			// X軸とY軸のレンジを取得する？
			// とりあえず、全要素を取得する。Titleは無視。
			var result = new Dictionary<int, ProbeCsvResult>();

			while (reader.Peek() > 0)
			{
				// ここを非同期にすると、10回ちょっとここを通ったときに固まる。
				string line = reader.ReadLine();
				//Debug.WriteLine(line);
				var cells = line.Split(separators, StringSplitOptions.None);

				foreach (int i in cols)
				{
					if (!result.ContainsKey(i))
					{
						result[i] = new ProbeCsvResult();
					}

					decimal data;
					if (decimal.TryParse(cells[i], out data))
					{
						result[i].UpdateData(data);
					}
				}
			}
			return result;
		}

	}
	#endregion


	#region XYColumnsクラス
	public class XYColumns
	{
		public HashSet<int> XColumns
		{
			get
			{
				return _xColumns;
			}
		}
		HashSet<int> _xColumns = new HashSet<int>();

		public HashSet<int> YColumns
		{
			get
			{
				return _yColumns;
			}
		}
		HashSet<int> _yColumns = new HashSet<int>();

		public IEnumerable<int> Columns
		{
			get
			{
				return _xColumns.Union(_yColumns);
			}
		}
	}
	#endregion

	#region ProbeCsvResultクラス
	public class ProbeCsvResult
	{
		public decimal? Maximum { get; set; }
		public decimal? Minimum { get; set; }
		public string Title { get; set; }

		public void UpdateData(decimal new_data)
		{
			if (!Maximum.HasValue || Maximum.Value < new_data)
			{
				Maximum = new_data;
			}
			if (!Minimum.HasValue || Minimum.Value > new_data)
			{
				Minimum = new_data;
			}
		}
	}
	#endregion


	public enum ChartFormat
	{
		// とりあえず2つ．
		Png,
		Svg
	}

	#region LineChartSeriesクラス
	public class LineChartSeries
	{
		// とりあえずusing x:y は必須で。

		/// <summary>
		/// X軸に使うデータの列番号を取得／設定します(1から始まる)。
		/// </summary>
		public int XColumn { get; set; }

		/// <summary>
		/// Y軸に使うデータの列番号を取得／設定します(1から始まる)。
		/// </summary>
		public int YColumn { get; set; }

		/// <summary>
		/// 元データのファイルを設定／取得します。
		/// </summary>
		public string SourceFile { get; set; }

		/// <summary>
		/// 系列名を取得／設定します。
		/// </summary>
		public string Title { get; set; }

		public LineChartSeriesStyle Style { get; set; }

		/// <summary>
		/// Gnuplotコマンド用の文字列を取得します。
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			List<string> format = new List<string>();
			format.Add($"'{SourceFile}' using {XColumn}:{YColumn}");
			if (Style != null)
			{
				format.Add($"w {Style.ToString()}");
			}
			if (!string.IsNullOrEmpty(Title))
			{
				format.Add($"title '{Title}'");
			}
			return string.Join(" ", format.ToArray());
		}
	}
	#endregion

	#region LineChartSeriesStyleクラス
	public class LineChartSeriesStyle
	{
		// FilledCurve, ErrorBars は未対応。

		#region *BasicStyleプロパティ
		public LineChartStyle BasicStyle
		{
			get
			{ return _basicStyle; }
		}
		readonly LineChartStyle _basicStyle;
		#endregion

		public LineChartSeriesStyle(LineChartStyle basicStyle)
		{
			this._basicStyle = basicStyle;
		}

		#region *BasicStyleStringプロパティ
		/// <summary>
		/// BasicStyleを文字列として取得します。(設定はBasicStyleプロパティを使用して下さい。)
		/// </summary>
		public string BasicStyleString
		{
			get
			{
				switch(BasicStyle)
				{
					case LineChartStyle.Lines:
						return "lines";
					case LineChartStyle.Points:
						return "points";
					case LineChartStyle.LinesPoints:
						return "linespoints";
					case LineChartStyle.Impulses:
						return "impulses";
					case LineChartStyle.Dots:
						return "dots";
					case LineChartStyle.Steps:
						return "steps";
					case LineChartStyle.FSteps:
						return "fsteps";
					case LineChartStyle.HiSteps:
						return "histeps";
					case LineChartStyle.Boxes:
						return "boxes";
					default:
						return string.Empty;
				}
			}
		}
		#endregion

		public LinePointStyle Style { get; set; }

		/// <summary>
		/// Gnuplotコマンド用の文字列を取得します。
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"{BasicStyleString} {Style}";
		}

	}
	#endregion


	#region LineChartStyle列挙体
	public enum LineChartStyle
	{
		/// <summary>
		/// 線のみを表示します。
		/// </summary>
		Lines,
		/// <summary>
		/// 点のみを表示します。
		/// </summary>
		Points,
		/// <summary>
		/// 点と線を表示します。
		/// </summary>
		LinesPoints,
		/// <summary>
		/// X軸からの棒グラフのように表示します。
		/// </summary>
		Impulses,
		/// <summary>
		/// 点線を表示します。
		/// </summary>
		Dots,
		/// <summary>
		/// 階段を表示します。
		/// </summary>
		Steps,
		/// <summary>
		/// 左寄りの階段を表示します。
		/// </summary>
		FSteps,
		/// <summary>
		/// ヒストグラムのような階段を表示します。
		/// </summary>
		HiSteps,
		/// <summary>
		/// 箱形ヒストグラムを表示します。
		/// </summary>
		Boxes
	}
	#endregion

	#region LinePointStyleクラス
	public class LinePointStyle
	{

		/// <summary>
		/// 線の色を表す文字列(省略名"red"、あるいは"#FF0000")を取得／設定します。
		/// LineColorIndexプロパティよりこちらの指定が優先します。。
		/// </summary>
		public string LineColor
		{
			get; set;
		}

		/// <summary>
		/// 線の色を表す番号(環境依存)を取得／設定します。
		/// LineColorプロパティが設定されている場合は、そちらが優先されます。
		/// </summary>
		public int LineColorIndex
		{
			get; set;
		}

		/// <summary>
		/// 線の種類を表す番号(環境依存)を取得／設定します。0であれば既定の値を使います。
		/// </summary>
		public int LineType { get; set; }

		/// <summary>
		/// 線の太さを取得／設定します。0であれば既定の値を使います。
		/// </summary>
		public double LineWidth { get; set; }

		/// <summary>
		/// 点の種類を表す番号(環境依存)を取得／設定します。0であれば既定の値を使います。
		/// </summary>
		public int PointType { get; set; }

		/// <summary>
		/// 点の大きさを取得／設定します(単位不明)。0であれば既定の値を使います。
		/// </summary>
		public double PointSize { get; set; }

		#region *文字列化(ToString)
		/// <summary>
		/// Gnuplotコマンド用の文字列を取得します。
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			List<string> format = new List<string>();

			if (LineType > 0)
			{
				format.Add($"lt {LineType}");
			}
			if (!string.IsNullOrEmpty(LineColor))
			{
				format.Add($"lc rgbcolor '{LineColor}'");
			}
			else
			{
				format.Add($"lc {LineColorIndex}");
			}
			if (LineWidth > 0)
			{
				format.Add($"lw {LineWidth.ToString("f1")}");
			}
			if (PointType > 0)
			{
				format.Add($"pt {PointType}");
			}
			if (PointSize > 0)
			{
				format.Add($"ps {LineWidth.ToString("f1")}");
			}

			return string.Join(" ", format.ToArray());
		}
		#endregion

	}
	#endregion



	#region ChartAxisSettingsクラス
	public class ChartAxisSettings
	{
		/// <summary>
		/// 軸のキャプションを取得／設定します。
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// 軸の範囲を取得／設定します。
		/// </summary>
		public Range Range { get; set; }

		/// <summary>
		/// 目盛りの設定を取得／設定します。
		/// </summary>
		public AxisTicsSettings Tics { get; set; }

		/// <summary>
		/// 設定用のコマンドを返します。
		/// </summary>
		/// <returns></returns>
		public List<string> GetCommands(string axis_name)
		{
			List<string> commands = new List<string>();
			if (!string.IsNullOrEmpty(Title))
			{
				commands.Add($"set {axis_name}label '{Title}'");
			}
			if (Range.IsValid)
			{
				commands.Add($"set {axis_name}range [ {Range.Start} : {Range.Stop} ]");
			}
			if (Tics != null)
			{
				commands.Add($"set {axis_name}tics {Tics.ToString()}");
			}
			return commands;
		}
	}
	#endregion

	#region Range構造体
	public struct Range
	{
		public decimal Start;
		public decimal Stop;

		public Range(decimal start, decimal stop)
		{
			this.Start = start;
			this.Stop = stop;
		}

		public bool IsValid
		{
			get
			{
				return Start < Stop;
			}
		}

		public decimal Width
		{
			get
			{
				return Stop - Start;
			}
		}

		// (0.0.5)
		#region *和集合を返す(Union)
		/// <summary>
		/// 与えられた範囲の和集合を返します。
		/// </summary>
		/// <param name="ranges"></param>
		/// <returns></returns>
		public static Range Union(params Range[] ranges)
		{
			if (ranges.Length < 1)
			{
				throw new ArgumentException("rangesには1つ以上のRangeを指定して下さい。", "ranges");
			}
			return new Range(ranges.Min(r => r.Start), ranges.Max(r => r.Stop));
		}
		#endregion

	}
	#endregion

	#region ScannedRange構造体
	public struct ScannedRange
	{
		public Range XRange;
		public Range YRange;

		// (0.0.5)
		#region *和集合を返す(Union)
		/// <summary>
		/// 与えられた範囲の和集合を返します。
		/// </summary>
		/// <param name="ranges"></param>
		/// <returns></returns>
		public static ScannedRange Union(params ScannedRange[] ranges)
		{
			return new ScannedRange
			{
				XRange = Range.Union(ranges.Select(r => r.XRange).ToArray()),
				YRange = Range.Union(ranges.Select(r => r.YRange).ToArray())
			};
		}
		#endregion

	}
	#endregion

	#region AxisTicsSettingsクラス
	public class AxisTicsSettings
	{
		// axisだとmirrorは無効？
		// とりあえずborderで決め打ちする。

		//commands.Add("set xtics border mirror norotate 400,5,440");
		//commands.Add("set ytics border -300,100,300");

		/// <summary>
		/// 反対側にも目盛りをつけるかどうかの値を取得／設定します。
		/// </summary>
		public bool? Mirror { get; set; } 

		/// <summary>
		/// 目盛りの見出しを回転させるかどうかの値を取得／設定します。
		/// </summary>
		public bool? Rotate { get; set; }

		/// <summary>
		/// 目盛りの見出しの開始位置を取得／設定します。
		/// </summary>
		public decimal Start { get; set; }

		/// <summary>
		/// 目盛りの見出しの間隔を取得／設定します。
		/// </summary>
		public decimal Increase { get; set; }

		/// <summary>
		/// 目盛りの見出しの終了位置を取得／設定します。
		/// </summary>
		public decimal Stop { get; set; }

		/// <summary>
		/// 目盛りの間隔などを自動設定するかどうかの値を取得／設定します。
		/// (※バグがある？ので使わない方がよさげ。)
		/// </summary>
		public bool AutoFrequency { get; set; }

		#region *[override]文字列化(ToString)
		/// <summary>
		/// Gnuplotコマンド用の文字列を取得します。
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			List<string> arguments = new List<string>();

			arguments.Add("border");
			if (Mirror.HasValue)
			{
				arguments.Add(Mirror.Value ? "mirror" : "nomirror");
			}
			if (Rotate.HasValue)
			{
				arguments.Add(Rotate.Value ? "rotate" : "norotate");
			}
			if (AutoFrequency)
			{
				arguments.Add("autofreq");
			}
			else
			{
				arguments.Add($"{Start},{Increase},{Stop}");
			}

			return string.Join(" ", arguments);
		}
		#endregion

	}
	#endregion

}
