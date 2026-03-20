namespace Wonderland.Domain.Enums;

public enum BattleState
{
    NotActive,
    Active,
    Ended,
}

public enum BattleType
{
    Pk = 2,
    Normal,
    Quest,
}

public enum BattleRoundState
{
    None,
    PrepState,
    ReadyState,
    CalculatingState,
    EndedState,
}

public enum BattleLeaveType
{
    BattleFinished,
    RunAway,
    Spawn,
    Disconnected,
}

public enum BattleRole
{
    None,
    Defending = 2,
    Watching = 4,
    Attacking = 5,
}

public enum FighterType
{
    None = 0,
    Player = 2,
    Pet = 4,
    NpcMob = 7,
}

public enum FighterState
{
    Unknown,
    Alive,
    Dead,
    Sealed,
}
