namespace Minerva.Components;

/// <summary>
/// Tankbuster delivered along a tether: a tank must grab it and carry the AOE away from the raid, while
/// everyone else stays clear. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class TankbusterTether(ModuleBase module, uint aid, uint tetherID, AOEShape shape, double activationDelay = default, bool centerAtTarget = false) : CastCounter(module, aid)
{
    public TankbusterTether(ModuleBase module, uint aid, uint tetherID, float radius, double activationDelay = default)
        : this(module, aid, tetherID, new AOEShapeCircle(radius), activationDelay, true) { }

    public readonly uint TID = tetherID;
    public readonly AOEShape Shape = shape;

    private readonly List<(Actor Player, Actor Enemy)> tethers = [];
    protected BitMask TetheredPlayers;
    private BitMask inAnyAOE; // players clipped by someone else's tether AOE
    protected DateTime Activation;

    public bool Active => this.TetheredPlayers != default;

    /// <summary>Where the AOE sits for a tether: on the carrier, or aimed from the enemy through them.</summary>
    private (WPos origin, Angle rotation) Placement(Actor player, Actor enemy)
        => centerAtTarget ? (player.Position, default) : (enemy.Position, Angle.FromDirection(player.Position - enemy.Position));

    public override void Update()
    {
        this.inAnyAOE = default;
        if (this.tethers.Count == 0)
            return;

        foreach (var (slot, actor) in this.World.Party.WithSlot())
        {
            foreach (var t in this.tethers)
            {
                if (t.Player == actor)
                    continue;
                var (origin, rotation) = this.Placement(t.Player, t.Enemy);
                if (this.Shape.Check(actor.Position, origin, rotation))
                    this.inAnyAOE.Set(slot);
            }
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!this.Active)
            return;

        if (actor.Role != Role.Tank)
        {
            if (this.TetheredPlayers[slot])
                hints.Add("Hit by tankbuster");
            if (this.inAnyAOE[slot])
                hints.Add("GTFO from tankbuster!");
            return;
        }

        if (!this.TetheredPlayers[slot])
        {
            hints.Add("Grab the tether!");
            return;
        }

        // carrying it: warn if anyone else is standing in our own AOE
        foreach (var t in this.tethers)
        {
            if (t.Player != actor)
                continue;
            var (origin, rotation) = this.Placement(t.Player, t.Enemy);
            foreach (var p in this.World.Party.WithoutSlot())
            {
                if (p != actor && this.Shape.Check(p.Position, origin, rotation))
                {
                    hints.Add("GTFO from raid!");
                    return;
                }
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var side in this.tethers)
        {
            this.Arena.AddLine(side.Enemy.Position, side.Player.Position, side.Player.Role == Role.Tank ? Colors.Safe : Colors.Danger);
            if (side.Player != pc)
                continue;
            var (origin, rotation) = this.Placement(side.Player, side.Enemy);
            this.Arena.OutlineShape(this.Shape, origin, rotation, Colors.Danger);
        }
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var side in this.tethers)
        {
            if (side.Player == pc)
                continue;
            var (origin, rotation) = this.Placement(side.Player, side.Enemy);
            this.Arena.ZoneShape(this.Shape, origin, rotation, Colors.AOE);
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (this.DetermineTetherSides(source, tether) is not { } side)
            return;
        this.tethers.Add((side.Player, side.Enemy));
        this.TetheredPlayers.Set(side.PlayerSlot);
        if (this.Activation == default)
            this.Activation = this.World.FutureTime(activationDelay);
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (this.DetermineTetherSides(source, tether) is not { } side)
            return;
        this.tethers.Remove((side.Player, side.Enemy));
        this.TetheredPlayers.Clear(side.PlayerSlot);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent cast)
    {
        base.OnEventCast(caster, cast);
        if (cast.Action.ID == this.WatchedAction)
            this.Activation = default;
    }

    /// <summary>Both player-to-enemy and enemy-to-player tether directions are supported.</summary>
    private (int PlayerSlot, Actor Player, Actor Enemy)? DetermineTetherSides(Actor source, ActorTetherInfo tether)
    {
        if (tether.ID != this.TID || this.World.Actors.Find(tether.Target) is not { } target)
            return null;
        var (player, enemy) = Array.IndexOf(this.World.Party.WithoutSlot(), source) >= 0 ? (source, target) : (target, source);
        var slot = this.World.Party.FindSlot(player.InstanceID);
        return slot >= 0 ? (slot, player, enemy) : null;
    }
}

/// <summary>
/// A tether someone must intercept (run into) so it does not resolve on the wrong person. Two ids mark
/// "not yet grabbed" and "grabbed". Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class InterceptTether(ModuleBase module, uint aid, uint tetherIDBad = 84u, uint tetherIDGood = 17u, uint[]? excludedAllies = null) : CastCounter(module, aid)
{
    public readonly uint TIDGood = tetherIDGood;
    public readonly uint TIDBad = tetherIDBad;
    public readonly uint[]? ExcludedAllies = excludedAllies;

    protected readonly List<(Actor Player, Actor Enemy)> Tethers = [];
    protected BitMask TetheredPlayers;
    protected const string Hint = "Grab the tether!";

    public bool Active => this.Tethers.Count != 0;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.Active && !this.TetheredPlayers[slot])
            hints.Add(Hint);
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

    public virtual (int PlayerSlot, Actor Player, Actor Enemy)? DetermineTetherSides(Actor source, ActorTetherInfo tether)
    {
        if (tether.ID != this.TIDGood && tether.ID != this.TIDBad)
            return null;
        if (this.World.Actors.Find(tether.Target) is not { } target)
            return null;
        var (player, enemy) = Array.IndexOf(this.World.Party.WithoutSlot(), source) >= 0 ? (source, target) : (target, source);
        return (this.World.Party.FindSlot(player.InstanceID), player, enemy);
    }
}
