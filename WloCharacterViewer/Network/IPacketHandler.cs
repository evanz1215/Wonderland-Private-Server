namespace WloCharacterViewer.Network
{
    public interface IPacketHandler
    {
        void Handle(PacketReader reader, Models.GameSession session);
    }
}
