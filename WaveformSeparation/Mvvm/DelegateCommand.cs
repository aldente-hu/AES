using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation.Mvvm
{

	#region DelegateCommandクラス
	public class DelegateCommand : ICommand
	{
		readonly Action<object> _execute;
		readonly Predicate<object> _canExecute;
		//private readonly Func<bool> _canExecute;

		#region *コンストラクタ(DelegateCommand)

		/// <summary>
		/// Initializes a new instance of the RelayCommand class that
		/// can always execute.
		/// </summary>
		/// <param name="execute">The execution logic.</param>
		/// <exception cref="ArgumentNullException">If the execute argument is null.</exception>
		public DelegateCommand(Action<object> execute)
				: this(execute, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the RelayCommand class.
		/// </summary>
		/// <param name="execute">The execution logic.</param>
		/// <param name="canExecute">The execution status logic.</param>
		/// <exception cref="ArgumentNullException">If the execute argument is null.</exception>
		public DelegateCommand(Action<object> execute, Predicate<object> canExecute)
		{
			if (execute == null)
			{
				throw new ArgumentNullException("execute");
			}

			_execute = execute;
			_canExecute = canExecute;
		}

		#endregion


		#region ICommand実装

		public event EventHandler CanExecuteChanged = delegate { };

		/// <summary>
		/// Raises the <see cref="CanExecuteChanged" /> event.
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate",
				Justification = "This cannot be an event")]
		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged(this, EventArgs.Empty);
		}


		[DebuggerStepThrough]
		public bool CanExecute(object parameter)
		{
			return _canExecute == null ? true : _canExecute(parameter);
		}

		public void Execute(object parameter)
		{
			if (CanExecute(parameter))
			{
				_execute(parameter);
			}
		}

		#endregion

	}
	#endregion
}
