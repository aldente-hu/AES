using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{

	public class Gnuplot
	{
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
		/// フォントサイズを取得／設定します。今のところsvgでのみ機能します。
		/// </summary>
		public int FontSize
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

		string _binary_path = @"c:\Program Files\gnuplot\bin\gnuplot.exe";

		

		// コマンド群を生成する部分と、それをwriterに流し込む部分を分けた方がよい。


		public async void Draw()
		{

			using (var process = new Process())
			{
				process.StartInfo.FileName = _binary_path;
				//process.StartInfo.Arguments = pltFile;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;  // これを設定しないと，CreateNoWindowは無視される．
				process.StartInfo.RedirectStandardInput = true;

				process.Start();
				//await OutputCommandsAsync(process.StandardInput);

				//	GenerateCommandSquence().ForEach(
				//		async line => await process.StandardInput.WriteLineAsync(line)
				//	);
				// ※↑は不可。(StandardInputへの同時アクセスが起こる。)
				foreach (var line in GenerateCommandSquence())
				{
					await process.StandardInput.WriteLineAsync(line);
				}

			}

		}


		public async Task OutputPltFileAsync(string destination)
		{
			using (var writer = new StreamWriter(destination))
			{
				foreach (var line in GenerateCommandSquence())
				{
					await writer.WriteLineAsync(line);
				}
			}
		}



		public List<string> GenerateCommandSquence()
		{
			List<string> commands = new List<string>();

			switch (Format)
			{
				case ChartFormat.Svg:
					commands.Add($"set terminal svg enhanced size {Width},{Height} fsize {FontSize}");
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

			// ※このあたりはデータ依存だけどどうする？
			commands.Add("set xlabel 'K.E. / eV'");
			commands.Add("set xrange [ 400 : 440 ]");
			commands.Add("set xtics border mirror norotate 400,5,440");
			commands.Add("set ylabel 'Intensity'");
			commands.Add("set yrange [ -300 : 300 ]");
			commands.Add("set ytics border -300,100,300");
			commands.Add($"set datafile separator '{DataFileSeparator}'");

			var series = new LineChartSeries
			{
				SourceFile = @"B:\depth.csv",
				XColumn = 1,
				YColumn = 2,
				Style = new LineChartSeriesStyle(LineChartStyle.Lines)
				{
					Style = new LinePointStyle
					{
						LineColor = "#FF0000"
					}
				}
			};

			commands.Add($"plot {series}");
			commands.Add("set output");

			return commands;
		}

		/*
		public async Task OutputCommandsAsync(TextWriter writer)
		{

			switch (Format)
			{
				case ChartFormat.Svg:
					await writer.WriteLineAsync($"set terminal svg enhanced size {Width},{Height} fsize {FontSize}");
					break;
				case ChartFormat.Png:
					await writer.WriteLineAsync($"set terminal png size {Width},{Height}");
					break;
			}


			await writer.WriteLineAsync($"set output '{Destination}'");
			if (!string.IsNullOrEmpty(Title))
			{
				await writer.WriteLineAsync($"set title '{Title}'");
			}

			// ※このあたりはデータ依存だけどどうする？
			await writer.WriteLineAsync("set xlabel 'K.E. / eV'");
			await writer.WriteLineAsync("set xrange [ 400 : 440 ]");
			await writer.WriteLineAsync("set xtics border mirror norotate 400,5,440");
			await writer.WriteLineAsync("set ylabel 'Intensity'");
			await writer.WriteLineAsync("set yrange [ -300 : 300 ]");
			await writer.WriteLineAsync("set ytics border -300,100,300");
			await writer.WriteLineAsync($"set datafile separator '{DataFileSeparator}'");

			var series = new LineChartSeries
			{
				SourceFile = @"B:\depth.csv",
				XColumn = 1,
				YColumn = 2,
				Style = new LineChartSeriesStyle(LineChartStyle.Lines)
				{
					Style = new LinePointStyle
					{
						LineColor = "#FF0000"
					}
				}
			};

			await writer.WriteLineAsync($"plot {series}");
			//Debug.WriteLine(series);
			//await writer.WriteLineAsync(@"plot 'B:\depth.csv' using 1:2 w lines lc rgbcolor '#FF0000'");
			await writer.WriteLineAsync("set output");
		}
		*/

	}
	
	public enum ChartFormat
	{
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
		/// 番号(環境依存)による指定には未対応です。
		/// </summary>
		public string LineColor
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
	}
	#endregion

}
