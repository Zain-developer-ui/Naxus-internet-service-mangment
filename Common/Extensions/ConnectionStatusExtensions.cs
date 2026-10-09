using NEXUS.Common.Constants;

namespace NEXUS.Common.Extensions;

/**
 * Which connection status moves are allowed. Kept in one place because both the
 * manual status change on the operations screen and the automatic sweep that
 * follows overdue bills have to agree - a line the policy suspends must be a
 * line the officer could have suspended by hand.
 */
public static class ConnectionStatusExtensions
{
    public static bool IsLegalMove(this ConnectionStatus from, ConnectionStatus to) => (from, to) switch
    {
        // A new line goes live.
        (ConnectionStatus.Pending, ConnectionStatus.Active) => true,

        // A line can be suspended by either party, temporarily or for good.
        (ConnectionStatus.Pending, ConnectionStatus.TemporarilyInactive) => true,
        (ConnectionStatus.Pending, ConnectionStatus.PermanentlyInactive) => true,
        (ConnectionStatus.Active, ConnectionStatus.TemporarilyInactive) => true,
        (ConnectionStatus.Active, ConnectionStatus.PermanentlyInactive) => true,

        // Only a temporary suspension can be reversed.
        (ConnectionStatus.TemporarilyInactive, ConnectionStatus.Active) => true,
        (ConnectionStatus.TemporarilyInactive, ConnectionStatus.PermanentlyInactive) => true,

        _ => false
    };

    /** A line that is carrying traffic - the only kind a bill can suspend. */
    public static bool IsLive(this ConnectionStatus status) =>
        status is ConnectionStatus.Active or ConnectionStatus.TemporarilyInactive;
}
