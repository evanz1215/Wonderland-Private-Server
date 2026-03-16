using System;
using System.Collections.Generic;
using System.IO;

namespace Server.DataFiles
{
    /// <summary>
    /// Lightweight Eve.emg parser that only extracts clickID → npcID mappings per map.
    /// </summary>
    public class EveNpcMapper
    {
        // mapID → list of (clickID, npcID)
        Dictionary<ushort, List<NpcMapping>> maps = new Dictionary<ushort, List<NpcMapping>>();

        public struct NpcMapping
        {
            public ushort ClickID;
            public ushort NpcID;
        }

        public bool Load(string filename)
        {
            maps.Clear();
            if (!File.Exists(filename))
            {
                DebugSystem.Write("EveNpcMapper: file not found: " + filename);
                return false;
            }

            try
            {
                byte[] d = File.ReadAllBytes(filename);
                int ptr = 0;

                // Header: 8 bytes skip + 4 bytes entry count
                ptr += 8;
                uint entryCount = GetDWord(d, ptr); ptr += 4;

                // Stage 1: Read map entries (mapID, sceneID, dataptr, datalen)
                var mapEntries = new List<MapEntry>();
                for (int a = 0; a < entryCount; a++)
                {
                    var entry = new MapEntry();
                    entry.MapID = GetWord(d, ptr); ptr += 2;
                    entry.SceneID = GetWord(d, ptr); ptr += 2;
                    entry.DataPtr = GetDWord(d, ptr); ptr += 4;
                    entry.DataLen = GetWord(d, ptr); ptr += 2;
                    mapEntries.Add(entry);
                }

                // Stage 2: Read category offsets for each map
                foreach (var entry in mapEntries)
                {
                    // Jump to end of map data block - 44 bytes (11 offsets × 4 bytes each)
                    int offsetPtr = (int)(entry.DataLen + entry.DataPtr) - 44;
                    uint npcOffset = GetDWord(d, offsetPtr); // First offset is NPC

                    // Stage 3: Read NPC entries for this map
                    int npcPtr = (int)npcOffset + (int)entry.DataPtr;
                    var npcList = ReadNpcEntries(d, npcPtr);

                    if (npcList != null && npcList.Count > 0)
                        maps[entry.MapID] = npcList;
                }

                int totalMappings = 0;
                foreach (var m in maps.Values) totalMappings += m.Count;
                DebugSystem.Write(string.Format("EveNpcMapper: loaded {0} maps, {1} total NPC entries", maps.Count, totalMappings));
                return true;
            }
            catch (Exception e)
            {
                DebugSystem.Write("EveNpcMapper error: " + e.Message);
                return false;
            }
        }

        List<NpcMapping> ReadNpcEntries(byte[] d, int ptr)
        {
            try
            {
                var result = new List<NpcMapping>();
                ushort count = GetWord(d, ptr); ptr += 2;

                for (int a = 0; a < count; a++)
                {
                    ushort clickId = GetWord(d, ptr); ptr += 2;
                    // Name: 1 byte length prefix + 19 bytes fixed = 20 bytes total
                    ptr += 20;
                    // unknownbyte1
                    ptr += 1;
                    // x, y (uint32 each)
                    ptr += 4; ptr += 4;
                    // Events byte array (1 byte length + N bytes)
                    int blen = d[ptr]; ptr++;
                    ptr += blen;
                    // unknownbytearray2 (1 byte length + N bytes)
                    blen = d[ptr]; ptr++;
                    ptr += blen;
                    // unknownbyte2
                    ptr += 1;
                    // npcId (uint32)
                    uint npcId = GetDWord(d, ptr); ptr += 4;
                    // rotation
                    ptr += 1;
                    // unknownbyte4, unknownbyte5
                    ptr += 1; ptr += 1;
                    // walksteps (1 byte count + N * 12 bytes)
                    blen = d[ptr]; ptr++;
                    ptr += blen * 12; // each step = x(4) + y(4) + delay(4)
                    // unknownbyte6, unknownbyte7
                    ptr += 1; ptr += 1;
                    // unknowndword1, unknowndword2
                    ptr += 4; ptr += 4;
                    // unknownbyte8, unknownbyte9, unknownbyte10
                    ptr += 1; ptr += 1; ptr += 1;
                    // walkpatterns (1 byte count + N * variable)
                    blen = d[ptr]; ptr++;
                    for (int c = 0; c < blen; c++)
                    {
                        // unknownbyte1-3, steps_needed
                        ptr += 4;
                        // unknowndword1, unknowndword2
                        ptr += 4; ptr += 4;
                        // 10 walksteps, each = x(4) + y(4) = 8 bytes
                        ptr += 10 * 8;
                    }
                    // unknownword1-4
                    ptr += 2; ptr += 2; ptr += 2; ptr += 2;

                    result.Add(new NpcMapping { ClickID = clickId, NpcID = (ushort)npcId });
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        public List<NpcMapping> GetMapNpcs(ushort mapID)
        {
            List<NpcMapping> result;
            if (maps.TryGetValue(mapID, out result))
                return result;
            return null;
        }

        public bool HasMap(ushort mapID)
        {
            return maps.ContainsKey(mapID);
        }

        struct MapEntry
        {
            public ushort MapID;
            public ushort SceneID;
            public uint DataPtr;
            public ushort DataLen;
        }

        static ushort GetWord(byte[] data, int ptr)
        {
            return (ushort)((data[ptr + 1] << 8) + data[ptr]);
        }

        static uint GetDWord(byte[] data, int ptr)
        {
            return (uint)((data[ptr + 3] << 24) + (data[ptr + 2] << 16) + (data[ptr + 1] << 8) + data[ptr]);
        }
    }
}
