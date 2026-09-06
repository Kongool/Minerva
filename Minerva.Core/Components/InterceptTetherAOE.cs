namespace Minerva.Components;

/// <summary>
/// An intercept tether whose holder also drops a circular AOE, so whoever grabs it must take it away from
/// the group. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class InterceptTetherAOE(ModuleBase module, uint aid, uint tetherID, float radius, uint[]? excludedAllies = null) : CastCounter(module, aid)
{
    public readonly uint[]? ExcludedAllies = excludedAllies;
    public readonly uint TID = tetherID;
    public readonly float Radius = radius;
    public readonly List<(Actor Player, Actor Enemy)> Tethers = [];
    public DateTime Activation;

    protected BitMask TetheredPlayers;
    protected BitMask InAnyAOE; // players caught in someone else's baited AOE

    public bool Active => this.Tethers.Count != 0;

    public override void Update()
    {
        this.InAnyAOE = default;
        foreach (var t in this.Tethers)
            foreach (var (slot, actor) in this.World.Party.WithSlot())
                if (actor != t.Player && actor.Position.InCircle(t.Player.Position, this.Radius))
                    this.InAnyAOE.Set(slot);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!this.Active)
            return;

        if (!this.TetheredPlayers[slot])
        {
            hints.Add("Grab the tether!");
        }
        else
        {
            hints.Add("Hit by baited AOE");
            foreach (var p in this.World.Party.WithoutSlot())
            {
                if (p != actor && p.Position.InCircle(actor.Position, this.Radius))
                {
                    hints.Add("GTFO from raid!");
                    break;
                }
            }
        }

        if (this.InAnyAOE[slot])
            hints.Add("GTFO from baited AOE!");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.Tethers.Count == 0)
            return;
        foreach (var tether in this.Tethers)
        {
            if (tether.Player != actor)
            {
                // stay clear of the carrier
                hints.AddForbiddenZone(new SDCircle(tether.Player.Position, this.Radius), this.Activation);
            }
            else
            {
                // carrying it: stay clear of everyone else
                foreach (var member in this.World.Party.WithoutSlot())
                    if (member != actor)
                        hints.AddForbiddenZone(new SDCircle(member.Position, this.Radius), this.Activation);
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (!this.Active)
            return;

        var exclude = new List<Actor>();
        if (this.ExcludedAllies != null)
            foreach (var oid in this.ExcludedAllies)
                exclude.AddRange(this.Module.Enemies(oid));

        var party = this.World.Party.WithoutSlot();
        foreach (var side in this.Tethers)
        {
            var grabbedByParty = Array.IndexOf(party, side.Player) >= 0 && !exclude.Contains(side.Player);
            this.Arena.AddLine(side.Enemy.Position, side.Player.Position, grabbedByParty ? Colors.Safe : Colors.Danger);
            this.Arena.ZoneCircleOutline(side.Player.Position, this.Radius, Colors.Danger);
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (this.DetermineTetherSides(source, tether) is not { } side)
            return;
        this.Tethers.Add((side.Player, side.Enemy));
        this.TetheredPlayers.Set(side.PlayerSlot);
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (this.DetermineTetherSides(source, tether) is not { } side)
            return;
        this.Tethers.Remove((side.Player, side.Enemy));
        this.TetheredPlayers.Clear(side.PlayerSlot);
    }

    protected (int PlayerSlot, Actor Player, Actor Enemy)? DetermineTetherSides(Actor source, ActorTetherInfo tether)
    {
        if (tether.ID != this.TID || this.World.Actors.Find(tether.Target) is not { } target)
            return null;
        var (player, enemy) = Array.IndexOf(this.World.Party.WithoutSlot(), source) >= 0 ? (source, target) : (target, source);
        var slot = this.World.Party.FindSlot(player.InstanceID);
        return slot >= 0 ? (slot, player, enemy) : null;
    }
}

/// <summary>
/// Intercept tether where a status decides who may hold it: players carrying <paramref name="sid"/> must
/// pass it on instead of keeping it. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class InterceptTetherStatus(ModuleBase module, uint aid, uint tetherID, uint sid, float radius = 0f, uint[]? excludedAllies = null)
    : InterceptTetherAOE(module, aid, tetherID, radius, excludedAllies)
{
    public readonly uint StatusID = sid;

    private BitMask hasStatus;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == this.StatusID)
            this.hasStatus.Set(this.World.Party.FindSlot(actor.InstanceID));
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == this.StatusID)
            this.hasStatus.Clear(this.World.Party.FindSlot(actor.InstanceID));
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!this.Active)
            return;

        if (!this.TetheredPlayers[slot] && !this.hasStatus[slot])
        {
            hints.Add("Grab the tether!");
            return;
        }

        foreach (var p in this.World.Party.WithoutSlot())
        {
            if (p != actor && p.Position.InCircle(actor.Position, this.Radius))
            {
                hints.Add("GTFO from raid!");
                break;
            }
        }

        if (this.TetheredPlayers[slot])
            hints.Add(this.hasStatus[slot] ? "Give tether away!" : "Hit by baited AOE");
        if (this.InAnyAOE[slot])
            hints.Add("GTFO from baited AOE!");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var side in this.Tethers)
        {
            var slot = this.World.Party.FindSlot(side.Player.InstanceID);
            var wrongHolder = slot >= 0 && this.hasStatus[slot];
            this.Arena.AddLine(side.Enemy.Position, side.Player.Position, wrongHolder ? Colors.Danger : Colors.Safe);
            this.Arena.ZoneCircleOutline(side.Player.Position, this.Radius, Colors.Danger);
        }
    }
}
