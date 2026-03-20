using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using WloCharacterViewer.Models;
using WloCharacterViewer.Network;

namespace WloCharacterViewer.Automation
{
    public class Waypoint
    {
        public ushort MapId { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public override string ToString() => $"Map {MapId} ({X}, {Y})";
    }

    public class AutoWalkEngine : INotifyPropertyChanged
    {
        private GameSession? _session;
        private CancellationTokenSource? _cts;
        private static readonly object _wpLock = new();

        public ObservableCollection<Waypoint> Waypoints { get; } = new();

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                Notify();
                if (value) StartWalking();
                else StopWalking();
            }
        }

        private bool _loop;
        public bool Loop { get => _loop; set { _loop = value; Notify(); } }

        private int _currentIndex;
        public int CurrentIndex { get => _currentIndex; set { _currentIndex = value; Notify(); } }

        public AutoWalkEngine()
        {
            BindingOperations.EnableCollectionSynchronization(Waypoints, _wpLock);
        }

        public void Attach(GameSession session)
        {
            _session = session;
        }

        public void Detach()
        {
            StopWalking();
            _session = null;
        }

        private void StartWalking()
        {
            if (_session == null || Waypoints.Count == 0) return;
            _cts = new CancellationTokenSource();
            _ = WalkLoopAsync(_cts.Token);
        }

        private void StopWalking()
        {
            _cts?.Cancel();
        }

        private async Task WalkLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _session != null)
                {
                    if (CurrentIndex >= Waypoints.Count)
                    {
                        if (Loop)
                            CurrentIndex = 0;
                        else
                        {
                            Enabled = false;
                            break;
                        }
                    }

                    var wp = Waypoints[CurrentIndex];
                    var map = _session.Map;

                    // If on correct map, walk toward waypoint
                    if (map.MapId == wp.MapId || wp.MapId == 0)
                    {
                        int dx = wp.X - map.X;
                        int dy = wp.Y - map.Y;

                        if (Math.Abs(dx) <= 2 && Math.Abs(dy) <= 2)
                        {
                            // Arrived at waypoint
                            _session.Log($"[Walk] Reached waypoint {CurrentIndex}: {wp}");
                            CurrentIndex++;
                            continue;
                        }

                        byte direction = CalculateDirection(dx, dy);
                        ushort nextX = (ushort)(map.X + Math.Sign(dx) * Math.Min(Math.Abs(dx), 10));
                        ushort nextY = (ushort)(map.Y + Math.Sign(dy) * Math.Min(Math.Abs(dy), 10));

                        SendMovement(direction, nextX, nextY);
                    }

                    await Task.Delay(500, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _session?.Log($"[Walk] Error: {ex.Message}");
            }
        }

        private void SendMovement(byte direction, ushort x, ushort y)
        {
            if (_session == null) return;

            // AC 6 sub 1: [direction:8][x:16][y:16]
            var pw = new PacketWriter();
            pw.Pack8(6);
            pw.Pack8(1);
            pw.Pack8(direction);
            pw.Pack16(x);
            pw.Pack16(y);
            _session.SendPacket(pw.Build());
        }

        private static byte CalculateDirection(int dx, int dy)
        {
            // 8 directions: 0=up, 1=up-right, 2=right, 3=down-right, 4=down, 5=down-left, 6=left, 7=up-left
            if (dx == 0 && dy < 0) return 0;
            if (dx > 0 && dy < 0) return 1;
            if (dx > 0 && dy == 0) return 2;
            if (dx > 0 && dy > 0) return 3;
            if (dx == 0 && dy > 0) return 4;
            if (dx < 0 && dy > 0) return 5;
            if (dx < 0 && dy == 0) return 6;
            if (dx < 0 && dy < 0) return 7;
            return 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
