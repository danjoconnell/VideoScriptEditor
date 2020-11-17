/*
    Based on code from MVVM Dialogs, located at https://github.com/FantasticFiasco/mvvm-dialogs
    Specifically IDialogService.cs, located at https://github.com/FantasticFiasco/mvvm-dialogs/blob/master/src/net/IDialogService.cs

    MVVM Dialogs is licensed under the Apache License 2.0, Copyright 2009-2020 Mattias Kindborg.
    The full text of the license is available at https://github.com/FantasticFiasco/mvvm-dialogs/blob/master/LICENSE
*/

using Microsoft.Win32;
using System;
using System.Text;
using System.Windows;

namespace VideoScriptEditor.Services.Dialog
{
    /// <summary>
    /// Class abstracting the interaction between view models and views when it comes to
    /// opening dialogs using the MVVM pattern in WPF.
    /// </summary>
    public class SystemDialogService : ISystemDialogService
    {
        /// <inheritdoc/>
        public bool? ShowOpenFileDialog(SystemOpenFileDialogSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = settings.AddExtension,
                CheckFileExists = settings.CheckFileExists,
                CheckPathExists = settings.CheckPathExists,
                DefaultExt = settings.DefaultExt,
                FileName = settings.FileName,
                Filter = settings.Filter,
                InitialDirectory = settings.InitialDirectory,
                Multiselect = settings.Multiselect,
                Title = settings.Title
            };

            bool? result = openFileDialog.ShowDialog();

            // Update settings
            settings.FileName = openFileDialog.FileName;
            settings.FileNames = openFileDialog.FileNames;

            return result;
        }

        /// <inheritdoc/>
        public bool? ShowSaveFileDialog(SystemSaveFileDialogSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                AddExtension = settings.AddExtension,
                CheckFileExists = settings.CheckFileExists,
                CheckPathExists = settings.CheckPathExists,
                CreatePrompt = settings.CreatePrompt,
                DefaultExt = settings.DefaultExt,
                FileName = settings.FileName,
                Filter = settings.Filter,
                InitialDirectory = settings.InitialDirectory,
                OverwritePrompt = settings.OverwritePrompt,
                Title = settings.Title
            };

            bool? result = saveFileDialog.ShowDialog();

            // Update settings
            settings.FileName = saveFileDialog.FileName;
            settings.FileNames = saveFileDialog.FileNames;

            return result;
        }

        /// <inheritdoc/>
        public void ShowMessageBox(string messageBoxText)
        {
            MessageBox.Show(messageBoxText);
        }

        /// <inheritdoc/>
        public bool ShowConfirmationDialog(string dialogText, string dialogCaption = "")
        {
            return MessageBox.Show(dialogText, dialogCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        /// <inheritdoc/>
        public void ShowErrorDialog(string dialogText, string dialogCaption = "", Exception exception = null)
        {
            if (exception != null)
            {
                StringBuilder sb = new StringBuilder(dialogText).AppendLine()
                                                                .AppendFormat(" ---> {0}", exception);
                dialogText = sb.ToString();
            }

            MessageBox.Show(dialogText, dialogCaption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
