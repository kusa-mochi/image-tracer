using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using ImageTracer.Views;

namespace ImageTracer.SystemTray
{
    public partial class NotifyIconWrapper : Component
    {
        public NotifyIconWrapper()
        {
            InitializeComponent();

            this.showMenuItem.Click += OnShowMenuItemClick;
            this.quitMenuItem.Click += OnQuitMenuItemClick;

            ShowMainWindow();
            ShowBalloonTip();
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

        private void OnQuitMenuItemClick(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void ShowMainWindow()
        {
            if (!_isMainWindowVisible)
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.ContentRendered += (sd, ev) =>
                {
                    _isMainWindowVisible = true;
                };
                mainWindow.Closed += (sd, ev) =>
                {
                    _isMainWindowVisible = false;
                };
                mainWindow.Show();
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

        private bool _isMainWindowVisible = false;
    }
}
