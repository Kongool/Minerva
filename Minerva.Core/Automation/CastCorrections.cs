using System;
using System.Collections.Generic;

namespace Minerva.Automation;

/// <summary>
/// What a content's author knows about casts the game's sheet describes wrongly, for content with no module.
/// <para>The cast-bar guess (<see cref="AutoHints"/>) reads a cast's shape off the sheet, and the sheet has no side: a
/// right-sided and a left-sided cleave are the same 180-degree cone, so both are drawn in front of the mob, and one of
/// them sends the character into the hit. It does not carry a ring's safe hole either, and a hit that follows a cast
/// with no cast bar of its own is not on it at all. Pilgrim's Traverse, 2026-10-17, against Splatoon's presets: six
/// side or rear cleaves drawn in front, five follow-up hits never drawn, three rings with the hole guessed too big.</para>
/// <para>A zone module builds one of these from its table. The guess stands aside for every cast listed
/// (<see cref="Replaces"/>) and this draws them instead, with what each leaves behind.</para>
/// </summary>
public sealed class CastCorrections : IDisposable
{
    /// <summary>
    /// One cast's real shape, built for the caster that casts it (a cone or a line starts at a large mob's edge), and
    /// the hits that follow it.
    /// </summary>
    public sealed record class Entry(Func<Actor, AOEShape> Shape, params FollowUp[] Then);

    /// <summary>
    /// A hit after the cast with no cast bar: drawn on the caster from the moment the cast lands, until
    /// <paramref name="EndedBy"/> lands <paramref name="Count"/> times or <paramref name="Seconds"/> pass, whichever
    /// is first. A follow-up whose own action is not known has only the time.
    /// </summary>
    public sealed record class FollowUp(Func<Actor, AOEShape> Shape, double Seconds, uint EndedBy = 0, int Count = 1);

    private sealed class Zone(ulong caster, uint action, AOEShape shape, WPos origin, Angle rotation, DateTime activation, DateTime expires, uint endedBy, int left, bool onCaster)
    {
        public readonly ulong Caster = caster;
        public readonly uint Action = action; // the cast this is the bar of; 0 for a follow-up
        public readonly AOEShape Shape = shape;
        public readonly WPos Origin = origin;
        public readonly Angle Rotation = rotation;
        public readonly DateTime Activation = activation;
        public readonly DateTime Expires = expires;
        public readonly uint EndedBy = endedBy;
        public int Left = left;
        public readonly bool OnCaster = onCaster; // a follow-up is the mob's own body: it goes where the mob goes
    }

    /// <summary>How long a cast's zone outlives its bar if the hit is never seen: a dropped packet must not leave it up.</summary>
    private const double CastGrace = 1d;

    private readonly WorldState world;
    private readonly IReadOnlyDictionary<uint, Entry> table;
    private readonly List<Zone> zones = [];
    private readonly EventSubscriptions subscriptions;

    public CastCorrections(WorldState world, IReadOnlyDictionary<uint, Entry> table)
    {
        this.world = world;
        this.table = table;
        this.subscriptions = new(
            world.Actors.CastStarted.Subscribe(this.OnCastStarted),
            world.Actors.CastFinished.Subscribe((caster, cast) => this.zones.RemoveAll(z => z.Caster == caster.InstanceID && z.Action == cast.Action.ID)),
            world.Actors.CastEvent.Subscribe(this.OnCastEvent),
            world.Actors.Removed.Subscribe(a => this.zones.RemoveAll(z => z.Caster == a.InstanceID)));
    }

    public void Dispose() => this.subscriptions.Dispose();

    /// <summary>Is this cast drawn here, so the guess must leave it alone?</summary>
    public bool Replaces(uint action) => this.table.ContainsKey(action);

    /// <summary>Zones being drawn right now: casts in progress and the follow-ups still to land.</summary>
    public int Count => this.zones.Count;

    private void OnCastStarted(Actor caster)
    {
        if (caster.CastInfo is not { } cast || !this.table.TryGetValue(cast.Action.ID, out var entry))
            return;
        var action = cast.Action.ID;
        this.zones.RemoveAll(z => z.Caster == caster.InstanceID && z.Action == action);
        var lands = this.world.FutureTime(cast.RemainingTime);
        var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position;
        this.zones.Add(new Zone(caster.InstanceID, action, entry.Shape(caster), origin, cast.Rotation, lands, lands.AddSeconds(CastGrace), 0, 1, false));
    }

    private void OnCastEvent(Actor caster, ActorCastEvent ev)
    {
        var action = ev.Action.ID;
        for (var i = this.zones.Count - 1; i >= 0; --i)
        {
            var z = this.zones[i];
            if (z.Caster != caster.InstanceID)
                continue;
            if (z.Action == action || z.EndedBy == action && --z.Left <= 0)
                this.zones.RemoveAt(i);
        }

        if (!this.table.TryGetValue(action, out var entry))
            return;
        var now = this.world.CurrentTime;
        foreach (var f in entry.Then)
            this.zones.Add(new Zone(caster.InstanceID, 0, f.Shape(caster), caster.Position, caster.Rotation, now, now.AddSeconds(f.Seconds), f.EndedBy, f.Count, true));
    }

    /// <summary>Every zone still to land, as forbidden ground. A follow-up is drawn where its caster is now, facing as it faces.</summary>
    public void AddForbiddenZones(AIHints hints)
    {
        var now = this.world.CurrentTime;
        this.zones.RemoveAll(z => now > z.Expires);
        foreach (var z in this.zones)
        {
            var caster = z.OnCaster ? this.world.Actors.Find(z.Caster) : null;
            hints.AddForbiddenZone(z.Shape, caster?.Position ?? z.Origin, caster?.Rotation ?? z.Rotation, z.Activation);
        }
    }
}
