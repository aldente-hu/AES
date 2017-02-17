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

					// なぜか以下がバインディングされない．

					this.FittingCondition.Name = CurrentROI.Name;
					// 微分を考慮していない！
					this.FittingCondition.RangeBegin = CurrentROI.Parameter.Start;
					this.FittingCondition.RangeEnd = CurrentROI.Parameter.Stop;
				}
			}
		}
		ROISpectra _currentROI = null;
		#endregion

		#region *FittingConditionプロパティ
		public FittingCondition FittingCondition
		{
			get
			{
				return _fittingCondition;
			}
		}
		FittingCondition _fittingCondition = new FittingCondition();
		#endregion

		public DepthProfileViewModel()
		{
			_selectCsvDestinationCommand = new DelegateCommand(SelectCsvDestination_Executed);
			//_exportCsvCommand = new DelegateCommand(ExportCsv_Executed);
			_exportCsvCommand = new DelegateCommand(ExportCsv_Executed, ExportCsv_CanExecute);
			_selectChartDestinationCommand = new DelegateCommand(SelectChartDestination_Executed);
			_addReferenceSpectrumCommand = new DelegateCommand(AddReferenceSpectrum_Executed);
			_fitSpectrumCommand = new DelegateCommand(FitSpectrum_Executed);
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

			var message = new SelectSaveFileMessage(this) { Message = "pngファイルの出力先を選んで下さい．" };
			message.Ext = new string[] { ".png" };
			Messenger.Default.Send(this, message);
			if (string.IsNullOrEmpty(message.SelectedFile))
			{
				//this.ChartDestination = string.Empty;
				FittingCondition.OutputDestination = string.Empty;
			}
			else
			{
				//this.ChartDestination = message.SelectedFile;
				FittingCondition.OutputDestination = message.SelectedFile;
			}

		}
/*
		#region *ChartDestinationプロパティ
		public string ChartDestination
		{
			get
			{
				return _chartDestination;
			}
			set
			{
				if (ExportCsvDestination != value)
				{
					_chartDestination = value;
					NotifyPropertyChanged();
					_fitSpectrumCommand.RaiseCanExecuteChanged();
				}
			}
		}
		string _chartDestination;
		#endregion
*/
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
				var id_file = message.SelectedFile;
				var dir = System.IO.Path.GetDirectoryName(id_file);

				if ((await IdFile.CheckTypeAsync(id_file)) == DataType.WideScan)
				{
					// OK
					FittingCondition.ReferenceSpectra.Add(new ReferenceSpectrum { DirectoryName = dir });
				}
				else
				{
					var error_message = new SimpleMessage(this) { Message = "WideScanじゃないとだめだよ！" };
					Messenger.Default.Send(this, error_message);
				}

			}

		}

		#endregion

		public DelegateCommand FitSpectrumCommand
		{
			get
			{
				return _fitSpectrumCommand;
			}
		}
		DelegateCommand _fitSpectrumCommand;

		async void FitSpectrum_Executed(object parameter)
		{
			var d_data = CurrentROI.Restrict(FittingCondition.RangeBegin, FittingCondition.RangeEnd)
						.Differentiate(3);


			// 固定参照スペクトルを取得する。
			//List<decimal> fixed_data = new List<decimal>();
			//if (FixedSpectra.Count > 0)
			//{
			//	var v_data = await LoadShiftedFixedStandardsData(FixedSpectra, d_data.Parameter);
			//	for (int j = 0; j < v_data.First().Count; j++)
			//	{
			//		fixed_data.Add(v_data.Sum(one => one[j]));
			//	}
			//}


			//if (radioButtonFitAll.IsChecked == true)
			//{
			//	// これをパラレルに行う。
			//	Parallel.For(0, d_data.Data.Length,
			//		i => FitOneLayer(i, d_data.Data[i], d_data.Parameter, ReferenceSpectra, fixed_data,
			//			_depthProfileSetting.OutputDestination,
			//			_depthProfileSetting.Name)
			//	);
			//}
			//else
			//{
			//int i = (int)comboBoxLayers.SelectedItem;
			// とりあえず1つだけ．
			int cycle = 0;

			//_depthProfile.Spectra[i]
			//	FitOneLayer(i, d_data.Data[i], d_data.Parameter, ReferenceSpectra, fixed_data,
			//		_depthProfileSetting.OutputDestination,
			//		_depthProfileSetting.Name);
			//}


			// これ以降は，WideScanのFit_Executeをコピペしただけ．

			// 固定参照スペクトルを取得する。
			/*
			List<decimal> fixed_data = new List<decimal>();
			if (FixedSpectra.Count > 0)
			{
				var v_data = await FixedSpectra.ForEachAsync(
					async sp => await sp.GetShiftedDataAsync(d_data.Parameter, 3), 10);

				for (int j = 0; j < v_data.First().Count; j++)
				{
					fixed_data.Add(v_data.Sum(one => one[j]));
				}
			}
			*/

			// 参照スペクトルを取得する．

			// 参照スペクトルのデータを，測定データの範囲に制限し，ピッチも測定データに合わせる．
			// →と考えたが，これはいずれもシフト値によって変わることに注意！

			// リファレンスをどう用意するか？

			var references = await FittingCondition.ReferenceSpectra.ForEachAsync(sp => sp.GetDataAsync(d_data.Parameter, 3), 10);

			var result = Fitting.WithConstant(d_data.Data[cycle], references);

			// とりあえず簡単に結果を出力する．
			string destination = Path.Combine(Path.GetDirectoryName(FittingCondition.OutputDestination), "result.txt");
			using (var writer = new StreamWriter(destination, false))
			{
				for (int i = 0; i < result.Factors.Count; i++)
				{
					writer.WriteLine($"Factor{i}  :  {result.Factors[i]}");
				}
				writer.WriteLine($"residual  :  {result.Residual}");
			}

			//FitOneLayer(0, d_data.Data, d_data.Parameter, WideFittingModel.ReferenceSpectra, fixed_data,
			//	WideFittingModel.OutputDestination,
			//	"Wide");

		}

	}

}
