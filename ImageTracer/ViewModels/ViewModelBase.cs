using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Livet;
using Livet.Messaging;

using ImageTracer;

namespace ImageTracer.ViewModels
{
    public class ViewModelBase : ViewModel
    {
        /// <summary>
        /// 情報メッセージダイアログを表示するメソッド。
        /// View側にInteractionMessageTriggerの記述が必要。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        public void ShowInfoMessageBox(string message, string title = "リソース作成予定：情報")
        {
            Messenger.Raise(new InformationMessage(message, title, MessageBoxImage.Information, "ShowInfoMessageBox"));
        }

        /// <summary>
        /// エラーメッセージダイアログを表示するメソッド。
        /// View側にInteractionMessageTriggerの記述が必要。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        public void ShowErrorMessageBox(string message, string title = "リソース作成予定：エラー")
        {
            Messenger.Raise(new InformationMessage(message, title, MessageBoxImage.Error, "ShowErrorMessageBox"));
        }

        /// <summary>
        /// 確認メッセージダイアログを表示するメソッド。
        /// View側にInteractionMessageTriggerの記述が必要。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public bool ShowConfirmationMessageBox(string message, string title = "リソース作成予定：確認")
        {
            ConfirmationMessage confirmationMessage = new ConfirmationMessage(message, title, MessageBoxImage.Question, "ShowConfirmationMessageBox");
            Messenger.Raise(confirmationMessage);
            return confirmationMessage.Response ?? false;
        }
    }
}
