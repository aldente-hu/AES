using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.Data.Portable
{

	public class IdFile
	{

		public static DataType CheckType(string source)
		{
			using (var reader = new StreamReader(new FileStream(source, FileMode.Open, FileAccess.Read)))
			{
				while (!reader.EndOfStream)
				{
					var cols = reader.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					if (cols.Length > 1)
					{
						switch (cols[0])
						{
							case "$AP_DATATYPE":
								switch (Convert.ToInt32(cols[1]))
								{
									case 3:
										return DataType.WideScan;
									case 5:
										return DataType.DepthProfile;
									default:
										throw new Exception("この測定形式には対応していません。");
								}
						}
					}
				}
			}
			throw new Exception("このデータファイルは対応していません。");
		}

		public static async Task<DataType> CheckTypeAsync(string source)
		{
			using (var reader = new StreamReader(new FileStream(source, FileMode.Open, FileAccess.Read)))
			{
				while (!reader.EndOfStream)
				{
					var line = await reader.ReadLineAsync();
					var cols = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					if (cols.Length > 1)
					{
						switch (cols[0])
						{
							case "$AP_DATATYPE":
								switch (Convert.ToInt32(cols[1]))
								{
									case 3:
										return DataType.WideScan;
									case 5:
										return DataType.DepthProfile;
									default:
										throw new Exception("この測定形式には対応していません。");
								}
						}
					}
				}
			}
			throw new Exception("このデータファイルは対応していません。");
		}

	}

	#region DataType列挙体
	public enum DataType
	{
		SEImage,
		WideScan,
		SplitScan,
		DepthProfile,
		LineProfile,
		SpectrumLineProfile,
		Mapping,
		LargeAreaMapping
	}
	#endregion

}
