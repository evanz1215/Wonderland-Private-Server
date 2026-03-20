using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GupdtSrv;
using DataFiles;
using System.Reflection;
using Wonderland_Private_Server.Config;
using System.Collections.Concurrent;
using Server;
using Server.System;
using System.Diagnostics;

namespace System
{
    public static class cGlobal
    {

        
        public static bool Run;

        public static string SrvVersion { get { return new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion).ToString(); } }
        public static Server.Config.Settings SrvSettings;

        public static DataBase.CharacterDataBase gCharacterDataBase;
        public static DataBase.UserDataBase gUserDataBase;
        public static DataBase.GameDataBase gGameDataBase;

        public static DataFiles.PhxItemDat ItemDatManager;
        public static DataFiles.PhxNpcDat gNpcManager;
        public static Wonderland_Private_Server.DataManagement.DataFiles.SkillDataFile gSkillManager;
        public static Server.DataFiles.EveNpcMapper gEveNpcMapper;

        public static LoginServer gLoginServer;
        public static WorldServer gWorld;
        public static ImMallManager gImMallManager = new ImMallManager();
        public static LotteryManager gLotteryManager = new LotteryManager();

        //public static Server.WloWorldNode WLO_World;
        //public static Game.Maps.MapManager gMapManager;


        #region Systems
        public static TaskManager ApplicationTasks;
        public static UpdateSystem Update_System;
        public static Game.InstanceSystem gInstanceSystem = new Game.InstanceSystem();
        public static Game.GuildSystem gGuildSystem = new Game.GuildSystem();
        public static Server.Events.WorldEventSystem gWorldEvents = new Server.Events.WorldEventSystem();
        public static Game.QuestTemplateManager gQuestTemplates = new Game.QuestTemplateManager();

        #endregion

        #region Settings
        #endregion


        
    }
}
