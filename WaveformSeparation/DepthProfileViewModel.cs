using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{
	using Mvvm;
	using Data.Portable;
	using Helpers;

	public class DepthProfileViewModel : ViewModelBase
	{

		#region *ROISpectraCollectionプロパティ
		public List<ROISpectra> ROISpectraCollection
		{
			get
			{
				if (_depthProfile.Spectra != null)
				{
					return _depthProfile.Spectra.Values.ToList();
				}
				else
				{
					return null;
				}
			}
		}
		//List<ROISpectra> _roiSpectraCollection = new List<ROISpectra>();
		DepthProfile _depthProfile = new DepthProfile();
		#endregion

		#region *CurrentROIプロパティ
		public ROISpectra CurrentROI
		{
			get
			{
				return _currentROI;
			}
			set
			{
				if (CurrentROI != value)
				{
					_currentROI = value;
					NotifyPropertyChanged();
				}
			}
		}
		ROISpectra _currentROI = null;
		#endregion

		public DepthProfileViewModel()
		{
			_selectCsvDestinationCommand = new DelegateCommand(SelectCsvDestination_Executed);
			//_exportCsvCommand = new DelegateCommand(ExportCsv_Executed);
			_exportCsvCommand = new DelegateCommand(ExportCsv_Executed, ExportCsv_CanExecute);

		}


		public async Task LoadFromAsync(string directory)
		{
			await _depthProfile.LoadFromAsync(directory);
			NotifyPropertyChanged("ROISpectraCollection");
		}


		#region ExportCsv

		public DelegateCommand ExportCsvCommand
		{
			get
			{
				return _exportCsvCommand;
			}
		}
		DelegateCommand _exportCsvCommand;

		async void ExportCsv_Executed(object parameter)
		{
			var diff = parameter is bool && (bool)parameter;
			if (!string.IsNullOrEmpty(this.ExportCsvDestination))
			{
				using (var writer = new StreamWriter(this.ExportCsvDestination, false))
				{
					if (diff)
					{
						await CurrentROI.Differentiate(3).ExportCsvAsync(writer);
					}
					else
					{
						await CurrentROI.ExportCsvAsync(writer);
					}
				}
			}
		}

		bool ExportCsv_CanExecute(object parameter)
		{
			return !string.IsNullOrEmpty(this.ExportCsvDestination);
		}

		#endregion

		#region *ExportCsvDestinationプロパティ
		public string ExportCsvDestination
		{
			get
			{
				return _exportCsvDestination;
			}
			set
			{
				if (ExportCsvDestination != value)
				{
					_exportCsvDestination = value;
					NotifyPropertyChanged();
					_exportCsvCommand.RaiseCanExecuteChanged();
				}
			}
		}
		string _exportCsvDestination;
		#endregion


		#region SelectCsvDestination

		public DelegateCommand SelectCsvDestinationCommand
		{
			get
			{
				return _selectCsvDestinationCommand;
			}
		}
		DelegateCommand _selectCsvDestinationCommand;

		void SelectCsvDestination_Executed(object parameter)
		{
			var message = new SelectSaveFileMessage(this) { Message = "csvファイルの出力先を選んで下さい．" };
			message.Ext = new string[] { ".csv" };
			Messenger.Default.Send(this, message);
			if (string.IsNullOrEmpty(message.SelectedFile))
			{
				this.ExportCsvDestination = string.Empty;
			}
			else
			{
				this.ExportCsvDestination = message.SelectedFile;
			}
		}


		#endregion


	}

}
