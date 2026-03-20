using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Game;
using Server;

namespace WonderlandServerWpf.Services
{
    public interface IServerEngine
    {
        void StartAsync();
        void Shutdown();

        bool IsRunning { get; }
        BindingList<ImMallItem> MallItems { get; }
        ImMallManager MallManager { get; }

        List<Player> GetOnlinePlayers();
        DataTable GetAllCharacterSummaries();
        int OnlineCount { get; }
        int MapCount { get; }
    }
}
