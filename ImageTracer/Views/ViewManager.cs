using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTracer.Views
{
    public static class ViewManager
    {
        /// <summary>
        /// メイン画面が表示されていない場合は表示する。
        /// </summary>
        public static void RequestShowMainWindow()
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
        public static void RequestShowSettingDialog()
        {
            // メイン画面が表示されていない場合は表示する。
            RequestShowMainWindow();

            // 設定画面が表示されていない場合
            if (!_isSettingDialogVisible)
            {
                SettingDialog settingDialog = new SettingDialog();
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

        /// <summary>
        /// メイン画面。
        /// 半透明画像を表示するための "枠なし、背景透明" のウィンドウ。
        /// </summary>
        private static MainWindow _mainWindow = null;

        private static bool _isMainWindowVisible = false;
        private static bool _isSettingDialogVisible = false;
    }
}
