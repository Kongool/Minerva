// Ported from Veyn's bossmod (awgil/ffxiv_bossmod, BSD-3; see THIRD-PARTY-NOTICES.txt). BossmodReborn has no
// module for this encounter (its Labyrinth numbering skips it); the A12 here is Veyn's, not BossmodReborn's.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A12Atomos;

public enum OID : uint
{
    Boss = 0x963, // R4.000, x3
    Dira = 0x966, // R2.000, x0-2 (spawn during fight)
    Valefor = 0x964, // R5.000, x0-2 (spawn during fight)
    GreaterDemon = 0x965, // R2.000, x0-2 (spawn during fight)

    RingA = 0x1E8F7D,
    RingB = 0x1E8F7E,
    RingC = 0x1E8F7F,

    PlatformA = 0x1E8F80,
    PlatformB = 0x1E8F81,
    PlatformC = 0x1E8F82,
}

public enum AID : uint
{
    AutoAttack = 1461, // GreaterDemon/Valefor/Dira->player, no cast, single-target
    DarkOrb = 911, // GreaterDemon->player, no cast, single-target
    TheLook = 1791, // Valefor->self, no cast, range 6+R 120?-degree cone
    VoidFireII = 1829, // Dira->location, 3.0s cast, range 5 circle
}

class Adds(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.Dira, (uint)OID.Valefor, (uint)OID.GreaterDemon]);
class VoidFireII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VoidFireII, new AOEShapeCircle(5f));

// Each Atomos is invulnerable until the pad on the neighbouring platform is held; adds on other platforms
// are unreachable. Both are forbidden targets rather than obstacles: nothing here needs dodging.
class Ring(ModuleBase module) : Components.GenericInvincible(module)
{
    private BitMask vulnerable;
    private readonly List<Actor> forbidden = [];

    protected override ReadOnlySpan<Actor> ForbiddenTargets(int slot, Actor actor)
    {
        this.forbidden.Clear();
        var platform = A12Atomos.GetPlatform(actor);
        foreach (var e in this.Module.Enemies([(uint)OID.Dira, (uint)OID.Valefor, (uint)OID.GreaterDemon]))
            if (platform != A12Atomos.GetPlatform(e))
                this.forbidden.Add(e);

        var m = (A12Atomos)this.Module;
        if (m.AtomosA is { } a && (platform != 0 || !this.vulnerable[0]))
            this.forbidden.Add(a);
        if (m.AtomosB is { } b && (platform != 1 || !this.vulnerable[1]))
            this.forbidden.Add(b);
        if (m.AtomosC is { } c && (platform != 2 || !this.vulnerable[2]))
            this.forbidden.Add(c);
        return CollectionsMarshal.AsSpan(this.forbidden);
    }

    public override void OnActorEAnim(Actor actor, uint state)
    {
        var id = (OID)actor.OID switch
        {
            OID.RingA => 0,
            OID.RingB => 1,
            OID.RingC => 2,
            _ => -1,
        };
        if (id < 0)
            return;
        if (state == 0x10042200)
            this.vulnerable.Clear(id);
        else if (state == 0x04400880)
            this.vulnerable.Set(id);
    }
}

// four players on a pad open the ring on the next platform; until then the pad is where this platform stands
class Pad(ModuleBase module) : ModuleComponent(module)
{
    private readonly Actor?[] pads = new Actor?[3];
    private BitMask activePads;

    public override void Update()
    {
        if (this.pads[0] == null)
        {
            this.pads[0] = this.Module.Enemies((uint)OID.PlatformA).FirstOrDefault();
            this.pads[1] = this.Module.Enemies((uint)OID.PlatformB).FirstOrDefault();
            this.pads[2] = this.Module.Enemies((uint)OID.PlatformC).FirstOrDefault();
        }
    }

    // pad activate = 00400080, deactivate = 00040200
    public override void OnActorEAnim(Actor actor, uint state)
    {
        var ix = (int)actor.OID - (int)OID.PlatformA;
        if (ix < 0 || ix > 2)
            return;
        if (state == 0x00400080)
            this.activePads.Set(ix);
        else if (state == 0x00040200)
            this.activePads.Clear(ix);
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        var myPlatform = A12Atomos.GetPlatform(pc);
        for (var i = 0; i < 3; i++)
        {
            if (this.pads[i] is not { } pad)
                continue;
            var color = this.GetPadBoss(i)?.IsDeadOrDestroyed == true ? Colors.Border
                : this.activePads[i] ? Colors.Safe
                : Colors.Danger;
            this.Arena.AddCircle(pad.Position, 4f, color, i == myPlatform ? 2f : 1f);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var platform = A12Atomos.GetPlatform(actor);
        if (this.pads[platform] is not { } myPad || this.GetPadBoss(platform)?.IsDeadOrDestroyed == true)
            return;
        if (this.Raid.WithoutSlot().InRadius(myPad.Position, 4f).Count() < 4)
            hints.Add("Stand on pad!");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var platform = A12Atomos.GetPlatform(actor);
        if (this.pads[platform] is not { } myPad || this.GetPadBoss(platform)?.IsDeadOrDestroyed == true)
            return;
        if (this.Raid.WithoutSlot().InRadius(myPad.Position, 4f).Exclude(actor).Count() < 4)
            hints.AddForbiddenZone(new SDDonut(myPad.Position, 4f, 500f), DateTime.MaxValue);
    }

    private Actor? GetPadBoss(int platform) => ((A12Atomos)this.Module).Bosses[(platform + 1) % 3];
}

class A12AtomosStates : StateMachineBuilder
{
    public A12AtomosStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<Adds>()
            .ActivateOnEnter<Ring>()
            .ActivateOnEnter<Pad>()
            .ActivateOnEnter<VoidFireII>()
            .Raw.Update = () => module.Enemies((uint)OID.Boss).All(b => b.IsDead);
    }
}

[ModuleInfo(CFCID = 92u, NameID = 1872u, PrimaryActorOID = (uint)OID.Boss, Maturity = ModuleMaturity.WIP, Contributors = "ported from Veyn's bossmod (awgil)")]
public class A12Atomos(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(232.5f, 280f), AtomosBounds())
{
    public static int GetPlatform(Actor a)
    {
        var zdist = a.Position.Z - 280f;
        return MathF.Abs(zdist) < 15f ? 1 : zdist < 0f ? 0 : 2;
    }

    // three platforms stacked north to south, each with four notches cut into its long edges
    private static ArenaBoundsCustom AtomosBounds()
    {
        var center = new WPos(232.5f, 280f);
        var platforms = new Shape[3];
        var notches = new List<Shape>(12);
        WDir[] notch = [new(-5.2f, 0f), new(-2.5f, 2.6f), new(2.5f, 2.6f), new(5.2f, 0f)];
        for (var i = 0; i < 3; ++i)
        {
            var pc = center + new WDir(0f, (i - 1) * 35.2f);
            platforms[i] = new Rectangle(pc, 37.65f, 12.3f);
            foreach (var dx in new[] { -18.8f, 6.16f })
            {
                foreach (var sign in new[] { -1f, 1f })
                {
                    var o = pc + new WDir(dx, sign * 12.8f);
                    notches.Add(new PolygonCustom([.. notch.Select(d => o + new WDir(d.X, -sign * d.Z))]));
                }
            }
        }
        return new ArenaBoundsCustom(platforms, [.. notches]);
    }

    public Actor? AtomosA { get; private set; }
    public Actor? AtomosB { get; private set; }
    public Actor? AtomosC { get; private set; }

    public Actor?[] Bosses => [this.AtomosA, this.AtomosB, this.AtomosC];

    protected override void UpdateModule()
    {
        this.AtomosA ??= this.Enemies((uint)OID.Boss).FirstOrDefault(b => b.Position.InCircle(new WPos(253f, 244f), 5f));
        this.AtomosB ??= this.Enemies((uint)OID.Boss).FirstOrDefault(b => b.Position.InCircle(new WPos(253f, 279f), 5f));
        this.AtomosC ??= this.Enemies((uint)OID.Boss).FirstOrDefault(b => b.Position.InCircle(new WPos(253f, 315f), 5f));
    }

    protected override void DrawEnemies(int pcSlot, Actor pc) => this.Arena.Actors(this.Enemies((uint)OID.Boss), Colors.Enemy);
}
