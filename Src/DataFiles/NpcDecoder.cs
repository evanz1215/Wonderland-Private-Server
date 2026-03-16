using System;
using DataFiles;

namespace Server.DataFiles
{
    public static class NpcDecoder
    {
        public static void DecodeAllNpcs(PhxNpcDat npcManager)
        {
            foreach (PhoneixNpc n in npcManager.NpcList)
            {
                n.Type = D8(n.Type);
                n.NpcID = D16id(n.NpcID);
                n.ImageNum = D16(n.ImageNum);
                n.ImageNumSmall = D16(n.ImageNumSmall);
                n.ColorCode1 = D32(n.ColorCode1);
                n.ColorCode2 = D32(n.ColorCode2);
                n.ColorCode3 = D32(n.ColorCode3);
                n.ColorCode4 = D32(n.ColorCode4);
                n.Catchable = D8(n.Catchable);
                n.UnknownByte2 = D8(n.UnknownByte2);
                n.UnknownByte3 = D8(n.UnknownByte3);
                n.Level = D8(n.Level);
                n.HP = D32(n.HP);
                n.SP = D32(n.SP);
                n.STR = D16(n.STR);
                n.CON = D16(n.CON);
                n.INT = D16(n.INT);
                n.WIS = D16(n.WIS);
                n.AGI = D16(n.AGI);
                n.ImageNumEnlarge = D8(n.ImageNumEnlarge);
                n.element = D8(n.element);
                n.SkillID1 = D16(n.SkillID1);
                n.SkillID2 = D16(n.SkillID2);
                n.SkillID3 = D16(n.SkillID3);
                n.ItemID1 = D16(n.ItemID1);
                n.ItemID2 = D16(n.ItemID2);
                n.ItemID3 = D16(n.ItemID3);
                n.ItemID4 = D16(n.ItemID4);
                n.ItemID5 = D16(n.ItemID5);
                n.UnknownByte5 = D8(n.UnknownByte5);
                n.UnknownWord14 = D16(n.UnknownWord14);
                n.UnknownWord15 = D16(n.UnknownWord15);
                n.UnknownWord16 = D16(n.UnknownWord16);
                n.UnknownWord17 = D16(n.UnknownWord17);
                n.GeneralAttack1 = D8(n.GeneralAttack1);
                n.UnknownWord18 = D16(n.UnknownWord18);
                n.GeneralAttack2 = D8(n.GeneralAttack2);
                n.UnknownByte8 = D8(n.UnknownByte8);
                n.TalkImage = D16(n.TalkImage);
                n.UnknownWord20 = D16(n.UnknownWord20);
                n.UnknownWord21 = D16(n.UnknownWord21);
                n.SPD = D16(n.SPD);
                n.GeneralAttack3 = D16(n.GeneralAttack3);
                n.UnknownByte9 = D8(n.UnknownByte9);
                n.Transferrable = D8(n.Transferrable);
                n.PK_NPC = D8(n.PK_NPC);
                n.UnknownWord24 = D16(n.UnknownWord24);
                n.UnknownByte12 = D8(n.UnknownByte12);
                n.NPCQuestID = D8(n.NPCQuestID);
                n.HumanNPC = D8(n.HumanNPC);
                n.UnknownByte15 = D8(n.UnknownByte15);
                n.HP_times2 = D8(n.HP_times2);
                n.UnknownWord25 = D16(n.UnknownWord25);
                n.Tradeable = D16(n.Tradeable);
                n.UnknownWord27 = D16(n.UnknownWord27);
                n.UnknownWord28 = D16(n.UnknownWord28);
                n.UnknownWord29 = D16(n.UnknownWord29);
                n.UnknownDword2 = D32(n.UnknownDword2);
                n.UnknownDword3 = D32(n.UnknownDword3);
                n.UnknownDword4 = D32(n.UnknownDword4);
                n.UnknownDword5 = D32(n.UnknownDword5);
                n.UnknownDword6 = D32(n.UnknownDword6);
            }
        }

        // Byte fields: XOR only (subtracting 9 causes underflow for small values 0-8)
        static byte D8(byte val) { return (byte)(val ^ 0xC8); }
        // UInt16 fields: XOR only (same underflow issue for small stat values)
        static ushort D16(ushort val) { return (ushort)(val ^ 0x5209); }
        // NpcID special case: needs -9 to match Eve data IDs
        static ushort D16id(ushort val) { return (ushort)((val ^ 0x5209) - 9); }
        // UInt32 fields: XOR then -9 (results are always > 9 for HP/SP/color values)
        static uint D32(uint val) { return (uint)((val ^ 0xBAEB716u) - 9); }
    }
}
