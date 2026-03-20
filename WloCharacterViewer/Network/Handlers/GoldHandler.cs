using System;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.Network.Handlers
{
    /// <summary>
    /// Handles AC 26 sub 4 (gold update).
    /// Format: [gold:32]
    /// </summary>
    public class GoldHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                uint gold = reader.Unpack32();
                session.Character.Gold = gold;
                session.Log($"[Gold] {gold}");
            }
            catch (Exception ex)
            {
                session.Log($"[GoldHandler] Parse error: {ex.Message}");
            }
        }
    }
}
