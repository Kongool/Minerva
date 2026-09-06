namespace Minerva;

/// <summary>
/// Role questions modules ask by name. The extension methods behind these already live on
/// <see cref="Class"/> in <c>Actor.cs</c>; this is the static-class spelling BossmodReborn's modules use,
/// so a port compiles without rewriting every call site.
/// </summary>
public static class ClassRole
{
    /// <summary>
    /// Whether two players share a role for mechanics that pair by role.
    ///
    /// <para>"Same role" is coarser than the category: every DPS counts as one role together, because a
    /// mechanic that pairs a melee with a caster is pairing DPS, not pairing melee. Tanks pair with tanks
    /// and healers with healers.</para>
    /// </summary>
    public static bool IsSameRole(Actor actor1, Actor actor2)
    {
        var a = actor1.Class.GetClassCategory();
        var b = actor2.Class.GetClassCategory();
        if (a == ClassCategory.Tank && b == ClassCategory.Tank)
            return true;
        if (a == ClassCategory.Healer && b == ClassCategory.Healer)
            return true;
        return actor1.Class.IsDD() && actor2.Class.IsDD();
    }

    /// <summary>Can this job clear a cleansable debuff? Healers, and Bard via the Warden's Paean.</summary>
    public static bool CanEsuna(this Class cls) => cls.GetRole() == Role.Healer || cls == Class.BRD;
}
