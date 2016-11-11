using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{

	public class ReferenceSpectrum : INotifyPropertyChanged
	{

		public string DirectoryName
		{
			get
			{
				return _directoryName;
			}
			set
			{
				if (_directoryName != value)
				{
					_directoryName = value;
					NotifyPropertyChanged("DirectoryName");
				}
			}
		}
		string _directoryName = string.Empty;

		public string Name
		{
			get
			{
				var layers = DirectoryName.Split('\\');
				return layers[layers.Length - 1].Replace(".A", string.Empty);
			}
		}


		protected void NotifyPropertyChanged(string propertyName)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };


	}



}
