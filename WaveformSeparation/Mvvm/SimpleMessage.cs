using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation.Mvvm
{
	public class SimpleMessage : MessageBase
	{
		public SimpleMessage(object sender) : base(sender)
		{ }

		public string Message { get; set; }

	}
}
