using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using VideoScriptEditor.Extensions;
using VideoScriptEditor.Models;
using VideoScriptEditor.PrismExtensions;
using VideoScriptEditor.Services.ScriptVideo;
using Ratio = VideoScriptEditor.Models.Primitives.Ratio;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.ViewModels.Dialogs
{
    /// <summary>
    /// View Model encapsulating presentation logic for the Output Video Properties dialog.
    /// </summary>
    public class OutputVideoPropertiesDialogViewModel : NotifyDataErrorInfoBindableBase, IDialogAware
    {
        private SizeI _sourceVideoFrameSize;

        private VideoResizeMode _resizeMode = VideoResizeMode.None;
        private int _videoWidth = 2;
        private int _videoHeight = 2;
        private uint _aspectRatioNumerator = 1u;
        private uint _aspectRatioDenominator = 1u;

        /// <inheritdoc cref="IDialogAware.Title"/>
        public string Title => "Change output video properties";

        /// <inheritdoc cref="IDialogAware.RequestClose"/>
        public event Action<IDialogResult> RequestClose;

        /// <summary>
        /// Gets or sets the method for resizing the video.
        /// </summary>
        public VideoResizeMode ResizeMode
        {
            get => _resizeMode;
            set
            {
                if (_resizeMode == value)
                {
                    return;
                }

                if (_errorsContainer.HasErrors)
                {
                    // Clear errors on properties associated with current (previous) resize mode
                    // as their values won't be included in the dialog result.
                    switch (_resizeMode)
                    {
                        case VideoResizeMode.LetterboxToAspectRatio:
                            _errorsContainer.ClearErrors(nameof(AspectRatioNumerator));
                            _errorsContainer.ClearErrors(nameof(AspectRatioDenominator));
                            break;
                        case VideoResizeMode.LetterboxToSize:
                            _errorsContainer.ClearErrors(nameof(VideoWidth));
                            _errorsContainer.ClearErrors(nameof(VideoHeight));
                            break;
                    }
                }

                SetProperty(ref _resizeMode, value);

                // Validate properties associated with current (new) resize mode.
                switch (_resizeMode)
                {
                    case VideoResizeMode.LetterboxToAspectRatio:
                        _errorsContainer.SetErrors(nameof(AspectRatioNumerator), ValidateAspectRatioNumeratorProperty(_aspectRatioNumerator));
                        _errorsContainer.SetErrors(nameof(AspectRatioDenominator), ValidateAspectRatioDenominatorProperty(_aspectRatioDenominator));
                        break;
                    case VideoResizeMode.LetterboxToSize:
                        _errorsContainer.SetErrors(nameof(VideoWidth), ValidateVideoWidthProperty(_videoWidth));
                        _errorsContainer.SetErrors(nameof(VideoHeight), ValidateVideoHeightProperty(_videoHeight));
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or sets the desired width of the video in pixels if the video is to be letterboxed to size.
        /// </summary>
        public int VideoWidth
        {
            get => _videoWidth;
            set => SetProperty(ref _videoWidth, value.RoundToNearestEvenIntegral(), ValidateVideoWidthProperty);
        }

        /// <summary>
        /// Gets or sets the desired height of the video in pixels if the video is to be letterboxed to size.
        /// </summary>
        public int VideoHeight
        {
            get => _videoHeight;
            set => SetProperty(ref _videoHeight, value.RoundToNearestEvenIntegral(), ValidateVideoHeightProperty);
        }

        /// <summary>
        /// Gets or sets the numerator of desired aspect ratio if the video is to be resized using an aspect ratio.
        /// </summary>
        public uint AspectRatioNumerator
        {
            get => _aspectRatioNumerator;
            set => SetProperty(ref _aspectRatioNumerator, value, ValidateAspectRatioNumeratorProperty);
        }

        /// <summary>
        /// Gets or sets the denominator of desired aspect ratio if the video is to be resized using an aspect ratio.
        /// </summary>
        public uint AspectRatioDenominator
        {
            get => _aspectRatioDenominator;
            set => SetProperty(ref _aspectRatioDenominator, value, ValidateAspectRatioDenominatorProperty);
        }

        /// <summary>
        /// Command for closing the dialog and applying changes to the output video properties.
        /// </summary>
        public DelegateCommand ApplyCommand { get; }

        /// <summary>
        /// Command for closing the dialog without making changes to the output video properties.
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// Creates a new <see cref="OutputVideoPropertiesDialogViewModel"/> instance.
        /// </summary>
        public OutputVideoPropertiesDialogViewModel() : base()
        {
            _sourceVideoFrameSize = new SizeI(2, 2);

            ApplyCommand = new DelegateCommand(
                executeMethod: ExecuteApplyCommand,
                canExecuteMethod: () => !HasErrors
            ).ObservesProperty(() => HasErrors);

            CancelCommand = new DelegateCommand(ExecuteCancelCommand);
        }

        /// <inheritdoc cref="IDialogAware.CanCloseDialog"/>
        public bool CanCloseDialog() => true;

        /// <inheritdoc cref="IDialogAware.OnDialogOpened(IDialogParameters)"/>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            _sourceVideoFrameSize = parameters.GetValue<SizeI>(nameof(IScriptVideoContext.VideoFrameSize));
            ResizeMode = parameters.GetValue<VideoResizeMode>(nameof(VideoProcessingOptionsModel.OutputVideoResizeMode));

            SizeI outputVideoSize = parameters.GetValue<SizeI?>(nameof(VideoProcessingOptionsModel.OutputVideoSize))
                                    ?? _sourceVideoFrameSize;

            if (!outputVideoSize.IsEmpty)
            {
                VideoWidth = outputVideoSize.Width;
                VideoHeight = outputVideoSize.Height;
            }
            else
            {
                SetProperty(ref _videoWidth, 2, nameof(VideoWidth));
                SetProperty(ref _videoHeight, 2, nameof(VideoHeight));
            }

            Ratio outputVideoAspectRatio = parameters.GetValue<Ratio?>(nameof(VideoProcessingOptionsModel.OutputVideoAspectRatio))
                                           ?? parameters.GetValue<Ratio>(nameof(IScriptVideoContext.AspectRatio));

            AspectRatioNumerator = outputVideoAspectRatio.Numerator;
            AspectRatioDenominator = outputVideoAspectRatio.Denominator;
        }

        /// <inheritdoc cref="IDialogAware.OnDialogClosed"/>
        public void OnDialogClosed()
        {
            ResetDialogValues();
        }

        /// <summary>
        /// Closes the dialog and applies changes to the output video properties.
        /// </summary>
        /// <remarks>
        /// Invoked on execution of the <see cref="ApplyCommand"/>.
        /// </remarks>
        private void ExecuteApplyCommand()
        {
            DialogResult dialogResult = new DialogResult(ButtonResult.OK, new DialogParameters
            {
                { nameof(VideoProcessingOptionsModel.OutputVideoResizeMode), _resizeMode }
            });

            switch (_resizeMode)
            {
                case VideoResizeMode.LetterboxToAspectRatio:
                    dialogResult.Parameters.Add(nameof(VideoProcessingOptionsModel.OutputVideoAspectRatio), new Ratio(_aspectRatioNumerator, _aspectRatioDenominator));
                    break;
                case VideoResizeMode.LetterboxToSize:
                    dialogResult.Parameters.Add(nameof(VideoProcessingOptionsModel.OutputVideoSize), new SizeI(_videoWidth, _videoHeight));
                    break;
            }

            RequestClose?.Invoke(dialogResult);

            ResetDialogValues();
        }

        /// <summary>
        /// Closes the dialog without making changes to the output video properties.
        /// </summary>
        /// <remarks>
        /// Invoked on execution of the <see cref="CancelCommand"/>.
        /// </remarks>
        private void ExecuteCancelCommand()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            ResetDialogValues();
        }

        /// <summary>
        /// Validates the <see cref="VideoWidth"/> property value
        /// and returns a collection of validation errors.
        /// </summary>
        /// <param name="videoWidthValue">The new <see cref="VideoWidth"/> property value.</param>
        /// <returns>A collection of validation errors or an empty collection if <paramref name="videoWidthValue"/> is valid.</returns>
        private IEnumerable<string> ValidateVideoWidthProperty(int videoWidthValue)
        {
            if (videoWidthValue <= 0)
            {
                yield return "Video width must be greater than 0";
            }
            else if (!IsVideoSizeLetterboxPaddingValid(videoWidthValue, _videoHeight, out string letterboxPaddingError))
            {
                yield return letterboxPaddingError;
            }
        }

        /// <summary>
        /// Validates the <see cref="VideoHeight"/> property value
        /// and returns a collection of validation errors.
        /// </summary>
        /// <param name="videoHeightValue">The new <see cref="VideoHeight"/> property value.</param>
        /// <returns>A collection of validation errors or an empty collection if <paramref name="videoHeightValue"/> is valid.</returns>
        private IEnumerable<string> ValidateVideoHeightProperty(int videoHeightValue)
        {
            if (videoHeightValue <= 0)
            {
                yield return "Video height must be greater than 0";
            }
            else if (!IsVideoSizeLetterboxPaddingValid(_videoWidth, videoHeightValue, out string letterboxPaddingError))
            {
                yield return letterboxPaddingError;
            }
        }

        /// <summary>
        /// Determines if the video can be letterboxed to the <see cref="VideoWidth"/> and <see cref="VideoHeight"/>
        /// without letterbox bars appearing horizontally and vertically.
        /// </summary>
        /// <param name="videoWidth">The <see cref="VideoWidth"/> property value.</param>
        /// <param name="videoHeight">The <see cref="VideoHeight"/> property value.</param>
        /// <param name="validationError">
        /// The validation error message if the video can't be letterboxed to size without letterbox bars appearing horizontally and vertically.
        /// A null <see cref="string"/> otherwise.
        /// </param>
        /// <returns>
        /// True if the video can be letterboxed to size without letterbox bars appearing horizontally and vertically,
        /// otherwise False.
        /// </returns>
        private bool IsVideoSizeLetterboxPaddingValid(int videoWidth, int videoHeight, out string validationError)
        {
            int horizontalLetterboxPadding = videoWidth - _sourceVideoFrameSize.Width;
            int verticalLetterboxPadding = videoHeight - _sourceVideoFrameSize.Height;
            if (horizontalLetterboxPadding > 0 && verticalLetterboxPadding > 0)
            {
                validationError = "Can't letterbox both video width and height";
                return false;
            }

            validationError = null;
            return true;
        }

        /// <summary>
        /// Validates the <see cref="AspectRatioNumerator"/> property value
        /// and returns a collection of validation errors.
        /// </summary>
        /// <param name="aspectRatioNumeratorValue">The new <see cref="AspectRatioNumerator"/> property value.</param>
        /// <returns>A collection of validation errors or an empty collection if <paramref name="aspectRatioNumeratorValue"/> is valid.</returns>
        private IEnumerable<string> ValidateAspectRatioNumeratorProperty(uint aspectRatioNumeratorValue)
        {
            if (aspectRatioNumeratorValue == 0u)
            {
                yield return "The numerator must be greater than 0";
            }
        }

        /// <summary>
        /// Validates the <see cref="AspectRatioDenominator"/> property value
        /// and returns a collection of validation errors.
        /// </summary>
        /// <param name="aspectRatioDenominatorValue">The new <see cref="AspectRatioDenominator"/> property value.</param>
        /// <returns>A collection of validation errors or an empty collection if <paramref name="aspectRatioDenominatorValue"/> is valid.</returns>
        private IEnumerable<string> ValidateAspectRatioDenominatorProperty(uint aspectRatioDenominatorValue)
        {
            if (aspectRatioDenominatorValue == 0u)
            {
                yield return "The denominator must be greater than 0";
            }
        }

        /// <summary>
        /// Resets the dialog to default values.
        /// </summary>
        private void ResetDialogValues()
        {
            _errorsContainer.ClearErrors();

            _sourceVideoFrameSize = new SizeI(2, 2);
            SetProperty(ref _resizeMode, VideoResizeMode.None, nameof(ResizeMode));
            SetProperty(ref _videoWidth, 2, nameof(VideoWidth));
            SetProperty(ref _videoHeight, 2, nameof(VideoHeight));
            SetProperty(ref _aspectRatioNumerator, 1u, nameof(AspectRatioNumerator));
            SetProperty(ref _aspectRatioDenominator, 1u, nameof(AspectRatioDenominator));
        }
    }
}
