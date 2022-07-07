using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation.Core.Mvvm
{
	public class MessageBase
	{
		public object Sender { get; protected set; }

		public MessageBase(object sender)
		{
			this.Sender = sender;
		}
	}
}
