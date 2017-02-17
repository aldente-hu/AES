using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{
	// 純粋に数学的なfittingのlayer．


	public static class Fitting
	{

		#region *定数項付きフィッティングを行う(WithConstant)
		public static FittingResult WithConstant(IList<decimal> data, IList<IList<decimal>> references)
		{
			// targetと各referenceは同じ要素数であることを信じる．

			#region 行列を作る．

			int n = references.Count;	// 参照スペクトルの数．
			int m = n + 1;						// 定数項を含める．

			// これdecimalではできないのかな？
			var a = DenseMatrix.Create(m, m, 0);
			var b = DenseVector.Create(m, 0);

			Dictionary<int, decimal> matrix = new Dictionary<int, decimal>();
			for(int r = 0; r < m * (m+1); r++)
			{
				matrix[r] = 0;
			}

			// 最後の行や列が定数項に関連する．

			for (int i = 0; i < data.Count; i++)
			{
				for (int p = 0; p < m; p++)
				{
					for (int q = p; q < m; q++)
					{
						//Debug.WriteLine($"{data[i]},{reference[i]}");
						if (q == n)
						{
							if (p != n)
							{
								//a[p, n] += Convert.ToDouble(references[p][i]);
								matrix[p * m + n] += references[p][i];
							}
						}
						else
						{
							//a[p, q] += Convert.ToDouble(references[p][i] * references[q][i]);
							matrix[p * m + q] += references[p][i] * references[q][i];
						}
					}
					if (p == n)
					{
						//b[p] += Convert.ToDouble(data[i]);
						matrix[m * m + p] += data[i];
					}
					else
					{
						//b[p] += Convert.ToDouble(references[p][i] * data[i]);
						matrix[m * m + p] += references[p][i] * data[i];
					}
				}
				//System.Diagnostics.Debug.WriteLine($"{matrix[2]} --- {matrix[6]}");
			}

			for (int p = 0; p < m; p++)
			{
				for (int q = p + 1; q < m; q++)
				{
					//a[q, p] += a[p, q];
					matrix[q * m + p] = matrix[p * m + q];
				}
			}
			//a[n, n] = data.Count;
			matrix[n * m + n] = data.Count;

			foreach (var pair in matrix)
			{
				System.Diagnostics.Debug.WriteLine($"{pair.Key} --- {pair.Value}");
				int r = pair.Key;
				if (r < m * m)
				{
					a[r / m, r % m] = Convert.ToDouble(pair.Value);
				}
				else
				{
					b[r % m] = Convert.ToDouble(pair.Value);
				}
			}

			#endregion

			#region 係数を求める．

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

			var factors = result.Select(f => Convert.ToDecimal(f)).ToList();

			#endregion

			#region  (2乗)残差を求める．
			decimal residual = 0;
			for (int i = 0; i < data.Count; i++)
			{
				var diff = data[i];
				for (int p=0; p < references.Count; p++)
				{
					diff -= references[p][i] * factors[p];
				}
				diff -= factors[n];	// 定数項
				residual += diff * diff;
			}

			#endregion

			return new FittingResult(factors, residual);

		}
		#endregion


		// ほとんどWithConstantのコピペ．

		#region *定数項なしフィッティングを行う(WithoutConstant)
		public static FittingResult WithoutConstant(IList<decimal> data, IList<IList<decimal>> references)
		{
			// targetと各referenceは同じ要素数であることを信じる．

			#region 行列を作る．

			int n = references.Count; // 参照スペクトルの数．
			int m = n;            // 定数項を含めない．

			// これdecimalではできないのかな？
			var a = DenseMatrix.Create(m, m, 0);
			var b = DenseVector.Create(m, 0);

			Dictionary<int, decimal> matrix = new Dictionary<int, decimal>();
			for (int r = 0; r < m * (m + 1); r++)
			{
				matrix[r] = 0;
			}

			// 最後の行や列が定数項に関連する．

			for (int i = 0; i < data.Count; i++)
			{
				for (int p = 0; p < m; p++)
				{
					for (int q = p; q < m; q++)
					{
						matrix[p * m + q] += references[p][i] * references[q][i];
					}
					matrix[m * m + p] += references[p][i] * data[i];
				}
			}

			for (int p = 0; p < m; p++)
			{
				for (int q = p + 1; q < m; q++)
				{
					matrix[q * m + p] = matrix[p * m + q];
				}
			}

			foreach (var pair in matrix)
			{
				System.Diagnostics.Debug.WriteLine($"{pair.Key} --- {pair.Value}");
				int r = pair.Key;
				if (r < m * m)
				{
					a[r / m, r % m] = Convert.ToDouble(pair.Value);
				}
				else
				{
					b[r % m] = Convert.ToDouble(pair.Value);
				}
			}

			#endregion

			#region 係数を求める．

			Vector<double> result = null;
			bool retry_flag = true;
			while (retry_flag)
			{
				retry_flag = false;
				result = a.Inverse() * b;

				// resultに負の値があったらやり直す。
				for (int i = 0; i < result.Count; i++)
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

			var factors = result.Select(f => Convert.ToDecimal(f)).ToList();

			#endregion

			#region  (2乗)残差を求める．
			decimal residual = 0;
			for (int i = 0; i < data.Count; i++)
			{
				var diff = data[i];
				for (int p = 0; p < references.Count; p++)
				{
					diff -= references[p][i] * factors[p];
				}
				residual += diff * diff;
			}

			#endregion

			return new FittingResult(factors, residual);

		}
		#endregion


	}



}
