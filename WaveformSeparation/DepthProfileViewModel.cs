using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{
	using Mvvm;
	using Data.Standard;
	using Helpers;
	using System.ComponentModel;

	#region DepthProfileViewModelクラス
	public class DepthProfileViewModel : ViewModelBase
	{

		#region *DepthProfileFittingDataプロパティ
		public DepthProfileFittingData DepthProfileFittingData
		{
			get
			{
				return _depthProfileFittingData;
			}
		}
		DepthProfileFittingData _depthProfileFittingData = new DepthProfileFittingData();
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

		#region *CurrentFittingProfileプロパティ
		/// <summary>
		/// 現在選択されているFittingProfileを取得／設定します．
		/// </summary>
		public FittingProfile CurrentFittingProfile
		{
			get
			{
				return _currentFittingProfile;
			}
			set
			{
				if (CurrentFittingProfile != value)
				{
					this._currentFittingProfile = value;
					NotifyPropertyChanged();
				}
			}
		}
		FittingProfile _currentFittingProfile = null;
		#endregion


		#region *コンストラクタ(DepthProfileViewModel)
		public DepthProfileViewModel()
		{
			_selectSimpleCsvDestinationCommand = new DelegateCommand(SelectSimpleCsvDestination_Executed);
			_exportCsvCommand = new DelegateCommand(ExportCsv_Executed, ExportCsv_CanExecute);

			_addFittingProfileCommand = new DelegateCommand(AddFittingProfile_Executed);
			_selectCsvDestinationCommand = new DelegateCommand(SelectCsvDestination_Executed);
			_selectChartDestinationCommand = new DelegateCommand(SelectChartDestination_Executed);
			_removeProfileCommand = new DelegateCommand(RemoveProfile_Executed, RemoveProfile_CanExecute);
			_fitSpectrumCommand = new DelegateCommand(FitSpectrum_Executed, FitSpectrum_CanExecute);
			_addReferenceSpectrumCommand = new DelegateCommand(AddReferenceSpectrum_Executed, AddReferenceSpectrum_CanExecute);

			_loadConditionCommand = new DelegateCommand(LoadCondition_Executed);
			_saveConditionCommand = new DelegateCommand(SaveCondition_Executed);

			this.PropertyChanged += DepthProfileViewModel_PropertyChanged;

			//DepthProfileFittingData.FittingCondition.PropertyChanged += FittingCondition_PropertyChanged;
			DepthProfileFittingData.FittingCondition.FittingProfiles.CollectionChanged += FittingProfiles_CollectionChanged;
		}

		private void DepthProfileViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			// RaiseCanExecuteChangedはここにまとめる．

			switch (e.PropertyName)
			{
				case "ExportCsvDestination":
					ExportCsvCommand.RaiseCanExecuteChanged();
					break;
				case "CurrentFittingProfile":
					AddReferenceSpectrumCommand.RaiseCanExecuteChanged();
					RemoveProfileCommand.RaiseCanExecuteChanged();
					break;
				case "FitCommandExecuting":
					FitSpectrumCommand.RaiseCanExecuteChanged();
					break;
			}
		}


		private void FittingProfiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			_fitSpectrumCommand.RaiseCanExecuteChanged();
		}

		#endregion

		



		#region 単純出力関連

		public DelegateCommand SelectSimpleCsvDestinationCommand
		{
			get
			{
				return _selectSimpleCsvDestinationCommand;
			}
		}
		DelegateCommand _selectSimpleCsvDestinationCommand;

		void SelectSimpleCsvDestination_Executed(object parameter)
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
			return CurrentROI != null && !string.IsNullOrEmpty(this.ExportCsvDestination);
		}

		void OutputCsv()
		{
			
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
				}
			}
		}
		string _exportCsvDestination;
		#endregion

		#endregion



		#region AddFittingProfileCommand

		void AddFittingProfile_Executed(object parameter)
		{
			DepthProfileFittingData.AddFittingProfile(CurrentROI);
		}


		public DelegateCommand AddFittingProfileCommand
		{
			get
			{
				return _addFittingProfileCommand;
			}
		}
		DelegateCommand _addFittingProfileCommand;




		#endregion


		#region LoadCondition

		public DelegateCommand LoadConditionCommand
		{
			get
			{
				return _loadConditionCommand;
			}
		}
		DelegateCommand _loadConditionCommand;

		void LoadCondition_Executed(object parameter)
		{
			var message = new SelectOpenFileMessage(this) { Message = "ロードするプロファイルを選択して下さい。" };
			message.Filter = new string[] { "*.fcd", "*" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				// ロードする．
				DepthProfileFittingData.LoadFittingCondition(message.SelectedFile);
			}
		}

		#endregion

		#region SaveCondition

		public DelegateCommand SaveConditionCommand
		{
			get
			{
				return _saveConditionCommand;
			}
		}
		DelegateCommand _saveConditionCommand;

		void SaveCondition_Executed(object parameter)
		{
			var message = new SelectSaveFileMessage(this) { Message = "プロファイルの出力先を選んで下さい。" };
			message.Ext = new string[] { ".fcd" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				DepthProfileFittingData.SaveFittingCondition(message.SelectedFile);
			}

		}

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


		#region SelectChartDestination

		public DelegateCommand SelectChartDestinationCommand
		{
			get
			{
				return _selectChartDestinationCommand;
			}
		}
		DelegateCommand _selectChartDestinationCommand;

		void SelectChartDestination_Executed(object parameter)
		{
			// ディレクトリを選ぶのか？
			// とりあえずファイル選択にしておく．
			// →ファイル選択と見せかけて、ディレクトリ選択にする。

			var message = new SelectSaveFileMessage(this) { Message = "pngファイルの出力先を選んで下さい．" };
			message.Ext = new string[] { ".png" };
			Messenger.Default.Send(this, message);
			DepthProfileFittingData.SetChartDestination(message.SelectedFile);

		}

		#endregion


		#region RemoveProfile

		public DelegateCommand RemoveProfileCommand
		{
			get
			{
				return _removeProfileCommand;
			}
		}
		DelegateCommand _removeProfileCommand;

		void RemoveProfile_Executed(object parameter)
		{
			if (CurrentFittingProfile != null)
			{
				// これで大丈夫かな？
				DepthProfileFittingData.RemoveFittingProfile(CurrentFittingProfile);
			}
		}

		bool RemoveProfile_CanExecute(object parameter)
		{
			return CurrentFittingProfile != null;
		}

		#endregion


		#region AddReferenceSpectrum
		public DelegateCommand AddReferenceSpectrumCommand
		{
			get
			{
				return _addReferenceSpectrumCommand;
			}
		}
		DelegateCommand _addReferenceSpectrumCommand;

		async void AddReferenceSpectrum_Executed(object parameter)
		{
			var message = new SelectOpenFileMessage(this) { Message = "参照スペクトルを選んで下さい．" };
			// 拡張子ではなくファイル名が指定されている場合ってどうするの？
			message.Filter = new string[] { "id" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				await DepthProfileFittingData.AddReferenceSpectrumAsync(message.SelectedFile, CurrentFittingProfile);
			}

		}

		bool AddReferenceSpectrum_CanExecute(object parameter)
		{
			return CurrentFittingProfile != null;
		}

		#endregion




		#region FitSpectrum


		public DelegateCommand FitSpectrumCommand
		{
			get
			{
				return _fitSpectrumCommand;
			}
		}
		DelegateCommand _fitSpectrumCommand;

		// (0.0.6)バグを修正．
		// parameterで数値が渡されれば、1サイクルに対して解析を行う。
		// さもなければ、全サイクルに対して解析を行う。
		async void FitSpectrum_Executed(object parameter)
		{
			// parameterがnullであれば，全てのprofileに対してフィッティングを行う．
			// parameterにprofileが与えられていれば，それに対してフィッティングを行う．

			// ★BaseROIを決める段階と，実際のフィッティングを行う段階を分離した方がいいのでは？

			this.FitCommandExecuting = true;

			try
			{

				var fitting_task = parameter is FittingProfile
					? DepthProfileFittingData.FitSingleProfile((FittingProfile)parameter) : DepthProfileFittingData.FitAsync();

				try
				{
					await fitting_task;
				}
				catch (Exception) { }

				if (fitting_task.Exception is AggregateException)
				{
					var message = string.Join("\n", fitting_task.Exception.InnerExceptions.Select(ex => ex.Message));
					Messenger.Default.Send(this, new SimpleMessage(this) { Message = message });
					return;
				}
			}
			finally
			{
				this.FitCommandExecuting = false;
			}
		}

		bool FitSpectrum_CanExecute(object parameter)
		{
			if (this.FitCommandExecuting)
			{
				return false;
			}
			else if (parameter is FittingProfile)
			{
				return true;
			}
			else
			{
				return DepthProfileFittingData.FittingCondition.FittingProfiles.Count > 0;
			}
		}

		#region *FitCommandExecutingプロパティ
		/// <summary>
		/// フィッティングが実行中であるか否かを取得します．
		/// </summary>
		public bool FitCommandExecuting {
			get
			{
				return _fitCommandExecuting;
			}
			protected set
			{
				if (FitCommandExecuting != value)
				{
					_fitCommandExecuting = value;
					NotifyPropertyChanged();
				}
			}
		}
		bool _fitCommandExecuting = false;
		#endregion


		#endregion

		/*
		string GetCsvFileName(int cycle, string name)
		{
			return Path.Combine(DepthProfileFittingData.OutputDestination, $"{name}_{cycle}.csv");
		}
		*/



	}
	#endregion


	// とりあえず用意しておく．
	#region MyExceptionクラス
		/*
	public class MyException : System.Exception
	{
		public MyException() : base() { }
		public MyException(string message) : base(message) { }
	}
	*/
	#endregion

}


