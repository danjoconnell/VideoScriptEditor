using System;
using System.Runtime.Serialization;
using VideoScriptEditor.Models.Cropping;
using VideoScriptEditor.Models.Masking;

namespace VideoScriptEditor.Models
{
    /// <summary>
    /// Model encapsulating a Video Script Editor project
    /// </summary>
    [Serializable()]
    [DataContract(Name = "Project", Namespace = "")]
    public class ProjectModel
    {
        /// <summary>
        /// The file path of an AviSynth script providing source video.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public string ScriptFileSource { get; set; }

        /// <summary>
        /// <see cref="VideoProcessingOptionsModel">Video processing options</see> such as video resizing.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public VideoProcessingOptionsModel VideoProcessingOptions { get; set; }

        /// <summary>
        /// <see cref="CroppingSubProjectModel">Cropping subproject data</see>.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public CroppingSubProjectModel Cropping { get; set; }

        /// <summary>
        /// <see cref="MaskingSubProjectModel">Masking subproject data</see>.
        /// </summary>
        /// <remarks>Serialized data item.</remarks>
        [DataMember]
        public MaskingSubProjectModel Masking { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="ProjectModel"/> class.
        /// </summary>
        public ProjectModel()
        {
            VideoProcessingOptions = new VideoProcessingOptionsModel();
            Cropping = new CroppingSubProjectModel();
            Masking = new MaskingSubProjectModel();

            ProjectFilePath = string.Empty;
            IsNew = true;
            HasChanges = false;
        }

        /// <summary>
        /// Gets the file path of this <see cref="ProjectModel">project</see>.
        /// </summary>
        [IgnoreDataMember]
        public string ProjectFilePath { get; protected internal set; }

        /// <summary>
        /// Gets or sets whether this is a new unsaved <see cref="ProjectModel">project</see>.
        /// </summary>
        [IgnoreDataMember]
        public bool IsNew { get; set; }

        /// <summary>
        /// Gets or sets whether this <see cref="ProjectModel">project</see> has unsaved changes.
        /// </summary>
        [IgnoreDataMember]
        public bool HasChanges { get; set; }
    }
}
