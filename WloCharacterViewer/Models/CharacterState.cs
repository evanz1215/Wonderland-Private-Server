using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WloCharacterViewer.Models
{
    public class CharacterState : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name { get => _name; set { _name = value; Notify(); } }

        private byte _level;
        public byte Level { get => _level; set { _level = value; Notify(); } }

        private byte _element;
        public byte Element { get => _element; set { _element = value; Notify(); } }

        private byte _job;
        public byte Job { get => _job; set { _job = value; Notify(); } }

        private bool _reborn;
        public bool Reborn { get => _reborn; set { _reborn = value; Notify(); } }

        private uint _curHP;
        public uint CurHP { get => _curHP; set { _curHP = value; Notify(); Notify(nameof(HPPercent)); } }

        private uint _fullHP;
        public uint FullHP { get => _fullHP; set { _fullHP = value; Notify(); Notify(nameof(HPPercent)); } }

        private uint _curSP;
        public uint CurSP { get => _curSP; set { _curSP = value; Notify(); Notify(nameof(SPPercent)); } }

        private uint _fullSP;
        public uint FullSP { get => _fullSP; set { _fullSP = value; Notify(); Notify(nameof(SPPercent)); } }

        private uint _totalExp;
        public uint TotalExp { get => _totalExp; set { _totalExp = value; Notify(); } }

        private uint _gold;
        public uint Gold { get => _gold; set { _gold = value; Notify(); } }

        private ushort _mapId;
        public ushort MapId { get => _mapId; set { _mapId = value; Notify(); } }

        private ushort _x;
        public ushort X { get => _x; set { _x = value; Notify(); } }

        private ushort _y;
        public ushort Y { get => _y; set { _y = value; Notify(); } }

        // Appearance
        public ushort Body { get; set; }
        public ushort Head { get; set; }

        public double HPPercent => FullHP > 0 ? (double)CurHP / FullHP * 100 : 0;
        public double SPPercent => FullSP > 0 ? (double)CurSP / FullSP * 100 : 0;

        public string ElementName => Element switch
        {
            1 => "金", 2 => "木", 3 => "水", 4 => "火", 5 => "土",
            _ => $"未知({Element})"
        };

        public string JobName => Job switch
        {
            0 => "無", 1 => "戰士", 2 => "法師", 3 => "弓手", 4 => "祭司",
            _ => $"未知({Job})"
        };

        public void LoadFromCharacterInfo(CharacterInfo info)
        {
            Name = info.Name;
            Level = info.Level;
            Element = info.Element;
            Job = info.Job;
            Reborn = info.Reborn;
            CurHP = info.CurHP;
            FullHP = info.FullHP;
            CurSP = info.CurSP;
            FullSP = info.FullSP;
            TotalExp = info.TotalExp;
            Gold = info.Gold;
            Body = info.Body;
            Head = info.Head;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
