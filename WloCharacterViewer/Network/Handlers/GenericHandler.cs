using System;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.Network.Handlers
{
    /// <summary>
    /// Catch-all handler that logs unhandled packets for debugging.
    /// </summary>
    public class GenericHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                int ptr = reader.GetPtr();
                int remaining = reader.Remaining;
                byte[] preview = remaining > 0 ? reader.UnpackBytes(Math.Min(remaining, 32)) : Array.Empty<byte>();
                reader.SetPtr(ptr); // restore pointer

                session.Log($"[Generic] ptr={ptr} remaining={remaining} data={BitConverter.ToString(preview)}");
            }
            catch (Exception ex)
            {
                session.Log($"[GenericHandler] Error: {ex.Message}");
            }
        }
    }
}
