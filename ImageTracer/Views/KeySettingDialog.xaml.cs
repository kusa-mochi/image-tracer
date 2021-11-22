using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using ImageTracer.ViewModels;

namespace ImageTracer.Views
{
    /// <summary>
    /// KeySettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class KeySettingDialog : Window
    {
        public KeySettingDialog()
        {
            InitializeComponent();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            // ユーザーが押したキーを、VMに格納する。
            // このパラメータにはMainWindowViewModelからもアクセスできる。
            ((KeySettingDialogViewModel)this.DataContext).Key = e.Key;

            this.Close();
        }
    }
}
