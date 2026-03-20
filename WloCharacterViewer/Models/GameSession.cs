using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using WloCharacterViewer.Network;

namespace WloCharacterViewer.Models
{
    public class GameSession : INotifyPropertyChanged, IDisposable
    {
        private readonly GameClient _client;
        private readonly PacketDispatcher _dispatcher;
        private CancellationTokenSource? _receiveCts;
        private static readonly object _chatLock = new();
        private static readonly object _packetLogLock = new();

        public GameClient Client => _client;
        public PacketDispatcher Dispatcher => _dispatcher;

        public CharacterState Character { get; } = new();
        public MapState Map { get; } = new();
        public InventoryState Inventory { get; } = new();
        public BattleState Battle { get; } = new();
        public ObservableCollection<ChatMessage> ChatMessages { get; } = new();
        public ObservableCollection<string> PacketLog { get; } = new();

        public uint UserId { get; set; }

        private bool _isInGame;
        public bool IsInGame { get => _isInGame; set { _isInGame = value; Notify(); } }

        private string _statusText = "";
        public string StatusText { get => _statusText; set { _statusText = value; Notify(); } }

        public event Action<string>? OnLog;

        public GameSession(GameClient client)
        {
            _client = client;
            _dispatcher = new PacketDispatcher();
            BindingOperations.EnableCollectionSynchronization(ChatMessages, _chatLock);
            BindingOperations.EnableCollectionSynchronization(PacketLog, _packetLogLock);
        }

        public void RegisterHandler(byte ac, byte? subAc, IPacketHandler handler)
        {
            if (subAc.HasValue)
                _dispatcher.Register(ac, subAc, handler);
            else
                _dispatcher.Register(ac, handler);
        }

        public void StartReceiveLoop()
        {
            _receiveCts = new CancellationTokenSource();
            _ = RunReceiveLoopAsync(_receiveCts.Token);
        }

        public void StopReceiveLoop()
        {
            _receiveCts?.Cancel();
        }

        private async Task RunReceiveLoopAsync(CancellationToken ct)
        {
            var accumulated = new System.Collections.Generic.List<byte>();
            var buffer = new byte[65536];

            try
            {
                while (!ct.IsCancellationRequested && _client.IsConnected)
                {
                    int bytesRead;
                    try
                    {
                        bytesRead = await _client.ReadRawAsync(buffer, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        Log("連線已關閉");
                        IsInGame = false;
                        break;
                    }

                    // XOR decode
                    var decoded = new byte[bytesRead];
                    Array.Copy(buffer, decoded, bytesRead);
                    PacketCipher.EncodeInPlace(decoded);

                    ClientLogger.Log($"<<< RECV dec ({bytesRead} bytes): {BitConverter.ToString(decoded, 0, Math.Min(bytesRead, 200))}");

                    for (int i = 0; i < bytesRead; i++)
                        accumulated.Add(decoded[i]);

                    // Extract and dispatch complete packets
                    bool extracted = true;
                    while (extracted && accumulated.Count >= 4)
                    {
                        extracted = false;
                        var arr = accumulated.ToArray();
                        ushort header = BitConverter.ToUInt16(arr, 0);

                        if (header != 0x44F4)
                        {
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
                            extracted = true;

                            byte ac = totalLen > 4 ? packet[4] : (byte)0;
                            byte? subAc = totalLen > 5 ? packet[5] : null;

                            var logLine = $"AC={ac} Sub={subAc} len={length}";
                            PacketLog.Add(logLine);
                            ClientLogger.Log($"    PACKET {logLine}: {BitConverter.ToString(packet, 0, Math.Min(packet.Length, 80))}");

                            _dispatcher.Dispatch(packet, this);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log($"接收迴圈錯誤: {ex.Message}");
                ClientLogger.Log($"[ReceiveLoop] EXCEPTION: {ex}");
                IsInGame = false;
            }
        }

        public void SendPacket(byte[] xorEncodedPacket)
        {
            _client.Send(xorEncodedPacket);
        }

        public void Log(string msg)
        {
            OnLog?.Invoke(msg);
            ClientLogger.Log($"[Session] {msg}");
        }

        public void Dispose()
        {
            StopReceiveLoop();
            _client.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
