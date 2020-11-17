using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Cropping;
using VideoScriptEditor.Models.Masking;
using Size = System.Drawing.Size;

namespace VideoScriptEditor.Services
{
    /// <summary>
    /// A service providing access to a <see cref="ProjectModel">project</see>
    /// and performing related I/O operations.
    /// </summary>
    public class ProjectService : IProjectService
    {
        /// <inheritdoc/>
        public event EventHandler ProjectOpened;

        /// <inheritdoc/>
        public event EventHandler ProjectClosing;

        /// <inheritdoc/>
        public event EventHandler ProjectClosed;

        /// <summary>
        /// Creates a new instance of the <see cref="ProjectService"/> class.
        /// </summary>
        public ProjectService()
        {
        }

        /// <inheritdoc/>
        public ProjectModel Project { get; private set; } = null;

        /// <inheritdoc/>
        public ProjectModel CreateNewProject()
        {
            if (Project != null)
            {
                CloseProject();
            }

            Project = new ProjectModel();

            ProjectOpened?.Invoke(this, new EventArgs());

            return Project;
        }

        /// <inheritdoc/>
        public ProjectModel OpenProject(string filePath)
        {
            if (Project != null)
            {
                CloseProject();
            }

            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                using XmlDictionaryReader xmlReader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
                DataContractSerializer serializer = new DataContractSerializer(typeof(ProjectModel));
                Project = (ProjectModel)serializer.ReadObject(xmlReader, true);

                // The absolute path of the file opened in the FileStream.
                Project.ProjectFilePath = fs.Name;
            }

            Project.IsNew = false;
            Project.HasChanges = false;

            if (Project.VideoProcessingOptions == null)
            {
                Project.VideoProcessingOptions = new VideoProcessingOptionsModel();
            }

            if (Project.Cropping == null)
            {
                Project.Cropping = new CroppingSubProjectModel();
            }

            if (Project.Masking == null)
            {
                Project.Masking = new MaskingSubProjectModel();
            }

            ProjectOpened?.Invoke(this, new EventArgs());

            return Project;
        }

        /// <inheritdoc/>
        public void SaveProject(string filePath, bool createBackup = false)
        {
            Size? tempOutputVideoSize = null;
            if (Project.VideoProcessingOptions != null && Project.VideoProcessingOptions.OutputVideoResizeMode != VideoResizeMode.LetterboxToSize)
            {
                tempOutputVideoSize = Project.VideoProcessingOptions.OutputVideoSize;
                Project.VideoProcessingOptions.OutputVideoSize = null;
            }

            if (createBackup && File.Exists(filePath))
            {
                // Create backup copy
                File.Copy(filePath, filePath + ".bak", true);
            }

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(ProjectModel));
                serializer.WriteObject(fs, Project);
            }

            if (Project.VideoProcessingOptions != null && Project.VideoProcessingOptions.OutputVideoResizeMode != VideoResizeMode.LetterboxToSize)
            {
                Project.VideoProcessingOptions.OutputVideoSize = tempOutputVideoSize;
            }

            Project.IsNew = false;
            Project.HasChanges = false;
            Project.ProjectFilePath = filePath;
        }

        /// <inheritdoc/>
        public void CloseProject()
        {
            if (Project != null)
            {
                ProjectClosing?.Invoke(this, new EventArgs());

                Project = null;

                ProjectClosed?.Invoke(this, new EventArgs());
            }
        }
    }
}
