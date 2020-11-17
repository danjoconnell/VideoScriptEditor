/*
    Based on code from MVVM Dialogs, located at https://github.com/FantasticFiasco/mvvm-dialogs
    Specifically IDialogService.cs, located at https://github.com/FantasticFiasco/mvvm-dialogs/blob/master/src/net/IDialogService.cs

    MVVM Dialogs is licensed under the Apache License 2.0, Copyright 2009-2020 Mattias Kindborg.
    The full text of the license is available at https://github.com/FantasticFiasco/mvvm-dialogs/blob/master/LICENSE
*/

using System;

namespace VideoScriptEditor.Services.Dialog
{
    /// <summary>
    /// Interface abstracting the interaction between view models and views when it comes to
    /// opening dialogs using the MVVM pattern in WPF.
    /// </summary>
    public interface ISystemDialogService
    {
        /// <summary>
        /// Displays the system open file dialog.
        /// </summary>
        /// <param name="settings">The settings for the open file dialog.</param>
        /// <returns>
        /// If the user clicks the OK button of the dialog that is displayed, true is returned;
        /// otherwise false.
        /// </returns>
        bool? ShowOpenFileDialog(SystemOpenFileDialogSettings settings);

        /// <summary>
        /// Shows the system save file dialog.
        /// </summary>
        /// <param name="settings">The settings for the save file dialog.</param>
        /// <returns>
        /// If the user clicks the OK button of the dialog that is displayed, true is returned;
        /// otherwise false.
        /// </returns>
        bool? ShowSaveFileDialog(SystemSaveFileDialogSettings settings);

        /// <summary>
        /// Displays a message box with specified text.
        /// </summary>
        /// <param name="messageBoxText">
        /// A <see cref="string"/> that specifies the text to display.
        /// </param>
        void ShowMessageBox(string messageBoxText);

        /// <summary>
        /// Displays a confirmation dialog with the specified text and title bar caption.
        /// </summary>
        /// <param name="dialogText">A <see cref="string"/> that specifies the text to display in the dialog.</param>
        /// <param name="dialogCaption">A <see cref="string"/> that specifies the caption to display in the title bar.</param>
        /// <returns>True if the user clicks the Yes button of the dialog that is displayed, False otherwise.</returns>
        bool ShowConfirmationDialog(string dialogText, string dialogCaption = "");

        /// <summary>
        /// Displays an error message dialog with the specified text and title bar caption.
        /// </summary>
        /// <param name="dialogText">A <see cref="string"/> that specifies the text to display in the dialog.</param>
        /// <param name="dialogCaption">A <see cref="string"/> that specifies the caption to display in the title bar. Optional</param>
        /// <param name="exception">The exception for which this dialog is being displayed. Optional.</param>
        void ShowErrorDialog(string dialogText, string dialogCaption = "", Exception exception = null);
    }
}
