using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Game;
using Network;


namespace Network.ActionCodes
{
    public class AC
    {
        protected readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public virtual int ID { get { return 0; } }
        public virtual void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                default: DebugSystem.Write("Action Code " + p.A + "," + p.B + "has not been coded"); break;
            }
        }

        static readonly object mlock = new object();
        static Dictionary<int, AC> AcList = new Dictionary<int, AC>(100);

        public static AC GetAction(int ID)
        {

            if (AcList.ContainsKey(ID))
                return AcList[ID];

            lock (mlock)
            {
                AC resp = null;
                if (resp == null)
                {
                    // Scan all loaded assemblies, not just the entry assembly.
                    // AC subclasses live in the main project assembly which may differ
                    // from the entry assembly (e.g. WPF host vs WinForms host).
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    var acTypes = new List<Type>();
                    foreach (var asm in assemblies)
                    {
                        try
                        {
                            foreach (var t in asm.GetTypes())
                                if (t.IsClass && !t.IsAbstract && t.IsPublic && t.IsSubclassOf(typeof(AC)))
                                    acTypes.Add(t);
                        }
                        catch { } // Skip assemblies that fail reflection (e.g. dynamic)
                    }
                    foreach (var y in acTypes)
                    {
                        AC m = null;
                        try
                        {
                            m = (Activator.CreateInstance(y) as AC);
                            if (m.ID == ID)
                            {
                                AcList.Add(m.ID, m);
                                return m;
                            }                                
                        }
                        catch { DebugSystem.Write(new ExceptionData(ExceptionSeverity.Warning, "failed to load AC " + m.ID)); m = null; }
                    }
                }
                return resp;
            }
        }
    }
}
