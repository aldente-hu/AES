using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation.Mvvm
{
	public class SelectSaveFileMessage  : SimpleMessage
	{
		public string SelectedFile
		{
			get; set;
		}
		
		public SelectSaveFileMessage(object sender) : base(sender)
		{
			
		}

		public string[] Ext
		{
			get
			{
				return _ext;
			}
			set
			{
				_ext = value;
			}
		}
		string[] _ext = null;
	}
}
