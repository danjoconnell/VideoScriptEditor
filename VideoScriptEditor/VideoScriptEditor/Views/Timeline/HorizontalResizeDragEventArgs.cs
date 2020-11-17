/*
    Adapted from https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/DragDeltaEventArgs.cs
    See LICENSE.TXT at https://github.com/dotnet/wpf/blob/master/LICENSE.TXT
*/

using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using VideoScriptEditor.Views.Common;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// Provides information about the <see cref="VideoTimelineSegment.HorizontalResizeDraggingEvent"/>
    /// that occurs one or more times when the user drags a border <see cref="Thumb"/> of a <see cref="VideoTimelineSegment"/>
    /// with the mouse horizontally.
    /// </summary>
    public class HorizontalResizeDragEventArgs : DragDeltaEventArgs
    {
        /// <summary>
        /// Gets the <see cref="Common.HorizontalSide">horizontal side</see> of the originating border <see cref="Thumb"/>.
        /// </summary>
        public HorizontalSide HorizontalSide { get; }

        /// <summary>
        /// Creates a new <see cref="HorizontalResizeDragEventArgs"/> instance.
        /// </summary>
        /// <param name="horizontalSide">
        /// The <see cref="Common.HorizontalSide">horizontal side</see> of the originating border <see cref="Thumb"/>.
        /// </param>
        /// <inheritdoc cref="DragDeltaEventArgs(double, double)"/>
        public HorizontalResizeDragEventArgs(HorizontalSide horizontalSide, double horizontalChange) : base(horizontalChange, verticalChange: 0d)
        {
            HorizontalSide = horizontalSide;
            RoutedEvent = VideoTimelineSegment.HorizontalResizeDraggingEvent;
        }

        /// <summary>
        /// Converts a method that handles the <see cref="VideoTimelineSegment.HorizontalResizeDraggingEvent"/>
        /// to the <see cref="HorizontalResizeDraggingEventHandler"/> type.
        /// </summary>
        /// <inheritdoc cref="RoutedEventArgs.InvokeEventHandler(Delegate, object)"/>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            HorizontalResizeDraggingEventHandler handler = (HorizontalResizeDraggingEventHandler)genericHandler;
            handler(genericTarget, this);
        }
    }

    /// <summary>
    /// Represents a method that will handle the <see cref="VideoTimelineSegment.HorizontalResizeDraggingEvent"/>
    /// of a <see cref="VideoTimelineSegment"/> element.
    /// </summary>
    /// <inheritdoc cref="DragDeltaEventHandler"/>
    public delegate void HorizontalResizeDraggingEventHandler(object sender, HorizontalResizeDragEventArgs e);
}
