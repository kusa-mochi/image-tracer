﻿using System;
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
        public NotifyIconWrapper()
        {
            InitializeComponent();

            this.showMenuItem.Click += OnShowMenuItemClick;
            this.settingMenuItem.Click += OnSettingMenuItemClick;
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

        private void OnSettingMenuItemClick(object sender, EventArgs e)
        {
            ShowSettingDialog();
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

        private void ShowSettingDialog()
        {
            if (!_isSettingDialogVisible)
            {
                SettingDialog settingDialog = new SettingDialog(ViewModelStaticContainer.MainWindowViewModel);
                settingDialog.ContentRendered += (sd, ev) =>
                {
                    _isSettingDialogVisible = true;
                };
                settingDialog.Closed += (sd, ev) =>
                {
                    _isSettingDialogVisible = false;
                };
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

        private bool _isMainWindowVisible = false;
        private bool _isSettingDialogVisible = false;
    }
}
