using System;

namespace WloCharacterViewer.Models
{
    public class CharacterInfo
    {
        public byte Slot { get; set; }
        public string Name { get; set; } = "";
        public byte Level { get; set; }
        public byte Element { get; set; }
        public uint FullHP { get; set; }
        public uint CurHP { get; set; }
        public uint FullSP { get; set; }
        public uint CurSP { get; set; }
        public uint TotalExp { get; set; }
        public uint Gold { get; set; }
        public ushort Body { get; set; }
        public ushort Head { get; set; }
        public ushort HairColor { get; set; }
        public ushort SkinColor { get; set; }
        public ushort ClothingColor { get; set; }
        public ushort EyeColor { get; set; }
        public bool Reborn { get; set; }
        public byte Job { get; set; }
        public ushort[] Equips { get; set; } = new ushort[6];

        public string ElementName => Element switch
        {
            1 => "金",
            2 => "木",
            3 => "水",
            4 => "火",
            5 => "土",
            _ => $"未知({Element})"
        };

        public string JobName => Job switch
        {
            0 => "無",
            1 => "戰士",
            2 => "法師",
            3 => "弓手",
            4 => "祭司",
            _ => $"未知({Job})"
        };

        public string DisplayName => string.IsNullOrEmpty(Name) ? "(空欄位)" : Name;
        public bool IsEmpty => string.IsNullOrEmpty(Name);
    }
}
