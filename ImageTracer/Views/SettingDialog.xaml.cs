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
    /// SettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingDialog : Window
    {
        public SettingDialog()
        {
            InitializeComponent();
        }

        public SettingDialog(MainWindowViewModel vm)
        {
            if (vm == null)
            {
                throw new ArgumentNullException("vm");
            }

            InitializeComponent();
            this.DataContext = vm;
        }

        private void _settingDialog_Closed(object sender, EventArgs e)
        {
            ViewModelStaticContainer.MainWindowViewModel.SettingDialogTransitionMessage = null;
        }
    }
}
