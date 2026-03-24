using ILSpyX.Backend.Model;

namespace ILSpyX.Backend.TreeProviders;

public static class NodeCommandsExtensions
{
    public static bool HasCommand(this Node node, AvailableNodeCommands command)
    {
        return (node.Metadata?.AvailableCommands ?? AvailableNodeCommands.None)
            .HasFlag(command);
    }
}