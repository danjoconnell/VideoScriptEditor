using MonitoredUndo;
using Prism.Commands;

namespace VideoScriptEditor.Commands
{
    /// <summary>
    /// Interface abstracting a common set of application related commands.
    /// </summary>
    public interface IApplicationCommands
    {
        /// <summary>
        /// Command for redoing <see cref="ChangeSet"/>s up to and including a specific <see cref="ChangeSet"/>.
        /// </summary>
        CompositeCommand RedoCommand { get; }

        /// <summary>
        /// Command for undoing all <see cref="ChangeSet"/>s up to and including a specific <see cref="ChangeSet"/>.
        /// </summary>
        CompositeCommand UndoCommand { get; }
    }
}