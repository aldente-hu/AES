using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation
{

	#region FittingResultクラス
	public class FittingResult
	{
		public IList<decimal> Factors
		{
			get; private set;
		}

		public decimal Residual
		{
			get; private set;
		}

		public FittingResult(List<decimal> factors, decimal residual)
		{
			this.Factors = factors;
			this.Residual = residual;
		}
	}
	#endregion

}
