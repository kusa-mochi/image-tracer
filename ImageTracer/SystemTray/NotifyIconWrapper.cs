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
        }

        public NotifyIconWrapper(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        private void OnShowMenuItemClick(object sender, EventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void OnQuitMenuItemClick(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }
    }
}
