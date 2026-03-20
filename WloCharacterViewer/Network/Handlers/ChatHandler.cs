using System;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.Network.Handlers
{
    /// <summary>
    /// Handles AC 2 sub 1 (Whisper). Format: [senderCharID:32][senderName:stringN][message:stringN]
    /// </summary>
    public class ChatWhisperHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                uint senderId = reader.Unpack32();
                string senderName = reader.UnpackStringN();
                string message = reader.UnpackStringN();

                session.ChatMessages.Add(new ChatMessage
                {
                    Channel = ChatChannel.Whisper,
                    Sender = senderName,
                    Content = message
                });
            }
            catch (Exception ex)
            {
                session.Log($"[ChatWhisperHandler] Parse error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles AC 2 sub 2 (Local). Format: [senderCharID:32][message:stringN]
    /// </summary>
    public class ChatLocalHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                uint senderId = reader.Unpack32();
                string message = reader.UnpackStringN();

                session.ChatMessages.Add(new ChatMessage
                {
                    Channel = ChatChannel.Local,
                    Sender = senderId.ToString(),
                    Content = message
                });
            }
            catch (Exception ex)
            {
                session.Log($"[ChatLocalHandler] Parse error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles AC 2 sub 3 (Party/Team). Format: [senderCharID:32][message:stringN]
    /// </summary>
    public class ChatPartyHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                uint senderId = reader.Unpack32();
                string message = reader.UnpackStringN();

                session.ChatMessages.Add(new ChatMessage
                {
                    Channel = ChatChannel.Party,
                    Sender = senderId.ToString(),
                    Content = message
                });
            }
            catch (Exception ex)
            {
                session.Log($"[ChatPartyHandler] Parse error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles AC 2 sub 5 (World). Format: [senderCharID:32][message:stringN]
    /// </summary>
    public class ChatWorldHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                uint senderId = reader.Unpack32();
                string message = reader.UnpackStringN();

                session.ChatMessages.Add(new ChatMessage
                {
                    Channel = ChatChannel.World,
                    Sender = senderId.ToString(),
                    Content = message
                });
            }
            catch (Exception ex)
            {
                session.Log($"[ChatWorldHandler] Parse error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles AC 2 sub 6 (System). Format: [message:stringN]
    /// </summary>
    public class ChatSystemHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                string message = reader.UnpackStringN();

                session.ChatMessages.Add(new ChatMessage
                {
                    Channel = ChatChannel.System,
                    Sender = "",
                    Content = message
                });
            }
            catch (Exception ex)
            {
                session.Log($"[ChatSystemHandler] Parse error: {ex.Message}");
            }
        }
    }
}
