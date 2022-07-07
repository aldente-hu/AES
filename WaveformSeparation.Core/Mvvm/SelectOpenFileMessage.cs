using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation.Core.Mvvm
{
	public class SelectOpenFileMessage  : SimpleMessage
	{
		public string SelectedFile
		{
			get; set;
		}
		
		public SelectOpenFileMessage(object sender) : base(sender)
		{
			
		}

		/// <summary>
		/// はじめの2文字が"*."であるものは，拡張子フィルタと解釈されます．
		/// </summary>
		public string[] Filter
		{
			get
			{
				return _filter;
			}
			set
			{
				_filter = value;
			}
		}
		string[] _filter = null;
	}
}
