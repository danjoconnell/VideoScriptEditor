using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace VideoScriptEditor.Views.Common
{
    /// <summary>
    /// <see cref="TextBox"/> behavior for updating the binding source of the <see cref="TextBox.Text"/>
    /// property when the Enter key is pressed.
    /// </summary>
    public class EnterKeyUpdatesTextBoxBindingBehavior : Behavior<TextBox>
    {
        /// <summary>
        /// Creates a new <see cref="EnterKeyUpdatesTextBoxBindingBehavior"/> instance.
        /// </summary>
        public EnterKeyUpdatesTextBoxBindingBehavior() : base()
        {
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnTextBoxKeyDown));
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnTextBoxKeyDown));
        }

        /// <summary>
        /// Handles the <see cref="UIElement.KeyDown"/> routed event for the <see cref="Behavior{TextBox}.AssociatedObject"/>.
        /// </summary>
        /// <remarks>
        /// Updates the binding source (if any) of the <see cref="TextBox.Text"/> property
        /// if the Enter key has been pressed.
        /// </remarks>
        /// <inheritdoc cref="KeyEventHandler"/>
        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BindingExpression be = AssociatedObject.GetBindingExpression(TextBox.TextProperty);
                be?.UpdateSource();
            }
        }
    }
}
