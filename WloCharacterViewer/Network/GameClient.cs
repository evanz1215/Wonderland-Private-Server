using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.Network
{
    public class GameClient : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        public event Action<string>? OnLog;

        public bool IsConnected => _client?.Connected ?? false;
        private readonly object _sendLock = new();

        private void Log(string msg)
        {
            ClientLogger.Log(msg);
            OnLog?.Invoke(msg);
        }

        public async Task ConnectAsync(string host, int port)
        {
            _client = new TcpClient();
            _client.ReceiveBufferSize = 65536;
            _client.NoDelay = true;
            Log($"正在連線到 {host}:{port}...");
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            _cts = new CancellationTokenSource();
            Log($"TCP 連線成功");
        }

        public void Send(byte[] xorEncodedPacket)
        {
            lock (_sendLock)
            {
                var raw = PacketCipher.Encode(xorEncodedPacket);
                ClientLogger.Log($">>> SEND raw ({raw.Length} bytes): {BitConverter.ToString(raw)}");
                ClientLogger.Log($">>> SEND xor ({xorEncodedPacket.Length} bytes): {BitConverter.ToString(xorEncodedPacket)}");
                Log($">>> 發送 {xorEncodedPacket.Length} bytes");
                _stream!.Write(xorEncodedPacket, 0, xorEncodedPacket.Length);
                _stream.Flush();
            }
        }

        public async Task<int> ReadRawAsync(byte[] buffer, CancellationToken ct)
        {
            return await _stream!.ReadAsync(buffer, 0, buffer.Length, ct);
        }

        public async Task<List<byte[]>> ReceivePacketsAsync(int timeoutMs = 5000)
        {
            var packets = new List<byte[]>();
            var accumulated = new List<byte>();
            var buffer = new byte[65536];

            using var cts = new CancellationTokenSource(timeoutMs);

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    int bytesRead;
                    try
                    {
                        bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Log("<<< 讀取超時");
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        Log("<<< 連線已關閉 (read 0 bytes)");
                        break;
                    }

                    // Log raw received bytes (before XOR decode)
                    ClientLogger.Log($"<<< RECV raw ({bytesRead} bytes): {BitConverter.ToString(buffer, 0, Math.Min(bytesRead, 300))}");

                    // XOR decode received data
                    var decoded = new byte[bytesRead];
                    Array.Copy(buffer, decoded, bytesRead);
                    PacketCipher.EncodeInPlace(decoded);
                    ClientLogger.Log($"<<< RECV dec ({bytesRead} bytes): {BitConverter.ToString(decoded, 0, Math.Min(bytesRead, 300))}");
                    Log($"<<< 收到 {bytesRead} bytes");

                    for (int i = 0; i < bytesRead; i++)
                        accumulated.Add(decoded[i]);

                    // Extract complete packets from accumulated buffer
                    bool extracted = true;
                    while (extracted && accumulated.Count >= 4)
                    {
                        extracted = false;
                        var arr = accumulated.ToArray();
                        ushort header = BitConverter.ToUInt16(arr, 0);

                        if (header != 0x44F4)
                        {
                            Log($"    跳過非標準 header byte: 0x{accumulated[0]:X2}");
                            ClientLogger.Log($"    BAD HEADER at offset, byte=0x{accumulated[0]:X2}, accumulated={BitConverter.ToString(arr, 0, Math.Min(arr.Length, 20))}");
                            accumulated.RemoveAt(0);
                            extracted = true;
                            continue;
                        }

                        ushort length = BitConverter.ToUInt16(arr, 2);
                        int totalLen = 4 + length;

                        if (accumulated.Count >= totalLen)
                        {
                            var packet = accumulated.GetRange(0, totalLen).ToArray();
                            accumulated.RemoveRange(0, totalLen);
                            packets.Add(packet);
                            extracted = true;

                            byte ac = totalLen > 4 ? packet[4] : (byte)0;
                            byte? subAc = totalLen > 5 ? packet[5] : null;
                            Log($"    封包: AC={ac} SubAC={subAc} len={length}");
                            ClientLogger.Log($"    PACKET AC={ac} SubAC={subAc} len={length}: {BitConverter.ToString(packet, 0, Math.Min(packet.Length, 100))}");
                        }
                    }

                    // After getting login response packets, wait briefly for trailing packets
                    if (packets.Count > 0)
                    {
                        using var shortCts = new CancellationTokenSource(1000);
                        try
                        {
                            while (!shortCts.Token.IsCancellationRequested)
                            {
                                if (!_stream.DataAvailable)
                                {
                                    await Task.Delay(100, shortCts.Token);
                                    if (!_stream.DataAvailable)
                                        break;
                                }

                                bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, shortCts.Token);
                                if (bytesRead == 0) break;

                                var decoded2 = new byte[bytesRead];
                                Array.Copy(buffer, decoded2, bytesRead);
                                PacketCipher.EncodeInPlace(decoded2);
                                ClientLogger.Log($"<<< RECV trailing dec ({bytesRead} bytes): {BitConverter.ToString(decoded2, 0, Math.Min(bytesRead, 300))}");
                                Log($"<<< 追加 {bytesRead} bytes");
                                for (int i = 0; i < bytesRead; i++)
                                    accumulated.Add(decoded2[i]);

                                bool ex2 = true;
                                while (ex2 && accumulated.Count >= 4)
                                {
                                    ex2 = false;
                                    var arr2 = accumulated.ToArray();
                                    if (BitConverter.ToUInt16(arr2, 0) != 0x44F4)
                                    {
                                        accumulated.RemoveAt(0);
                                        ex2 = true;
                                        continue;
                                    }
                                    ushort len2 = BitConverter.ToUInt16(arr2, 2);
                                    int total2 = 4 + len2;
                                    if (accumulated.Count >= total2)
                                    {
                                        var pkt2 = accumulated.GetRange(0, total2).ToArray();
                                        accumulated.RemoveRange(0, total2);
                                        packets.Add(pkt2);
                                        ex2 = true;
                                        Log($"    封包: AC={pkt2[4]} SubAC={(total2 > 5 ? pkt2[5] : 0)} len={len2}");
                                        ClientLogger.Log($"    PACKET AC={pkt2[4]} SubAC={(total2 > 5 ? pkt2[5] : 0)} len={len2}: {BitConverter.ToString(pkt2, 0, Math.Min(pkt2.Length, 100))}");
                                    }
                                }
                            }
                        }
                        catch (OperationCanceledException) { }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"!!! 接收錯誤: {ex.GetType().Name}: {ex.Message}");
                ClientLogger.Log($"!!! EXCEPTION: {ex}");
            }

            Log($"共收到 {packets.Count} 個完整封包, 剩餘 {accumulated.Count} bytes 未解析");
            return packets;
        }

        public byte[] BuildLoginPacket(string username, string password)
        {
            var pw = new PacketWriter();
            pw.Pack8(63);    // AC
            pw.Pack8(4);     // SubAction = Recv4
            pw.Pack16(1096); // Version

            byte[] nameBytes = Encoding.ASCII.GetBytes(username.ToLower());
            pw.Pack8((byte)nameBytes.Length);
            pw.PackArray(nameBytes);

            byte[] passBytes = Encoding.ASCII.GetBytes(password);
            pw.Pack8((byte)passBytes.Length);
            pw.PackArray(passBytes);

            string loginCode = "wloclient";
            byte key = (byte)(new Random().Next(1, 255));
            byte[] lcBytes = Encoding.ASCII.GetBytes(loginCode);
            pw.Pack8((byte)lcBytes.Length);
            pw.Pack8(key);
            byte[] xored = new byte[lcBytes.Length];
            for (int i = 0; i < lcBytes.Length; i++)
                xored[i] = (byte)(lcBytes[i] ^ key);
            pw.PackArray(xored);

            return pw.Build();
        }

        public void SendLogin(string username, string password)
        {
            Log($"發送登入: user={username}");
            Send(BuildLoginPacket(username, password));
        }

        public void SendCharacterSelect(byte slot)
        {
            var pw = new PacketWriter();
            pw.Pack8(63);
            pw.Pack8(2);
            pw.Pack8(slot);
            Send(pw.Build());
        }

        public static CharacterInfo? ParseCharacterData(PacketReader reader)
        {
            try
            {
                if (reader.Remaining < 2)
                    return null;

                var info = new CharacterInfo();
                info.Slot = reader.Unpack8();

                if (info.Slot == 0 || info.Slot > 2)
                    return null;

                info.Name = reader.UnpackString();
                info.Level = reader.Unpack8();
                info.Element = reader.Unpack8();
                info.FullHP = reader.Unpack32();
                info.CurHP = reader.Unpack32();
                info.FullSP = reader.Unpack32();
                info.CurSP = reader.Unpack32();
                info.TotalExp = reader.Unpack32();
                info.Gold = reader.Unpack32();
                info.Body = reader.Unpack16();
                info.Head = reader.Unpack16();
                info.HairColor = reader.Unpack16();
                info.SkinColor = reader.Unpack16();
                info.ClothingColor = reader.Unpack16();
                info.EyeColor = reader.Unpack16();
                info.Reborn = reader.UnpackBool();
                info.Job = reader.Unpack8();

                for (int i = 0; i < 6; i++)
                    info.Equips[i] = reader.Unpack16();

                ClientLogger.Log($"    PARSED char: slot={info.Slot} name={info.Name} lv={info.Level}");
                return info;
            }
            catch (Exception ex)
            {
                ClientLogger.Log($"    PARSE ERROR: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _stream?.Dispose();
            _client?.Dispose();
        }
    }
}
