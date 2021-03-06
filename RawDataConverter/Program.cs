﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace HirosakiUniversity.Aldente.AES.RawDataConverter
{
	using Data.Standard;

	class Program
	{
		// (0.1.0)C#7.1の非同期Mainを導入。
		static async Task Main(string[] args)
		{
			string source;
			string destination;
			if (args.Length > 0)
			{
				source = args[0];
			}
			else
			{
				source = @".\data";
			}


			Trace.WriteLine($"入力ファイル名は {source}です。");
			switch (Path.GetExtension(source))
			{
				case ".txt":
					Trace.WriteLine($"バイナリファイルを出力します。");
					if (args.Length > 1)
					{
						destination = args[1];
					}
					else
					{
						destination = source.Substring(0, source.Length - 4);
					}
					Trace.WriteLine($"入力ファイル名は {destination}です。");
					ConvertBack(source, destination);
					Trace.WriteLine("バイナリファイルを出力しました。");
					break;
				//case "":
				default:
					Trace.WriteLine($"テキストファイルを出力します。");
					if (args.Length > 1)
					{
						destination = args[1];
					}
					else
					{
						destination = source + ".txt";
					}
					Trace.WriteLine($"入力ファイル名は {destination}です。");
					await Convert(source, destination);
					Trace.WriteLine("テキストファイルを出力しました。");
					break;
					//Trace.WriteLine("入力ファイル名は、拡張子なし(バイナリ)、または.txt(テキスト)としてください。");
					//break;
			}

			Console.ReadKey();
		}

		// バイナリファイル→数値ファイル
		static async Task Convert(string source, string destination)
		{
			EqualIntervalData data;
			using (var reader = new BinaryReader(new FileStream(source, FileMode.Open)))
			{
				data = await EqualIntervalData.GenerateAsync(reader);
			}
			using (var writer = new StreamWriter(destination))
			{
				await data.OutputText(writer);
			}
		}

		// 数値ファイル→バイナリファイル
		// とりあえず同期メソッドとして実装する。
		static void ConvertBack(string source,string destination)
		{
			EqualIntervalData data;
			using (var reader = new StreamReader(source))
			{
				data = EqualIntervalData.GenerateFromText(reader, true).Result;
			}
			using (var writer = new BinaryWriter(new FileStream(destination, FileMode.Create)))
			{
				data.OutputBinary(writer);
			}

		}

	}
}
