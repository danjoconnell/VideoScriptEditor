/*
    Based on code from WpfClipboardMonitor, located at https://github.com/Bassman2/WpfClipboardMonitor
    Specifically ClipboardMonitor.cs, located at https://github.com/Bassman2/WpfClipboardMonitor/blob/master/WpfClipboardMonitorShare/ClipboardMonitor.cs

    WpfClipboardMonitor is licensed under the MIT License, Copyright (c) 2019 Bassman2.
    The full text of the license is available at https://github.com/Bassman2/WpfClipboardMonitor/blob/master/LICENSE
*/

using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VideoScriptEditor.Services
{
    public partial class ClipboardService
    {
        /// <summary>
        /// Clipboard Monitor class to notify if the clipboard content changes
        /// </summary>
        private class ClipboardMonitor : IDisposable
        {
            private readonly HwndSource _hwndSource;

            /// <summary>
            /// Event for clipboard update notification.
            /// </summary>
            public event EventHandler ClipboardUpdated;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="windowHandle">Handle of the main window of the application.</param>
            public ClipboardMonitor(IntPtr windowHandle)
            {
                _hwndSource = HwndSource.FromHwnd(windowHandle);
                _hwndSource.AddHook(HwndHandler);
            }

            /// <summary>
            /// Enable clipboard notification.
            /// </summary>
            public bool Start()
            {
                return NativeMethods.AddClipboardFormatListener(_hwndSource.Handle);
            }

            /// <summary>
            /// Disable clipboard notification.
            /// </summary>
            public bool Stop()
            {
                return NativeMethods.RemoveClipboardFormatListener(_hwndSource.Handle);
            }

            private IntPtr HwndHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
            {
                if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
                {
                    ClipboardUpdated?.Invoke(this, new EventArgs());
                }
                handled = false;
                return IntPtr.Zero;
            }

            private bool disposedValue;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        Stop();
                        _hwndSource.RemoveHook(HwndHandler);
                    }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            private static class NativeMethods
            {
                public const int WM_CLIPBOARDUPDATE = 0x031D;   // Winuser.h

                [DllImport("user32.dll", SetLastError = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool AddClipboardFormatListener(IntPtr hwnd);

                [DllImport("user32.dll", SetLastError = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
            }
        }
    }
}
