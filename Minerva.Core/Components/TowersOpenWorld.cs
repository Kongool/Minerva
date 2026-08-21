using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Towers in open-world content, where the soakers are every player in the zone rather than the eight
/// party slots — so membership is an actor set, not a bitmask. Otherwise the same mechanic as
/// <see cref="GenericTowers"/>: each zone needs between min and max bodies standing in it. Ported from
/// BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class GenericTowersOpenWorld(ModuleBase module, uint aid = default, bool prioritizeInsufficient = false, bool prioritizeEmpty = false, AIHints.PredictedDamageType damageType = AIHints.PredictedDamageType.Raidwide) : CastCounter(module, aid)
{
    public sealed class Tower(WPos position, AOEShape shape, int minSoakers = 1, int maxSoakers = 1, HashSet<Actor>? allowedSoakers = null, DateTime activation = default, Angle rotation = default, ulong actorID = default)
    {
        public Tower(WPos position, float radius, int minSoakers = 1, int maxSoakers = 1, HashSet<Actor>? allowedSoakers = null, DateTime activation = default, ulong actorID = default)
            : this(position, new AOEShapeCircle(radius), minSoakers, maxSoakers, allowedSoakers, activation, default, actorID) { }

        public WPos Position = position;
        public Angle Rotation = rotation;
        public AOEShape Shape = shape;
        public int MinSoakers = minSoakers;
        public int MaxSoakers = maxSoakers;
        public HashSet<Actor>? AllowedSoakers = allowedSoakers;
        public DateTime Activation = activation;
        public ulong ActorID = actorID;

        public bool IsInside(WPos pos) => this.Shape.Check(pos, this.Position, this.Rotation);
        public bool IsInside(Actor actor) => this.IsInside(actor.Position);

        public int NumInside(ModuleBase module)
        {
            var count = 0;
            foreach (var a in this.AllowedSoakers ??= Soakers(module))
                if (this.IsInside(a))
                    ++count;
            return count;
        }

        public bool CorrectAmountInside(ModuleBase module) => this.NumInside(module) is var c && c >= this.MinSoakers && c <= this.MaxSoakers;
        public bool InsufficientAmountInside(ModuleBase module) => this.NumInside(module) < this.MaxSoakers;
        public void InitializeAllowedSoakers(ModuleBase module) => this.AllowedSoakers ??= Soakers(module);
    }

    /// <summary>Every player in the zone (OID 0 identifies a player actor).</summary>
    protected static HashSet<Actor> Soakers(ModuleBase module)
    {
        var actors = new HashSet<Actor>();
        foreach (var a in module.World.Actors)
            if (a.OID == default)
                actors.Add(a);
        return actors;
    }

    /// <summary>Every tank in the zone — for towers only tanks may soak.</summary>
    protected static HashSet<Actor> TankSoakers(ModuleBase module)
    {
        var actors = new HashSet<Actor>();
        foreach (var a in module.World.Actors)
            if (a.OID == default && a.Role == Role.Tank)
                actors.Add(a);
        return actors;
    }

    public readonly List<Tower> Towers = [];
    public readonly bool PrioritizeInsufficient = prioritizeInsufficient; // prefer towers that have some, but too few, soakers
    public readonly bool PrioritizeEmpty = prioritizeEmpty;               // prefer towers nobody is in
    public readonly AIHints.PredictedDamageType DamageType = damageType;

    public virtual ReadOnlySpan<Tower> ActiveTowers(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.Towers);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var towers = this.ActiveTowers(slot, actor);
        if (towers.Length == 0)
            return;

        Tower? soaked = null;
        var wrongTower = false;
        var anyNeedsSoaking = false;
        for (var i = 0; i < towers.Length; ++i)
        {
            var t = towers[i];
            t.InitializeAllowedSoakers(this.Module);
            var allowed = t.AllowedSoakers!.Contains(actor);
            if (!allowed && t.IsInside(actor))
                wrongTower = true;
            else if (allowed && t.IsInside(actor))
                soaked ??= t;
            else if (allowed && t.InsufficientAmountInside(this.Module))
                anyNeedsSoaking = true;
        }

        if (wrongTower)
        {
            hints.Add("GTFO from tower!");
        }
        else if (soaked != null)
        {
            var count = soaked.NumInside(this.Module);
            if (count < soaked.MinSoakers)
                hints.Add("Too few soakers in the tower!");
            else if (count > soaked.MaxSoakers)
                hints.Add("Too many soakers in the tower!");
            else
                hints.Add("Soak the tower!", false);
        }
        else if (anyNeedsSoaking)
        {
            hints.Add("Soak the tower!");
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var towers = this.ActiveTowers(slot, actor);
        if (towers.Length == 0)
            return;

        // pick one tower to aim at, then make everything outside it forbidden
        Tower? goal = null;
        for (var i = 0; i < towers.Length; ++i)
        {
            var t = towers[i];
            t.InitializeAllowedSoakers(this.Module);
            if (!t.AllowedSoakers!.Contains(actor))
                continue;
            if (t.IsInside(actor))
            {
                goal = t; // already in one: stay put
                break;
            }
            var inside = t.NumInside(this.Module);
            var wanted = this.PrioritizeEmpty ? inside == 0
                : this.PrioritizeInsufficient ? inside > 0 && inside < t.MinSoakers
                : t.InsufficientAmountInside(this.Module);
            if (wanted)
                goal ??= t;
        }

        if (goal != null)
            hints.AddForbiddenZone(goal.Shape.InvertedDistance(goal.Position, goal.Rotation), goal.Activation);

        // towers we may not soak stay off-limits
        for (var i = 0; i < towers.Length; ++i)
        {
            var t = towers[i];
            if (!t.AllowedSoakers!.Contains(actor))
                hints.AddForbiddenZone(t.Shape, t.Position, t.Rotation, t.Activation);
        }
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        var towers = this.ActiveTowers(pcSlot, pc);
        for (var i = 0; i < towers.Length; ++i)
        {
            var t = towers[i];
            t.InitializeAllowedSoakers(this.Module);
            var needsSoaking = t.InsufficientAmountInside(this.Module) && t.AllowedSoakers!.Contains(pc);
            this.Arena.ZoneShape(t.Shape, t.Position, t.Rotation, needsSoaking ? Colors.Safe : Colors.AOE);
        }
    }
}

/// <summary>Open-world towers created by a cast at its target location.</summary>
public class CastTowersOpenWorld(ModuleBase module, uint aid, float radius, int minSoakers = 1, int maxSoakers = 1, bool prioritizeInsufficient = false, bool prioritizeEmpty = false, AIHints.PredictedDamageType damageType = AIHints.PredictedDamageType.Raidwide)
    : GenericTowersOpenWorld(module, aid, prioritizeInsufficient, prioritizeEmpty, damageType)
{
    public readonly float Radius = radius;
    public readonly int MinSoakers = minSoakers;
    public readonly int MaxSoakers = maxSoakers;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Towers.Add(new Tower(cast.LocXZ, this.Radius, this.MinSoakers, this.MaxSoakers, activation: this.Module.CastFinishAt(cast), actorID: caster.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        var id = caster.InstanceID;
        for (var i = 0; i < this.Towers.Count; ++i)
        {
            if (this.Towers[i].ActorID == id)
            {
                this.Towers.RemoveAt(i);
                return;
            }
        }
    }
}
