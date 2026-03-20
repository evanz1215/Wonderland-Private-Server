using System;
using System.IO;

namespace WloCharacterViewer.Network
{
    public static class ClientLogger
    {
        private static readonly object _lock = new();
        private static string? _logPath;

        public static void Initialize()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(dir);
            _logPath = Path.Combine(dir, $"client_{DateTime.Now:yyyy-MM-dd_HHmmss}.log");
            Log("=== WloCharacterViewer 啟動 ===");
        }

        public static void Log(string message)
        {
            if (_logPath == null) return;
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            lock (_lock)
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }

        public static void LogHex(string prefix, byte[] data, int maxLen = 500)
        {
            var hex = BitConverter.ToString(data, 0, Math.Min(data.Length, maxLen));
            Log($"{prefix} ({data.Length} bytes): {hex}");
        }

        public static string? LogPath => _logPath;
    }
}
