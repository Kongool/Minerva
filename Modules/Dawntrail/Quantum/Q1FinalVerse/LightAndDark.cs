// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Quantum.Q1FinalVerse;

[SkipLocalsInit]
sealed class LightAndDark(ModuleBase module) : Endwalker.DeepDungeon.PilgrimsTraverse.LightAndDarkBase(module)
{
    private readonly Q1FinalVerse bossmod = (Q1FinalVerse)module;
    private bool boundsOfSinTowers;

    // From the towers appearing to their resolve Bounds of Sin has its own places to stand (out or in, the pairs, a
    // tower each); the split's "take your colour" zone left no ground beside them and the toon stood still.
    protected override bool ColourOnHold => boundsOfSinTowers;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(Aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.DrainAether1:
                UpdateWantedStatus(0, true);
                break;
            case (uint)AID.DrainAether2:
                UpdateWantedStatus(1, false);
                break;
            case (uint)AID.DrainAether3:
                UpdateWantedStatus(0, false);
                break;
            case (uint)AID.DrainAether4:
                UpdateWantedStatus(1, true);
                break;
        }

        void UpdateWantedStatus(int slot, bool wantL)
        {
            wantLight[slot] = wantL;
            aetherdrainActive = true;
            activations[slot] = Module.CastFinishAt(spell, -1d);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.DrainAether1 or (uint)AID.DrainAether2 or (uint)AID.DrainAether3 or (uint)AID.DrainAether4)
        {
            if ((++NumCasts & 1) == 0)
            {
                aetherdrainActive = false;
            }
            wantLight[0] = wantLight[1];
            activations[0] = activations[1];
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        switch (status.ID)
        {
            // a recording carries no expiry, and UpdateStatus reads "no time" as the buff being lost
            case (uint)SID.LightVengeance:
                UpdateStatus(ref lightBuff, actor, status.ExpireAt == default ? DateTime.MaxValue : status.ExpireAt);
                break;
            case (uint)SID.DarkVengeance:
                UpdateStatus(ref darkBuff, actor, status.ExpireAt == default ? DateTime.MaxValue : status.ExpireAt);
                break;
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        switch (status.ID)
        {
            case (uint)SID.LightVengeance:
                UpdateStatus(ref lightBuff, actor, default);
                break;
            case (uint)SID.DarkVengeance:
                UpdateStatus(ref darkBuff, actor, default);
                break;
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Module.StateMachine.ActivePhaseIndex == -1)
        {
            var countL = lightBuff.NumSetBits();
            var countD = darkBuff.NumSetBits();
            if (!combined[slot])
            {
                if (combined.NumSetBits() == 0 || countL == 1 && countD == 1)
                {
                    hints.Add(either);
                }
                else if (countL > countD)
                {
                    hints.Add(dark);
                }
                else
                {
                    hints.Add(light);
                }
            }
            else if (darkBuff[slot] && countD > 2 || lightBuff[slot] && countL > 2)
            {
                hints.Add(switchColor);
            }
        }
        else if (boundsOfSinTowers)
        {
            hints.Add(light, !lightBuff[slot]);
        }
        else if (aetherdrainActive)
        {
            if (wantLight[0])
            {
                hints.Add(light, !lightBuff[slot]);
            }
            else
            {
                hints.Add(dark, !darkBuff[slot]);
            }
        }
        else if (!aetherdrainActive)
        {
            if (eaterHP <= 1u && !lightBuff[slot])
            {
                hints.Add(light);
            }
            else if (griefHP <= 1u && !darkBuff[slot])
            {
                hints.Add(dark);
            }
            else if (Math.Abs(hpDifference) > 25f || !combined[slot])
            {
                if (hpDifference > 0f)
                {
                    hints.Add(dark);
                }
                else
                {
                    hints.Add(light);
                }
            }
            else
            {
                hints.Add($"Target {(darkBuff[slot] ? bossmod.BossEater?.Name : Module.PrimaryActor.Name)}!", false);
            }
        }
    }

    public override void Update()
    {
        if (bossmod.BossEater is Actor eater)
        {
            ref var eaterHPref = ref eater.HPMP;
            ref var primaryHPref = ref Module.PrimaryActor.HPMP;
            var eaterHPs = eaterHP = eaterHPref.CurHP;
            var griefHPs = griefHP = primaryHPref.CurHP;
            hpDifference = (int)(eaterHPs - griefHPs) * 100f / primaryHPref.MaxHP;
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            var oid = e.Actor.OID;
            ref var hp = ref e.Actor.HPMP;
            e.Priority = hp.CurHP <= 1u ? AIHints.Enemy.PriorityInvincible : darkBuff[slot] && oid == (uint)OID.DevouredEater ? 0
                : lightBuff[slot] && oid == (uint)OID.EminentGrief ? 0 : oid is (uint)OID.VodorigaMinion or (uint)OID.BloodguardMinion ? 1 : AIHints.Enemy.PriorityInvincible;
        }

        base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x1B)
        {
            switch (state)
            {
                case 0x00020001u:
                    boundsOfSinTowers = true;
                    break;
                case 0x00080004u:
                    boundsOfSinTowers = false;
                    break;
            }
        }
    }
}

[SkipLocalsInit]
sealed class LightDarkNeutralize(ModuleBase module) : Components.GenericStackSpread(module)
{
    public int NumCasts;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.Dark)
        {
            Stacks.Add(new(actor, 2f, 2, 2, World.FutureTime(5.1d)));
        }
    }

    // Not BMR, whose two-yalm stacks on the dark marks say nothing of who joins whom: the two dark toons stood on each
    // other, the light ones four yalms off, and both stacks failed (two Big Bursts, 2026-11-26 22:16). Each dark mark is
    // paired with a light one (BoundsOfSinTowers keeps the marks) and each pair is given its own spot.
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Module.FindComponent<BoundsOfSinTowers>() is { Pairing: true } marks && marks.PairSpot(actor) is { } spot)
            hints.AddForbiddenZone(new SDInvertedCircle(spot, 1f), marks.PairsResolve);
    }

    public override void OnEventVFX(Actor actor, uint vfxID, ulong targetID)
    {
        if (vfxID == 40u)
        {
            ++NumCasts;
            Stacks.Clear(); // below Quantum 40 no state takes this component down after the stacks resolve
        }
    }
}

[SkipLocalsInit]
sealed class BoundsOfSinTowers(ModuleBase module) : Components.GenericTowers(module)
{
    private BitMask forbidden = ~(BitMask)default;

    // Not BMR from here to AddAIHints. The light and dark marks (two of each at Quantum 40) are kept here because this
    // component lives from before the pull until the towers resolve; the stack component is taken down in between.
    // Pair 0 is the lowest dark id with the lowest light id and stands west, pair 1 east: inside the pillars when the
    // outside is hit, outside them when the middle is. After the pairs resolve each toon has a tower of its own on its
    // side, dark north and light south. The base only keeps soakers out and sends nobody in: all four towers went
    // unsoaked and Abyssal Sun killed everyone left (2026-11-26 22:16). Which colour may soak is not known; with four
    // toons and four towers everyone has to, so the mask above is not applied.
    private const float InsideYalms = 2.5f, OutsideYalms = 11.5f, TowerOffset = 7.778f, WalkSpeed = 6f;
    private readonly List<Actor> lights = [], darks = [];
    private bool outside; // kept here: the in-or-out component is taken down right after its blast
    public DateTime PairsResolve;

    public bool Pairing => PairsResolve != default && World.CurrentTime <= PairsResolve.AddSeconds(0.3d);

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID is not ((uint)IconID.Light or (uint)IconID.Dark))
            return;
        if (PairsResolve != default && World.CurrentTime > PairsResolve.AddSeconds(8d))
        {
            lights.Clear();
            darks.Clear();
        }
        var marks = iconID == (uint)IconID.Light ? lights : darks;
        if (!marks.Contains(actor))
        {
            marks.Add(actor);
            marks.Sort(static (a, b) => a.InstanceID.CompareTo(b.InstanceID));
        }
        PairsResolve = World.FutureTime(5.2d);
    }

    private (int Pair, bool Dark)? MarkOf(Actor actor)
    {
        var i = darks.IndexOf(actor);
        if (i >= 0)
            return (i, true);
        i = lights.IndexOf(actor);
        return i >= 0 ? (i, false) : null;
    }

    public WPos? PairSpot(Actor actor)
    {
        if (MarkOf(actor) is not { } mark || mark.Pair > 1)
            return null;
        if (Module.FindComponent<BoundsOfSinEnd>() is { Out: { } o })
            outside = o;
        var reach = outside ? OutsideYalms : InsideYalms;
        return Center + new WDir(mark.Pair == 0 ? -reach : reach, default);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Towers.Count == 0 || Pairing)
            return;
        var activation = Towers[0].Activation;
        WDir offset;
        if (MarkOf(actor) is { Pair: <= 1 } mark)
            offset = new(mark.Pair == 0 ? -TowerOffset : TowerOffset, mark.Dark ? -TowerOffset : TowerOffset);
        else if ((activation - World.CurrentTime).TotalSeconds < 5d)
        {
            // no marks seen: a tower each by id order
            var before = 0;
            var party = Raid.WithoutSlot(false, true, true);
            for (var i = 0; i < party.Length; ++i)
                if (party[i].InstanceID < actor.InstanceID)
                    ++before;
            offset = new((before & 1) == 0 ? -TowerOffset : TowerOffset, (before & 2) == 0 ? -TowerOffset : TowerOffset);
        }
        else
            return;
        var tower = Center + offset;
        // BMR's own text hint wants light for the towers. A toon without it goes by way of a light pool while the walk
        // pool-then-tower still fits before the towers resolve, and straight to its tower otherwise.
        if (Module.FindComponent<LightAndDark>() is { } pools && !pools.HoldsLight(slot) && pools.LightPoolOnTheWay(actor.Position, tower) is { } pool)
        {
            var walk = ((pool.Middle - actor.Position).Length() + (tower - pool.Middle).Length()) / WalkSpeed;
            if ((activation - World.CurrentTime).TotalSeconds > walk + 1.5d)
            {
                hints.AddForbiddenZone(pool.Outside, activation);
                return;
            }
        }
        hints.AddForbiddenZone(new SDInvertedCircle(tower, 1.5f), activation);
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x1B)
        {
            switch (state)
            {
                case 0x00020001u:
                    var party = Raid.WithSlot(true, false, false);
                    var len = party.Length;
                    for (var i = 0; i < len; ++i)
                    {
                        ref var p = ref party[i];
                        if (p.Item2.FindStatus((uint)SID.LightVengeance) == null)
                        {
                            forbidden.Clear(p.Item1);
                        }
                    }
                    var act = World.FutureTime(14.2d);
                    var center = Center;
                    var a45 = 45f.Degrees();
                    var a90 = 90f.Degrees();
                    for (var i = 0; i < 4; ++i)
                    {
                        Towers.Add(new(center + 11f * (i * a90 + a45).ToDirection(), 2f, 1, 1, forbidden, act));
                    }
                    break;
                case 0x00080004u:
                    ++NumCasts;
                    Towers.Clear(); // below Quantum 40 no state takes this component down after the towers resolve
                    break;
            }
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.LightVengeance)
        {
            forbidden.Clear(Raid.FindSlot(actor.InstanceID));
            UpdateTowers();
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.LightVengeance)
        {
            forbidden.Set(Raid.FindSlot(actor.InstanceID));
            UpdateTowers();
        }
    }

    private void UpdateTowers()
    {
        var count = Towers.Count;
        var towers = CollectionsMarshal.AsSpan(Towers);
        for (var i = 0; i < count; ++i)
        {
            towers[i].ForbiddenSoakers = forbidden;
        }
    }
}
