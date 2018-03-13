using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{
	using Mvvm;
	using Data.Standard;
	using Helpers;

	#region WideScanViewModelクラス
	public class WideScanViewModel : ViewModelBase
	{

		// データをロードする前とした後を識別するフラグが，今のところ存在しない．
		// (_WideScanがnullということはありえない．)

		#region *WideScanFittingDataプロパティ
		public WideScanFittingData WideScanFittingData { get; } = new WideScanFittingData();
		#endregion





		// 現在機能していません．
		#region *FixedSpectraプロパティ
		public ObservableCollection<FixedSpectrum> FixedSpectra
		{
			get
			{
				return _fixedSpectra;
			}
		}
		ObservableCollection<FixedSpectrum> _fixedSpectra = new ObservableCollection<FixedSpectrum>();
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


		#region *コンストラクタ(WideScanViewModel)
		public WideScanViewModel()
		{
			// コマンドの設定をここで行う．

			SelectSimpleCsvDestinationCommand = new DelegateCommand(SelectSimpleCsvDestination_Executed);
			_exportCsvCommand = new DelegateCommand(ExportCsv_Executed, ExportCsv_CanExecute);
			SelectDestinationDirectoryCommand = new DelegateCommand(SelectDestinationDirectory_Executed);
			_addFixedSpectrumCommand = new DelegateCommand(AddFixedSpectrum_Executed);
			AddReferenceSpectrumCommand = new DelegateCommand(AddReferenceSpectrum_Executed);
			SelectChartDestinationCommand = new DelegateCommand(SelectChartDestination_Executed);
			AddFittingProfileCommand = new DelegateCommand(AddFittingProfile_Executed);
			RemoveProfileCommand = new DelegateCommand(RemoveProfile_Executed, RemoveProfile_CanExecute);
			FitSpectrumCommand = new DelegateCommand(FitSpectrum_Executed, FitSpectrum_CanExecute);

			LoadConditionCommand = new DelegateCommand(LoadCondition_Executed);
			SaveConditionCommand = new DelegateCommand(SaveCondition_Executed);

			WideScanFittingData.FittingCondition.PropertyChanged += FittingCondition_PropertyChanged;

			WideScanFittingData.FittingCondition.FittingProfiles.CollectionChanged += FittingProfiles_CollectionChanged;

		}

		// DepthProfileViewModelからのコピペ．
		private void FittingProfiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			FitSpectrumCommand.RaiseCanExecuteChanged();
		}

		// DepthProfileViewModelからのコピペ．
		private void FittingCondition_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			// RaiseCanExecuteChangedはここにまとめる．

			switch (e.PropertyName)
			{
				case nameof(ExportCsvDestination):
					ExportCsvCommand.RaiseCanExecuteChanged();
					break;
				case nameof(CurrentFittingProfile):
					AddReferenceSpectrumCommand.RaiseCanExecuteChanged();
					RemoveProfileCommand.RaiseCanExecuteChanged();
					break;
				case nameof(FitCommandExecuting):
					FitSpectrumCommand.RaiseCanExecuteChanged();
					break;
			}
		}

		#endregion



		#region 単純出力関連

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
		string _exportCsvDestination = string.Empty;
		#endregion


		public DelegateCommand SelectSimpleCsvDestinationCommand { get; }

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
			if (!string.IsNullOrEmpty(this.ExportCsvDestination))
			{
				bool diff = parameter is bool && (bool)parameter;
				await WideScanFittingData.ExportCsv(this.ExportCsvDestination, diff ? ExportCsvFlag.Raw : ExportCsvFlag.Raw);
			}
		}

		bool ExportCsv_CanExecute(object parameter)
		{
			return !string.IsNullOrEmpty(this.ExportCsvDestination);
		}

		#endregion


		#endregion

		#region SelectDestinationDirectory

		public DelegateCommand SelectDestinationDirectoryCommand { get; }

		void SelectDestinationDirectory_Executed(object parameter)
		{
			// フォルダダイアログの方が望ましい．
			var dialog = new Microsoft.Win32.SaveFileDialog { DefaultExt = ".csv" };

			// WPFでは同期的に実行するしかない？
			if (dialog.ShowDialog() == true)
			{
				this.WideScanFittingData.FittingCondition.OutputDestination = Path.GetDirectoryName(dialog.FileName);
			}
		}

		#endregion



		#region AddFittingProfileCommand

		void AddFittingProfile_Executed(object parameter)
		{
			WideScanFittingData.FittingCondition.AddFittingProfile();
		}

		public DelegateCommand AddFittingProfileCommand { get; }

		#endregion


		// DepthProfileViewModelからのコピペ．
		#region LoadCondition

		public DelegateCommand LoadConditionCommand { get; }

		void LoadCondition_Executed(object parameter)
		{
			var message = new SelectOpenFileMessage(this) { Message = "ロードするプロファイルを選択して下さい。" };
			message.Filter = new string[] { "*.fcd", "*" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				// ロードする．
				WideScanFittingData.LoadFittingCondition(message.SelectedFile);
			}
		}

		#endregion

		// DepthProfileViewModelからのコピペ．
		#region SaveCondition

		public DelegateCommand SaveConditionCommand { get; }

		void SaveCondition_Executed(object parameter)
		{
			var message = new SelectSaveFileMessage(this) { Message = "プロファイルの出力先を選んで下さい。" };
			message.Ext = new string[] { ".fcd" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				WideScanFittingData.SaveFittingCondition(message.SelectedFile);
			}

		}

		#endregion



		#region SelectChartDestination

		public DelegateCommand SelectChartDestinationCommand { get; }

		void SelectChartDestination_Executed(object parameter)
		{
			// ディレクトリを選ぶのか？
			// とりあえずファイル選択にしておく．
			// →ファイル選択と見せかけて、ディレクトリ選択にする。

			var message = new SelectSaveFileMessage(this) { Message = "pngファイルの出力先を選んで下さい．" };
			message.Ext = new string[] { ".png" };
			Messenger.Default.Send(this, message);
			WideScanFittingData.SetChartDestination(message.SelectedFile);

		}

		#endregion


		#region RemoveProfile

		public DelegateCommand RemoveProfileCommand { get; }

		void RemoveProfile_Executed(object parameter)
		{
			if (CurrentFittingProfile != null)
			{
				// これで大丈夫かな？
				WideScanFittingData.RemoveFittingProfile(CurrentFittingProfile);
			}
		}

		bool RemoveProfile_CanExecute(object parameter)
		{
			return CurrentFittingProfile != null;
		}

		#endregion


		#region AddReferenceSpectrum
		public DelegateCommand AddReferenceSpectrumCommand { get; }

		async void AddReferenceSpectrum_Executed(object parameter)
		{
			var message = new SelectOpenFileMessage(this) { Message = "参照スペクトルを選んで下さい．" };
			// 拡張子ではなくファイル名が指定されている場合ってどうするの？
			message.Filter = new string[] { "id" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				await WideScanFittingData.AddReferenceSpectrumAsync(message.SelectedFile, CurrentFittingProfile);
			}

		}

		bool AddReferenceSpectrum_CanExecute(object parameter)
		{
			return CurrentFittingProfile != null;
		}

		#endregion

		#region AddFixedSpectrum

		public DelegateCommand AddFixedSpectrumCommand
		{
			get
			{
				return _addFixedSpectrumCommand;
			}
		}
		DelegateCommand _addFixedSpectrumCommand;

		async void AddFixedSpectrum_Executed(object parameter)
		{
			string dir = await SelectSpectrumAsync("追加する固定スペクトルを選んで下さい。");
			if (!string.IsNullOrEmpty(dir))
			{
				FixedSpectra.Add(new FixedSpectrum { DirectoryName = dir });
			}
		}

		#endregion





		#region FitSpectrum


		public DelegateCommand FitSpectrumCommand { get; }

		// (0.0.6)バグを修正．
		// parameterで数値が渡されれば、1サイクルに対して解析を行う。
		// さもなければ、全サイクルに対して解析を行う。
		async void FitSpectrum_Executed(object parameter)
		{
			// parameterがnullであれば，全てのprofileに対してフィッティングを行う．
			// parameterにprofileが与えられていれば，それに対してフィッティングを行う．

			// ★BaseROIを決める段階と，実際のフィッティングを行う段階を分離した方がいいのでは？

			var fitting_task = parameter is FittingProfile ?
						Task.WhenAll(WideScanFittingData.FitSingleProfile((FittingProfile)parameter)) :
						Task.WhenAll(WideScanFittingData.FittingCondition.FittingProfiles.Select(p => WideScanFittingData.FitSingleProfile(p)));

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

		bool FitSpectrum_CanExecute(object parameter)
		{
			if (parameter is FittingProfile)
			{
				return true;
			}
			else
			{
				return WideScanFittingData.FittingCondition.FittingProfiles.Count > 0;
			}
		}



		#endregion



		#region *FitCommandExecutingプロパティ
		/// <summary>
		/// フィッティングが実行中であるか否かを取得します．
		/// </summary>
		public bool FitCommandExecuting
		{
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







		async Task<string> SelectSpectrumAsync(string description)
		{
			var message = new SelectOpenFileMessage(this) { Message = description};
			message.Filter = new string[] { "id" };
			Messenger.Default.Send(this, message);
			if (!string.IsNullOrEmpty(message.SelectedFile))
			{
				var id_file = message.SelectedFile;
				var dir = System.IO.Path.GetDirectoryName(id_file);
				if ((await IdFile.CheckTypeAsync(id_file)) == DataType.WideScan)
				{
					return dir;
				}
				else
				{
					// ※ViewModelからUIにメッセージを送る仕組みはどうする？
					// WideScanじゃないよ！
					throw new NotWideScanException("WideScanのデータしか使えませんよ！");
				}
			}
			else
			{
				// 単なるキャンセル．
				return string.Empty;
			}
		}

	}
	#endregion


	public class NotWideScanException : Exception
	{
		public NotWideScanException() : base() { }
		public NotWideScanException(string message) : base(message) { }

	}




}
