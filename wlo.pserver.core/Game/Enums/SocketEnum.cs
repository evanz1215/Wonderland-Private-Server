using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    /// <summary>
    /// Forge level — how many times an equipment has been refined.
    /// Each level adds a percentage bonus to base stats.
    /// </summary>
    public enum ForgeLevel : byte
    {
        None = 0,
        Plus1 = 1,
        Plus2 = 2,
        Plus3 = 3,
        Plus4 = 4,
        Plus5 = 5,
        Plus6 = 6,
        Plus7 = 7,
        Plus8 = 8,
        Plus9 = 9,
        Plus10 = 10
    }

    /// <summary>
    /// Enhancement operation type for AC29
    /// </summary>
    public enum EnhanceType : byte
    {
        Forge = 1,      // 鍛造 — increase equipment stats
        Socket = 2,     // 嵌寶 — insert gem into equipment
        Unsocket = 3,   // 拆寶 — remove gem from equipment
        Bomb = 4,       // 轟炸 — add bomb effect to equipment
        Sew = 5         // 縫紉 — add sew effect to equipment
    }
}
