using System;

namespace VideoScriptEditor.Services
{
    /// <summary>
    /// Abstraction of a service providing access to the system clipboard.
    /// </summary>
    public interface IClipboardService
    {
        /// <summary>
        /// Occurs when the contents of the clipboard have changed.
        /// </summary>
        event EventHandler ClipboardUpdated;

        /// <summary>
        /// Queries the Clipboard for the presence of data of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the data to look for.</typeparam>
        /// <returns>True if data of the specified type is available on the Clipboard; otherwise, false.</returns>
        bool ContainsData<T>();

        /// <summary>
        /// Retrieves data of a specified type from the Clipboard.
        /// </summary>
        /// <typeparam name="T">The type of the data to retrieve.</typeparam>
        /// <returns>An object of the specified type, or null if data of the specified type is unavailable.</returns>
        T GetData<T>() where T : class;

        /// <summary>
        /// Stores the specified data on the Clipboard.
        /// </summary>
        /// <typeparam name="T">The type of the data to store.</typeparam>
        /// <param name="data">The data to store on the Clipboard.</param>
        void SetData<T>(T data);
    }
}