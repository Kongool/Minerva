namespace Minerva.Components;

/// <summary>
/// A boss→player tether that must be pulled taut: two tether ids mark the same mechanic, one for
/// "still slack" and one for "stretched far enough", and the player has to get <c>MinimumDistance</c>
/// away before it resolves. The tethered player's minimum distance becomes a forbidden circle around
/// the enemy, so the auto-dodge runs them out. Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt); BMR additionally queues Surecast/Arm's Length through its action queue,
/// which Minerva does not do (it executes no actions) — the immunity is only reported as a hint.
/// </summary>
public class StretchTetherDuo(ModuleBase module, float minimumDistance, double activationDelay, uint tetherIDBad = 57u, uint tetherIDGood = 1u, AOEShape? shape = null, uint aid = default, uint enemyOID = default, bool knockbackImmunity = false)
    : GenericBaitAway(module, aid, tankbuster: true)
{
    public readonly AOEShape? Shape = shape;
    public readonly uint TIDGood = tetherIDGood;
    public readonly uint TIDBad = tetherIDBad;
    public readonly float MinimumDistance = minimumDistance;
    public readonly bool KnockbackImmunity = knockbackImmunity;
    public readonly double ActivationDelay = activationDelay;
    public readonly List<Actor> Enemies = module.Enemies(enemyOID);
    public readonly List<(Actor, uint)> TetherOnActor = [];
    public readonly List<(Actor actor, DateTime activation)> ActivationDelayOnActor = [];

    private readonly float minSq = minimumDistance * minimumDistance;
    private readonly uint enemyOID = enemyOID;

    public const string HintGood = "Tether is stretched!";
    public const string HintBad = "Stretch tether further!";
    public const string HintImmunityGood = "Immune against tether mechanic!";
    public const string HintImmunityBad = "Tether can be ignored with knockback immunity!";

    // knockback-immunity statuses, by the group that grants them (a player can hold one of each at once)
    private static readonly uint[] roleImmunities = [3054u, 160u, 1209u];  // Guard (PVP), Surecast, Arm's Length
    private static readonly uint[] jobImmunities = [1722u, 1176u];         // Diamondback (BLU), Inner Strength (WAR)
    private static readonly uint[] dutyImmunities = [2345u];               // Lost Manawall (Bozja)

    protected struct PlayerImmuneState
    {
        public DateTime RoleBuffExpire;
        public DateTime JobBuffExpire;
        public DateTime DutyBuffExpire;

        public readonly bool ImmuneAt(DateTime time) => this.RoleBuffExpire > time || this.JobBuffExpire > time || this.DutyBuffExpire > time;
    }

    protected PlayerImmuneState[] PlayerImmunes = new PlayerImmuneState[PartyState.MaxSlots];

    public bool IsImmune(int slot, DateTime time) => this.KnockbackImmunity && slot >= 0 && slot < PartyState.MaxSlots && this.PlayerImmunes[slot].ImmuneAt(time);

    public override void OnStatusGain(Actor actor, ref ActorStatus status) => this.TrackImmunity(actor, status.ID, status.ExpireAt);

    public override void OnStatusLose(Actor actor, ref ActorStatus status) => this.TrackImmunity(actor, status.ID, default);

    private void TrackImmunity(Actor actor, uint sid, DateTime expireAt)
    {
        var slot = this.World.Party.FindSlot(actor.InstanceID);
        if (slot < 0)
            return;
        if (Array.IndexOf(roleImmunities, sid) >= 0)
            this.PlayerImmunes[slot].RoleBuffExpire = expireAt;
        else if (Array.IndexOf(jobImmunities, sid) >= 0)
            this.PlayerImmunes[slot].JobBuffExpire = expireAt;
        else if (Array.IndexOf(dutyImmunities, sid) >= 0)
            this.PlayerImmunes[slot].DutyBuffExpire = expireAt;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        var baits = this.ActiveBaitsOn(pc);
        if (baits.Count == 0 || this.IsImmune(pcSlot, baits[0].Activation))
            return;

        if (this.IsTether(pc, this.TIDBad))
            this.DrawTetherLines(pc);
        else if (this.IsTether(pc, this.TIDGood))
            this.DrawTetherLines(pc, Colors.Safe);
    }

    protected bool IsTether(Actor actor, uint tetherID) => this.TetherOnActor.Contains((actor, tetherID));

    private void DrawTetherLines(Actor target, uint color = default)
    {
        foreach (var bait in this.CurrentBaits)
            if (bait.Target == target)
                this.Arena.AddLine(bait.Source.Position, bait.Target.Position, color == default ? Colors.Danger : color);
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        var (player, enemy) = this.DetermineTetherSides(source, tether);
        if (player == null || enemy == null || (this.enemyOID != default && !this.Enemies.Contains(source)))
            return;

        // the first tether on a player starts its clock; further tethers on the same player share it
        var existing = this.ActivationDelayOnActor.FindIndex(a => a.actor == player);
        if (existing < 0)
        {
            this.ActivationDelayOnActor.Add((player, this.World.FutureTime(this.ActivationDelay)));
            existing = this.ActivationDelayOnActor.Count - 1;
        }

        this.CurrentBaits.Add(new Bait(enemy, player, this.Shape ?? new AOEShapeCircle(default), this.ActivationDelayOnActor[existing].activation));
        this.TetherOnActor.Add((player, tether.ID));
    }

    public override void Update()
    {
        for (var i = this.ActivationDelayOnActor.Count - 1; i >= 0; --i)
            if (this.ActivationDelayOnActor[i].activation.AddSeconds(1d) <= this.World.CurrentTime)
                this.ActivationDelayOnActor.RemoveAt(i);
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        var (player, enemy) = this.DetermineTetherSides(source, tether);
        if (player == null || enemy == null)
            return;
        this.CurrentBaits.RemoveAll(b => b.Source == enemy && b.Target == player);
        if (this.World.Actors.Find(tether.Target) is { } target)
            this.TetherOnActor.Remove((target, tether.ID));
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var baits = this.ActiveBaitsOn(actor);
        if (baits.Count == 0)
            return;

        var immunity = this.IsImmune(slot, baits[0].Activation);
        var dist = (baits[0].Source.Position - actor.Position).LengthSq();
        if (immunity)
            hints.Add(HintImmunityGood, false);
        else if (dist < this.minSq && this.TetherOnActor.Contains((actor, this.TIDBad)))
            hints.Add(HintBad);
        else if (dist >= this.minSq || this.TetherOnActor.Contains((actor, this.TIDGood)))
            hints.Add(HintGood, false);

        if (this.KnockbackImmunity && !immunity)
            hints.Add(HintImmunityBad);
    }

    /// <summary>Split a tether into its player and enemy ends (either end can be the source).</summary>
    public (Actor? player, Actor? enemy) DetermineTetherSides(Actor source, ActorTetherInfo tether)
    {
        if (tether.ID != this.TIDGood && tether.ID != this.TIDBad)
            return (null, null);
        if (this.World.Actors.Find(tether.Target) is not { } target)
            return (null, null);
        return Array.IndexOf(this.World.Party.WithoutSlot(), source) >= 0 ? (source, target) : (target, source);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var baits = this.ActiveBaits;
        if (baits.Count == 0)
            return;

        var activation = baits.Find(b => b.Target == actor).Activation;
        var isImmune = this.IsImmune(slot, activation);

        if (this.Shape != null)
            base.AddAIHints(slot, actor, assignment, hints);

        // being immune means the tether can be ignored — otherwise run out to the minimum distance
        if (isImmune)
            return;
        foreach (var b in baits)
            if (b.Target == actor)
                hints.AddForbiddenZone(new SDCircle(b.Source.Position, this.MinimumDistance), b.Activation);
    }
}

/// <summary>
/// Single-tether-id form of <see cref="StretchTetherDuo"/> — the same tether means both "slack" and
/// "taut". With <paramref name="needToKite"/> the tethered add must be dragged around rather than just
/// out-ranged. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class StretchTetherSingle(ModuleBase module, uint tetherID, float minimumDistance, AOEShape? shape = null, uint aid = default, uint enemyOID = default, double activationDelay = default, bool knockbackImmunity = false, bool needToKite = false)
    : StretchTetherDuo(module, minimumDistance, activationDelay, tetherID, tetherID, shape, aid, enemyOID, knockbackImmunity)
{
    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.CurrentBaits.Count == 0)
            return;
        if (needToKite && this.TetherOnActor.Contains((actor, this.TIDBad)))
            hints.Add("Kite the add!");
        else
            base.AddHints(slot, actor, hints);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        if (!needToKite)
            return;
        var baits = this.ActiveBaitsOn(pc);
        if (baits.Count != 0 && this.IsTether(pc, this.TIDBad))
            this.Arena.Actor(baits[0].Source, Colors.Object);
    }
}

/// <summary>
/// A charge/dash tether: the enemy will rush along the tether, so the danger is a rectangle whose length
/// tracks the live distance to the tethered player. Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
/// </summary>
public class BaitAwayChargeTether(ModuleBase module, float halfWidth, double activationDelay, uint aidGood, uint aidBad = default, uint tetherIDBad = 57u, uint tetherIDGood = 1u, uint enemyOID = default, float minimumDistance = default)
    : StretchTetherDuo(module, minimumDistance, activationDelay, tetherIDBad, tetherIDGood, new AOEShapeRect(default, halfWidth), default, enemyOID)
{
    public readonly uint AidGood = aidGood;
    public readonly uint AidBad = aidBad; // some fights change the AID between the good and bad tether
    public readonly float HalfWidth = halfWidth;

    public override void Update()
    {
        base.Update();
        // the charge reaches exactly as far as the target, so the rect grows/shrinks with them
        for (var i = 0; i < this.CurrentBaits.Count; ++i)
        {
            ref var b = ref this.CurrentBaits.Ref(i);
            var length = (b.Target.Position - b.Source.Position).Length();
            if (b.Shape is AOEShapeRect rect && rect.LenFront != length)
                b.Shape = new AOEShapeRect(length, this.HalfWidth);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent cast)
    {
        if (cast.Action.ID != this.AidGood && cast.Action.ID != this.AidBad)
            return;
        ++this.NumCasts;
        var id = cast.MainTargetID;
        for (var i = 0; i < this.CurrentBaits.Count; ++i)
        {
            if (this.CurrentBaits[i].Target.InstanceID == id)
            {
                this.CurrentBaits.RemoveAt(i);
                return;
            }
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.ActiveBaits.Count == 0)
            return;
        base.AddHints(slot, actor, hints);
        foreach (var b in this.CurrentBaits)
        {
            if (b.Target.InstanceID != actor.InstanceID)
                continue;
            if (this.PlayersClippedBy(in b).Count != 0)
            {
                hints.Add(BaitAwayHint);
                return;
            }
        }
    }
}
