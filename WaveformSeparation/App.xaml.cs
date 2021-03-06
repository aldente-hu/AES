﻿using System;
using System.IO;
using System.Configuration;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application
	{
		TextWriter output;

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			if (WaveformSeparation.Properties.Settings.Default.OutputLog)
			{
				output = new StreamWriter(Path.GetTempFileName(), false, Encoding.UTF8);
				TextWriterTraceListener fileTraceListener = new TextWriterTraceListener(output);
				Trace.Listeners.Add(fileTraceListener) ;
				// Trace.WriteLine("Hello 15Seconds Reader -- This is first trace message") ;
				//Console.SetOut(output);
			}

			var gnuplot_path = WaveformSeparation.Properties.Settings.Default.GnuplotExecutable;
			if (!string.IsNullOrEmpty(gnuplot_path) && Path.IsPathRooted(gnuplot_path))
			{
				Data.Standard.Gnuplot.BinaryPath = gnuplot_path;
			}
			Trace.WriteLine($"Gnuplot executable is placed at {Data.Standard.Gnuplot.BinaryPath}.");

		}



		private async void Application_Exit(object sender, ExitEventArgs e)
		{
			if (output != null)
			{
				await output.FlushAsync();
				output.Dispose();
			}
		}

	}
}
