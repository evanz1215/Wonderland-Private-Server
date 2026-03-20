using System;
using System.Collections.Generic;
using System.Text;

namespace WloCharacterViewer.Network
{
    /// <summary>
    /// XOR cipher used by RCLibrary Client3 for all packet communication.
    /// Both send and receive are XOR'd with this key.
    /// </summary>
    public static class PacketCipher
    {
        public const byte XorKey = 0xAD; // 173

        public static byte[] Encode(byte[] data)
        {
            var result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                result[i] = (byte)(data[i] ^ XorKey);
            return result;
        }

        public static void EncodeInPlace(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] ^= XorKey;
        }
    }

    /// <summary>
    /// Builds packets matching the WLO server protocol.
    /// Format: [Header:0x44F4 LE][Length:u16 LE][Data...]
    /// All bytes are XOR'd with 0xAD before sending.
    /// </summary>
    public class PacketWriter
    {
        private readonly List<byte> _data = new();
        private const ushort Header = 0x44F4;

        public void Pack8(byte val) => _data.Add(val);
        public void Pack16(ushort val) => _data.AddRange(BitConverter.GetBytes(val));
        public void Pack32(uint val) => _data.AddRange(BitConverter.GetBytes(val));

        public void PackString(string val)
        {
            _data.Add((byte)val.Length);
            _data.AddRange(Encoding.ASCII.GetBytes(val));
        }

        public void PackArray(byte[] val) => _data.AddRange(val);

        public void PackStringN(string val)
        {
            Pack16((ushort)val.Length);
            _data.AddRange(Encoding.ASCII.GetBytes(val));
        }

        /// <summary>
        /// Build the raw (unencrypted) packet.
        /// </summary>
        public byte[] BuildRaw()
        {
            var header = BitConverter.GetBytes(Header);
            var length = BitConverter.GetBytes((ushort)_data.Count);
            var result = new byte[4 + _data.Count];
            header.CopyTo(result, 0);
            length.CopyTo(result, 2);
            _data.CopyTo(result, 4);
            return result;
        }

        /// <summary>
        /// Build the packet with XOR encryption applied (ready to send).
        /// </summary>
        public byte[] Build()
        {
            return PacketCipher.Encode(BuildRaw());
        }
    }

    /// <summary>
    /// Reads packets received from the WLO server (after XOR decryption).
    /// </summary>
    public class PacketReader
    {
        private readonly byte[] _buffer;
        private int _ptr;

        public PacketReader(byte[] buffer)
        {
            _buffer = buffer;
            _ptr = 4; // Skip header (2) + length (2)
        }

        public int Remaining => _buffer.Length - _ptr;
        public byte ActionCode => _buffer.Length > 4 ? _buffer[4] : (byte)0;
        public byte? SubAction => _buffer.Length > 5 ? _buffer[5] : null;

        public byte Unpack8()
        {
            return _buffer[_ptr++];
        }

        public ushort Unpack16()
        {
            var val = BitConverter.ToUInt16(_buffer, _ptr);
            _ptr += 2;
            return val;
        }

        public uint Unpack32()
        {
            var val = BitConverter.ToUInt32(_buffer, _ptr);
            _ptr += 4;
            return val;
        }

        public bool UnpackBool()
        {
            return _buffer[_ptr++] != 0;
        }

        public string UnpackString()
        {
            byte len = _buffer[_ptr++];
            var val = Encoding.ASCII.GetString(_buffer, _ptr, len);
            _ptr += len;
            return val;
        }

        public string UnpackStringN()
        {
            ushort len = Unpack16();
            var val = Encoding.ASCII.GetString(_buffer, _ptr, len);
            _ptr += len;
            return val;
        }

        public ulong Unpack64()
        {
            var val = BitConverter.ToUInt64(_buffer, _ptr);
            _ptr += 8;
            return val;
        }

        public byte[] UnpackBytes(int count)
        {
            var val = new byte[count];
            Array.Copy(_buffer, _ptr, val, 0, count);
            _ptr += count;
            return val;
        }

        public int GetPtr() => _ptr;
        public void SetPtr(int ptr) => _ptr = ptr;
    }
}
