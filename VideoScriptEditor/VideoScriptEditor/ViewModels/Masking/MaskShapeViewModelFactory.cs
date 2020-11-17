using MonitoredUndo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Masking.Shapes;
using VideoScriptEditor.ViewModels.Masking.Shapes;
using VideoScriptEditor.ViewModels.Timeline;
using PointD = VideoScriptEditor.Models.Primitives.PointD;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.ViewModels.Masking
{
    /// <summary>
    /// A factory for creating view models derived from <see cref="MaskShapeViewModelBase"/>.
    /// </summary>
    /// <inheritdoc cref="SegmentViewModelFactoryBase"/>
    public class MaskShapeViewModelFactory : SegmentViewModelFactoryBase
    {
        /// <summary>
        /// Creates a new <see cref="MaskShapeViewModelFactory"/> instance.
        /// </summary>
        /// <inheritdoc cref="SegmentViewModelFactoryBase(Services.ScriptVideo.IScriptVideoContext, IUndoService, IChangeFactory, object, Services.IClipboardService)"/>
        public MaskShapeViewModelFactory(Services.ScriptVideo.IScriptVideoContext scriptVideoContext, IUndoService undoService, IChangeFactory undoChangeFactory, object rootUndoObject, Services.IClipboardService clipboardService) : base(scriptVideoContext, undoService, undoChangeFactory, rootUndoObject, clipboardService)
        {
        }

        /// <inheritdoc/>
        public override SegmentViewModelBase CreateSegmentViewModel(SegmentModelBase segmentModel)
        {
            return segmentModel switch
            {
                PolygonMaskShapeModel polygonMaskShapeModel
                    => new PolygonMaskShapeViewModel(polygonMaskShapeModel, _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService),
                RectangleMaskShapeModel rectangleMaskShapeModel
                    => new RectangleMaskShapeViewModel(rectangleMaskShapeModel, _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService),
                EllipseMaskShapeModel ellipseMaskShapeModel
                    => new EllipseMaskShapeViewModel(ellipseMaskShapeModel, _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService),
                _   // default
                    => throw new ArgumentException("Invalid or null SegmentModelBase instance", nameof(segmentModel)),
            };
        }

        /// <inheritdoc/>
        public override SegmentViewModelBase CreateSegmentModelViewModel(Enum segmentTypeDescriptor, int trackNumber, int startFrame, int endFrame, string name = null)
        {
            switch (segmentTypeDescriptor)
            {
                case PolygonShapeType.IsoscelesTriangle:
                case PolygonShapeType.RightTriangle:
                    return CreatePolygonShapeModelViewModel((PolygonShapeType)segmentTypeDescriptor, trackNumber, startFrame, endFrame, name);
                case MaskShapeType.Polygon:
                    return CreatePolygonShapeModelViewModel(PolygonShapeType.IsoscelesTriangle, trackNumber, startFrame, endFrame, name);
                case MaskShapeType.Rectangle:
                case MaskShapeType.Ellipse:
                    return CreateMaskShapeViewModel((MaskShapeType)segmentTypeDescriptor, trackNumber, startFrame, endFrame, name);
                default:
                    throw new InvalidEnumArgumentException(nameof(segmentTypeDescriptor));
            }
        }

        /// <inheritdoc/>
        public override SegmentViewModelBase CreateSegmentModelViewModel(SegmentViewModelBase sourceViewModel, int? trackNumber = null, int? startFrame = null, int? endFrame = null, string name = null, KeyFrameViewModelCollection keyFrameViewModels = null)
        {
            int newTrackNumber = trackNumber.GetValueOrDefault(sourceViewModel.TrackNumber);
            int newStartFrame = startFrame.GetValueOrDefault(sourceViewModel.StartFrame);
            int newEndFrame = endFrame.GetValueOrDefault(sourceViewModel.EndFrame);
            string newName = name ?? sourceViewModel.Name;

            KeyFrameModelCollection keyFrameModels = keyFrameViewModels != null
                                                     ? GetKeyFrameModels(keyFrameViewModels)
                                                     : GetKeyFrameModels(sourceViewModel.KeyFrameViewModels,
                                                                         createCopies: true,
                                                                         frameOffset: newStartFrame - sourceViewModel.StartFrame);

            return sourceViewModel switch
            {
                PolygonMaskShapeViewModel _
                    => new PolygonMaskShapeViewModel(
                            new PolygonMaskShapeModel(newStartFrame, newEndFrame, newTrackNumber, keyFrameModels, newName),
                            _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService, keyFrameViewModels
                       ),
                RectangleMaskShapeViewModel _
                    => new RectangleMaskShapeViewModel(
                            new RectangleMaskShapeModel(newStartFrame, newEndFrame, newTrackNumber, keyFrameModels, newName),
                            _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService, keyFrameViewModels
                       ),
                EllipseMaskShapeViewModel _
                    => new EllipseMaskShapeViewModel(
                            new EllipseMaskShapeModel(newStartFrame, newEndFrame, newTrackNumber, keyFrameModels, newName),
                            _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService, keyFrameViewModels
                       ),
                _   // default
                    => throw new ArgumentException("Invalid or null SegmentViewModelBase instance", nameof(sourceViewModel)),
            };
        }

        /// <summary>
        /// Creates a new <see cref="PolygonMaskShapeViewModel"/> instance containing a new <see cref="PolygonMaskShapeModel"/> instance
        /// of an initial <see cref="PolygonShapeType">classification</see>, with a specific track number, start frame number, end frame number
        /// and descriptive name.
        /// </summary>
        /// <param name="polygonShapeType">A <see cref="PolygonShapeType"/> describing the initial polygon classification.</param>
        /// <param name="trackNumber">The zero-based timeline track number of the polygon masking shape segment.</param>
        /// <param name="startFrame">The inclusive zero-based start frame number of the polygon masking shape segment.</param>
        /// <param name="endFrame">The inclusive zero-based end frame number of the polygon masking shape segment.</param>
        /// <param name="name">A descriptive name for the polygon masking shape segment.</param>
        /// <returns>
        /// A new <see cref="PolygonMaskShapeViewModel"/> instance containing a new <see cref="PolygonMaskShapeModel"/> instance
        /// of the initial <see cref="PolygonShapeType">classification</see>, with the specified track number, start frame number, end frame number and descriptive name.
        /// </returns>
        private PolygonMaskShapeViewModel CreatePolygonShapeModelViewModel(PolygonShapeType polygonShapeType, int trackNumber, int startFrame, int endFrame, string name = null)
        {
            SizeI videoFrameSize = _scriptVideoContext.VideoFrameSize;
            Point shapeCenterPoint = new Point(videoFrameSize.Width / 2d, videoFrameSize.Height / 2d);
            Size shapeSize = new Size(videoFrameSize.Width / 4d, videoFrameSize.Height / 4d); // Quarter size

            List<PointD> polygonPoints = new List<PointD>();

            if (polygonShapeType == PolygonShapeType.IsoscelesTriangle)
            {
                // Create a new isosceles triangle one quarter size of video width and height and aligned center of video.
                polygonPoints.Add(new PointD(shapeCenterPoint.X, shapeCenterPoint.Y - (shapeSize.Height / 2d)));
                polygonPoints.Add(new PointD(shapeCenterPoint.X - (shapeSize.Width / 2d), shapeCenterPoint.Y + (shapeSize.Height / 2d)));
                polygonPoints.Add(new PointD(shapeCenterPoint.X + (shapeSize.Width / 2d), shapeCenterPoint.Y + (shapeSize.Height / 2d)));
            }
            else if (polygonShapeType == PolygonShapeType.RightTriangle)
            {
                // Create a new right triangle one quarter size of video width and height and aligned center of video.
                polygonPoints.Add(new PointD(shapeCenterPoint.X - (shapeSize.Width / 2d), shapeCenterPoint.Y - (shapeSize.Height / 2d)));
                polygonPoints.Add(new PointD(shapeCenterPoint.X - (shapeSize.Width / 2d), shapeCenterPoint.Y + (shapeSize.Height / 2d)));
                polygonPoints.Add(new PointD(shapeCenterPoint.X + (shapeSize.Width / 2d), shapeCenterPoint.Y + (shapeSize.Height / 2d)));
            }
            else
            {
                throw new InvalidEnumArgumentException($"Unexpected {nameof(PolygonShapeType)} value: {polygonShapeType}");
            }

            PolygonMaskShapeModel model = new PolygonMaskShapeModel(startFrame, endFrame, trackNumber,
                                                                    new KeyFrameModelCollection()
                                                                    {
                                                                        new PolygonMaskShapeKeyFrameModel(startFrame, polygonPoints)
                                                                    },
                                                                    name ?? nameof(MaskShapeType.Polygon));

            return new PolygonMaskShapeViewModel(model, _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService);
        }

        /// <summary>
        /// Creates a new masking shape segment view model instance containing a new <see cref="SegmentModelBase">data model</see> instance
        /// of a type described by a <see cref="MaskShapeType"/> enumeration value, with a specific track number, start frame number, end frame number
        /// and descriptive name.
        /// </summary>
        /// <param name="maskShapeType">
        /// A <see cref="MaskShapeType"/> value describing the type of masking shape segment <see cref="SegmentModelBase">data model</see> to create.
        /// </param>
        /// <param name="trackNumber">The zero-based timeline track number of the masking shape segment.</param>
        /// <param name="startFrame">The inclusive zero-based start frame number of the masking shape segment.</param>
        /// <param name="endFrame">The inclusive zero-based end frame number of the masking shape segment.</param>
        /// <param name="name">A descriptive name for the masking shape segment.</param>
        /// <returns>
        /// A new masking shape segment view model instance containing a new <see cref="SegmentModelBase">data model</see> instance
        /// of the described type, with the specified track number, start frame number, end frame number and descriptive name.
        /// </returns>
        private MaskShapeViewModelBase CreateMaskShapeViewModel(MaskShapeType maskShapeType, int trackNumber, int startFrame, int endFrame, string name = null)
        {
            SizeI videoFrameSize = _scriptVideoContext.VideoFrameSize;
            Point shapeCenterPoint = new Point(videoFrameSize.Width / 2d, videoFrameSize.Height / 2d);
            Size shapeSize = new Size(videoFrameSize.Width / 4d, videoFrameSize.Height / 4d); // Quarter size

            if (name == null)
            {
                name = maskShapeType.ToString();
            }

            if (maskShapeType == MaskShapeType.Rectangle)
            {
                var rectangleKeyFrameModel = new RectangleMaskShapeKeyFrameModel(startFrame,
                                                                                 shapeCenterPoint.X - (shapeSize.Width / 2d),
                                                                                 shapeCenterPoint.Y - (shapeSize.Height / 2d),
                                                                                 shapeSize.Width,
                                                                                 shapeSize.Height);

                RectangleMaskShapeModel rectangleModel = new RectangleMaskShapeModel(startFrame, endFrame, trackNumber,
                                                                                     new KeyFrameModelCollection()
                                                                                     {
                                                                                         rectangleKeyFrameModel
                                                                                     },
                                                                                     name);

                return new RectangleMaskShapeViewModel(rectangleModel, _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService);
            }
            else if (maskShapeType == MaskShapeType.Ellipse)
            {
                Rect ellipseBounds = new Rect(
                    new Point(
                        shapeCenterPoint.X - (shapeSize.Width / 2d),
                        shapeCenterPoint.Y - (shapeSize.Height / 2d)
                    ),
                    shapeSize
                );
                Geometry.Ellipse ellipseGeometry = new Geometry.Ellipse(ellipseBounds);

                EllipseMaskShapeModel ellipseModel = new EllipseMaskShapeModel(startFrame, endFrame, trackNumber,
                                                                               new KeyFrameModelCollection()
                                                                               {
                                                                                  new EllipseMaskShapeKeyFrameModel(startFrame,
                                                                                                                    ellipseGeometry.CenterPoint.ToPointD(),
                                                                                                                    ellipseGeometry.RadiusX,
                                                                                                                    ellipseGeometry.RadiusY)
                                                                               },
                                                                               name);

                return new EllipseMaskShapeViewModel(ellipseModel, _scriptVideoContext, _rootUndoObject, _undoService, _undoChangeFactory, _clipboardService);
            }
            else
            {
                throw new InvalidEnumArgumentException($"Unexpected {nameof(MaskShapeType)} value: {maskShapeType}");
            }
        }
    }
}
