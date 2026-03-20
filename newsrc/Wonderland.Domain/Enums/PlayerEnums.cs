namespace Wonderland.Domain.Enums;

public enum SendMode
{
    Normal,
    Multi,
}

public enum PlayerFlag
{
    CreatingCharacter,
    LoggingIntoMap,
    Warping,
    InGame,
    InMap,
    InTent,
}

public enum GmStatus
{
    Player = 0,
    GameMaster = 1,
    Admin = 2,
}
