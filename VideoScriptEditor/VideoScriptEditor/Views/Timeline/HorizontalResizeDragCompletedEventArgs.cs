/*
    Adapted from https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/DragCompletedEventArgs.cs
    See LICENSE.TXT at https://github.com/dotnet/wpf/blob/master/LICENSE.TXT
*/

using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using VideoScriptEditor.Views.Common;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// Provides information about the <see cref="VideoTimelineSegment.HorizontalResizeDragCompletedEvent"/>
    /// that occurs when the user completes a horizontal drag operation with the mouse on a border <see cref="Thumb"/
    /// of a <see cref="VideoTimelineSegment"/>.
    /// </summary>
    public class HorizontalResizeDragCompletedEventArgs : DragCompletedEventArgs
    {
        /// <summary>
        /// Gets the <see cref="Common.HorizontalSide">horizontal side</see> of the originating border <see cref="Thumb"/>.
        /// </summary>
        public HorizontalSide HorizontalSide { get; }

        /// <summary>
        /// Creates a new <see cref="HorizontalResizeDragCompletedEventArgs"/> instance.
        /// </summary>
        /// <param name="horizontalSide">
        /// The <see cref="Common.HorizontalSide">horizontal side</see> of the originating border <see cref="Thumb"/>.
        /// </param>
        /// <inheritdoc cref="DragCompletedEventArgs(double, double, bool)"/>
        public HorizontalResizeDragCompletedEventArgs(HorizontalSide horizontalSide, double horizontalChange, bool canceled)
            : base(horizontalChange, verticalChange: 0d, canceled)
        {
            HorizontalSide = horizontalSide;
            RoutedEvent = VideoTimelineSegment.HorizontalResizeDragCompletedEvent;
        }

        /// <summary>
        /// Converts a method that handles the <see cref="VideoTimelineSegment.HorizontalResizeDragCompletedEvent"/>
        /// to the <see cref="HorizontalResizeDragCompletedEventHandler"/> type.
        /// </summary>
        /// <inheritdoc cref="RoutedEventArgs.InvokeEventHandler(Delegate, object)"/>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            HorizontalResizeDragCompletedEventHandler handler = (HorizontalResizeDragCompletedEventHandler)genericHandler;
            handler(genericTarget, this);
        }
    }

    /// <summary>
    /// Represents a method that will handle the <see cref="VideoTimelineSegment.HorizontalResizeDragCompletedEvent"/>
    /// of a <see cref="VideoTimelineSegment"/> element.
    /// </summary>
    /// <inheritdoc cref="DragCompletedEventHandler"/>
    public delegate void HorizontalResizeDragCompletedEventHandler(object sender, HorizontalResizeDragCompletedEventArgs e);
}
