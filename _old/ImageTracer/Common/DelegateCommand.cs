using System;
using System.Windows.Input;

namespace ImageTracer.Common
{
    public class DelegateCommand : ICommand
    {
        public Action<object> ExecuteHandler { get; set; }
        public Func<object, bool> CanExecuteHandler { get; set; }

        #region ICommand メンバー

        public bool CanExecute(object parameter)
        {
            var d = CanExecuteHandler;
            return d == null ? true : d(parameter);
        }

        public void Execute(object parameter = null)
        {
            var d = ExecuteHandler;
            //if (d != null) d(parameter);
            d(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            var d = CanExecuteChanged;
            if (d != null) d(this, null);
        }

        #endregion
    }
}