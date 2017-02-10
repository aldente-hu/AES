using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation.Mvvm
{
	public class ViewModelBase : INotifyPropertyChanged
	{
		//public bool IsInDesignMode
		//{
		//	get { return Windows.ApplicationModel.DesignMode.DesignModeEnabled; }
		//}

		public bool IsInEditMode
		{
			get
			{
				return _isInEditMode;
			}
			set
			{
				if (IsInEditMode != value)
				{
					_isInEditMode = value;
					_editCommand.RaiseCanExecuteChanged();
				}
			}
		}
		bool _isInEditMode = false;

		protected DelegateCommand _editCommand;

		#region INotifyPropertyChanged実装
		protected void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		#endregion

	}
}
