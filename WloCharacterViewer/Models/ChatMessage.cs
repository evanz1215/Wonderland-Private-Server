using System;

namespace WloCharacterViewer.Models
{
    public class ChatMessage
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public ChatChannel Channel { get; set; }
        public string Sender { get; set; } = "";
        public string Content { get; set; } = "";

        public string Display => Channel switch
        {
            ChatChannel.Whisper => $"[密語] {Sender}: {Content}",
            ChatChannel.Local => $"[本地] {Sender}: {Content}",
            ChatChannel.Party => $"[隊伍] {Sender}: {Content}",
            ChatChannel.World => $"[世界] {Sender}: {Content}",
            ChatChannel.System => $"[系統] {Content}",
            _ => $"[{Channel}] {Sender}: {Content}"
        };
    }

    public enum ChatChannel : byte
    {
        Whisper = 1,
        Local = 2,
        Party = 3,
        World = 5,
        System = 6
    }
}
