﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using ImageTracer.Common;
using ImageTracer.SystemTray;
using ImageTracer.Views;

namespace ImageTracer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        /* コマンド、プロパティの定義にはそれぞれ 
         * 
         *  lvcom   : ViewModelCommand
         *  lvcomn  : ViewModelCommand(CanExecute無)
         *  llcom   : ListenerCommand(パラメータ有のコマンド)
         *  llcomn  : ListenerCommand(パラメータ有のコマンド・CanExecute無)
         *  lprop   : 変更通知プロパティ(.NET4.5ではlpropn)
         *  
         * を使用してください。
         * 
         * Modelが十分にリッチであるならコマンドにこだわる必要はありません。
         * View側のコードビハインドを使用しないMVVMパターンの実装を行う場合でも、ViewModelにメソッドを定義し、
         * LivetCallMethodActionなどから直接メソッドを呼び出してください。
         * 
         * ViewModelのコマンドを呼び出せるLivetのすべてのビヘイビア・トリガー・アクションは
         * 同様に直接ViewModelのメソッドを呼び出し可能です。
         */

        /* ViewModelからViewを操作したい場合は、View側のコードビハインド無で処理を行いたい場合は
         * Messengerプロパティからメッセージ(各種InteractionMessage)を発信する事を検討してください。
         */

        /* Modelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedEventListenerや
         * CollectionChangedEventListenerを使うと便利です。各種ListenerはViewModelに定義されている
         * CompositeDisposableプロパティ(LivetCompositeDisposable型)に格納しておく事でイベント解放を容易に行えます。
         * 
         * ReactiveExtensionsなどを併用する場合は、ReactiveExtensionsのCompositeDisposableを
         * ViewModelのCompositeDisposableプロパティに格納しておくのを推奨します。
         * 
         * LivetのWindowテンプレートではViewのウィンドウが閉じる際にDataContextDisposeActionが動作するようになっており、
         * ViewModelのDisposeが呼ばれCompositeDisposableプロパティに格納されたすべてのIDisposable型のインスタンスが解放されます。
         * 
         * ViewModelを使いまわしたい時などは、ViewからDataContextDisposeActionを取り除くか、発動のタイミングをずらす事で対応可能です。
         */

        /* UIDispatcherを操作する場合は、DispatcherHelperのメソッドを操作してください。
         * UIDispatcher自体はApp.xaml.csでインスタンスを確保してあります。
         * 
         * LivetのViewModelではプロパティ変更通知(RaisePropertyChanged)やDispatcherCollectionを使ったコレクション変更通知は
         * 自動的にUIDispatcher上での通知に変換されます。変更通知に際してUIDispatcherを操作する必要はありません。
         */

        public void Initialize()
        {
            _keySettingDialogViewModel.KeyInput += OnImageDisplayShortcutKeySet;

            Key tmpKey = Key.None;

            // 設定ファイルからショートカットキーの設定値を読み込む。
            if (!Enum.TryParse<Key>(Properties.Settings.Default.ImageDisplayShortcutKey, out tmpKey))
            {
                // 設定値が不正な場合
                // 設定値が不正である旨をユーザに通知する。
                ShowErrorMessageBox("リソース作成予定：設定ファイルの値が不正です。");

                // プロパティと設定ファイルの両方に既定値を設定する。
                ImageDisplayShortcutKey = _defaultShortcutKeyToDisplayImage.ToString();
                Properties.Settings.Default.ImageDisplayShortcutKey = _defaultShortcutKeyToDisplayImage.ToString();
                Properties.Settings.Default.Save();
            }
            else
            {
                // 設定値をプロパティに反映させる。
                ImageDisplayShortcutKey = tmpKey.ToString();
            }
        }

        #region イベント

        public event EventHandler<ThroughHitChangedEventArgs> ThroughHitChanged;

        #endregion

        #region CurrentImage変更通知プロパティ
        private BitmapImage _CurrentImage = null;

        public BitmapImage CurrentImage
        {
            get
            { return _CurrentImage; }
            set
            {
                if (_CurrentImage == value)
                    return;
                _CurrentImage = value;
                if (HoldAspectRatio)
                {
                    _holdRatio = _CurrentImage.Height / _CurrentImage.Width;
                }

                _sizeInitializing = true;
                Width = (int)_CurrentImage.Width;
                Height = (int)_CurrentImage.Height;
                _sizeInitializing = false;

                RaisePropertyChanged();
            }
        }
        #endregion

        #region ImageTopmost変更通知プロパティ
        private bool _ImageTopmost = true;

        public bool ImageTopmost
        {
            get
            { return _ImageTopmost; }
            set
            {
                if (_ImageTopmost == value)
                    return;
                _ImageTopmost = value;
                if (!value)
                {
                    ThroughHit = false;
                }
                RaisePropertyChanged();
            }
        }
        #endregion

        #region HoldAspectRatio変更通知プロパティ
        private bool _HoldAspectRatio = true;

        public bool HoldAspectRatio
        {
            get
            { return _HoldAspectRatio; }
            set
            {
                if (_HoldAspectRatio == value)
                    return;
                _HoldAspectRatio = value;
                RaisePropertyChanged();
                if (value)
                {
                    FixRateCommand.Execute(null);
                    _holdRatio = (double)_Height / _Width;
                }
            }
        }
        #endregion

        #region IsEnableShortcutKey変更通知プロパティ

        private bool _IsEnableShortcutKey = false;

        public bool IsEnableShortcutKey
        {
            get
            { return _IsEnableShortcutKey; }
            set
            {
                if (_IsEnableShortcutKey == value)
                    return;
                _IsEnableShortcutKey = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region ImageDisplayShortcutKey変更通知プロパティ

        /// <summary>
        /// 画像表示切替ショートカットキーの指定。
        /// </summary>
        private string _ImageDisplayShortcutKey = _defaultShortcutKeyToDisplayImage.ToString();

        public string ImageDisplayShortcutKey
        {
            get
            { return _ImageDisplayShortcutKey; }
            set
            {
                if (_ImageDisplayShortcutKey == value)
                    return;
                _ImageDisplayShortcutKey = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Height変更通知プロパティ
        private int _Height = 480;

        public int Height
        {
            get
            { return _Height; }
            set
            {
                if (_Height == value)
                    return;

                if (_HoldAspectRatio && !_sizeInitializing)
                {
                    _Width = (int)((double)value / _holdRatio);
                    RaisePropertyChanged("Width");
                }

                _Height = value;

                RaisePropertyChanged("Height");
            }
        }
        #endregion

        #region Width変更通知プロパティ
        private int _Width = 640;

        public int Width
        {
            get
            { return _Width; }
            set
            {
                if (_Width == value)
                    return;

                if (_HoldAspectRatio && !_sizeInitializing)
                {
                    _Height = (int)((double)value * _holdRatio);
                    RaisePropertyChanged("Height");
                }

                _Width = value;

                RaisePropertyChanged("Width");
            }
        }
        #endregion

        #region Alpha変更通知プロパティ
        private double _Alpha = 0.5;

        public double Alpha
        {
            get
            { return _Alpha; }
            set
            {
                if ((_Alpha == value) || (value < 0.002))
                    return;
                _Alpha = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region ImageLoaded変更通知プロパティ
        private bool _ImageLoaded = false;

        public bool ImageLoaded
        {
            get
            { return _ImageLoaded; }
            set
            {
                if (_ImageLoaded == value)
                    return;
                _ImageLoaded = value;
                RaisePropertyChanged();
                ImageUnloaded = !value;
            }
        }
        #endregion

        #region ImageUnloaded変更通知プロパティ
        private bool _ImageUnloaded = true;

        public bool ImageUnloaded
        {
            get
            { return _ImageUnloaded; }
            set
            {
                if (_ImageUnloaded == value)
                    return;
                _ImageUnloaded = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region ThroughHit変更通知プロパティ

        private bool _ThroughHit = false;

        public bool ThroughHit
        {
            get
            {
                return _ThroughHit;
            }

            set
            {
                if (_ThroughHit == value) return;
                _ThroughHit = value;
                if (ThroughHitChanged != null)
                {
                    ThroughHitChanged.Invoke(this, new ThroughHitChangedEventArgs { NewValue = value });
                }
                RaisePropertyChanged();
            }
        }

        #endregion

        #region IsImageVisible変更通知プロパティ

        private bool _IsImageVisible = true;

        public bool IsImageVisible
        {
            get
            { return _IsImageVisible; }
            set
            {
                if (_IsImageVisible == value)
                    return;
                _IsImageVisible = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region ShowSettingDialogCommand
        private ViewModelCommand _ShowSettingDialogCommand;

        public ViewModelCommand ShowSettingDialogCommand
        {
            get
            {
                if (_ShowSettingDialogCommand == null)
                {
                    _ShowSettingDialogCommand = new ViewModelCommand(ShowSettingDialog);
                }
                return _ShowSettingDialogCommand;
            }
        }

        private TransitionMessage _settingDialogTransitionMessage = null;
        public TransitionMessage SettingDialogTransitionMessage
        {
            get { return _settingDialogTransitionMessage; }
            set { _settingDialogTransitionMessage = value; }
        }

        public void ShowSettingDialog()
        {
            // 既に設定画面が開いている場合は何もしない。
            if (_settingDialogTransitionMessage != null) return;

            // 設定画面を表示する。
            ViewManager.RequestShowSettingDialog();
        }
        #endregion

        #region ShowKeySettingCommand

        private ViewModelCommand _ShowKeySettingDialogCommand;

        /// <summary>
        /// 画像表示切替ショートカットキー設定画面を開くコマンド
        /// </summary>
        public ViewModelCommand ShowKeySettingDialogCommand
        {
            get
            {
                if (_ShowKeySettingDialogCommand == null)
                {
                    _ShowKeySettingDialogCommand = new ViewModelCommand(ShowKeySettingDialog);
                }
                return _ShowKeySettingDialogCommand;
            }
        }

        private TransitionMessage _keySettingDialogTransitionMessage = null;
        public TransitionMessage KeySettingDialogTransitionMessage
        {
            get { return _keySettingDialogTransitionMessage; }
            set { _keySettingDialogTransitionMessage = value; }
        }

        public void ShowKeySettingDialog()
        {
            // 既にダイアログが開いている場合は何もしない。
            if (_keySettingDialogTransitionMessage != null) return;

            _keySettingDialogTransitionMessage = new TransitionMessage(_keySettingDialogViewModel, "ShowKeySettingDialogCommand");
            Messenger.Raise(_keySettingDialogTransitionMessage);
        }

        #endregion

        #region CloseCommand
        private ViewModelCommand _CloseCommand;

        public ViewModelCommand CloseCommand
        {
            get
            {
                if (_CloseCommand == null)
                {
                    _CloseCommand = new ViewModelCommand(Close);
                }
                return _CloseCommand;
            }
        }

        public void Close()
        {
            Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
        }
        #endregion

        #region FixRateCommand
        public DelegateCommand FixRateCommand { get; set; } = new DelegateCommand();
        #endregion

        #region KeyInputCommand

        private ListenerCommand<Key> _KeyInputCommand;

        public ListenerCommand<Key> KeyInputCommand
        {
            get
            {
                if (_KeyInputCommand == null)
                {
                    _KeyInputCommand = new ListenerCommand<Key>(KeyInput);
                }
                return _KeyInputCommand;
            }
        }

        public void KeyInput(Key parameter)
        {
            string parameterString = parameter.ToString();

            // 入力されたキーが画像表示切替ショートカットキーに一致した場合
            if (parameterString == ImageDisplayShortcutKey)
            {
                // 画像表示切替ショートカットキーが有効な場合
                if (IsEnableShortcutKey)
                {
                    // 画像の表示／非表示を切り替える。
                    IsImageVisible = !IsImageVisible;
                }
            }
        }

        #endregion

        #region イベントコールバックメソッド

        private void OnImageDisplayShortcutKeySet(object sender, EventArgs e)
        {
            // ユーザーが押したキー
            string pressedKey = _keySettingDialogViewModel.Key.ToString();

            // プロパティにキーを反映させる。
            ImageDisplayShortcutKey = pressedKey;

            // 設定ファイルにキーを反映させる。
            Properties.Settings.Default.ImageDisplayShortcutKey = pressedKey;
            Properties.Settings.Default.Save();

            _keySettingDialogTransitionMessage = null;
        }

        #endregion

        /// <summary>
        /// Height / Width 比
        /// </summary>
        private double _holdRatio = 1.0;

        /// <summary>
        /// HoldAspectRatioプロパティがtrueのときに，
        /// WidthまたはHeightを個別に変更する必要がある場合にtrueにする。
        /// </summary>
        private bool _sizeInitializing = false;

        /// <summary>
        /// ショートカットキー設定ダイアログのVM
        /// </summary>
        private KeySettingDialogViewModel _keySettingDialogViewModel = new KeySettingDialogViewModel();

        /// <summary>
        /// 画像表示切替ショートカットキーの既定値
        /// </summary>
        private const Key _defaultShortcutKeyToDisplayImage = Key.RightCtrl;
    }
}
