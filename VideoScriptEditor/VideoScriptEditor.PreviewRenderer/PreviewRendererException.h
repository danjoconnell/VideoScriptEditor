#pragma once

namespace VideoScriptEditor::Services::ScriptVideo
{
    /// <summary>
    /// The exception that is thrown when an error occurs in the Preview Renderer.
    /// </summary>
    [System::Serializable]
    public ref class PreviewRendererException : public System::Exception
    {
    public:
        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewRendererException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        PreviewRendererException(System::String^ message) : System::Exception(message)
        {
            HResult = E_FAIL;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewRendererException"/> class with a specified message and error code.
        /// </summary>
        /// <param name="message">The message that indicates the reason the exception occurred.</param>
        /// <param name="hResult">The error code (HRESULT) value associated with this exception.</param>
        PreviewRendererException(System::String^ message, HRESULT hResult) : System::Exception(message)
        {
            HResult = hResult;
        }

    internal:
        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewRendererException"/> class with a specified message and error code
        /// for an unmanaged <see cref="_com_error"/> exception.
        /// </summary>
        /// <param name="message">The message that indicates the reason the exception occurred.</param>
        /// <param name="unmanagedComError">A reference to an unmanaged <see cref="_com_error"/> exception.</param>
        PreviewRendererException(System::String^ message, const _com_error& unmanagedComError)
            : PreviewRendererException(GetExceptionMessageForComError(message, unmanagedComError), unmanagedComError.Error())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewRendererException"/> class with a specified message and error code
        /// for an unmanaged <see cref="std::exception"/> exception.
        /// </summary>
        /// <param name="message">The message that indicates the reason the exception occurred.</param>
        /// <param name="stdException">A reference to the unmanaged <see cref="std::exception"/> exception.</param>
        PreviewRendererException(System::String^ message, const std::exception& stdException)
            : PreviewRendererException(GetExceptionMessageForStdException(message, stdException))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewRendererException"/> class
        /// for an unmanaged <see cref="_com_error"/> exception.
        /// </summary>
        /// <param name="unmanagedComError">A reference to the unmanaged <see cref="_com_error"/> exception.</param>
        PreviewRendererException(const _com_error& unmanagedComError)
            : PreviewRendererException(nullptr, unmanagedComError)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewRendererException"/> class
        /// for an unmanaged <see cref="std::exception"/> exception.
        /// </summary>
        /// <param name="stdException">A reference to the unmanaged <see cref="std::exception"/> exception.</param>
        PreviewRendererException(const std::exception& stdException)
            : PreviewRendererException(nullptr, stdException)
        {
        }

    protected:
        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewRendererException"/> class from serialization data.
        /// </summary>
        /// <param name="info">The <see cref="System::Runtime::Serialization::SerializationInfo"/> object that holds the serialized object data.</param>
        /// <param name="context">The <see cref="System::Runtime::Serialization::StreamingContext"/> object that supplies the contextual information about the source or destination.</param>
        PreviewRendererException(System::Runtime::Serialization::SerializationInfo^ info, System::Runtime::Serialization::StreamingContext context)
            : System::Exception(info, context)
        {
        }

    private:
        /// <summary>
        /// Gets an exception message <see cref="System::String"/> for constructing a <see cref="PreviewRendererException"/>
        /// from an unmanaged <see cref="_com_error"/> exception.
        /// </summary>
        /// <param name="message">The message that indicates the reason the exception occurred.</param>
        /// <param name="unmanagedComError">A reference to the unmanaged <see cref="_com_error"/> exception.</param>
        /// <returns>A <see cref="System::String"/> containing the exception message.</returns>
        System::String^ GetExceptionMessageForComError(System::String^ message, const _com_error& unmanagedComError)
        {
            using namespace System;
            using namespace System::Text;

            String^ comErrorMessage = gcnew String(unmanagedComError.ErrorMessage());

            if (!String::IsNullOrEmpty(message))
            {
                StringBuilder^ sb = gcnew StringBuilder(message);
                sb->Append(": ");
                sb->Append(comErrorMessage);
                return sb->ToString();
            }
            else
            {
                return comErrorMessage;
            }
        }

        /// <summary>
        /// Gets an exception message <see cref="System::String"/> for constructing a <see cref="PreviewRendererException"/>
        /// from an unmanaged <see cref="std::exception"/> exception.
        /// </summary>
        /// <param name="message">The message that indicates the reason the exception occurred.</param>
        /// <param name="stdException">A reference to the unmanaged <see cref="std::exception"/> exception.</param>
        /// <returns>A <see cref="System::String"/> containing the exception message.</returns>
        System::String^ GetExceptionMessageForStdException(System::String^ message, const std::exception& stdException)
        {
            using namespace System;
            using namespace System::Text;

            String^ stdExceptionMessage = gcnew String(stdException.what());

            if (!String::IsNullOrEmpty(message))
            {
                StringBuilder^ sb = gcnew StringBuilder(message);
                sb->Append(": ");
                sb->Append(stdExceptionMessage);
                return sb->ToString();
            }
            else
            {
                return stdExceptionMessage;
            }
        }
    };
}