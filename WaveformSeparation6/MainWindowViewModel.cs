using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation.Net6
{
	using Data.Standard;
	using Core.Mvvm;

	#region MainWindowViewModelクラス
	public class MainWindowViewModel : Core.Mvvm.ViewModelBase
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

		#region *コンストラクタ(MainWindowViewModel)
		public MainWindowViewModel()
		{
			LoadCommand = new DelegateCommand(Load_Executed, Load_CanExecuted);

			this.JampDataOpened += MainWindowViewModel_JampDataOpened;
		}
		#endregion


		#region ロード関連

		#region *NowLoadingプロパティ
		/// <summary>
		/// データをロード中かどうかの値を取得します．
		/// </summary>
		public bool NowLoading
		{
			get
			{
				return _nowLoading;
			}
			private set
			{
				if (NowLoading != value)
				{
					_nowLoading = value;
					NotifyPropertyChanged();
					LoadCommand.RaiseCanExecuteChanged();
				}
			}
		}
		bool _nowLoading = false;
		#endregion

		#region Load

		public DelegateCommand LoadCommand { get; }

		async void Load_Executed(object parameter)
		{
			//var directory = (string)parameter;
			//_wideScan = await WideScan.GenerateAsync(directory);

			//NotifyPropertyChanged("ScanParameter");
			NowLoading = true;
			await OpenJampData();
		}

		// (0.2.1)
		bool Load_CanExecuted(object parameter)
		{
			return !this.NowLoading;
		}

		#endregion

		#region *JEOLの測定データを開く(OpenJampData)
		/// <summary>
		/// JEOLの測定データを開きます．
		/// </summary>
		/// <returns></returns>
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
						await _wideScanData.WideScanFittingData.LoadFromAsync(dir);
						JampDataOpened(this, new JampDataEventArgs(DataType.WideScan));
						break;
					case DataType.DepthProfile:
						// DepthProfileモードにする．
						await _depthProfileData.DepthProfileFittingData.LoadFromAsync(dir);
						JampDataOpened(this, new JampDataEventArgs(DataType.DepthProfile));
						//tabControlData.SelectedIndex = 1;
						//_depthProfileData = await DepthProfile.GenerateAsync(dir);

						//TestOpenDepthProfile(dir, true);
						break;
				}

			}
		}
		#endregion

		// これいるのかな？VMのJampDataOpenedで事足りるのでは？
		public event EventHandler<JampDataEventArgs> JampDataOpened = delegate { };

		// (0.2.1)NowLoadingプロパティによるコマンド実行の無効化を実装．
		#region *データロード完了時(MainWindowViewModel_JampDataOpened)
		private void MainWindowViewModel_JampDataOpened(object sender, JampDataEventArgs e)
		{
			switch(e.DataType)
			{
				case DataType.DepthProfile:
					//DepthProfileData.Layers
					break;
			}
			NowLoading = false;
		}
		#endregion

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
