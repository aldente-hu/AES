﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

using System.Diagnostics;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

using System.Xml.Linq;


namespace HirosakiUniversity.Aldente.AES.Data.Standard
{

	// (0.2.0) 一部メソッドをローカル関数化．
	#region FittingProfileクラス
	/// <summary>
	/// フィッティングの最小単位を表すクラス．データの置き場所ぐらいで活用するのがいいでしょう．
	/// </summary>
	public class FittingProfile 
	{

		#region プロパティ

		#region *Nameプロパティ
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if (_name != value)
				{
					_name = value;
					NotifyPropertyChanged("Name");
				}
			}
		}
		string _name = string.Empty;
		#endregion

		#region *RangeBeginプロパティ
		public decimal RangeBegin
		{
			get
			{
				return _rangeBegin;
			}
			set
			{
				if (_rangeBegin != value)
				{
					_rangeBegin = value;
					NotifyPropertyChanged("RangeBegin");
				}
			}
		}
		decimal _rangeBegin = 100;
		#endregion

		#region *RangeEndプロパティ
		public decimal RangeEnd
		{
			get
			{
				return _rangeEnd;
			}
			set
			{
				if (_rangeEnd != value)
				{
					_rangeEnd = value;
					NotifyPropertyChanged("RangeEnd");
				}
			}
		}
		decimal _rangeEnd = 200;
		#endregion

		#region *WithOffsetプロパティ
		/// <summary>
		/// フィッティングの際に定数項を考慮するか否かの値を取得／設定します．
		/// </summary>
		public bool WithOffset
		{
			get
			{
				return _with_offset;
			}
			set
			{
				if (WithOffset != value)
				{
					_with_offset = value;
					NotifyPropertyChanged("WithOffset");
				}
			}
		}
		bool _with_offset = true;
		#endregion

		#region エネルギーシフト関連

		#region *FixEnergyShiftプロパティ
		/// <summary>
		/// エネルギーシフト値を固定するかどうかの値を取得／設定します。
		/// </summary>
		public bool FixEnergyShift
		{
			get
			{
				return _fixEnergyShift;
			}
			set
			{
				if (FixEnergyShift != value)
				{
					_fixEnergyShift = value;
					NotifyPropertyChanged();
				}
			}
		}
		bool _fixEnergyShift = false;
		#endregion

		#region *FixedEnergyShiftプロパティ
		/// <summary>
		/// 固定されたエネルギーシフト値を取得／設定します。このプロパティは、FixEnergyShiftプロパティがtrueの場合にのみ有効です。
		/// </summary>
		public decimal FixedEnergyShift
		{
			get
			{
				return _fixedEnergyShift;
			}
			set
			{
				if (FixedEnergyShift != value)
				{
					_fixedEnergyShift = value;
					NotifyPropertyChanged();
				}
			}
		}
		decimal _fixedEnergyShift = 0.0M;
		#endregion

		#endregion

		#region 参照スペクトル関連

		#region *ReferenceSpectraプロパティ
		/// <summary>
		/// 参照スペクトルのコレクションを取得します．
		/// </summary>
		public ObservableCollection<ReferenceSpectrum> ReferenceSpectra { get; } = new ObservableCollection<ReferenceSpectrum>();
		#endregion

		// これいるのかな？と思うけど、とりあえず実装しておく。
		#region *FixedSpectraプロパティ
		public ObservableCollection<FixedSpectrum> FixedSpectra { get; } = new ObservableCollection<FixedSpectrum>();
		#endregion

		#endregion

		#endregion


		#region *コンストラクタ(FittingProfile)
		public FittingProfile()
		{
		}
		#endregion

		#region *1サイクル分をフィッティングする(FitOneCycle)
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cycle">対象サイクル．デバッグ出力にしか使用していません．</param>
		/// <param name="data"></param>
		/// <param name="originalParameter"></param>
		/// <returns></returns>
		//public async Task<FittingResult> FitOneCycle(int cycle, EqualIntervalData data, ScanParameter originalParameter)
		public FittingResult FitOneCycle(int cycle, EqualIntervalData data, ScanParameter originalParameter)
		{
			#region  固定参照スペクトルを取得する。(一時的にコメントアウト中)
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
			#endregion


			/// フィッティング対象となるデータ。すなわち、もとのデータからFixされた分を差し引いたデータ。
			//var target_data = fixed_data.Count > 0 ? data.Substract(fixed_data) : data;
			// なんだけど、とりあえずはFixedを考慮しない。
			var target_data = data;


			if (!FixEnergyShift)
			{
				#region A.最適なエネルギーシフト量を見つける場合

				Trace.WriteLine($"Cycle {cycle} START!! {DateTime.Now}  [{Thread.CurrentThread.ManagedThreadId}]");

				var best_fitting_result = DecideShift(target_data, originalParameter);
				var best_shift = best_fitting_result.Shift;

				Trace.WriteLine($" {cycle} 本当に最適なシフト値は {best_shift} だよ！");
				Trace.WriteLine($"Cycle {cycle} END!! {DateTime.Now}  [{Thread.CurrentThread.ManagedThreadId}]");



				// シフトされた参照スペクトルを読み込む。
				var best_shifted_parameter = originalParameter.GetShiftedParameter(best_shift);
				//var best_standards = LoadShiftedStandardsDataAsync(ReferenceSpectra, best_shifted_parameter);
				var best_standards = LoadShiftedStandardsData(ReferenceSpectra, best_shifted_parameter);
				var best_gain = best_fitting_result.Gain;

				return new FittingResult { Shift = best_shift, Gains = best_gain.Select(d => Convert.ToDecimal(d)).ToArray(), Standards = best_standards };

				#endregion
			}
			else
			{
				#region B.エネルギーシフト量を自分で与える場合

				var shifted_parameter = originalParameter.GetShiftedParameter(FixedEnergyShift);

				// シフトされた参照スペクトルを読み込む。
				var standards = LoadShiftedStandardsData(ReferenceSpectra, shifted_parameter);

				// フィッティングを行い、
				Trace.WriteLine($"Cycle {cycle}");
				var gains = GetOptimizedGains(WithOffset, target_data, standards.ToArray());
				for (int j = 0; j < ReferenceSpectra.Count; j++)
				{
					Trace.WriteLine($"    {ReferenceSpectra[j].Name} : {gains[j]}");
				}
				Trace.WriteLine($"    Const : {gains[ReferenceSpectra.Count]}");

				return new FittingResult
				{
					Shift = FixedEnergyShift,
					Standards = standards,
					Gains = gains.Select(d => Convert.ToDecimal(d)).ToArray()
				};
				#endregion
			}

			// 出力は呼び出し元で行う！
		}
		#endregion

		#region *最適なシフト量を決定(DecideShift)
		FitForOneShiftResult DecideShift(EqualIntervalData targetData, ScanParameter originalParameter)
		{
			var gains = new Dictionary<decimal, Vector<double>>();
			Dictionary<decimal, decimal> residuals = new Dictionary<decimal, decimal>();
			var best_fit_result = new FitForOneShiftResult();
			List<decimal> shiftList = new List<decimal>();

			// 1回目
			for (int m = -6; m < 7; m++)
			{
				shiftList.Add(0.5M * m); // とりあえず。
			}
			Trace.WriteLine($"SearchShift START!! {DateTime.Now:O}  [{Thread.CurrentThread.ManagedThreadId}]");
			best_fit_result = SearchBestShift(shiftList, best_fit_result);
			var best_shift = best_fit_result.Shift;
			Trace.WriteLine($"SearchShift Completed!! {DateTime.Now:O}  [{Thread.CurrentThread.ManagedThreadId}]");
			Trace.WriteLine($"シフト値は {best_shift} がよさそうだよ！");

			// 2回目
			shiftList.Clear();
			for (int i = 1; i < 5; i++)
			{
				shiftList.Add(best_shift + 0.1M * i);
				shiftList.Add(best_shift - 0.1M * i);
			}
			return SearchBestShift(shiftList, best_fit_result);

			#region ローカル関数

			#region **与えられたシフト量から最適なものを探す(SearchBestShift)
			FitForOneShiftResult SearchBestShift(
				IEnumerable<decimal> shiftCandidates,
				FitForOneShiftResult previousResult)
			{
				var best_result = previousResult;

				// ※たぶん，メンバ変数にする必要がある．
				var _cancellation_token = new CancellationToken();
				object lockObject = new object();

				var loopResult = Parallel.ForEach<decimal, FitForOneShiftResult>(
					shiftCandidates,
					new ParallelOptions { CancellationToken = _cancellation_token },
					() => best_result,
					(shift, state, best) =>
					{
						return FitForOneShift(shift);
					},
					(result) => {
						lock (lockObject)
						{
							if (!best_result.Residual.HasValue || best_result.Residual.Value > result.Residual.Value)
							{
								best_result = result;
							}
						}
					}
				);

				return best_result;

				#region ローカル関数

				#region ***1つのシフト量に対してフィッティングを行う(FitForOneShift)

				// とりあえず同期版を使う．
				// IO待ちのメソッドではないので，asyncにしない方がよい？

				FitForOneShiftResult FitForOneShift(decimal shift)
				{
					Trace.WriteLine($"shift : {shift}         {DateTime.Now:O}  [{Thread.CurrentThread.ManagedThreadId}]");

					var shifted_parameter = originalParameter.GetShiftedParameter(shift);

					// ここを非同期にすると，制御が返らなくなる？
					// シフトされた参照スペクトルを読み込む。
					var standards = LoadShiftedStandardsData(ReferenceSpectra, shifted_parameter);

					// フィッティングを行い、
					//Trace.WriteLine($"Cycle {cycle}");
					var gain = GetOptimizedGains(WithOffset, targetData, standards.ToArray());
					//gains.Add(shift, );
					for (int j = 0; j < ReferenceSpectra.Count; j++)
					{
						Trace.WriteLine($"    {ReferenceSpectra[j].Name} : {gain[j]}        [{Thread.CurrentThread.ManagedThreadId}]");
					}
					Trace.WriteLine($"    Const : {gain[ReferenceSpectra.Count]}        [{Thread.CurrentThread.ManagedThreadId}]");

					// 残差を取得する。
					var residual = EqualIntervalData.GetTotalSquareResidual(targetData, gain.ToArray(), standards.ToArray()); // 残差2乗和
																																																										//residuals.Add(shift, residual);
					Trace.WriteLine($"residual = {residual}        [{Thread.CurrentThread.ManagedThreadId}]");

					return new FitForOneShiftResult { Gain = gain, Shift = shift, Residual = residual };
				}
				#endregion

				#endregion

			}
			#endregion

			#endregion

		}
		#endregion


		#region staticメソッド(ここでいいのかな？)

		static List<List<decimal>> LoadShiftedStandardsData(ICollection<ReferenceSpectrum> references, ScanParameter parameter)
		{
			List<List<decimal>> standards = new List<List<decimal>>();
			foreach (var item in references)
			{
				var ws = WideScan.Generate(item.DirectoryName);
				standards.Add(
					ws.Differentiate(3)
						.GetInterpolatedData(parameter.Start, parameter.Step, parameter.PointsCount)
						.Select(d => d * ws.Parameter.NormalizationGain / parameter.NormalizationGain).ToList()
				);
			}
			return standards;
		}

		#region *[static]標準データをシフトして読み込む(LoadShiftedStandardData)
		/// <summary>
		/// シフトを考慮した標準試料データを読み込みます．
		/// </summary>
		/// <param name="references"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		static async Task<List<List<decimal>>> LoadShiftedStandardsDataAsync(ICollection<ReferenceSpectrum> references, ScanParameter parameter)
		{
			List<List<decimal>> standards = new List<List<decimal>>();
			foreach (var item in references)
			{
				var ws = await WideScan.GenerateAsync(item.DirectoryName);
				standards.Add(
					ws.Differentiate(3)
						.GetInterpolatedData(parameter.Start, parameter.Step, parameter.PointsCount)
						.Select(d => d * ws.Parameter.NormalizationGain / parameter.NormalizationGain).ToList()
				);
			}
			return standards;
		}
		#endregion

		#region *[static]固定参照データをシフトして読み込む(LoadShiftedFixedStandardData)
		/// <summary>
		/// シフトを考慮した固定参照データを読み込みます．
		/// </summary>
		/// <param name="references"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		static async Task<List<List<decimal>>> LoadShiftedFixedStandardsData(ICollection<FixedSpectrum> references, ScanParameter parameter)
		{
			List<List<decimal>> standards = new List<List<decimal>>();
			foreach (var item in references)
			{
				var ws = await WideScan.GenerateAsync(item.DirectoryName);
				standards.Add(
					ws.Differentiate(3)
						.GetInterpolatedData(parameter.Start - item.Shift, parameter.Step, parameter.PointsCount)
						.Select(d => d * ws.Parameter.NormalizationGain / parameter.NormalizationGain * item.Gain).ToList()
				);
			}
			return standards;
		}
		#endregion

		#region *[static]最適なシフト量を決定(DecideBestShift)
		/// <summary>
		/// 残差データから，最適なシフト量を決定します．
		/// </summary>
		/// <param name="residuals"></param>
		/// <returns></returns>
		static decimal DecideBestShift(Dictionary<decimal, decimal> residuals)
		{
			// ↓これでいいのかなぁ？
			// return residuals.First(r => r.Value == residuals.Values.Min()).Key;

			KeyValuePair<decimal, decimal>? best = null;
			foreach (var residual in residuals)
			{
				if (!best.HasValue || best.Value.Value > residual.Value)
				{
					best = residual;
				}
			}
			return best.Value.Key;
		}
		#endregion

		#region *[static]残差2乗和を計算(CulculateResidual)
		/// <summary>
		/// 残差2乗和を求めてそれを返します。
		/// </summary>
		/// <param name="data">測定データ．</param>
		/// <param name="reference">(一般には複数の)参照データ．</param>
		/// <returns></returns>
		static decimal CulculateResidual(bool with_offset, IList<decimal> data, params IList<decimal>[] references)  // C# 6.0 で，params引数に配列型以外も使えるようになった．
		{
			// 2.最適値なゲイン係数(+オフセット値)を求める
			var gains = GetOptimizedGains(with_offset, data, references);

			// 3.残差を求める
			var residual = EqualIntervalData.GetTotalSquareResidual(data, gains.ToArray(), references); // 残差2乗和
			Trace.WriteLine($"residual = {residual}");

			return residual;
		}
		#endregion

		#region *[static]最適なゲイン係数などを取得(GetOptimizedGains)
		/// <summary>
		/// 最適なゲイン係数＋オフセット定数を配列として返します。
		/// </summary>
		/// <param name="data">測定データ．</param>
		/// <param name="references">(一般には複数の)参照データ．</param>
		/// <returns></returns>
		public static Vector<double> GetOptimizedGains(bool with_offset, IList<decimal> data, params IList<decimal>[] references)
		{
			int n = references.Length;
			int m = n + 1;

			// これdecimalではできないのかな？
			var a = DenseMatrix.Create(m, m, 0);
			var b = DenseVector.Create(m, 0);

			// p<n, q<n とすると，
			// a[p,q] = Σ (references[p][*] * references[q][*])
			// a[p,n] = Σ references[p][*]
			// a[n,n] = data.Count
			// b[p] = Σ (references[p][*] * data[*])
			// b[n] = Σ data[*]

			for (int i = 0; i < data.Count; i++)
			{
				for (int p = 0; p < m; p++)
				{
					for (int q = p; q < m; q++)
					{
						//Debug.WriteLine($"{data[i]},{reference[i]}");
						if (q == n)
						{
							if (with_offset && p != n)
							{
								a[p, n] += Convert.ToDouble(references[p][i]);
							}
						}
						else
						{
							a[p, q] += Convert.ToDouble(references[p][i] * references[q][i]);
						}
					}
					if (with_offset && p == n)
					{
						b[p] += Convert.ToDouble(data[i]);
					}
					else
					{
						b[p] += Convert.ToDouble(references[p][i] * data[i]);
					}
				}
			}

			for (int p = 0; p < m; p++)
			{
				for (int q = p + 1; q < m; q++)
				{
					a[q, p] += a[p, q];
				}
			}
			a[n, n] = data.Count; // 0ではないはずなので，with_offset = falseの場合でもaがregularにならずに困ることはない．

			Vector<double> result = null;
			bool retry_flag = true;
			while (retry_flag)
			{
				retry_flag = false;
				result = a.Inverse() * b;

				// 定数項以外にresultに負の値があったらやり直す。
				for (int i = 0; i < result.Count - 1; i++)
				{
					if (result[i] < 0)
					{
						retry_flag = true;
						// i行とi列をゼロベクトルにする。
						for (int j = 0; j < a.ColumnCount; j++)
						{
							a[i, j] = 0;
							a[j, i] = 0;
						}
						a[i, i] = 1;
						b[i] = 0;
					}
				}
			}
			return result;

		}
		#endregion

		#endregion



		#region 入出力関連

		#region XML要素名
		public const string ELEMENT_NAME = "Profile";
		const string NAME_ATTRIBUTE = "Name";
		const string ENERGYBEGIN_ATTRIBUTE = "Begin";
		const string ENERGYEND_ATTRIBUTE = "End";
		const string WITHOFFSET_ATTRIBUTE = "Offset";
		const string ENERGYSHIFT_ATTRIBUTE = "EnergyShift";
		const string REFERENCES_ELEMENT = "References";
		const string REFERENCE_ELEMENT = "Reference";
		const string DIRECTORY_ATTRIBUTE = "Directory";
		const string FIXED_REFERENCES_ELEMENT = "FixedReferences";
		const string GAIN_ATTRIBUTE = "Gain";
		const string SHIFT_ATTRIBUTE = "Shift";
		#endregion

		#region *XML要素を生成(GenerateElement)
		public XElement GenerateElement()
		{
			XElement element = new XElement(ELEMENT_NAME);

			element.Add(new XAttribute(NAME_ATTRIBUTE, this.Name));
			element.Add(new XAttribute(ENERGYBEGIN_ATTRIBUTE, this.RangeBegin));
			element.Add(new XAttribute(ENERGYEND_ATTRIBUTE, this.RangeEnd));
			element.Add(new XAttribute(WITHOFFSET_ATTRIBUTE, this.WithOffset));

			if (this.FixEnergyShift)
			{
				element.Add(new XAttribute(ENERGYSHIFT_ATTRIBUTE, this.FixedEnergyShift));
			}

			var ref_element = new XElement(REFERENCES_ELEMENT);
			foreach (var reference in ReferenceSpectra)
			{
				ref_element.Add(
					new XElement(REFERENCE_ELEMENT,
						new XAttribute(DIRECTORY_ATTRIBUTE, reference.DirectoryName))
				);
			}
			foreach (var fixed_reference in FixedSpectra)
			{
				ref_element.Add(
					new XElement(FIXED_REFERENCES_ELEMENT,
						new XAttribute(DIRECTORY_ATTRIBUTE, fixed_reference.DirectoryName),
						new XAttribute(GAIN_ATTRIBUTE, fixed_reference.Gain),
						new XAttribute(SHIFT_ATTRIBUTE, fixed_reference.Shift)
					)
				);
			}
			element.Add(ref_element);

			return element;
		}
		#endregion

		#region *[static]XML要素からプロファイルをロード(LoadProfile)
		public static FittingProfile LoadProfile(XElement profileElement)
		{
			var profile = new FittingProfile();

			profile.Name = (string)profileElement.Attribute(NAME_ATTRIBUTE);
			profile.RangeBegin = (decimal)profileElement.Attribute(ENERGYBEGIN_ATTRIBUTE);
			profile.RangeEnd = (decimal)profileElement.Attribute(ENERGYEND_ATTRIBUTE);
			profile.WithOffset = (bool)profileElement.Attribute(WITHOFFSET_ATTRIBUTE);

			var energy_shift = (decimal?)profileElement.Attribute(ENERGYSHIFT_ATTRIBUTE);
			if (profile.FixEnergyShift = energy_shift.HasValue)
			{
				profile.FixedEnergyShift = energy_shift.Value;
			}

			profile.ReferenceSpectra.Clear();
			foreach (var reference in profileElement.Element(REFERENCES_ELEMENT).Elements(REFERENCE_ELEMENT))
			{
				profile.ReferenceSpectra.Add(new ReferenceSpectrum {
					DirectoryName = (string)reference.Attribute(DIRECTORY_ATTRIBUTE)
				});
			}

			profile.FixedSpectra.Clear();
			foreach (var reference in profileElement.Element(REFERENCES_ELEMENT).Elements(FIXED_REFERENCES_ELEMENT))
			{
				profile.FixedSpectra.Add(new FixedSpectrum {
					DirectoryName = (string)reference.Attribute(DIRECTORY_ATTRIBUTE),
					Gain = (decimal)reference.Attribute(GAIN_ATTRIBUTE),
					Shift = (decimal)reference.Attribute(SHIFT_ATTRIBUTE)
				});
			}

			return profile;
		}
		#endregion

		#endregion




		#region INotifyPropertyChanged実装

		protected void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		#endregion



		// ※これをどこに置くべきか...
		#region FittingResultクラス
		public class FittingResult
		{
			/// <summary>
			/// シフト値を取得／設定します。
			/// </summary>
			public decimal Shift { get; set; }
			public decimal[] Gains { get; set; }

			/// <summary>
			/// Gainを乗じる前の参照データ(Shiftは考慮済み)を取得／設定します。
			/// </summary>
			public List<List<decimal>> Standards { get; set; }

			public decimal GetGainedStandard(int standard, int position)
			{
				return Convert.ToDecimal(Gains[standard]) * Standards[standard][position];
			}
		}
		#endregion


	}
	#endregion

	// ※これをどこに置くべきか...
	#region FitForOneShiftResultクラス
	public class FitForOneShiftResult
	{
		/// <summary>
		/// 計算最小のシフト値を取得／設定します．
		/// </summary>
		public decimal Shift { get; set; }
		/// <summary>
		/// 各標準データに適用する係数を取得／設定します．
		/// </summary>
		public Vector<double> Gain { get; set; }
		/// <summary>
		/// 残差を取得／設定します．
		/// </summary>
		public decimal? Residual { get; set; }

		public FitForOneShiftResult()
		{
			Residual = null;
		}
	}
	#endregion


}
