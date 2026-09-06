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

/// <summary>
/// The five job categories, as BossmodReborn splits them. Finer than <see cref="Role"/>, which folds
/// physical ranged and casters together into <c>Ranged</c> — a distinction mechanics do care about, since
/// "casters go left" is not "ranged go left".
/// </summary>
public enum ClassCategory { Undefined, Tank, Healer, Melee, PhysRanged, Caster, Limited }

/// <summary>Helpers over <see cref="Class"/>.</summary>
public static class ClassExtensions
{
    /// <summary>Which of the five categories a class belongs to. <paramref name="allowLimited"/> decides
    /// whether Blue Mage reports as <see cref="ClassCategory.Limited"/> or as a plain caster.</summary>
    public static ClassCategory GetClassCategory(this Class c, bool allowLimited = true) => c switch
    {
        Class.GLA or Class.PLD or Class.MRD or Class.WAR or Class.DRK or Class.GNB => ClassCategory.Tank,
        Class.CNJ or Class.WHM or Class.SCH or Class.AST or Class.SGE => ClassCategory.Healer,
        Class.PGL or Class.MNK or Class.LNC or Class.DRG or Class.ROG or Class.NIN or Class.SAM or Class.RPR or Class.VPR => ClassCategory.Melee,
        Class.ARC or Class.BRD or Class.MCH or Class.DNC => ClassCategory.PhysRanged,
        Class.THM or Class.BLM or Class.ACN or Class.SMN or Class.RDM or Class.PCT => ClassCategory.Caster,
        Class.BLU => allowLimited ? ClassCategory.Limited : ClassCategory.Caster,
        _ => ClassCategory.Undefined,
    };

    /// <summary>Tank or healer — the half of the party that mechanics tend to address separately.</summary>
    public static bool IsSupport(this Class c) => c.GetClassCategory() is ClassCategory.Tank or ClassCategory.Healer;

    /// <summary>A damage dealer of any flavour.</summary>
    public static bool IsDD(this Class c) => c.GetClassCategory() is ClassCategory.Melee or ClassCategory.PhysRanged or ClassCategory.Caster;

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

/// <summary>
/// An actor's Occult Crescent standing: their Knowledge Level and elemental affinity.
/// <para>Not cosmetic. Forbidden Folios resolves against the level a player is *acting* at, so a module
/// reading zero here would send everyone the same way regardless of what the mechanic actually asks of
/// them — the sort of wrong that looks like it is working.</para>
/// <para>Zero outside Occult Crescent, and zero for anything that is not a player.</para>
/// </summary>
public readonly record struct ActorForayInfo(byte Level, byte Element);

/// <summary>
/// An actor's model and animation state. Bosses signal phase changes through these — a dragon that has
/// landed reports a different model state than one still airborne, and a mechanic can key off that
/// without any cast to watch.
///
/// <para><see cref="AnimState1"/> and <see cref="AnimState2"/> are always 0: Minerva's sync captures the
/// model byte only. Components reading them get a consistent zero rather than a wrong value, but a
/// module that genuinely needs them needs the sync widened first.</para>
/// </summary>
public readonly struct ActorModelState(byte modelState, byte animState1 = 0, byte animState2 = 0)
{
    public readonly byte ModelState = modelState;
    public readonly byte AnimState1 = animState1;
    public readonly byte AnimState2 = animState2;
}

/// <summary>
/// An effect the server has announced but the client has not applied yet.
/// <para><paramref name="Expiration"/> is when to stop believing it: some effects never confirm at all —
/// overkill damage, a heal into a full bar, a reapplied buff — so a pending list pruned by confirmation
/// alone would leak rather than settle. <paramref name="TargetIndex"/> is carried for BossmodReborn
/// compatibility; Minerva matches on instance id instead.</para>
/// </summary>
public readonly record struct PendingEffect(uint GlobalSequence, int TargetIndex, ulong SourceInstanceID, DateTime Expiration);

/// <summary>In-progress cast on an actor. Advanced each frame by <see cref="ActorState.Tick"/>.</summary>
public sealed class ActorCastInfo
{
    /// <summary>Is this a spell at all? BossmodReborn puts this on the cast rather than on the action id,
    /// and its modules call it that way.</summary>
    public bool IsSpell() => this.Action.Type == ActionType.Spell;

    public ActionID Action;
    public ulong TargetID;
    public Angle Rotation;
    public Vector3 Location; // for area-targeted casts
    public float ElapsedTime;
    public float TotalTime;
    public bool Interruptible;

    public WPos LocXZ => new(Location.X, Location.Z);
    public float RemainingTime => this.TotalTime - this.ElapsedTime;

    /// <summary>
    /// Remaining cast time as the game will actually resolve it. NPC casts report their remaining time
    /// consistently 0.3s short of reality, so a component that dodges on <see cref="RemainingTime"/>
    /// alone moves you a third of a second early — which is the wrong direction on a mechanic that
    /// snapshots on cast end.
    /// </summary>
    public float NPCRemainingTime => this.RemainingTime + 0.3f;

    /// <summary>True when this cast is the given spell.</summary>
    public bool IsSpell(uint aid) => this.Action == ActionID.MakeSpell(aid);

    public bool IsSpell<AID>(AID aid) where AID : Enum => this.Action == ActionID.MakeSpell(aid);

    public ActorCastInfo Clone() => (ActorCastInfo)this.MemberwiseClone();
}

/// <summary>
/// Fires when a cast resolves (snapshots). Carries the action, primary target, and location —
/// the raw material boss modules and the module generator key off of.
/// </summary>
public sealed class ActorCastEvent(ActionID action, ulong mainTargetID, Angle rotation, Vector3 targetPos, uint globalSequence)
{
    /// <summary>Is this a spell at all? BossmodReborn puts this on the cast rather than on the action id,
    /// and its modules call it that way.</summary>
    public bool IsSpell() => this.Action.Type == ActionType.Spell;

    /// <inheritdoc cref="IsSpell()"/>
    public bool IsSpell(uint id) => this.Action.Type == ActionType.Spell && this.Action.ID == id;

    /// <inheritdoc cref="IsSpell()"/>
    public bool IsSpell<TAID>(TAID id) where TAID : Enum => this.IsSpell((uint)(object)id);

    public readonly ActionID Action = action;
    public readonly ulong MainTargetID = mainTargetID;
    public readonly Angle Rotation = rotation;
    public readonly Vector3 TargetPos = targetPos;
    public readonly uint GlobalSequence = globalSequence;

    /// <summary>
    /// Who the action actually hit, and what it did to them. The game sends this with every resolved
    /// action; components use it to tell a real hit from a whiffed one and to count per-target resolves.
    /// Empty when the recording predates target capture (older logs) — treat that as "unknown", not "none".
    /// </summary>
    public readonly List<Target> Targets = [];

    /// <summary>One resolved target. <see cref="Effects"/> are the game's raw 8 effect slots, kept
    /// undecoded — modules only ever test them for presence, and decoding them is a separate concern.</summary>
    public readonly struct Target(ulong id, ulong[] effects)
    {
        public readonly ulong ID = id;
        public readonly ulong[] Effects = effects;

        public const int MaxEffects = 8;
    }

    public WPos TargetXZ => new(this.TargetPos.X, this.TargetPos.Z);
}

/// <summary>
/// One entry of an actor's incoming-effect ring — an action that has resolved against them, with the raw
/// effect slots the game sent.
///
/// <para>Distinct from the pending-effect lists next door, which are the same information reshaped into
/// "what will my HP be". This is the unreshaped record, and what a module wants when the fight delivers a
/// MARKER as an action rather than as a status: no HP changes, nothing to predict, just an action id
/// arriving against you.</para>
/// </summary>
public readonly struct ActorIncomingEffect(uint globalSequence, int targetIndex, ulong sourceInstanceID, ActionID action, ulong[] effects)
{
    public readonly uint GlobalSequence = globalSequence;
    public readonly int TargetIndex = targetIndex;
    public readonly ulong SourceInstanceID = sourceInstanceID;
    public readonly ActionID Action = action;

    /// <summary>The game's raw effect slots, undecoded — read them with <see cref="ActionEffect"/>.</summary>
    public readonly ulong[] Effects = effects ?? [];
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

    /// <summary>
    /// The map-layout id the game spawned this object under. Fights that place several identical objects
    /// tell them apart by it — "the prison in the north-west corner" is a layout id, not a position.
    /// Always 0: Minerva's sync does not read the layout table, so a module switching on it falls to its
    /// default branch.
    /// </summary>
    public uint LayoutID;

    public Vector4 PosRot = posRot;        // W = rotation (0 = south, CCW)
    public Vector4 PrevPosRot = posRot;    // last frame, for speed/movement
    public float HitboxRadius = hitboxRadius;

    public ActorHPMP HPMP = hpmp;
    public bool IsTargetable = targetable;
    public bool IsAlly = ally;
    public Role Role;   // combat role (None until the game sync fills it)
    public Class Class; // job/class (None until the game sync fills it)
    public ActorModelState ModelState; // model/animation state; bosses signal phase changes through it
    public ActorForayInfo ForayInfo;  // Occult Crescent knowledge level / element (0 elsewhere)
    public byte EventState;  // GameObject event state (0 until the game sync fills it)
    public int Renderflags;  // GameObject render flags (0 until the game sync fills it)
    public bool IsDead;
    public bool IsDestroyed;               // removed from world; object may linger via references
    public bool InCombat;

    /// <summary>The mount this actor is riding, 0 when on foot.
    /// <para>Some fights key off it rather than off a status — a Naadam-style duty where being mounted IS
    /// the mechanic, and dismounting is the mistake.</para></summary>
    public uint MountId;

    // --- effects announced but not yet applied ---
    // The server tells you an action resolved before the client shows it. In that window a healer can
    // already know a raidwide has landed and for how much, and a module can know a status is about to
    // exist. Both lists are pruned by expiry rather than by confirmation: some effects never confirm at
    // all -- overkill damage, a heal into a full bar, a reapplied buff -- so waiting for one would leak.

    /// <summary>Damage (negative) and healing (positive) announced against this actor, unconfirmed.</summary>
    /// <summary>The game's own ring size for incoming effects; it overwrites in place rather than growing.</summary>
    public const int NumIncomingEffects = 32;

    /// <summary>
    /// Actions that have resolved against this actor, in the game's own fixed ring.
    ///
    /// <para>Slots are overwritten in place, so an entry is only meaningful until the ring wraps — read it
    /// on the frame you care about rather than accumulating from it. Empty slots have a
    /// <see cref="ActorIncomingEffect.GlobalSequence"/> of 0.</para>
    /// </summary>
    public readonly ActorIncomingEffect[] IncomingEffects = new ActorIncomingEffect[NumIncomingEffects];

    public readonly List<PendingEffectDelta> PendingHPDifferences = [];

    /// <summary>Statuses announced against this actor, unconfirmed.</summary>
    public readonly List<PendingEffectStatusExtra> PendingStatuses = [];

    /// <summary>Statuses announced as removed but not yet gone — a dispel in flight. Lets a module avoid
    /// spending a second cleanse on something already cleansed.</summary>
    public readonly List<PendingEffectStatus> PendingDispels = [];

    /// <summary>Net unconfirmed HP change: negative if damage is inbound.</summary>
    public int PendingHPDifference
    {
        get
        {
            var sum = 0;
            for (var i = 0; i < this.PendingHPDifferences.Count; ++i)
                sum += this.PendingHPDifferences[i].Value;
            return sum;
        }
    }

    /// <summary>HP once everything announced has landed. Can go negative — that is the point, since it is
    /// how "this hit is lethal" is answerable before the hit is drawn.</summary>
    public int PendingHPRaw => (int)this.HPMP.CurHP + this.PendingHPDifference;

    /// <summary>As <see cref="PendingHPRaw"/>, held inside the real HP range.</summary>
    public int PendingHPClamped => Math.Clamp(this.PendingHPRaw, 0, (int)this.HPMP.MaxHP);

    /// <summary>Is this actor already dead, in effects that have not been drawn yet?</summary>
    public bool PendingDead => this.PendingHPRaw <= 1;

    /// <summary>Drop pending effects that were never confirmed. Called once per frame by the world state.</summary>
    public void PruneExpiredPendingEffects(DateTime now)
    {
        this.PendingHPDifferences.RemoveAll(e => e.Effect.Expiration <= now);
        this.PendingStatuses.RemoveAll(e => e.Effect.Expiration <= now);
        this.PendingDispels.RemoveAll(e => e.Effect.Expiration <= now);
    }

    public ulong TargetID;
    public ActorCastInfo? CastInfo;
    public ActorTetherInfo Tether;
    public ActorStatus[] Statuses = new ActorStatus[NumStatuses];

    public WPos Position => new(this.PosRot.X, this.PosRot.Z);
    public WPos PrevPosition => new(this.PrevPosRot.X, this.PrevPosRot.Z);
    public WDir LastFrameMovement => this.Position - this.PrevPosition;

    /// <summary>Last frame's movement including height and facing, as BossmodReborn exposes it.
    /// <para>Modules compare it against <c>default</c> to ask "did this actor move or turn at all", which
    /// the flat <see cref="LastFrameMovement"/> cannot answer: an actor that only rotated, or only
    /// changed height, reads as stationary there.</para></summary>
    public Vector4 LastFrameMovementVec4 => this.PosRot - this.PrevPosRot;
    public Angle Rotation => this.PosRot.W.Radians();
    public bool IsDeadOrDestroyed => this.IsDead || this.IsDestroyed;

    /// <summary>
    /// HP fraction after damage already in flight. Identical to <c>HPRatio</c>: Minerva deliberately does
    /// not track pending damage (BossmodReborn predicts it from cast events), so this reports what the actor
    /// has now rather than what it is about to have. A module gating a phase on it fires slightly late
    /// rather than wrongly.
    /// </summary>
    public float PendingHPRatio => this.HPRatio;

    /// <summary>
    /// Knockbacks already in flight toward this actor. Always empty: BossmodReborn predicts pending effects
    /// from cast events and Minerva deliberately does not, so a module asking "is a knockback about to land
    /// on me" is told no. It therefore treats the knockback as not yet pending and acts a moment later,
    /// which is the recoverable direction — the alternative would be inventing effects that may not land.
    /// </summary>
    public List<PendingEffect> PendingKnockbacks { get; } = [];
    public float HPRatio => this.HPMP.MaxHP != 0 ? (float)this.HPMP.CurHP / this.HPMP.MaxHP : 0f;

    public WDir DirectionTo(WPos other) => (other - this.Position).Normalized();
    public WDir DirectionTo(Actor other) => this.DirectionTo(other.Position);
    public Angle AngleTo(Actor other) => Angle.FromDirection(other.Position - this.Position);

    /// <summary>Direction from this actor to a point. Modules use it to aim a cone at a voidzone or a
    /// marker, which has a position but no actor of its own.</summary>
    public Angle AngleTo(WPos other) => Angle.FromDirection(other - this.Position);
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

    /// <summary>
    /// The status, only if it expires no later than <paramref name="expireBefore"/>. Modules use this to
    /// ignore a stack of the same debuff that lasts past the mechanic they are resolving.
    /// </summary>
    public ActorStatus? FindStatus(uint sid, DateTime expireBefore)
        => this.FindStatus(sid) is { } s && s.ExpireAt <= expireBefore ? s : null;

    public ActorStatus? FindStatus<TSID>(TSID sid, DateTime expireBefore) where TSID : Enum
        => this.FindStatus((uint)(object)sid, expireBefore);

    public override string ToString() => $"{this.OID:X} '{this.Name}' <{this.InstanceID:X}>";
}
