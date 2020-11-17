using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using VideoScriptEditor.PrismExtensions;

namespace VideoScriptEditor.ViewModels.Dialogs
{
    /// <summary>
    /// View Model encapsulating presentation logic for the Input Value Prompt dialog.
    /// </summary>
    public class InputValuePromptDialogViewModel : NotifyDataErrorInfoBindableBase, IDialogAware
    {
        private const string DEFAULT_TITLE = "Please enter a value";

        private string _title = DEFAULT_TITLE;
        private string _inputValue;

        /// <inheritdoc cref="IDialogAware.RequestClose"/>
        public event Action<IDialogResult> RequestClose;

        /// <inheritdoc cref="IDialogAware.Title"/>
        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Gets or sets the dialog's input value.
        /// </summary>
        public string InputValue
        {
            get => _inputValue;
            set => SetProperty(ref _inputValue, value, (inputValue) => !string.IsNullOrWhiteSpace(inputValue), "The input value can't be empty");
        }

        /// <summary>
        /// Command for closing the dialog and submitting the <see cref="InputValue"/>.
        /// </summary>
        public DelegateCommand SubmitCommand { get; }

        /// <summary>
        /// Command for canceling and closing the dialog.
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// Creates a new <see cref="InputValuePromptDialogViewModel"/> instance.
        /// </summary>
        public InputValuePromptDialogViewModel()
        {
            SubmitCommand = new DelegateCommand(
                executeMethod: ExecuteSubmitCommand,
                canExecuteMethod: () => !HasErrors
            ).ObservesProperty(() => HasErrors);

            CancelCommand = new DelegateCommand(ExecuteCancelCommand);
        }

        /// <inheritdoc cref="IDialogAware.CanCloseDialog"/>
        public bool CanCloseDialog() => true;

        /// <inheritdoc cref="IDialogAware.OnDialogOpened(IDialogParameters)"/>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            Title = parameters.GetValue<string>(nameof(Title)) ?? DEFAULT_TITLE;
            InputValue = parameters.GetValue<string>(nameof(InputValue));
        }

        /// <inheritdoc cref="IDialogAware.OnDialogClosed"/>
        public void OnDialogClosed()
        {
            SetProperty(ref _inputValue, string.Empty, nameof(InputValue));
            Title = DEFAULT_TITLE;
        }

        /// <summary>
        /// Closes the dialog and submits the <see cref="InputValue"/>.
        /// </summary>
        /// <remarks>
        /// Invoked on execution of the <see cref="SubmitCommand"/>.
        /// </remarks>
        private void ExecuteSubmitCommand()
        {
            DialogResult dialogResult = new DialogResult(ButtonResult.OK, new DialogParameters
            {
                { nameof(InputValue), _inputValue }
            });

            RequestClose?.Invoke(dialogResult);
        }

        /// <summary>
        /// Cancels and closes the dialog.
        /// </summary>
        /// <remarks>
        /// Invoked on execution of the <see cref="CancelCommand"/>.
        /// </remarks>
        private void ExecuteCancelCommand()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }
    }
}
