﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{
	using Data.Portable;
	using Mvvm;

	#region MainWindowViewModelクラス
	public class MainWindowViewModel : Mvvm.ViewModelBase
	{
		#region *WideScanDataプロパティ
		public WideScanViewModel WideScanData
		{
			get
			{
				return _wideScanData;
			}
			set
			{
				_wideScanData = value;
				NotifyPropertyChanged();
			}
		}
		WideScanViewModel _wideScanData = new WideScanViewModel();
		#endregion

		#region *DepthProfileDataプロパティ
		public DepthProfileViewModel DepthProfileData
		{
			get
			{
				return _depthProfileData;
			}
			set
			{
				_depthProfileData = value;
				NotifyPropertyChanged();
			}
		}
		DepthProfileViewModel _depthProfileData = new DepthProfileViewModel();
		#endregion


		public MainWindowViewModel()
		{
			_loadCommand = new DelegateCommand(Load_Executed);

		}

		#region ロード関連


		#region Load

		public DelegateCommand LoadCommand
		{
			get
			{
				return _loadCommand;
			}
		}
		DelegateCommand _loadCommand;

		async void Load_Executed(object parameter)
		{
			//var directory = (string)parameter;
			//_wideScan = await WideScan.GenerateAsync(directory);

			//NotifyPropertyChanged("ScanParameter");
			await OpenJampData();
		}

		#endregion



		public async Task OpenJampData()
		{
			var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "idファイル(id)|id" };
			if (dialog.ShowDialog() == true)
			{
				var id_file = dialog.FileName;
				var dir = System.IO.Path.GetDirectoryName(id_file);

				switch (await IdFile.CheckTypeAsync(id_file))
				{
					case DataType.WideScan:
						// WideScanモードにする．
						//tabControlData.SelectedIndex = 0;
						//_wideScanData = await WideScanViewModel.GenerateAsync(dir);
						await _wideScanData.LoadFromAsync(dir);
						JampDataOpened(this, new JampDataEventArgs(DataType.WideScan));
						break;
					case DataType.DepthProfile:
						// DepthProfileモードにする．
						await _depthProfileData.LoadFromAsync(dir);
						JampDataOpened(this, new JampDataEventArgs(DataType.DepthProfile));
						//tabControlData.SelectedIndex = 1;
						//_depthProfileData = await DepthProfile.GenerateAsync(dir);

						// リストボックスを設定する。
						//foreach (var element in _depthProfileData.Spectra.Keys)
						//{
						//	comboBoxElement.Items.Add(element);
						//}
						//int n = _depthProfileData.Spectra.First().Value.Data.Length;
						//for (int i = 0; i < n; i++)
						//{
						//	comboBoxLayers.Items.Add(i);
						//}

						//TestOpenDepthProfile(dir, true);
						break;
				}

			}
		}

		public event EventHandler<JampDataEventArgs> JampDataOpened = delegate { };

		#endregion


	}
	#endregion

	#region JampDataEventArgsクラス
	public class JampDataEventArgs : EventArgs
	{
		public DataType DataType
		{
			get
			{
				return _dataType;
			}
		}
		readonly DataType _dataType;

		public JampDataEventArgs(DataType type) : base()
		{
			this._dataType = type;
		}
	}
	#endregion

}
