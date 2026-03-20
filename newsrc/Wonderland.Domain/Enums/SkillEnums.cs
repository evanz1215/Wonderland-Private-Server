namespace Wonderland.Domain.Enums;

public enum SkillTarget
{
    AnyEnemy = 1,
    AnyAlly = 2,
    Self = 3,
    Pet = 4,
    AnyAllyButSelf = 5,
}

public enum EffectLayer
{
    None,
    Physical = 1,
    Magical = 2,
    Seals1,
    Buffs1 = 4,
    Debuffs1,
    Mana,
    Healing1,
    Revival,
    Defend = 10,
    Capture = 11,
    Flee = 12,
    Seals2,
}

public enum AdditionalEffect
{
    Mess = 1,
    HitSelf = 2,
    Freeze,
}

public enum AttackType
{
    Near = 1,
    Distance1 = 2,
    Distance2 = 3,
    OutsideOfBattle = 4,
}

public enum AttackPattern
{
    Single,
    HorizontalLine,
    VerticalLine,
}

public enum RequiredWeaponType
{
    None = 0,
    Arrow = 1,
    Gun = 2,
    Undefined = 242,
}
