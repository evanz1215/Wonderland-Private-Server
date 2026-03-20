using System;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.Network.Handlers
{
    /// <summary>
    /// Handles AC 3 (character appear on map).
    /// Registered as AC-only handler. Dispatcher sets ptr=5.
    /// Format from ptr=5: [charID:32][body:8][mapId:16][x:16][y:16]...
    /// </summary>
    public class CharacterDataHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                uint charId = reader.Unpack32();
                byte body = reader.Unpack8();
                ushort mapId = reader.Unpack16();
                ushort x = reader.Unpack16();
                ushort y = reader.Unpack16();

                // Update character appearance
                session.Character.Body = body;
                session.Character.MapId = mapId;
                session.Character.X = x;
                session.Character.Y = y;

                // Sync map state
                session.Map.MapId = mapId;
                session.Map.X = x;
                session.Map.Y = y;

                session.Log($"[CharData] CharID={charId} Body={body} Map={mapId} Pos=({x},{y})");
            }
            catch (Exception ex)
            {
                session.Log($"[CharacterDataHandler] Parse error: {ex.Message}");
            }
        }
    }
}
