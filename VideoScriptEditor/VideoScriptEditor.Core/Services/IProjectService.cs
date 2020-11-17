using System;
using VideoScriptEditor.Models;

namespace VideoScriptEditor.Services
{
    /// <summary>
    /// Abstraction of a service providing access to a <see cref="ProjectModel">project</see>
    /// and performing related I/O operations.
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// Occurs when a <see cref="ProjectModel">project</see> is opened or created.
        /// </summary>
        event EventHandler ProjectOpened;

        /// <summary>
        /// Occurs before the current <see cref="ProjectModel">project</see> is closed.
        /// </summary>
        event EventHandler ProjectClosing;

        /// <summary>
        /// Occurs when the current <see cref="ProjectModel">project</see> is closed.
        /// </summary>
        event EventHandler ProjectClosed;

        /// <summary>
        /// Gets a reference to the current <see cref="ProjectModel">project</see>.
        /// </summary>
        /// <returns>A reference to the current <see cref="ProjectModel">project</see> or <c>null</c> if no project is open.</returns>
        ProjectModel Project { get; }

        /// <summary>
        /// Creates a new <see cref="ProjectModel">project</see>.
        /// </summary>
        /// <remarks>If a project is currently open, it is closed before the new project is created.</remarks>
        /// <returns>A reference to the created <see cref="ProjectModel">project</see>.</returns>
        ProjectModel CreateNewProject();

        /// <summary>
        /// Opens a <see cref="ProjectModel">project</see> from the specified file path.
        /// </summary>
        /// <param name="filePath">The file path of the project to open.</param>
        /// <returns>A reference to the opened <see cref="ProjectModel">project</see>.</returns>
        ProjectModel OpenProject(string filePath);

        /// <summary>
        /// Saves the current project to the specified file,
        /// optionally creating a backup before overwriting an existing file.
        /// </summary>
        /// <remarks>
        /// Backup files are created with the .bak extension.
        /// </remarks>
        /// <param name="filePath">The file path to save the project to.</param>
        /// <param name="createBackup">Whether to create a backup before overwriting an existing file. Defaults to false.</param>
        void SaveProject(string filePath, bool createBackup = false);

        /// <summary>
        /// Closes the current project.
        /// </summary>
        void CloseProject();
    }
}
