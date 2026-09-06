namespace Minerva.Components;

/// <summary>
/// Breakable chains: a tether pairs two players who must run apart to snap it (or, with
/// <paramref name="spreadChains"/> false, stay together). <paramref name="chainLength"/> is the minimum
/// separation the AI aims for, assuming the pair started perfectly stacked. Ported from BossmodReborn
/// (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class Chains(ModuleBase module, uint tetherID, uint aid = default, float chainLength = default, bool spreadChains = true) : CastCounter(module, aid)
{
    public readonly uint TID = tetherID;
    public bool TethersAssigned;

    private readonly Actor?[] partner = new Actor?[PartyState.MaxSlots];

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (slot >= 0 && slot < this.partner.Length && this.partner[slot] != null)
            hints.Add(spreadChains ? "Break the chains!" : "Stay with partner!");
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID != this.TID)
            return;
        this.TethersAssigned = true;
        if (this.World.Actors.Find(tether.Target) is { } target)
        {
            this.SetPartner(source.InstanceID, target);
            this.SetPartner(target.InstanceID, source);
        }
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID != this.TID)
            return;
        this.SetPartner(source.InstanceID, null);
        this.SetPartner(tether.Target, null);
    }

    private void SetPartner(ulong source, Actor? target)
    {
        var slot = this.World.Party.FindSlot(source);
        if (slot >= 0 && slot < this.partner.Length)
            this.partner[slot] = target;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (pcSlot >= 0 && pcSlot < this.partner.Length && this.partner[pcSlot] is { } p)
            this.Arena.AddLine(pc.Position, p.Position, spreadChains ? Colors.Danger : Colors.Safe);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (slot < 0 || slot >= this.partner.Length || this.partner[slot] is not { } p)
            return;
        // to break: forbid everything nearer than we already are, so the only improvement is running further
        hints.AddForbiddenZone(spreadChains
            ? new SDCircle(p.Position, (p.Position - actor.Position).Length() + 1f)
            : new SDInvertedCircle(p.Position, chainLength), this.World.FutureTime(10d));
    }
}
