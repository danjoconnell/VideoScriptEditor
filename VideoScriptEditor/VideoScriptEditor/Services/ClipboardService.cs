using System;
using System.Windows;

namespace VideoScriptEditor.Services
{
    /// <summary>
    /// A service providing access to the system clipboard.
    /// </summary>
    public partial class ClipboardService : IClipboardService, IDisposable
    {
        private ClipboardMonitor _clipboardMonitor;

        /// <inheritdoc/>
        public event EventHandler ClipboardUpdated;

        /// <summary>
        /// Creates a new instance of the <see cref="ClipboardService"/> class.
        /// </summary>
        public ClipboardService()
        {
            _clipboardMonitor = null;
        }

        /// <inheritdoc/>
        public void SetData<T>(T data)
        {
            Clipboard.SetDataObject(
                new DataObject(typeof(T).FullName, data)
            );
        }

        /// <inheritdoc/>
        public bool ContainsData<T>()
        {
            return Clipboard.ContainsData(typeof(T).FullName);
        }

        /// <inheritdoc/>
        public T GetData<T>() where T : class
        {
            return Clipboard.GetData(typeof(T).FullName) as T;
        }

        /// <summary>
        /// Sets the window to use for monitoring system clipboard changes and begins monitoring.
        /// </summary>
        /// <param name="monitorWindowHandle">The handle of the window.</param>
        /// <returns>A <see cref="bool"/> value indicating success or failure.</returns>
        public bool SetMonitorWindow(IntPtr monitorWindowHandle)
        {
            DisposeClipboardMonitor();

            if (monitorWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            _clipboardMonitor = new ClipboardMonitor(monitorWindowHandle);
            _clipboardMonitor.ClipboardUpdated += OnClipboardUpdated;

            return _clipboardMonitor.Start();
        }

        private void OnClipboardUpdated(object sender, EventArgs e)
        {
            ClipboardUpdated?.Invoke(this, e);
        }

        /// <summary>
        /// Stops system clipboard change monitoring and disposes the Clipboard Monitor.
        /// </summary>
        private void DisposeClipboardMonitor()
        {
            if (_clipboardMonitor != null)
            {
                _clipboardMonitor.ClipboardUpdated -= OnClipboardUpdated;
                _clipboardMonitor.Dispose();
                _clipboardMonitor = null;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeClipboardMonitor();        }
    }
}
