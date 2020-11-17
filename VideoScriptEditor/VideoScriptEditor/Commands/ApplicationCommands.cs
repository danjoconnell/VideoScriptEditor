using Prism.Commands;

namespace VideoScriptEditor.Commands
{
    /// <summary>
    /// Provides a common set of application related commands.
    /// </summary>
    public class ApplicationCommands : IApplicationCommands
    {
        /// <inheritdoc cref="IApplicationCommands.RedoCommand"/>
        public CompositeCommand RedoCommand { get; } = new CompositeCommand();

        /// <inheritdoc cref="IApplicationCommands.UndoCommand"/>
        public CompositeCommand UndoCommand { get; } = new CompositeCommand();
    }
}
