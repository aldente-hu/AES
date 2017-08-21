using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace HirosakiUniversity.Aldente.AES.Data.Portable
{

	#region WideScanクラス
	public class WideScan
	{

		#region *Parameterプロパティ
		public ScanParameter Parameter
		{
			get
			{
				return _scanParameter;
			}
		}
		ScanParameter _scanParameter;
		#endregion

		#region *Dataプロパティ
		public EqualIntervalData Data
		{
			get
			{
				return _data;
			}
		}
		EqualIntervalData _data;
		#endregion

		public WideScan()
		{ }

		public static async Task<WideScan> GenerateAsync(string directory)
		{
			var scan = new WideScan();
			await scan.LoadFromAsync(directory);
			return scan;
		}

		public async Task LoadFromAsync(string directory)
		{
			// パラメータを読み込む。
			_scanParameter = await ScanParameter.GenerateAsync(directory);
			

			// データを読み込む。
			using (var reader = new BinaryReader(new FileStream(Path.Combine(directory, "data"), FileMode.Open, FileAccess.Read)))
			{
				_data = await EqualIntervalData.GenerateAsync(reader);
			}
			//Loaded(this, EventArgs.Empty);
		}

		/// <summary>
		/// データがロードされた後に発生します．
		/// </summary>
		//public event EventHandler<EventArgs> Loaded = delegate { };

		/// <summary>
		/// 範囲を制限したデータを返します。
		/// </summary>
		/// <param name="start"></param>
		/// <param name="stop"></param>
		/// <returns></returns>
		public WideScan GetRestrictedData(decimal start, decimal stop)
		{
			var b = Convert.ToInt32(Decimal.Floor((start - Parameter.Start) / Parameter.Step));
			var e = Convert.ToInt32(Decimal.Ceiling((stop - Parameter.Start) / Parameter.Step));

			return new WideScan
			{
				_data = this.Data.GetSubData(b, e),
				_scanParameter = this.Parameter.ShrinkRange(b, e)
			};
		}


		/// <summary>
		/// 前後のデータから線形補間した値を返します。
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		public decimal GetInterpolatedDataAt(decimal x)
		{
			if (x >= Parameter.Start && x <= Parameter.Stop)
			{
				var index = (x - Parameter.Start) / Parameter.Step;
				int i = Convert.ToInt32(Decimal.Floor(index));
				decimal d = index - i;

				return (1 - d) * Data[i] + d * Data[i + 1];
			}
			else
			{
				throw new ArgumentException();
			}
		}

		// ※XShiftを考慮するのはここだけかな？

		/// <summary>
		/// 指定した範囲の、線形補間されたデータを返します。XShiftを考慮します。
		/// </summary>
		/// <param name="start"></param>
		/// <param name="step"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public List<decimal> GetInterpolatedData(decimal start, decimal step, int count)
		{
			List<decimal> data = new List<decimal>();
			for (int i = 0; i < count; i++)
			{
				data.Add(GetInterpolatedDataAt(start + step * i - _scanParameter.XShift));
			}
			return data;
		}

		public WideScan Differentiate(int m)
		{
			return new WideScan
			{
				_scanParameter = this._scanParameter.GetDifferentiatedParameter(m),
				_data = this.Data.Differentiate(m, _scanParameter.Step)
			};
		}

		#region *csvとしてエクスポート(ExportCsvAsync)
		public async Task ExportCsvAsync(TextWriter writer)
		{
			for (int i = 0; i < Parameter.PointsCount; i++)
			{
				decimal x = Parameter.Start + i * Parameter.Step;
				await writer.WriteLineAsync($"{x},{Data.GetDataForCsv(i)}");
			}
		}
		#endregion

	}
	#endregion


}
