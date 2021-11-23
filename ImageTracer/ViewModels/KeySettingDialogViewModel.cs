using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Livet;
using Livet.Commands;

namespace ImageTracer.ViewModels
{
    /// <summary>
    /// ショートカットキー設定ダイアログのVM。
    /// </summary>
    public class KeySettingDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// 新しく設定するショートカットキーがユーザーにより入力されたときに発火するイベント。
        /// </summary>
        public event EventHandler KeyInput = (sender, e) => { };

        /// <summary>
        /// ユーザーによって押下されたキーを格納するプロパティ。
        /// </summary>
        private Key _key = Key.None;
        public Key Key
        {
            get { return _key; }
            set
            {
                _key = value;
                KeyInput?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
