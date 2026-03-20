using System;
using System.Collections.Generic;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.Network
{
    public class PacketDispatcher
    {
        private readonly Dictionary<(byte ac, byte? subAc), IPacketHandler> _handlers = new();
        private readonly Dictionary<byte, IPacketHandler> _acOnlyHandlers = new();

        public event Action<string>? OnLog;

        public void Register(byte ac, byte? subAc, IPacketHandler handler)
        {
            _handlers[(ac, subAc)] = handler;
        }

        public void Register(byte ac, IPacketHandler handler)
        {
            _acOnlyHandlers[ac] = handler;
        }

        public void Dispatch(byte[] rawPacket, GameSession session)
        {
            if (rawPacket.Length < 5) return;

            var reader = new PacketReader(rawPacket);
            byte ac = reader.Unpack8();
            byte? subAc = rawPacket.Length > 5 ? reader.Unpack8() : null;

            // Try exact match first (ac + subAc)
            if (subAc.HasValue && _handlers.TryGetValue((ac, subAc), out var exactHandler))
            {
                try
                {
                    exactHandler.Handle(reader, session);
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"[Dispatcher] AC{ac}.{subAc} handler error: {ex.Message}");
                    ClientLogger.Log($"[Dispatcher] AC{ac}.{subAc} EXCEPTION: {ex}");
                }
                return;
            }

            // Try AC-only match
            if (_acOnlyHandlers.TryGetValue(ac, out var acHandler))
            {
                // Reset reader to after AC byte so handler can read subAc itself
                reader.SetPtr(5);
                try
                {
                    acHandler.Handle(reader, session);
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"[Dispatcher] AC{ac} handler error: {ex.Message}");
                    ClientLogger.Log($"[Dispatcher] AC{ac} EXCEPTION: {ex}");
                }
                return;
            }

            // Unhandled — log it
            var hexPreview = BitConverter.ToString(rawPacket, 0, Math.Min(rawPacket.Length, 40));
            ClientLogger.Log($"[Dispatcher] Unhandled AC={ac} SubAC={subAc} len={rawPacket.Length}: {hexPreview}");
        }
    }
}
