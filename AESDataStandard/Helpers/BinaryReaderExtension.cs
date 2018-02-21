using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.Data.Standard.Helpers
{
	public static class BinaryReaderExtension
	{
		/// <summary>
		/// 4バイトを読み込み，ビッグエンディアン(後のバイトが下位)で整数に変換して返します．
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static async Task<int> ReadInt32Async(this BinaryReader reader)
		{
			int count = 0;
			byte[] buf = await ReadBytesAsync(reader, 4);
			for (int j = 0; j < 4; j++)
			{
				count += buf[j] * (1 << (24 - 8 * j));
			}
			return count;
		}

		public static async Task<byte[]> ReadBytesAsync(this BinaryReader reader, int length)
		{
			byte[] buf = new byte[length];
			await reader.BaseStream.ReadAsync(buf, 0, length);
			return buf;
		}
	}
}
