namespace Minerva;

/// <summary>
/// The player's party as a fixed set of slots. Phase 1 keeps this minimal — each slot holds a
/// content id + the actor instance id — enough to tell which world actors are party members
/// (what the radar needs). Buddies/alliance and richer membership come later.
/// </summary>
public sealed class PartyState
{
    public const int MaxSlots = 8;

    /// <summary>BossmodReborn's name for <see cref="MaxSlots"/>. Its <c>MaxAllianceSize</c> has no Minerva
    /// equivalent — only the player's own party is tracked.</summary>
    public const int MaxPartySize = MaxSlots;

    /// <summary>
    /// Upper bound BossmodReborn uses when sizing per-member arrays. It counts alliance members and NPCs
    /// past slot 8; Minerva tracks only the player's own party, so this is the same 8 — an array sized by it
    /// is simply never filled past the party.
    /// </summary>
    public const int MaxAllies = MaxSlots;

    /// <summary>
    /// Size of a full alliance (24). Ported modules size per-member arrays with it and iterate the alliance
    /// in 24-player content. Minerva tracks only the player's own eight slots, so the extra entries stay
    /// empty — an alliance mechanic resolves for your party and says nothing about the other two, which is
    /// better than claiming to know where strangers are standing.
    /// </summary>
    public const int MaxAllianceSize = 24;

    /// <summary>
    /// The slot the viewing player occupies. Always 0: the game puts you first in your own party list, and
    /// Minerva's sync preserves that ordering.
    /// </summary>
    public const int PlayerSlot = 0;

    public readonly struct Member(ulong contentID, ulong instanceID)
    {
        public readonly ulong ContentID = contentID;

        /// <summary>BossmodReborn's spelling of <see cref="ContentID"/>.</summary>
        public ulong ContentId => this.ContentID;
        public readonly ulong InstanceID = instanceID;
        public bool IsValid => this.ContentID != 0 || this.InstanceID != 0;
    }

    private readonly ActorState actors;
    public readonly Member[] Slots = new Member[MaxSlots];

    /// <summary>BossmodReborn's name for <see cref="Slots"/>.</summary>
    public Member[] Members => this.Slots;

    /// <summary>Instance id of the local player (POV), set by the plugin. 0 in replays/tests.</summary>
    public ulong PlayerInstanceID;

    public PartyState(ActorState actors) => this.actors = actors;

    public readonly Event<int> Modified = new(); // slot index

    /// <summary>Resolve a slot's member to a live actor, if present in the world.</summary>
    /// <summary>Indexer form, matching how ported modules reach a slot (<c>Raid[slot]</c>).</summary>
    public Actor? this[int slot] => slot >= 0 && slot < MaxSlots ? this.Actor(slot) : null;

    public Actor? Actor(int slot) => this.actors.Find(this.Slots[slot].InstanceID);

    /// <summary>The local player's actor (POV), or the first resolvable member as a fallback.</summary>
    public Actor? Player()
    {
        if (this.PlayerInstanceID != 0 && this.actors.Find(this.PlayerInstanceID) is { } pc)
            return pc;
        for (var i = 0; i < MaxSlots; ++i)
            if (this.Actor(i) is { } a)
                return a;
        return null;
    }

    /// <summary>
    /// Party members paired with their slot index (skipping empty/unresolved/dead slots).
    /// <para><paramref name="excludeAlliance"/> and <paramref name="excludeNPCs"/> exist so BossmodReborn
    /// modules port unchanged, but are no-ops for the same reason they are on <see cref="WithoutSlot"/>:
    /// Minerva's PartyState holds only the player's own 8 slots, so neither alliance members nor NPCs are in
    /// it to exclude. BMR's equivalents index past slot 8 into alliance and NPC ranges Minerva does not
    /// track.</para>
    /// </summary>
    public (int slot, Actor actor)[] WithSlot(bool includeDead = false, bool excludeAlliance = false, bool excludeNPCs = false)
    {
        var result = new List<(int, Actor)>(MaxSlots);
        for (var i = 0; i < MaxSlots; ++i)
            if (this.Actor(i) is { } a && (includeDead || !a.IsDeadOrDestroyed))
                result.Add((i, a));
        return [.. result];
    }

    /// <summary>
    /// Party member actors (skipping empty/unresolved/dead slots). <paramref name="excludeAlliance"/> and
    /// <paramref name="excludeNPCs"/> exist so BMR modules port unchanged, but are no-ops: Minerva's
    /// PartyState holds only the player's own 8 slots, so neither alliance members nor NPCs are in it.
    /// </summary>
    public Actor[] WithoutSlot(bool includeDead = false, bool excludeAlliance = false, bool excludeNPCs = false)
    {
        var result = new List<Actor>(MaxSlots);
        for (var i = 0; i < MaxSlots; ++i)
            if (this.Actor(i) is { } a && (includeDead || !a.IsDeadOrDestroyed))
                result.Add(a);
        return [.. result];
    }

    /// <summary>Slot index of the member with the given instance id, or -1.</summary>
    public int FindSlot(ulong instanceID)
    {
        if (instanceID == 0)
            return -1;
        for (var i = 0; i < MaxSlots; ++i)
            if (this.Slots[i].InstanceID == instanceID)
                return i;
        return -1;
    }

    public List<WorldState.Operation> CompareToInitial()
    {
        List<WorldState.Operation> ops = [];
        for (var i = 0; i < MaxSlots; ++i)
            if (this.Slots[i].IsValid)
                ops.Add(new OpModify(i, this.Slots[i]));
        return ops;
    }

    public sealed class OpModify(int slot, Member member) : WorldState.Operation
    {
        public readonly int Slot = slot;
        public readonly Member Member = member;
        protected override void Exec(WorldState ws)
        {
            ws.Party.Slots[this.Slot] = this.Member;
            ws.Party.Modified.Fire(this.Slot);
        }
        public override void Write(OperationOutput o) => o.Tag("PAR ").Emit(this.Slot).Emit(this.Member.ContentID, "X").Emit(this.Member.InstanceID, "X");
    }
}
