using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.Data
{

	public class IdFile
	{
		public static DataType CheckType(string source)
		{
			using (var reader = new StreamReader(source))
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
										throw new ApplicationException("この測定形式には対応していません。");
								}
						}
					}
				}
			}
			throw new ApplicationException("このデータファイルは対応していません。");
		}
	}

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

}
