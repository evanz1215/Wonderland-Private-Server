using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using Game;
using Plugin;
using Server;

namespace WonderlandServerWpf.Services
{
    public class ServerEngine : IServerEngine
    {
        readonly ILogService _log;
        PluginManager _pluginManager;

        public bool IsRunning { get { return cGlobal.Run; } }
        public BindingList<ImMallItem> MallItems { get { return cGlobal.gImMallManager.Items; } }
        public ImMallManager MallManager { get { return cGlobal.gImMallManager; } }

        public List<Player> GetOnlinePlayers()
        {
            if (cGlobal.gWorld == null) return new List<Player>();
            return cGlobal.gWorld.GetAllOnlinePlayers();
        }

        public DataTable GetAllCharacterSummaries()
        {
            if (cGlobal.gCharacterDataBase == null) return null;
            return cGlobal.gCharacterDataBase.GetAllCharacterSummaries();
        }

        public int OnlineCount
        {
            get { return cGlobal.gWorld != null ? cGlobal.gWorld.OnlineCount : 0; }
        }

        public int MapCount
        {
            get { return cGlobal.gWorld != null ? cGlobal.gWorld.MapCount : 0; }
        }

        public ServerEngine(ILogService log)
        {
            _log = log;
        }

        public void StartAsync()
        {
            var thread = new Thread(MainThreadWork);
            thread.IsBackground = true;
            thread.Init();
        }

        public void Shutdown()
        {
            cGlobal.Run = false;
            // Stop the WinForms message pump so the STA thread can exit
            try { System.Windows.Forms.Application.ExitThread(); } catch { }
        }

        void MainThreadWork()
        {
            cGlobal.Run = true;
            // Data files live in the main project's bin\Debug
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string mainBinDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\bin\Debug"));
            if (Directory.Exists(Path.Combine(mainBinDir, "Data")))
                Environment.CurrentDirectory = mainBinDir;
            else
                Environment.CurrentDirectory = baseDir;

            // Create a dedicated STA thread with a WinForms message pump for DebugSystem's RichTextBox.
            // DebugSystem.Write uses Control.Invoke, which requires the control's owner thread
            // to have an active message pump.
            var rtbReady = new ManualResetEventSlim(false);
            var rtbThread = new Thread(() =>
            {
                var rtb = new System.Windows.Forms.RichTextBox();
                rtb.TextChanged += (s, ev) =>
                {
                    var tb = (System.Windows.Forms.RichTextBox)s;
                    string text = tb.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            string lastLine = lines[lines.Length - 1].Trim();
                            if (!string.IsNullOrEmpty(lastLine))
                                _log.WriteMain(lastLine);
                        }
                    }
                };
                // Force handle creation so Invoke works
                var handle = rtb.Handle;
                // Set DebugSystem's rtfbox via reflection
                var rtfField = typeof(DebugSystem).GetField("rtfbox", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (rtfField != null) rtfField.SetValue(null, rtb);
                rtbReady.Set();
                System.Windows.Forms.Application.Run();
            });
            rtbThread.IsBackground = true;
            rtbThread.SetApartmentState(ApartmentState.STA);
            rtbThread.Start();
            rtbReady.Wait();

            // Initialize DebugSystem file logging (no control param) from background thread
            DebugSystem.Initialize(true);
            DebugSystem.VerboseLvl = 1;

            _log.WriteMain("[Init] - Initializing DataFile Objects");
            cGlobal.ItemDatManager = new DataFiles.PhxItemDat();
            cGlobal.ItemDatManager.Load(Environment.CurrentDirectory + "\\Data\\itemDat.wpdat");

            _log.WriteMain("[Init] - Initializing DataBase Objects");
            cGlobal.gUserDataBase = new DataBase.UserDataBase();
            cGlobal.gCharacterDataBase = new DataBase.CharacterDataBase();
            cGlobal.gCharacterDataBase.ItemDat = cGlobal.ItemDatManager;
            cGlobal.gGameDataBase = new DataBase.GameDataBase();
            cGlobal.gGameDataBase.ItemDat = cGlobal.ItemDatManager;

            _log.WriteMain("[Init] - Initializing Systems Please Wait.....");
            cGlobal.ApplicationTasks = new Server.TaskManager();
            cGlobal.Update_System = new Server.System.UpdateSystem();
            _pluginManager = new PluginManager();
            _pluginManager.Intialize();

            cGlobal.gLoginServer = new Server.LoginServer();
            cGlobal.gWorld = new Server.WorldServer(_pluginManager);
            cGlobal.gSkillManager = new Wonderland_Private_Server.DataManagement.DataFiles.SkillDataFile();
            cGlobal.gNpcManager = new DataFiles.PhxNpcDat();
            cGlobal.SrvSettings = new Server.Config.Settings();

            #region Load Settings
            _log.WriteMain("Loading Settings File");
            string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PServer\\Config.settings.wlo";
            if (File.Exists(settingsPath))
            {
                var serializer = new XmlSerializer(typeof(Server.Config.Settings));
                try
                {
                    using (var file = new StreamReader(settingsPath))
                        cGlobal.SrvSettings = (Server.Config.Settings)serializer.Deserialize(file);
                    _log.WriteMain("Settings File loaded successfully");
                }
                catch { _log.WriteMain("Settings File failed to load"); }
            }
            else
            {
                _log.WriteMain("Settings File not found");
            }

            if (!string.IsNullOrEmpty(cGlobal.SrvSettings.DB.TableName_Ref))
                cGlobal.gUserDataBase.TableName = cGlobal.SrvSettings.DB.TableName_Ref;
            if (!string.IsNullOrEmpty(cGlobal.SrvSettings.DB.Username_Ref))
                cGlobal.gUserDataBase.Username_Ref = cGlobal.SrvSettings.DB.Username_Ref;
            if (!string.IsNullOrEmpty(cGlobal.SrvSettings.DB.Password_Ref))
                cGlobal.gUserDataBase.Password_Ref = cGlobal.SrvSettings.DB.Password_Ref;
            cGlobal.gUserDataBase.DataBaseID_Ref = cGlobal.SrvSettings.DB.UserID_Ref;
            cGlobal.gUserDataBase.IM_Ref = cGlobal.SrvSettings.DB.IM_Ref;
            cGlobal.gUserDataBase.CharacterID1_Ref = cGlobal.SrvSettings.DB.CharacterID1_Ref;
            cGlobal.gUserDataBase.CharacterID2_Ref = cGlobal.SrvSettings.DB.CharacterID2_Ref;
            cGlobal.gUserDataBase.Char_Delete_Code_Ref = cGlobal.SrvSettings.DB.Char_Delete_Code_Ref;
            cGlobal.gUserDataBase.PassVerification = (Game.VerifyPassType)cGlobal.SrvSettings.DB.PassVerification;

            // Configure DB connection
            if (!string.IsNullOrEmpty(cGlobal.SrvSettings.DB.ServerIP))
            {
                var dbType = typeof(RCLibrary.Core.DataBase);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                foreach (RCLibrary.Core.DataBase db in new RCLibrary.Core.DataBase[] { cGlobal.gUserDataBase, cGlobal.gCharacterDataBase, cGlobal.gGameDataBase })
                {
                    dbType.GetField("ServerIP", flags).SetValue(db, cGlobal.SrvSettings.DB.ServerIP);
                    dbType.GetField("Port", flags).SetValue(db, cGlobal.SrvSettings.DB.Port.ToString());
                    dbType.GetField("DB", flags).SetValue(db, cGlobal.SrvSettings.DB.DataBase);
                    dbType.GetField("User", flags).SetValue(db, cGlobal.SrvSettings.DB.User);
                    dbType.GetField("Pass", flags).SetValue(db, cGlobal.SrvSettings.DB.Pass);
                    dbType.GetField("ServType", flags).SetValue(db, cGlobal.SrvSettings.DB.Server_Type);
                }
                _log.WriteMain(string.Format("[DB] Configured: {0}:{1} db={2} user={3}",
                    cGlobal.SrvSettings.DB.ServerIP, cGlobal.SrvSettings.DB.Port,
                    cGlobal.SrvSettings.DB.DataBase, cGlobal.SrvSettings.DB.User));
            }
            #endregion

            #region Database Init
            _log.WriteMain("Testing Connection to UserDatabase");
            try
            {
                if (cGlobal.gUserDataBase.TestConnection())
                    _log.WriteMain("Connection Successful");
                else
                    _log.WriteMain("Connection not successful\r\n unable to authenticate users connecting to server");

                _log.WriteMain("Testing Connection to Character Database");
                if (cGlobal.gCharacterDataBase.TestConnection())
                {
                    _log.WriteMain("Connection Successful");
                    _log.WriteMain("Verifying DataBase Tables");
                    cGlobal.gCharacterDataBase.VerifySetup();
                }
                else
                    _log.WriteMain("Connection not successful\r\n unable to create necessary tables for the server");
            }
            catch (Exception e) { _log.WriteMain("[Error] " + e.Message); }
            #endregion

            #region Load Data Files
            cGlobal.gSkillManager.LoadSkills("Data\\Skill.dat");
            cGlobal.gNpcManager.onDebug = (obj) => { /* skip DebugSystem to avoid deadlock */ };
            _log.WriteMain("[Init] - Loading Npc.dat...");
            var npcLoadResult = cGlobal.gNpcManager.Load("Data\\Npc.dat");
            if (!npcLoadResult.Wait(60000))
                _log.WriteMain("[WARN] NPC loading timed out after 60s, continuing without NPCs");
            else
                _log.WriteMain(string.Format("Npc.dat loaded: {0} NPCs, success={1}", cGlobal.gNpcManager.NpcList.Count, npcLoadResult.Result));

            if (cGlobal.gNpcManager.NpcList.Count > 0)
            {
                Server.DataFiles.NpcDecoder.DecodeAllNpcs(cGlobal.gNpcManager);
                _log.WriteMain(string.Format("NpcDecoder: decoded {0} NPCs", cGlobal.gNpcManager.NpcList.Count));
            }

            cGlobal.gEveNpcMapper = new Server.DataFiles.EveNpcMapper();
            cGlobal.gEveNpcMapper.Load("Data\\eve.Emg");
            #endregion

            _log.WriteMain("[Init] - Initializing Server Please Wait.....");

            #region Server Components
            cGlobal.gWorld.Initialize();
            cGlobal.gLoginServer.Initialize();

            int questCount = cGlobal.gQuestTemplates.LoadFromFile("Data\\quests.txt");
            if (questCount > 0)
                _log.WriteMain("[Init] - Loaded " + questCount + " quest templates");

            global::Server.System.GameLogger.Initialize("logs");
            Network.PacketLogger.Initialize("logs");

            // Item Mall - must run on WPF Dispatcher because BindingList is bound to DataGrid
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                cGlobal.gImMallManager.Load();
                if (cGlobal.gImMallManager.Items.Count == 0)
                {
                    cGlobal.gImMallManager.AutoPopulate(cGlobal.ItemDatManager);
                    cGlobal.gImMallManager.Save();
                }
            });

            // Initialize Lottery
            cGlobal.gLotteryManager.Load();
            if (cGlobal.gLotteryManager.GoldPrizes.Count == 0 && cGlobal.gLotteryManager.ImPrizes.Count == 0)
            {
                cGlobal.gLotteryManager.AutoPopulate(cGlobal.ItemDatManager);
                cGlobal.gLotteryManager.Save();
            }
            #endregion

            _log.WriteMain("[Init] - Server Started Successfully");

            // Main loop
            do
            {
                cGlobal.ApplicationTasks.onUpdateTick();
                Thread.Sleep(10);
            }
            while (cGlobal.Run);
        }
    }
}
