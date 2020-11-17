using Prism.Mvvm;
using System;
using System.ComponentModel;
using System.IO;
using VideoScriptEditor.Extensions;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Primitives;
using Size = System.Drawing.Size;

namespace VideoScriptEditor.Services.ScriptVideo
{
    /// <summary>
    /// A base class implementation of the <see cref="IScriptVideoContext"/> interface
    /// which represents the runtime context of the <see cref="IScriptVideoService"/>.
    /// </summary>
    /// <remarks>
    /// Intended only for use as a base class for the ScriptVideoContext class in the VideoScriptEditor.PreviewRenderer assembly.
    /// The ScriptVideoContext is essentially split up into two parts;
    /// A C# base class containing purely managed code and not practical for coding in C++/CLI
    /// and a C++/CLI derived class leveraging the unmanaged C++ interop handling C++/CLI provides.
    /// </remarks>
    public abstract class ScriptVideoContextBase : BindableBase, IScriptVideoContext
    {
        /// <summary>The instance of the <see cref="IScriptVideoService"/> for which this class represents the runtime context.</summary>
        protected readonly IScriptVideoService _scriptVideoService;

        /// <summary>The instance of the <see cref="Dialog.ISystemDialogService"/> for forwarding messages to the UI.</summary>
        protected readonly Dialog.ISystemDialogService _systemDialogService;

        /// <summary>Dedicated lock object for acquiring a mutual-exclusion lock.</summary>
        protected readonly object _syncLock = new object();

        /// <summary>Backing field for the <see cref="Project"/> property.</summary>
        protected ProjectModel _project = null;

        /// <summary>Backing field for the <see cref="ScriptFileSource"/> property.</summary>
        protected string _scriptFileSource = string.Empty;

        /// <summary>Backing field for the <see cref="HasVideo"/> property.</summary>
        protected bool _hasVideo = false;

        /// <summary>Backing field for the <see cref="IsVideoPlaying"/> property.</summary>
        protected bool _isVideoPlaying = false;

        /// <summary>Backing field for the <see cref="FrameNumber"/> property.</summary>
        protected int _frameNumber = 0;

        /// <summary>Backing field for the <see cref="VideoFrameCount"/> property.</summary>
        protected int _videoFrameCount = 0;

        /// <summary>Backing field for the <see cref="SeekableVideoFrameCount"/> property.</summary>
        protected int _seekableVideoFrameCount = 0;

        /// <summary>Backing field for the <see cref="AspectRatio"/> property.</summary>
        protected Ratio _aspectRatio = Ratio.OneToOne;

        /// <summary>Backing field for the <see cref="VideoFramerate"/> property.</summary>
        protected Fraction _videoFramerate = new Fraction(1, 1);

        /// <summary>Backing field for the <see cref="VideoDuration"/> property.</summary>
        protected TimeSpan _videoDuration = TimeSpan.Zero;

        /// <summary>Backing field for the <see cref="VideoPosition"/> property.</summary>
        protected TimeSpan _videoPosition = TimeSpan.Zero;

        /// <summary>Backing field for the <see cref="VideoFrameSize"/> property.</summary>
        protected Size _videoFrameSize = Size.Empty;

        /// <summary>Backing field for the <see cref="OutputPreviewSize"/> property.</summary>
        protected VideoSizeOptions _outputPreviewSize;

        /// <summary>
        /// Gets or sets the <see cref="ProjectModel"/> providing the <see cref="ScriptFileSource"/>
        /// and editing data for previewing through the <see cref="IScriptVideoService"/>.
        /// </summary>
        /// <remarks>Setting a null value resets the <see cref="IScriptVideoService"/> via the <see cref="IScriptVideoService.CloseScript"/> service method.</remarks>
        public ProjectModel Project
        {
            get => _project;
            set
            {
                lock (_syncLock)
                {
                    SetProperty(ref _project, value, OnProjectChanged);
                }
            }
        }

        /// <summary>
        /// Gets or sets the file path of the AviSynth script
        /// providing source video to the <see cref="IScriptVideoService"/>.
        /// </summary>
        public string ScriptFileSource
        {
            get => _scriptFileSource;
            set
            {
                if (_scriptFileSource != value)
                {
                    string scriptFileSourceValue = value;

                    try
                    {
                        _scriptVideoService.LoadScriptFromFileSource(scriptFileSourceValue);
                    }
                    catch (Exception ex)
                    {
                        _systemDialogService.ShowErrorDialog("An exception occurred while attempting to open the script", exception: ex);
                        scriptFileSourceValue = string.Empty;
                    }

                    lock (_syncLock)
                    {
                        SetProperty(ref _scriptFileSource, scriptFileSourceValue);
                    }
                }
            }
        }

        /// <summary>
        /// Gets whether the loaded script (if any) contains renderable video.
        /// </summary>
        /// <remarks>Settable internally and by derived classes.</remarks>
        public bool HasVideo
        {
            get => _hasVideo;
            protected internal set
            {
                lock (_syncLock)
                {
                    SetProperty(ref _hasVideo, value);
                }
            }
        }

        /// <summary>
        /// Gets whether video playback is in progress.
        /// </summary>
        /// <remarks>Settable internally and by derived classes.</remarks>
        public bool IsVideoPlaying
        {
            get => _isVideoPlaying;
            protected internal set
            {
                lock (_syncLock)
                {
                    SetProperty(ref _isVideoPlaying, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current zero-based frame number of the video.
        /// </summary>
        public int FrameNumber
        {
            get => _frameNumber;
            set
            {
                if (_frameNumber != value)
                {
                    try
                    {
                        _scriptVideoService.SeekFrame(value);
                    }
                    catch (Exception ex)
                    {
                        _systemDialogService.ShowErrorDialog($"An exception occurred while seeking frame {value}", exception: ex);
                    }

                    // property backing field '_frameNumber' will be updated via service callback to SetFrameNumberInternal
                }
            }
        }

        /// <summary>
        /// Gets the total number of video frames contained in the video.
        /// </summary>
        /// <remarks>Settable internally and by derived classes.</remarks>
        public int VideoFrameCount
        {
            get => _videoFrameCount;
            protected internal set
            {
                lock (_syncLock)
                {
                    SetProperty(ref _videoFrameCount, value);
                }
            }
        }

        /// <summary>
        /// Gets the total number of seekable video frames contained in the video.
        /// Since frame numbering is zero-based, this is one frame less than the <see cref="VideoFrameCount"/>.
        /// </summary>
        /// <remarks>Settable internally and by derived classes.</remarks>
        public int SeekableVideoFrameCount
        {
            get => _seekableVideoFrameCount;
            protected internal set
            {
                lock (_syncLock)
                {
                    SetProperty(ref _seekableVideoFrameCount, value);
                }
            }
        }

        /// <summary>
        /// Gets the original aspect ratio of the video in the form of a rational fraction.
        /// </summary>
        /// <remarks>Settable internally and by derived classes.</remarks>
        public Ratio AspectRatio
        {
            get => _aspectRatio;
            protected internal set
            {
                lock (_syncLock)
                {
                    SetProperty(ref _aspectRatio, value);
                }
            }
        }

        /// <summary>
        /// Gets the approximate video frame rate in the form of a fraction.
        /// </summary>
        /// <remarks>Settable internally and by derived classes.</remarks>
        public Fraction VideoFramerate
        {
            get => _videoFramerate;
            protected internal set
            {
                lock (_syncLock)
                {
                    SetProperty(ref _videoFramerate, value);
                }
            }
        }

        /// <summary>
        /// Gets the approximate duration of the video.
        /// </summary>
        /// <remarks>Settable internally and by derived classes.</remarks>
        public TimeSpan VideoDuration
        {
            get => _videoDuration;
            protected internal set
            {
                lock (_syncLock)
                {
                    SetProperty(ref _videoDuration, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current position of the video as measured in time.
        /// </summary>
        public TimeSpan VideoPosition
        {
            get => _videoPosition;
            set
            {
                if (_videoPosition != value)
                {
                    long ticksPerFrame = TimeSpan.FromSeconds(1d / _videoFramerate.PrecisionValue).Ticks;
                    FrameNumber = (int)(value.Ticks / ticksPerFrame);

                    // property backing field '_videoPosition' will be updated via service callback to SetVideoPositionInternal or RefreshVideoPositionInternal
                }
            }
        }

        /// <summary>
        /// Gets the original pixel width and height of the video frame.
        /// </summary>
        /// <remarks>Settable internally and by derived classes.</remarks>
        public Size VideoFrameSize
        {
            get => _videoFrameSize;
            protected internal set
            {
                lock (_syncLock)
                {
                    SetProperty(ref _videoFrameSize, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets video resizing preview options
        /// such as resize mode, aspect ratio or pixel width and height.
        /// </summary>
        public VideoSizeOptions OutputPreviewSize
        {
            get => _outputPreviewSize;
            set
            {
                if (_outputPreviewSize != value)
                {
                    try
                    {
                        _scriptVideoService.SetOutputPreviewSize(value);
                    }
                    catch (Exception ex)
                    {
                        _systemDialogService.ShowErrorDialog("An exception occurred while attempting to set the requested output preview size", exception: ex);
                    }

                    // property backing field '_outputPreviewSize' will be updated via service callback to SetOutputPreviewSizeInternal
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="Dialog.ISystemDialogService"/> instance for forwarding messages to the UI.
        /// </summary>
        internal Dialog.ISystemDialogService SystemDialogService => _systemDialogService;

        /// <summary>
        /// Base constructor for classes derived from the <see cref="ScriptVideoContextBase"/> class.
        /// </summary>
        /// <param name="scriptVideoService">The instance of the <see cref="IScriptVideoService"/> for which this class represents the runtime context.</param>
        /// <param name="systemDialogService">An instance of a <see cref="Dialog.ISystemDialogService"/> for forwarding messages to the UI.</param>
        protected ScriptVideoContextBase(IScriptVideoService scriptVideoService, Dialog.ISystemDialogService systemDialogService)
        {
            _scriptVideoService = scriptVideoService;
            _systemDialogService = systemDialogService;

            _outputPreviewSize = new VideoSizeOptions()
            {
                ResizeMode = VideoResizeMode.None,
                AspectRatio = null,
                PixelWidth = 0,
                PixelHeight = 0
            };
        }

        /// <summary>
        /// Callback method for directly setting the <see cref="OutputPreviewSize"/> property backing field value.
        /// </summary>
        /// <remarks>Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for the <see cref="OutputPreviewSize"/> property.</remarks>
        /// <param name="outputPreviewSize">A <see cref="VideoSizeOptions"/> structure specifying video resizing preview options.</param>
        protected internal void SetOutputPreviewSizeInternal(VideoSizeOptions outputPreviewSize)
        {
            lock (_syncLock)
            {
                SetProperty(ref _outputPreviewSize, outputPreviewSize, nameof(OutputPreviewSize));
            }
        }

        /// <summary>
        /// Callback method for directly setting the <see cref="FrameNumber"/> property backing field value.
        /// </summary>
        /// <remarks>Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for the <see cref="FrameNumber"/> property.</remarks>
        /// <param name="frameNumber">The current zero-based frame number of the video.</param>
        protected internal void SetFrameNumberInternal(int frameNumber)
        {
            lock (_syncLock)
            {
                SetProperty(ref _frameNumber, frameNumber, nameof(FrameNumber));
            }
        }

        /// <summary>
        /// Callback method for directly setting the <see cref="VideoPosition"/> property backing field value.
        /// </summary>
        /// <remarks>Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for the <see cref="VideoPosition"/> property.</remarks>
        /// <param name="videoPosition">The current position of the video as measured in time.</param>
        protected internal void SetVideoPositionInternal(TimeSpan videoPosition)
        {
            lock (_syncLock)
            {
                SetProperty(ref _videoPosition, videoPosition, nameof(VideoPosition));
            }
        }

        /// <summary>
        /// Refreshes the value of the <see cref="VideoPosition"/> property by recalculating using the <see cref="FrameNumber"/> and <see cref="VideoFramerate"/> property values.
        /// </summary>
        protected internal void RefreshVideoPositionInternal()
        {
            SetVideoPositionInternal(
                _frameNumber > 0 ? TimeSpan.FromTicks(TimeSpan.TicksPerSecond * _frameNumber * _videoFramerate.Denominator / _videoFramerate.Numerator) : TimeSpan.Zero
            );
        }

        /// <summary>
        /// Refreshes the <see cref="VideoSizeOptions.PixelWidth"/> and <see cref="VideoSizeOptions.PixelHeight"/> fields of the <see cref="OutputPreviewSize"/> property value
        /// by recalculating resized <see cref="VideoFrameSize"/>.
        /// </summary>
        protected internal void RefreshOutputPreviewPixelSize()
        {
            Size pixelSize = _outputPreviewSize.ResizeMode == VideoResizeMode.LetterboxToAspectRatio && _outputPreviewSize.AspectRatio.HasValue
                             ? _videoFrameSize.ExpandToAspectRatio(_outputPreviewSize.AspectRatio.Value)
                             : _videoFrameSize;

            _outputPreviewSize.PixelWidth = pixelSize.Width;
            _outputPreviewSize.PixelHeight = pixelSize.Height;
        }

        /// <summary>
        /// Invoked whenever the value of the <see cref="Project"/> property changes.
        /// </summary>
        protected void OnProjectChanged()
        {
            if (!string.IsNullOrWhiteSpace(_project?.ScriptFileSource))
            {
                //
                // Set output preview size from project
                //
                if (_project.VideoProcessingOptions == null)
                {
                    _project.VideoProcessingOptions = new VideoProcessingOptionsModel();
                }

                VideoProcessingOptionsModel videoProcessingOptions = _project.VideoProcessingOptions;
                Size outputVideoSize = videoProcessingOptions.OutputVideoSize ?? _videoFrameSize;

                VideoSizeOptions outputPreviewSize = new VideoSizeOptions()
                {
                    ResizeMode = videoProcessingOptions.OutputVideoResizeMode,
                    AspectRatio = videoProcessingOptions.OutputVideoAspectRatio,
                    PixelWidth = outputVideoSize.Width,
                    PixelHeight = outputVideoSize.Height
                };

                SetOutputPreviewSizeInternal(outputPreviewSize);

                //
                // Load script from project
                //
                string scriptFilePath = _project.ScriptFileSource;
                if (!Path.IsPathFullyQualified(scriptFilePath))
                {
                    // The AviSynth Import source filter doesn't like relative file paths.
                    // Assuming the script file is relative to or in the same directory as the project file.
                    scriptFilePath = Path.GetFullPath(scriptFilePath, Path.GetDirectoryName(_project.ProjectFilePath));
                }

                ScriptFileSource = scriptFilePath;
            }
            else
            {
                _scriptVideoService.CloseScript();
            }
        }
    }
}
