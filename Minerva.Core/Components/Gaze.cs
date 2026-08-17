namespace Minerva.Components;

/// <summary>
/// Look-away mechanic: while a cast of <c>WatchedAction</c> is active, the player must not face its
/// source. Warns when the source is in the player's front hemisphere and marks the source on the
/// radar. Not a positional dodge, so it contributes no forbidden zone.
/// </summary>
public class Gaze(ModuleBase module, uint aid) : ModuleComponent(module)
{
    public readonly uint WatchedAction = aid;
    protected readonly List<ulong> Sources = [];

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Sources.Add(caster.InstanceID);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Sources.Remove(caster.InstanceID);
    }

    public bool Active => this.Sources.Count > 0;

    /// <summary>True if <paramref name="actor"/> is facing toward the source (front hemisphere).</summary>
    public static bool FacingToward(Actor actor, WPos source)
    {
        var toSource = (source - actor.Position).Normalized();
        return WDir.Dot(actor.Rotation.ToDirection(), toSource) > 0f;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var id in this.Sources)
        {
            var a = this.World.Actors.Find(id);
            if (a != null)
                this.Arena.AddCircle(a.Position, 1f, Colors.Danger, 2f);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var id in this.Sources)
        {
            var a = this.World.Actors.Find(id);
            if (a != null && FacingToward(actor, a.Position))
            {
                hints.Add("Look away!");
                return;
            }
        }
    }
}

/// <summary>
/// Generic gaze/weakpoint base with positioned "eyes": subclasses expose <see cref="ActiveEyes"/> and
/// this warns the player to look away (or, for an inverted eye, to face it) and marks each eye. Ported
/// from BossmodReborn's GenericGaze (BSD-3; see THIRD-PARTY-NOTICES.txt), adapted to Minerva — the eye is
/// drawn on the arena (no off-screen ImGui indicator) and there is no directional AI hint yet.
/// </summary>
public abstract class GenericGaze(ModuleBase module, uint aid = default) : CastCounter(module, aid)
{
    public readonly struct Eye(WPos position, DateTime activation = default, Angle forward = default, float range = 10000f, bool inverted = false, ulong actorID = default)
    {
        public readonly WPos Position = position;
        public readonly DateTime Activation = activation;
        public readonly Angle Forward = forward;   // if non-zero, treat this side as "forward" for the hit test
        public readonly float Range = range;
        public readonly bool Inverted = inverted;  // if true, the player must FACE the eye instead of looking away
        public readonly ulong ActorID = actorID;
    }

    public abstract ReadOnlySpan<Eye> ActiveEyes(int slot, Actor actor);

    // hit = the eye is within ~45° of the actor's facing (accounting for the eye's Forward offset)
    public static bool HitByEye(Actor actor, in Eye eye)
        => (actor.Rotation + eye.Forward).ToDirection().Dot((eye.Position - actor.Position).Normalized()) >= 0.707107f;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var eyes = this.ActiveEyes(slot, actor);
        foreach (ref readonly var eye in eyes)
        {
            if (actor.Position.InCircle(eye.Position, eye.Range) && HitByEye(actor, in eye) != eye.Inverted)
            {
                hints.Add(eye.Inverted ? "Face the eye!" : "Turn away from gaze!");
                break;
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var eyes = this.ActiveEyes(pcSlot, pc);
        foreach (ref readonly var eye in eyes)
        {
            var danger = HitByEye(pc, in eye) != eye.Inverted;
            this.Arena.AddCircle(eye.Position, 1.5f, danger ? Colors.Danger : Colors.Safe, 2f);
        }
    }
}

/// <summary>Gaze that resolves at the end of a cast. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).</summary>
public class CastGaze(ModuleBase module, uint aid, bool inverted = false, float range = 10000f, int maxCasts = int.MaxValue) : GenericGaze(module, aid)
{
    public readonly List<Eye> Eyes = [];
    public int MaxCasts = maxCasts;
    public readonly bool Inverted = inverted;
    public readonly float Range = range;

    public override ReadOnlySpan<Eye> ActiveEyes(int slot, Actor actor)
    {
        var max = Math.Min(this.Eyes.Count, this.MaxCasts);
        return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(this.Eyes)[..max];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
        {
            var pos = cast.LocXZ != default ? cast.LocXZ : caster.Position;
            this.Eyes.Add(new Eye(pos, this.Module.CastFinishAt(cast), default, this.Range, this.Inverted, caster.InstanceID));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        for (var i = 0; i < this.Eyes.Count; ++i)
        {
            if (this.Eyes[i].ActorID == caster.InstanceID)
            {
                this.Eyes.RemoveAt(i);
                return;
            }
        }
    }
}

/// <summary>A <see cref="CastGaze"/> that watches several actions. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).</summary>
public class CastGazes(ModuleBase module, uint[] aids, bool inverted = false, float range = 10000f, int maxCasts = int.MaxValue) : CastGaze(module, default, inverted, range, maxCasts)
{
    private readonly uint[] AIDs = aids;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (Array.IndexOf(this.AIDs, cast.Action.ID) >= 0)
        {
            var pos = cast.LocXZ != default ? cast.LocXZ : caster.Position;
            this.Eyes.Add(new Eye(pos, this.Module.CastFinishAt(cast), default, this.Range, this.Inverted, caster.InstanceID));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (Array.IndexOf(this.AIDs, cast.Action.ID) < 0)
            return;
        for (var i = 0; i < this.Eyes.Count; ++i)
        {
            if (this.Eyes[i].ActorID == caster.InstanceID)
            {
                this.Eyes.RemoveAt(i);
                return;
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (Array.IndexOf(this.AIDs, spell.Action.ID) >= 0)
            ++this.NumCasts;
    }
}
