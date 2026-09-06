namespace Minerva;

/// <summary>
/// Palette shared by modules and the radar. Values are packed the way ImGui expects
/// (0xAABBGGRR — alpha, blue, green, red), so the renderer can use them directly.
/// </summary>
public static class Colors
{
    public static uint RGBA(byte r, byte g, byte b, byte a) => (uint)(r | (g << 8) | (b << 16) | (a << 24));

    public static readonly uint AOE = RGBA(255, 0, 0, 90);        // translucent red fill (future danger)
    public static readonly uint AOEImminent = RGBA(255, 40, 0, 160); // brighter fill for an AOE about to resolve
    public static readonly uint Danger = RGBA(255, 0, 0, 255);    // bright red outline
    public static readonly uint Safe = RGBA(0, 200, 0, 70);       // translucent green
    public static readonly uint SafeFromAOE = RGBA(0, 200, 0, 70); // BMR alias for a safe zone fill
    public static readonly uint Border = RGBA(255, 255, 255, 255);
    public static readonly uint Shadow = RGBA(0, 0, 0, 200);
    public static readonly uint PC = RGBA(255, 140, 0, 255);     // local player (orange)
    public static readonly uint Player = RGBA(120, 120, 255, 255);// other party members

    // Party colouring. Roles use the hues the game itself uses for them, so the radar agrees with the party
    // list rather than inventing a second scheme to learn.
    public static readonly uint RoleTank = RGBA(70, 130, 255, 255);
    public static readonly uint RoleHealer = RGBA(70, 210, 120, 255);
    public static readonly uint RoleMelee = RGBA(230, 90, 90, 255);
    public static readonly uint RoleRanged = RGBA(240, 190, 80, 255);

    /// <summary>Marker colour for a role, falling back to the generic party colour.</summary>
    public static uint ForRole(Role role) => role switch
    {
        Role.Tank => RoleTank,
        Role.Healer => RoleHealer,
        Role.Melee => RoleMelee,
        Role.Ranged => RoleRanged,
        _ => Player,
    };

    // Light-party colouring: two hues far enough apart to read at a glance on a red field.
    public static readonly uint Group1 = RGBA(90, 200, 255, 255);
    public static readonly uint Group2 = RGBA(255, 160, 60, 255);

    /// <summary>Marker colour for a light party, by index.</summary>
    public static uint ForGroup(int group) => group switch
    {
        0 => Group1,
        1 => Group2,
        _ => Player,
    };

    // The party-priority palette ported components ask for by name when they highlight whoever is
    // carrying a mechanic. Same roles as BossmodReborn's, in Minerva's own hues.
    public static readonly uint PlayerGeneric = RGBA(120, 120, 255, 255);
    public static readonly uint PlayerInteresting = RGBA(255, 210, 80, 255);
    public static readonly uint PlayerDanger = RGBA(255, 80, 80, 255);
    public static readonly uint PlayerCritical = RGBA(255, 40, 200, 255);
    public static readonly uint Enemy = RGBA(255, 0, 255, 255);   // primary/boss actor
    public static readonly uint Object = RGBA(255, 210, 0, 255);  // helpers/objects
    public static readonly uint Text = RGBA(255, 255, 255, 255);
    public static readonly uint Vulnerable = RGBA(255, 128, 0, 255); // BMR palette slot: "this will hurt you" outline
    public static readonly uint Other1 = RGBA(0, 190, 190, 255);  // BMR palette slot: call-out highlight (dispel targets, ...)

    // Remaining BMR palette slots. Modules pick these purely to tell one group of markers from
    // another — a fight with four towers colours them Other1..Other4 — so the exact hues carry no
    // meaning beyond being distinguishable from each other and from the danger/safe colours.
    public static readonly uint FutureVulnerable = RGBA(255, 160, 64, 160); // a vulnerability that is not live yet
    public static readonly uint Other2 = RGBA(255, 0, 128, 255); // magenta
    public static readonly uint Other3 = RGBA(128, 255, 0, 255); // lime
    public static readonly uint Other4 = RGBA(0, 128, 255, 255); // azure
    public static readonly uint Other5 = RGBA(255, 128, 255, 255); // pink
    public static readonly uint Other6 = RGBA(128, 128, 255, 255); // periwinkle
    public static readonly uint Other7 = RGBA(255, 255, 128, 255); // pale yellow

    // --- arena furniture, matching BossmodReborn's defaults (its values are user-configurable; these are
    // the shipped ones, since Minerva has no per-colour config) ---

    /// <summary>The arena floor fill. BMR ArenaBackground, 0xc00f0f0f.</summary>
    public static readonly uint Background = 0xc00f0f0fu;

    /// <summary>A trap on the floor. BMR ArenaTrap, 0x80000080 — translucent blue.</summary>
    public static readonly uint Trap = 0x80000080u;

    /// <summary>A light source or lit region. BMR ArenaLight, opaque white.</summary>
    public static readonly uint Light = 0xffffffffu;
    public static readonly uint Other8 = RGBA(128, 255, 255, 255); // pale cyan
    public static readonly uint Other9 = RGBA(200, 120, 255, 255); // violet
}
