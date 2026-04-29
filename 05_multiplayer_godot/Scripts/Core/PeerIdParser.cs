using System.Globalization;

namespace SwarmSurvivor;

public static class PeerIdParser
{
    public const int InvalidPeerId = 0;

    public static bool TryParseFromName(string name, out int peerId)
    {
        if (string.IsNullOrEmpty(name))
        {
            peerId = InvalidPeerId;
            return false;
        }

        if (int.TryParse(name, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
        {
            peerId = parsed;
            return true;
        }

        peerId = InvalidPeerId;
        return false;
    }
}
