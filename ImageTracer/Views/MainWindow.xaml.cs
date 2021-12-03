using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using ImageTracer.Common;
using ImageTracer.ViewModels;

namespace ImageTracer.Views
{
    /* 
	 * ViewModelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedWeakEventListenerや
     * CollectionChangedWeakEventListenerを使うと便利です。独自イベントの場合はLivetWeakEventListenerが使用できます。
     * クローズ時などに、LivetCompositeDisposableに格納した各種イベントリスナをDisposeする事でイベントハンドラの開放が容易に行えます。
     *
     * WeakEventListenerなので明示的に開放せずともメモリリークは起こしませんが、できる限り明示的に開放するようにしましょう。
     */

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private double _fixRate;
        private double _horizontalMargin;
        private double _verticalMargin;
        private MainWindowViewModel _vm = null;

        public MainWindow()
        {
            InitializeComponent();

            _vm = ViewModelManager.MainWindowViewModel;
            _vm.ThroughHitChanged += OnThroughHitChanged;
            _vm.FixRateCommand.ExecuteHandler = FixRateCommandExecute;
            _vm.FixRateCommand.CanExecuteHandler = CanFixRateCommandExecute;
            this.DataContext = _vm;

            // キーボードのコールバックメソッドをフックする。
            if (_keyboardHookId == IntPtr.Zero)
            {
                using (Process currentProcess = Process.GetCurrentProcess())
                using (ProcessModule currentModule = currentProcess.MainModule)
                {
                    _keyboardHookId = NativeMethods.SetWindowsHookEx(
                        (int)NativeMethods.HookType.WH_KEYBOARD_LL,
                        _keyboardProc,
                        NativeMethods.GetModuleHandle(currentModule.ModuleName),
                        0
                        );
                }
            }
        }

        ~MainWindow()
        {
            // キーボードのコールバックメソッドをアンフックする。
            NativeMethods.UnhookWindowsHookEx(_keyboardHookId);
        }

        private void OnThroughHitChanged(object sender, ThroughHitChangedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd, e.NewValue);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //マウスボタン押下状態でなければ何もしない
            if (e.ButtonState != MouseButtonState.Pressed) return;

            this.DragMove();
        }

        private void FileOpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter =
                "画像ファイル|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff"
                    + "|BMPファイル(*.bmp)|*.bmp"
                    + "|GIFファイル(*.gif)|*.gif"
                    + "|JPEGファイル(*.jpg;*.jpeg)|*.jpg;*.jpeg"
                    + "|PNGファイル(*.png)|*.png"
                    + "|TIFFファイル(*.tif;*.tiff)|*.tif;*.tiff";
            Nullable<bool> result = dialog.ShowDialog();
            if (result == true)
            {
                _vm.ImageLoaded = true;
                BitmapImage bm = FileToBitmapImage(dialog.FileName);
                _vm.CurrentImage = bm;
                //_image.Source = bm;
                this.Width = bm.Width;
                this.Height = bm.Height;
                this.GetFixRate();
            }
        }

        public static BitmapImage FileToBitmapImage(string filePath)
        {
            BitmapImage bi = null;

            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                {
                    bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = fs;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                }
            }
            catch
            {
                bi = null;
            }

            return bi;
        }

        private void window_SourceInitialized(object sender, EventArgs e)
        {
            var hwndSource = (HwndSource)HwndSource.FromVisual(this);
            hwndSource.AddHook(WndHookProc);
        }

        private const int WM_SIZING = 0x214;
        private const int WMSZ_LEFT = 1;
        private const int WMSZ_RIGHT = 2;
        private const int WMSZ_TOP = 3;
        private const int WMSZ_TOPLEFT = 4;
        private const int WMSZ_TOPRIGHT = 5;
        private const int WMSZ_BOTTOM = 6;
        private const int WMSZ_BOTTOMLEFT = 7;
        private const int WMSZ_BOTTOMRIGHT = 8;

        private IntPtr WndHookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SIZING)
            {
                if (!_vm.HoldAspectRatio) return IntPtr.Zero;

                // MainWindowの範囲を表す四角形
                var rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));

                var w = rect.right - rect.left - this._horizontalMargin;
                var h = rect.bottom - rect.top - this._verticalMargin;

                switch (wParam.ToInt32())
                {
                    case WMSZ_LEFT:
                    case WMSZ_RIGHT:
                        rect.bottom = (int)(rect.top + w / this._fixRate + this._verticalMargin);
                        break;
                    case WMSZ_TOP:
                    case WMSZ_BOTTOM:
                        rect.right = (int)(rect.left + h * this._fixRate + this._horizontalMargin);
                        break;
                    case WMSZ_TOPLEFT:
                        if (w / h > this._fixRate)
                        {
                            rect.top = (int)(rect.bottom - w / this._fixRate - this._verticalMargin);
                        }
                        else
                        {
                            rect.left = (int)(rect.right - h * this._fixRate - this._horizontalMargin);
                        }
                        break;
                    case WMSZ_TOPRIGHT:
                        if (w / h > this._fixRate)
                        {
                            rect.top = (int)(rect.bottom - w / this._fixRate - this._verticalMargin);
                        }
                        else
                        {
                            rect.right = (int)(rect.left + h * this._fixRate + this._horizontalMargin);
                        }
                        break;
                    case WMSZ_BOTTOMLEFT:
                        if (w / h > this._fixRate)
                        {
                            rect.bottom = (int)(rect.top + w / this._fixRate + this._verticalMargin);
                        }
                        else
                        {
                            rect.left = (int)(rect.right - h * this._fixRate - this._horizontalMargin);
                        }
                        break;
                    case WMSZ_BOTTOMRIGHT:
                        if (w / h > this._fixRate)
                        {
                            rect.bottom = (int)(rect.top + w / this._fixRate + this._verticalMargin);
                        }
                        else
                        {
                            rect.right = (int)(rect.left + h * this._fixRate + this._horizontalMargin);
                        }
                        break;
                    default:
                        break;
                }
                Marshal.StructureToPtr(rect, lParam, true);
            }
            return IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            this.GetFixRate();
        }

        private void GetFixRate()
        {
            this.SizeToContent = SizeToContent.Manual;

            this._horizontalMargin = this.ActualWidth - this.Width;
            this._verticalMargin = this.ActualHeight - this.Height;
            this._fixRate = this.Width / this.Height;

            this.Width = double.NaN;
            this.Height = double.NaN;
        }

        public DelegateCommand FixRateCommand
        {
            get
            {
                return _vm.FixRateCommand;
            }
            set
            {
                if (_vm == null) return;
                _vm.FixRateCommand = value;
            }
        }

        private bool CanFixRateCommandExecute(object param)
        {
            return param != null;
        }

        private void FixRateCommandExecute(object param)
        {
            this.GetFixRate();
        }

        private bool IsValidDataExtension(string ext)
        {
            switch (ext)
            {
                case ".bmp":
                    break;
                case ".BMP":
                    break;
                case ".gif":
                    break;
                case ".GIF":
                    break;
                case ".jpg":
                    break;
                case ".JPG":
                    break;
                case ".jpeg":
                    break;
                case ".JPEG":
                    break;
                case ".png":
                    break;
                case ".PNG":
                    break;
                case ".tif":
                    break;
                case ".TIF":
                    break;
                case ".tiff":
                    break;
                case ".TIFF":
                    break;
                default:
                    return false;
            }

            return true;
        }

        private void window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    this.Left--;
                    break;
                case Key.Up:
                    this.Top--;
                    break;
                case Key.Right:
                    this.Left++;
                    break;
                case Key.Down:
                    this.Top++;
                    break;
                default:
                    // do nothing.
                    break;
            }
        }

        private void _image_DragOver(object sender, DragEventArgs e)
        {
            // ファイルをドロップされた場合のみ e.Handled を True にする
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);

            if (e.Handled)
            {
                // ドラッグしている最初のファイルのファイル名を得る。
                string filename = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

                // ファイルの拡張子
                string ext = System.IO.Path.GetExtension(filename);

                // 表示できるデータの拡張子のときのみ、マウスポインタのアイコンを＋記号に変化させる。
                e.Effects = IsValidDataExtension(ext) ? DragDropEffects.Copy : DragDropEffects.None;
            }
        }

        private void _image_Drop(object sender, DragEventArgs e)
        {
            // ファイルをドロップされた場合のみ e.Handled を True にする
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);

            if (e.Handled)
            {
                // ドラッグしている最初のファイルのファイル名を得る。
                string filename = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

                // ファイルの拡張子
                string ext = System.IO.Path.GetExtension(filename);

                if (!IsValidDataExtension(ext)) return;

                _vm.ImageLoaded = true;
                BitmapImage bm = FileToBitmapImage(filename);
                _vm.CurrentImage = bm;
                this.Width = bm.Width;
                this.Height = bm.Height;
                this.GetFixRate();
            }
        }

        #region アプリ内外でグローバルに有効なイベントハンドラ

        private static IntPtr GlobalKeyboardInputCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                // キーボードのイベントに紐付けられた次のメソッドを実行する。メソッドがなければ処理終了。
                return NativeMethods.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
            }

            // キーコードを取得する。
            KBDLLHOOKSTRUCT param = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            // キーボードの「押し上げ」が検出された場合
            if ((NativeMethods.KeyboardMessage)wParam == NativeMethods.KeyboardMessage.WM_KEYUP)
            {
                // キーコードを抽出する。
                int keyCode = param.vkCode;
                Key key = KeyInterop.KeyFromVirtualKey(keyCode);

                // VMのコマンドを実行する。
                ViewModelManager.MainWindowViewModel.KeyInputCommand.Execute(key);
            }

            // キーボードのイベントに紐付けられた次のメソッドを実行する。メソッドがなければ処理終了。
            return NativeMethods.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        private static IntPtr _keyboardHookId = IntPtr.Zero;
        private static readonly NativeMethods.LowLevelKeyboardProc _keyboardProc = GlobalKeyboardInputCallback;

        #endregion
    }
}
