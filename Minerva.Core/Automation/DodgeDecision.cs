using System;
using System.Globalization;

namespace Minerva;

/// <summary>Why the dodge wanted to move this frame -- or did not.</summary>
public enum DodgeReason : byte
{
    None = 0,
    /// <summary>Standing here gets hit within the horizon.</summary>
    Danger = 1,
    /// <summary>Safe, but out of the uptime band (range) and walking back.</summary>
    Uptime = 2,
    /// <summary>Safe and in range, but on the wrong side for the requested positional.</summary>
    Positional = 3,
    /// <summary>Wanted to move and found nowhere reachable and safe in time.</summary>
    NoSafeSpot = 4,
    /// <summary>Standing on ground that will fire, with time still to spare: stepping off it now, while the
    /// step is short, rather than at the last second when the way out may be through another telegraph.</summary>
    Clearing = 5,
}

/// <summary>What stopped a wanted move from being steered, if anything.</summary>
public enum DodgeBlocker : byte
{
    None = 0,
    /// <summary>No boss module and nothing guessable: the dodge had no picture of the fight at all.</summary>
    NoModule = 1,
    /// <summary>Auto-move is off; the radar showed the spot and nobody walked there.</summary>
    AutoDodgeOff = 2,
    /// <summary>A stand-still mechanic forbade moving.</summary>
    MustNotMove = 3,
    /// <summary>A rotation's hold (hardcast, raise) kept the character still.</summary>
    Hold = 4,
    /// <summary>No movement controller could move anyone: hook signature outdated and no navmesh.</summary>
    NoController = 5,
    /// <summary>A gaze resolves within the next moment: a step turns the character along its walk, so it holds.</summary>
    Gaze = 6,
    /// <summary>The character is hardcasting on ground that is not yet lethal; the cast was left to finish.</summary>
    Casting = 7,
    /// <summary>Stunned, asleep, bound or petrified: the game ignores movement, whoever asks for it.</summary>
    Incapacitated = 8,
}

/// <summary>Which mover a steer went through. Mirrors the plugin's movement controller so a log can say
/// whether a steer had anything under it.</summary>
public enum Mover : byte
{
    None = 0,
    /// <summary>The raw input override.</summary>
    Hook = 1,
    /// <summary>The navmesh plugin steering straight at a point.</summary>
    NavSteer = 2,
    /// <summary>The navmesh plugin walking a handed-over path.</summary>
    NavPath = 3,
}

/// <summary>
/// What the dodge decided at one moment of a fight, in the shape a person asks about afterwards: did it
/// want to move, to where, why, and what stopped it. Recorded into the replay beside the world (see
/// <see cref="OpDodgeDecision"/>) so a hit taken can be explained from the log rather than from memory.
/// </summary>
public readonly record struct DodgeDecision(bool NeedToMove, bool Found, WPos Target, DodgeReason Reason, DodgeBlocker Blocker, bool Steering)
{
    /// <summary>True once a recorded decision has been applied; the default is "nothing known yet".</summary>
    public bool Known { get; init; }

    /// <summary>A gaze was up: the hints carried forbidden directions this frame.</summary>
    public bool GazeUp { get; init; }

    /// <summary>Minerva issued a facing this frame (Face is on and a safe heading existed).</summary>
    public bool Turning { get; init; }

    /// <summary>Which mover the steer went through.</summary>
    public Mover Mover { get; init; }

    /// <summary>The mover reported it was actually driving. A steer with this false never became motion.</summary>
    public bool MoverBusy { get; init; }

    /// <summary>The mover fields were recorded (logs before 2026-09-05 evening have none).</summary>
    public bool MoverKnown { get; init; }

    /// <summary>One line for a live readout: what the dodge is doing right now.</summary>
    public string Describe(float distanceToTarget)
    {
        var dist = distanceToTarget.ToString("0.0", CultureInfo.InvariantCulture);
        if (this.Blocker == DodgeBlocker.NoModule)
            return "no module and nothing guessable";
        if (!this.NeedToMove)
            return "safe where you stand" + this.GazeSuffix;
        if (!this.Found)
            return "wants to move -- no safe spot reachable in time";
        var why = this.Reason switch
        {
            DodgeReason.Danger => "danger",
            DodgeReason.Uptime => "regaining uptime",
            DodgeReason.Clearing => "clearing ground that will fire",
            DodgeReason.Positional => "positional",
            _ => "move",
        };
        return this.Blocker switch
        {
            DodgeBlocker.None when this.Steering => $"steering to a safe spot {dist}y away ({why})" + (this.MoverKnown && !this.MoverBusy ? " -- mover idle" : ""),
            DodgeBlocker.AutoDodgeOff => $"safe spot {dist}y away ({why}) -- auto-move is off",
            DodgeBlocker.MustNotMove => $"safe spot {dist}y away ({why}) -- holding: stand-still mechanic",
            DodgeBlocker.Hold => $"safe spot {dist}y away ({why}) -- holding for a rotation's cast",
            DodgeBlocker.NoController => $"safe spot {dist}y away ({why}) -- no movement controller",
            DodgeBlocker.Gaze => $"safe spot {dist}y away ({why}) -- holding still: a gaze resolves in under a second",
            DodgeBlocker.Casting => $"safe spot {dist}y away ({why}) -- hardcasting; the ground is not lethal yet, so the cast is left to finish",
            DodgeBlocker.Incapacitated => $"safe spot {dist}y away ({why}) -- stunned, asleep or bound: the game will not move you",
            _ => $"safe spot {dist}y away ({why}) -- not steering",
        } + this.GazeSuffix;
    }

    private string GazeSuffix => !this.GazeUp ? "" : this.Turning ? " -- gaze up, turning away" : " -- gaze up, not turning (Face off)";

    /// <summary>
    /// The answer to "why did I eat that", given whether the module had drawn anything for the hit and how
    /// far the chosen spot was. Each branch names the one thing to change.
    /// </summary>
    public string Explain(bool drawn, float distanceToTarget)
    {
        var dist = distanceToTarget.ToString("0.0", CultureInfo.InvariantCulture);
        if (!drawn)
            return "Minerva drew nothing for this: the module has no component for it, so no dodge was possible (module gap).";
        if (!this.Known)
            return "the module drew it, but this recording predates decision logging, so why it was not dodged is not on record.";
        if (this.Blocker == DodgeBlocker.NoModule)
            return "no module was active for this fight, so there was nothing to dodge with.";
        if (!this.NeedToMove)
            return "Minerva judged the spot safe inside its margin and lead and did not move: the AOE was larger than drawn, or resolved earlier than the cast said.";
        if (!this.Found)
            return "Minerva wanted to move and found no reachable safe cell in time.";
        return this.Blocker switch
        {
            DodgeBlocker.AutoDodgeOff => $"a safe spot {dist}y away was shown, but auto-move is off -- guidance only.",
            DodgeBlocker.MustNotMove => $"a safe spot {dist}y away was found, but a stand-still mechanic forbade moving.",
            DodgeBlocker.Hold => $"a safe spot {dist}y away was found, but a rotation's hold kept the character still for a cast.",
            DodgeBlocker.NoController => $"a safe spot {dist}y away was found, but no movement controller could walk there (hook signature or navmesh).",
            DodgeBlocker.Gaze => $"a safe spot {dist}y away was found, but Minerva held still for a gaze about to resolve; the ground cost what the gaze would have.",
            DodgeBlocker.Casting => $"a safe spot {dist}y away was found while you were hardcasting, and Minerva judged the ground not yet lethal, so it let the cast finish instead of moving.",
            DodgeBlocker.Incapacitated => $"a safe spot {dist}y away was found, but you were stunned, asleep or bound: no dodge could move you, and nothing here was a dodge failure. The mechanic to answer is whatever applied it.",
            // walking for uptime or a positional means the destination was judged clear, so a hit there is a zone
            // the module never drew (Web of Terror, 2026-09-05: back toward the boss into an undrawn funnel lane)
            DodgeBlocker.None when this.Steering && this.Reason == DodgeReason.Clearing
                => $"Minerva was stepping off ground that was going to fire, to a spot {dist}y away, and something landed on the way: the zone that caught you resolved sooner than the one being cleared.",
            DodgeBlocker.None when this.Steering && this.Reason is DodgeReason.Uptime or DodgeReason.Positional
                => $"Minerva was walking {(this.Reason == DodgeReason.Uptime ? "back for uptime" : "for a positional")} to a spot {dist}y away that the module considered clear, and the hit landed on the way: the module did not draw this AOE where it fell (module gap), not a late dodge.",
            DodgeBlocker.None when this.Steering && this.MoverKnown && !this.MoverBusy => $"Minerva was steering to a safe spot {dist}y away but the movement layer reported nothing driving (mover: {this.Mover}), so the steer never became motion.",
            DodgeBlocker.None when this.Steering => $"Minerva was steering to a safe spot {dist}y away and did not arrive in time: more clearance lead, or the AOE resolved early.",
            _ => $"a safe spot {dist}y away was chosen but steering did not run that frame.",
        };
    }
}

/// <summary>
/// The dodge decision as a replay op: recorded when it changes, so the log carries what Minerva chose
/// beside what the world did. Applying it sets <see cref="WorldState.LastDodge"/> and nothing else.
/// </summary>
public sealed class OpDodgeDecision(DodgeDecision value) : WorldState.Operation
{
    public readonly DodgeDecision Value = value;

    protected override void Exec(WorldState ws) => ws.LastDodge = this.Value with { Known = true };

    public override void Write(OperationOutput o)
    {
        var flags = (this.Value.NeedToMove ? 1u : 0u) | (this.Value.Found ? 2u : 0u) | (this.Value.Steering ? 4u : 0u)
            | (this.Value.GazeUp ? 8u : 0u) | (this.Value.Turning ? 16u : 0u);
        o.Tag("DODG").Emit(flags).Emit(this.Value.Target.X).Emit(this.Value.Target.Z).Emit((uint)this.Value.Reason).Emit((uint)this.Value.Blocker)
            .Emit((uint)this.Value.Mover).Emit(this.Value.MoverBusy);
    }

    /// <summary>The parser's half of <see cref="Write"/>. The mover fields are trailing and optional, so
    /// a log written before they existed still reads.</summary>
    public static OpDodgeDecision Read(OpTokenReader r)
    {
        var flags = r.NextU32();
        var x = r.NextFloat();
        var z = r.NextFloat();
        var reason = (DodgeReason)r.NextU32();
        var blocker = (DodgeBlocker)r.NextU32();
        var mover = Mover.None;
        var busy = false;
        var moverKnown = false;
        if (r.HasMore)
        {
            mover = (Mover)r.NextU32();
            busy = r.NextBool();
            moverKnown = true;
        }
        return new(new DodgeDecision((flags & 1u) != 0, (flags & 2u) != 0, new WPos(x, z), reason, blocker, (flags & 4u) != 0)
        {
            GazeUp = (flags & 8u) != 0,
            Turning = (flags & 16u) != 0,
            Mover = mover,
            MoverBusy = busy,
            MoverKnown = moverKnown,
        });
    }
}
