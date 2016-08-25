using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.Data
{

	#region SpectrumBaseクラス
	public class SpectrumBase
	{
		public SpectrumParameter Parameter
		{
			get { return _parameter; }
		}
		SpectrumParameter _parameter;

		public decimal Start
		{
			get
			{
				return _parameter.Start;
			}
		}

		public decimal Stop
		{
			get
			{
				return _parameter.Stop;
			}
		}

		public decimal Step
		{
			get
			{
				return _parameter.Step;
			}
		}

		public int Length
		{
			get
			{
				return _parameter.Count;
			}
		}

		public SpectrumBase(SpectrumParameter parameter)
		{
			_parameter = parameter;
		}

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


	}
	#endregion


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

		public void ExportCsv(string destination)
		{
			using (StreamWriter writer = new StreamWriter(destination, false))
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
		}

	}
	#endregion


	#region EqualIntervalDataクラス
	public class EqualIntervalData : List<decimal>
	{

		public EqualIntervalData()
		{ }

		public EqualIntervalData(BinaryReader reader)
		{
			while(reader.PeekChar() > -1)
			{
				// エンディアンが逆なので、単純にreader.ReadInt32()とはいかない！
				int count = 0;
				for (int j = 0; j < 4; j++)
				{
					count += reader.ReadByte() * (1 << (24 - 8 * j));
				}
				this.Add(count);
			}
		}

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
		}


		public EqualIntervalData Differentiate(int m)
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
				data.Add(3 * count / (m * (m+1) * (2*m+1) ));
			}
			return data;
		}

	}
	#endregion

}
