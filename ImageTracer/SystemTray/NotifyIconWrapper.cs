using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using ImageTracer.Views;
using ImageTracer.ViewModels;

namespace ImageTracer.SystemTray
{
    public partial class NotifyIconWrapper : Component
    {
        public static NotifyIconWrapper Instance = null;

        public NotifyIconWrapper()
        {
            InitializeComponent();

            this.showMenuItem.Click += OnShowMenuItemClick;
            this.settingMenuItem.Click += OnSettingMenuItemClick;
            this.quitMenuItem.Click += OnQuitMenuItemClick;

            ShowMainWindow();
            ShowBalloonTip();

            Instance = this;
        }

        public NotifyIconWrapper(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        private void OnShowMenuItemClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void OnSettingMenuItemClick(object sender, EventArgs e)
        {
            ShowSettingDialog();
        }

        private void OnQuitMenuItemClick(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        /// <summary>
        /// メイン画面が表示されていない場合は表示する。
        /// </summary>
        private void ShowMainWindow()
        {
            // メイン画面が表示されていない場合
            if (!_isMainWindowVisible)
            {
                _mainWindow = new MainWindow();
                _mainWindow.ContentRendered += (sd, ev) =>
                {
                    _isMainWindowVisible = true;
                };
                _mainWindow.Closed += (sd, ev) =>
                {
                    _isMainWindowVisible = false;
                };

                // メイン画面を表示する。
                _mainWindow.Show();
            }
        }

        /// <summary>
        /// 設定画面が表示されていない場合は表示する。
        /// </summary>
        public void ShowSettingDialog()
        {
            // メイン画面が表示されていない場合は表示する。
            ShowMainWindow();

            // 設定画面が表示されていない場合
            if (!_isSettingDialogVisible)
            {
                SettingDialog settingDialog = new SettingDialog(ViewModelStaticContainer.MainWindowViewModel);
                settingDialog.Owner = _mainWindow;

                settingDialog.ContentRendered += (sd, ev) =>
                {
                    _isSettingDialogVisible = true;
                };
                settingDialog.Closed += (sd, ev) =>
                {
                    _isSettingDialogVisible = false;
                };

                // 設定画面を表示する。
                settingDialog.Show();
            }
        }

        private void ShowBalloonTip()
        {
            // 初回起動の場合
            if (Properties.Settings.Default.InitialRunning)
            {
                this.notifyIcon.ShowBalloonTip(3000);
                Properties.Settings.Default.InitialRunning = false;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// メイン画面。
        /// 半透明画像を表示するための "枠なし、背景透明" のウィンドウ。
        /// </summary>
        private MainWindow _mainWindow = null;

        private bool _isMainWindowVisible = false;
        private bool _isSettingDialogVisible = false;
    }
}
