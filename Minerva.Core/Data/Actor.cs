namespace Minerva;

/// <summary>Combat role. Defaults to <see cref="None"/> until populated from the game.</summary>
public enum Role { None, Tank, Melee, Ranged, Healer }

/// <summary>Job/class id (subset used by modules). Defaults to <see cref="None"/> until populated.</summary>
public enum Class : byte
{
    None = 0,
    GLA = 1, PGL = 2, MRD = 3, LNC = 4, ARC = 5, CNJ = 6, THM = 7,
    PLD = 19, MNK = 20, WAR = 21, DRG = 22, BRD = 23, WHM = 24, BLM = 25,
    ACN = 26, SMN = 27, SCH = 28, ROG = 29, NIN = 30, MCH = 31, DRK = 32,
    AST = 33, SAM = 34, RDM = 35, BLU = 36, GNB = 37, DNC = 38,
    RPR = 39, SGE = 40, VPR = 41, PCT = 42,
}

/// <summary>Helpers over <see cref="Class"/>.</summary>
public static class ClassExtensions
{
    /// <summary>The combat role a class belongs to.</summary>
    public static Role GetRole(this Class c) => c switch
    {
        Class.GLA or Class.PLD or Class.MRD or Class.WAR or Class.DRK or Class.GNB => Role.Tank,
        Class.CNJ or Class.WHM or Class.SCH or Class.AST or Class.SGE => Role.Healer,
        Class.PGL or Class.MNK or Class.LNC or Class.DRG or Class.ROG or Class.NIN or Class.SAM or Class.RPR or Class.VPR => Role.Melee,
        Class.ARC or Class.BRD or Class.MCH or Class.DNC or Class.THM or Class.BLM or Class.ACN or Class.SMN or Class.RDM or Class.PCT or Class.BLU => Role.Ranged,
        _ => Role.None,
    };
}

/// <summary>Object kind &lt;&lt; 8 | subkind, matching the game's classification.</summary>
public enum ActorType : ushort
{
    None = 0,
    Player = 0x104,
    Pet = 0x202,
    Chocobo = 0x203,
    Enemy = 0x205,
    Buddy = 0x209,
    Helper = 0x20B, // invisible actor that casts many AOEs on the boss's behalf
    EventNpc = 0x300,
    Treasure = 0x400,
    Aetheryte = 0x500,
    GatheringPoint = 0x600,
    EventObj = 0x700,
    Mount = 0x800,
    Companion = 0x900,
    Retainer = 0xA00,
    Area = 0xB00,
    Cutscene = 0xD00,
}

/// <summary>Current/max HP and MP plus a shield value (percent of max HP).</summary>
public struct ActorHPMP(uint curHP, uint maxHP, uint shield, uint curMP, uint maxMP)
{
    public uint CurHP = curHP;
    public uint MaxHP = maxHP;
    public uint Shield = shield;
    public uint CurMP = curMP;
    public uint MaxMP = maxMP;

    public static bool operator ==(ActorHPMP a, ActorHPMP b) => a.CurHP == b.CurHP && a.MaxHP == b.MaxHP && a.Shield == b.Shield && a.CurMP == b.CurMP && a.MaxMP == b.MaxMP;
    public static bool operator !=(ActorHPMP a, ActorHPMP b) => !(a == b);
    public bool Equals(ActorHPMP other) => this == other;
    public override bool Equals(object? obj) => obj is ActorHPMP other && this == other;
    public override int GetHashCode() => HashCode.Combine(CurHP, MaxHP, Shield, CurMP, MaxMP);
}

/// <summary>A single status-effect slot on an actor. Empty slots have <see cref="ID"/> == 0.</summary>
public struct ActorStatus(uint id, ushort extra, DateTime expireAt, ulong sourceID)
{
    public uint ID = id;
    public ushort Extra = extra;
    public DateTime ExpireAt = expireAt;
    public ulong SourceID = sourceID;
}

/// <summary>
/// A tether from this actor to <see cref="Target"/> (an instance id). N:1 — an actor tethers
/// to at most one other, but many actors can tether to the same target.
/// </summary>
public readonly struct ActorTetherInfo(uint id, ulong target)
{
    public readonly uint ID = id;
    public readonly ulong Target = target;
}

/// <summary>In-progress cast on an actor. Advanced each frame by <see cref="ActorState.Tick"/>.</summary>
public sealed class ActorCastInfo
{
    public ActionID Action;
    public ulong TargetID;
    public Angle Rotation;
    public Vector3 Location; // for area-targeted casts
    public float ElapsedTime;
    public float TotalTime;
    public bool Interruptible;

    public WPos LocXZ => new(Location.X, Location.Z);
    public float RemainingTime => this.TotalTime - this.ElapsedTime;

    public ActorCastInfo Clone() => (ActorCastInfo)this.MemberwiseClone();
}

/// <summary>
/// Fires when a cast resolves (snapshots). Carries the action, primary target, and location —
/// the raw material boss modules and the module generator key off of.
/// </summary>
public sealed class ActorCastEvent(ActionID action, ulong mainTargetID, Angle rotation, Vector3 targetPos, uint globalSequence)
{
    public readonly ActionID Action = action;
    public readonly ulong MainTargetID = mainTargetID;
    public readonly Angle Rotation = rotation;
    public readonly Vector3 TargetPos = targetPos;
    public readonly uint GlobalSequence = globalSequence;

    public WPos TargetXZ => new(this.TargetPos.X, this.TargetPos.Z);
}

/// <summary>
/// A single entity in the world (player, enemy, helper, object). This is the Phase-1 spine:
/// identity, transform, vitals, targeting, cast, tether, and statuses — the fields boss
/// modules and the radar read. Combat/action-confirmation prediction is intentionally omitted
/// (that was BMR's autorotation concern, which Minerva does not include).
/// </summary>
public sealed class Actor(
    ulong instanceID, uint oid, int spawnIndex, string name, uint nameID, ActorType type,
    Vector4 posRot, float hitboxRadius = 1f, ActorHPMP hpmp = default, bool targetable = true,
    bool ally = false, ulong ownerID = default)
{
    public const int NumStatuses = 60;

    public ulong InstanceID = instanceID; // unique per-spawn id ('uuid')
    public uint OID = oid;                 // object/type id shared by all spawns of a kind
    public int SpawnIndex = spawnIndex;
    public string Name = name;
    public uint NameID = nameID;
    public ActorType Type = type;
    public ulong OwnerID = ownerID;        // for pets/dependents

    public Vector4 PosRot = posRot;        // W = rotation (0 = south, CCW)
    public Vector4 PrevPosRot = posRot;    // last frame, for speed/movement
    public float HitboxRadius = hitboxRadius;

    public ActorHPMP HPMP = hpmp;
    public bool IsTargetable = targetable;
    public bool IsAlly = ally;
    public Role Role;   // combat role (None until the game sync fills it)
    public Class Class; // job/class (None until the game sync fills it)
    public ushort EventState; // actor event/animation state (0 until the game sync fills it)
    public bool IsDead;
    public bool IsDestroyed;               // removed from world; object may linger via references
    public bool InCombat;

    public ulong TargetID;
    public ActorCastInfo? CastInfo;
    public ActorTetherInfo Tether;
    public ActorStatus[] Statuses = new ActorStatus[NumStatuses];

    public WPos Position => new(this.PosRot.X, this.PosRot.Z);
    public WPos PrevPosition => new(this.PrevPosRot.X, this.PrevPosRot.Z);
    public WDir LastFrameMovement => this.Position - this.PrevPosition;
    public Angle Rotation => this.PosRot.W.Radians();
    public bool IsDeadOrDestroyed => this.IsDead || this.IsDestroyed;
    public float HPRatio => this.HPMP.MaxHP != 0 ? (float)this.HPMP.CurHP / this.HPMP.MaxHP : 0f;

    public WDir DirectionTo(WPos other) => (other - this.Position).Normalized();
    public WDir DirectionTo(Actor other) => this.DirectionTo(other.Position);
    public Angle AngleTo(Actor other) => Angle.FromDirection(other.Position - this.Position);
    public float DistanceToPoint(WPos pos) => (pos - this.Position).Length();
    public float DistanceToHitbox(Actor? other) => other == null ? float.MaxValue : (other.Position - this.Position).Length() - other.HitboxRadius - this.HitboxRadius;

    /// <summary>First matching non-empty status slot, or null.</summary>
    public ActorStatus? FindStatus(uint sid)
    {
        for (var i = 0; i < NumStatuses; ++i)
        {
            if (this.Statuses[i].ID == sid)
                return this.Statuses[i];
        }
        return null;
    }

    public ActorStatus? FindStatus<TSID>(TSID sid) where TSID : Enum => this.FindStatus((uint)(object)sid);

    public override string ToString() => $"{this.OID:X} '{this.Name}' <{this.InstanceID:X}>";
}
