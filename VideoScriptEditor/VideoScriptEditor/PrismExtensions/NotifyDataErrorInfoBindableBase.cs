using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoScriptEditor.PrismExtensions
{
    /// <summary>
    /// Implementation of <see cref="INotifyDataErrorInfo"/> and <see cref="INotifyPropertyChanged"/> to simplify models.
    /// </summary>
    public abstract class NotifyDataErrorInfoBindableBase : BindableBase, INotifyDataErrorInfo
    {
        /// <inheritdoc cref="ErrorsContainer{T}"/>
        protected ErrorsContainer<string> _errorsContainer;

        /// <inheritdoc cref="INotifyDataErrorInfo.ErrorsChanged"/>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <inheritdoc cref="INotifyDataErrorInfo.HasErrors"/>
        public bool HasErrors => _errorsContainer.HasErrors;

        /// <inheritdoc cref="INotifyDataErrorInfo.GetErrors(string)"/>
        public IEnumerable GetErrors(string propertyName) => _errorsContainer.GetErrors(propertyName);

        /// <summary>
        /// Base constructor for models derived from the <see cref="NotifyDataErrorInfoBindableBase"/> class.
        /// </summary>
        protected NotifyDataErrorInfoBindableBase() : base()
        {
            _errorsContainer = new ErrorsContainer<string>(propertyName => RaiseErrorsChanged(propertyName));
        }

        /// <summary>
        /// Raises this object's <see cref="ErrorsChanged"/> event.
        /// </summary>
        /// <inheritdoc cref="DataErrorsChangedEventArgs(string)"/>
        protected void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            RaisePropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Checks if a property already matches a desired value. Sets the property,
        /// validates its value via a delegate and error message for an invalid value
        /// and notifies event listeners only when necessary.
        /// </summary>
        /// <param name="validationDelegate">A delegate for performing validation of the property value.</param>
        /// <param name="validationErrorMessage">
        /// A <see cref="string"/> containing the error message to set for an invalid property value.
        /// </param>
        /// <inheritdoc cref="BindableBase.SetProperty{T}(ref T, T, string)"/>
        protected virtual bool SetProperty<T>(ref T storage, T value, Func<T, bool> validationDelegate, string validationErrorMessage, [CallerMemberName] string propertyName = null)
        {
            if (validationDelegate == null)
            {
                throw new ArgumentNullException(nameof(validationDelegate));
            }

            if (!base.SetProperty(ref storage, value, propertyName))
            {
                // No change to property value as it was equal to the desired value.
                // Therefore no need to (re)validate.
                return false;
            }

            bool isValid = validationDelegate(value);
            string[] propertyErrors = !isValid ? new[] { validationErrorMessage } : Array.Empty<string>();
            _errorsContainer.SetErrors(propertyName, propertyErrors);

            // Property set and validated.
            return true;
        }

        /// <summary>
        /// Checks if a property already matches a desired value. Sets the property,
        /// validates its value via a delegate that provides a collection of validation errors,
        /// and notifies event listeners only when necessary.
        /// </summary>
        /// <param name="validationErrorsDelegate">
        /// A delegate for validating the property value and providing a collection of validation errors.
        /// </param>
        /// <inheritdoc cref="BindableBase.SetProperty{T}(ref T, T, string)"/>
        protected virtual bool SetProperty<T>(ref T storage, T value, Func<T, IEnumerable<string>> validationErrorsDelegate, [CallerMemberName] string propertyName = null)
        {
            if (validationErrorsDelegate == null)
            {
                throw new ArgumentNullException(nameof(validationErrorsDelegate));
            }

            if (!base.SetProperty(ref storage, value, propertyName))
            {
                // No change to property value as it was equal to the desired value.
                // Therefore no need to (re)validate.
                return false;
            }

            _errorsContainer.SetErrors(propertyName, validationErrorsDelegate(value));

            // Property set and validated.
            return true;
        }
    }
}
