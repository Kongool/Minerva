namespace Minerva;

/// <summary>
/// The player's party as a fixed set of slots. Phase 1 keeps this minimal — each slot holds a
/// content id + the actor instance id — enough to tell which world actors are party members
/// (what the radar needs). Buddies/alliance and richer membership come later.
/// </summary>
public sealed class PartyState
{
    public const int MaxSlots = 8;

    public readonly struct Member(ulong contentID, ulong instanceID)
    {
        public readonly ulong ContentID = contentID;
        public readonly ulong InstanceID = instanceID;
        public bool IsValid => this.ContentID != 0 || this.InstanceID != 0;
    }

    private readonly ActorState actors;
    public readonly Member[] Slots = new Member[MaxSlots];

    public PartyState(ActorState actors) => this.actors = actors;

    public readonly Event<int> Modified = new(); // slot index

    /// <summary>Resolve a slot's member to a live actor, if present in the world.</summary>
    public Actor? Actor(int slot) => this.actors.Find(this.Slots[slot].InstanceID);

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
