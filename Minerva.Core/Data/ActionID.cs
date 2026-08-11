namespace Minerva;

/// <summary>Kind of action an <see cref="ActionID"/> refers to. Values match the game's action category.</summary>
public enum ActionType : byte
{
    None = 0,
    Spell = 1,
    Item = 2,
    KeyItem = 3,
    Ability = 4,
    General = 5,
    Companion = 6,
    CraftAction = 9,
    MainCommand = 10,
    PetAction = 11,
    Mount = 13,
    BozjaHolsterSlot0 = 25,
}

/// <summary>
/// A packed (type, id) reference to a game action. Pure data — name/icon lookups (which need
/// Lumina/Dalamud) live in the plugin layer, keeping this usable from the game-free core.
/// Modules compare casts with <c>spell.Action == ActionID.MakeSpell(AID.X)</c>.
/// </summary>
public readonly struct ActionID(ActionType type, uint id) : IEquatable<ActionID>
{
    public readonly ActionType Type = type;
    public readonly uint ID = id;

    public bool IsValid => this.ID != 0;

    public static ActionID MakeSpell(uint id) => id != 0 ? new(ActionType.Spell, id) : default;
    public static ActionID MakeSpell<TAID>(TAID id) where TAID : Enum => MakeSpell((uint)(object)id);

    public bool IsSpell() => this.Type == ActionType.Spell;
    public bool IsSpell(uint id) => this.Type == ActionType.Spell && this.ID == id;
    public bool IsSpell<TAID>(TAID id) where TAID : Enum => this.IsSpell((uint)(object)id);

    public static bool operator ==(ActionID a, ActionID b) => a.Type == b.Type && a.ID == b.ID;
    public static bool operator !=(ActionID a, ActionID b) => a.Type != b.Type || a.ID != b.ID;
    public bool Equals(ActionID other) => this == other;
    public override bool Equals(object? obj) => obj is ActionID other && this == other;
    public override int GetHashCode() => HashCode.Combine(this.Type, this.ID);
    public override string ToString() => $"{this.Type} {this.ID}";
}
