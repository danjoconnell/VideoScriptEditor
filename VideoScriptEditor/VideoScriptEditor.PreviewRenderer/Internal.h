#pragma once

namespace VideoScriptEditor::PreviewRenderer
{
    /// <summary>
    /// Copies the contents of a managed <see cref="System::String"/> to an unmanaged <see cref="std::string"/>.
    /// </summary>
    /// <remarks>Based on sample code from https://docs.microsoft.com/en-us/cpp/dotnet/how-to-convert-system-string-to-standard-string?view=vs-2019</remarks>
    /// <param name="s">(IN) The managed <see cref="System::String"/> to be copied.</param>
    /// <param name="os">(OUT) A reference to the unmanaged <see cref="std::string"/> to copy converted ANSI formatted string characters to.</param>
    inline void MarshalString(System::String^ s, std::string& os)
    {
        using namespace System;
        using namespace System::Runtime::InteropServices;

        IntPtr chars = IntPtr::Zero;
        try
        {
            chars = Marshal::StringToHGlobalAnsi(s);
            os = static_cast<const char*>(chars.ToPointer());
        }
        finally
        {
            if (chars != IntPtr::Zero)
            {
                Marshal::FreeHGlobal(chars);
            }
        }
    }

    /// <summary>
    /// Converts a managed <see cref="Models::VideoResizeMode"/> enum value to an unmanaged <see cref="Unmanaged::VideoSizeMode"/> enum value.
    /// </summary>
    /// <param name="videoResizeMode">The managed <see cref="Models::VideoResizeMode"/> enum value to convert.</param>
    /// <returns>The resulting unmanaged <see cref="Unmanaged::VideoSizeMode"/> enum value.</returns>
    inline Unmanaged::VideoSizeMode VideoResizeModeToUnmanagedVideoSizeMode(Models::VideoResizeMode videoResizeMode)
    {
        switch (videoResizeMode)
        {
        case Models::VideoResizeMode::None:
            return Unmanaged::VideoSizeMode::None;
        case Models::VideoResizeMode::LetterboxToSize:
        case Models::VideoResizeMode::LetterboxToAspectRatio:
            return Unmanaged::VideoSizeMode::Letterbox;
        default:
            throw gcnew System::ComponentModel::InvalidEnumArgumentException("No matching " NAMEOF(Unmanaged::VideoSizeMode) " member for " NAMEOF(Models::VideoResizeMode) "  member " + videoResizeMode.ToString());
        }
    }
}