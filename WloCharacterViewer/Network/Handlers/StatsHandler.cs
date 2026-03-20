using System;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.Network.Handlers
{
    /// <summary>
    /// Handles AC 5 sub 3 (full stats blob) and AC 8 sub 1/2 (individual stat updates).
    /// </summary>
    public class StatsHandler53 : IPacketHandler
    {
        // AC 5.3 — ptr is at position 6 (past AC=5, Sub=3)
        // Format: [element:8][curHP:32][curSP:16][str:16][con:16][int:16][wis:16][agi:16]
        //         [level:8][totalExp:64][fullHP:32][fullSP:16] + 7 DWords + [skillCount:16]
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                var c = session.Character;

                c.Element = reader.Unpack8();
                c.CurHP = reader.Unpack32();
                c.CurSP = reader.Unpack16();

                // Base stats (str/con/int/wis/agi) — read but not stored on CharacterState yet
                ushort str = reader.Unpack16();
                ushort con = reader.Unpack16();
                ushort intel = reader.Unpack16();
                ushort wis = reader.Unpack16();
                ushort agi = reader.Unpack16();

                c.Level = reader.Unpack8();
                ulong totalExp = reader.Unpack64();
                c.TotalExp = (uint)(totalExp & 0xFFFFFFFF);
                c.FullHP = reader.Unpack32();
                c.FullSP = reader.Unpack16();

                session.Log($"[Stats] Lv{c.Level} HP={c.CurHP}/{c.FullHP} SP={c.CurSP}/{c.FullSP} Elem={c.ElementName}");
            }
            catch (Exception ex)
            {
                session.Log($"[StatsHandler53] Parse error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles AC 8 (stat update). Registered as AC-only handler so it receives subAc at ptr=5.
    /// Each packet: [8][sub:8][statID:8][1:8][value:32][0:32]
    /// The dispatcher sets ptr=5 for AC-only handlers.
    /// </summary>
    public class StatUpdateHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                byte sub = reader.Unpack8(); // sub action (1 or 2)
                byte statId = reader.Unpack8();
                byte _pad = reader.Unpack8(); // always 1
                uint value = reader.Unpack32();
                // uint zero = reader.Unpack32(); // trailing zero — may not always be present

                var c = session.Character;
                switch (statId)
                {
                    case 25: c.CurHP = value; break;
                    case 26: c.CurSP = value; break;
                    case 207: c.FullHP = value; break;
                    case 208: c.FullSP = value; break;
                    case 35: c.Level = (byte)value; break;
                    case 36:
                        // TotalExp sent as u64 but we read u32 here
                        c.TotalExp = value;
                        break;
                    default:
                        // StatIDs 34=Potential, 210=ATK, 211=DEF, 41=FullAtk, 42=FullDef, 28=Str
                        // Not stored on CharacterState yet
                        break;
                }
            }
            catch (Exception ex)
            {
                session.Log($"[StatUpdateHandler] Parse error: {ex.Message}");
            }
        }
    }
}
