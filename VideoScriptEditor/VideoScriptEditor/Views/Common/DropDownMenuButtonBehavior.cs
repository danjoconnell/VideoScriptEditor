/*
   Based on DropDownButtonBehavior.cs by Ryan Van Slooten at https://gist.github.com/ryanvs/8059757
   This is free and unencumbered software released into the public domain.
   For more information, please refer to <http://unlicense.org/>
*/

using Microsoft.Xaml.Behaviors;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// WPF Behavior for <see cref="Button"/>s to show a drop down <see cref="ContextMenu"/>
    /// when the <see cref="Button"/> is pressed.
    /// </summary>
    /// <remarks>
    /// <para>Uses Microsoft.Xaml.Behaviors for Blend behavior.</para>
    /// Possible solution to --
    /// http://stackoverflow.com/questions/8958946/how-to-open-a-popup-menu-when-a-button-is-clicked
    /// </remarks>
    public class DropDownMenuButtonBehavior : Behavior<Button>
    {
        private long _attachedCount;
        private bool _isContextMenuOpen;

        public DropDownMenuButtonBehavior() : base()
        {
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(AssociatedObject_Click), true);
        }

        private void AssociatedObject_Click(object sender, RoutedEventArgs e)
        {
            Button source = sender as Button;
            if (source?.ContextMenu != null)
            {
                // Only open the ContextMenu when it is not already open. If it is already open,
                // when the button is pressed the ContextMenu will lose focus and automatically close.
                if (!_isContextMenuOpen)
                {
                    source.ContextMenu.AddHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed), true);
                    Interlocked.Increment(ref _attachedCount);
                    // If there is a drop-down assigned to this button, then position and display it 
                    source.ContextMenu.PlacementTarget = source;
                    source.ContextMenu.Placement = PlacementMode.Bottom;
                    source.ContextMenu.IsOpen = true;
                    _isContextMenuOpen = true;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(AssociatedObject_Click));
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            _isContextMenuOpen = false;
            if (sender is ContextMenu contextMenu)
            {
                contextMenu.RemoveHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed));
                Interlocked.Decrement(ref _attachedCount);
            }
        }
    }
}
