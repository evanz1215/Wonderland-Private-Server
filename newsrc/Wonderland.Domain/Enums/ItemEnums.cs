namespace Wonderland.Domain.Enums;

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
    Plus10 = 10,
}

public enum EnhanceType : byte
{
    Forge = 1,
    Socket = 2,
    Unsocket = 3,
    Bomb = 4,
    Sew = 5,
}

public enum QuestState : byte
{
    NotStarted,
    InProgress,
    Completed,
    TurnedIn,
}
