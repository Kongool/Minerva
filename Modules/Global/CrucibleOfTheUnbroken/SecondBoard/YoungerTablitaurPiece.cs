// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondBoard.YoungerTablitaurPiece;

public enum OID : uint
{
    YoungerTablitaurPiece = 0x4C60,
    ElderTablitaurPiece = 0x4C5F, // R3.600, x1
    Gen = 0x4E00, // R1.000, x2
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 50216, // 4C5F/YoungerTablitaurPiece->player, no cast, single-target
    Teleport = 48188, // YoungerTablitaurPiece/4C5F->location, no cast, single-target

    TonzeSwipe1000Visual = 48189, // 4C5F->self, 6.5+0.5s cast, single-target
    TonzeSwipe1000 = 48190, // Helper->self, 7.0s cast, range 60 width 60 rect
    TonzeSwipe1000VisualLong = 48191, // YoungerTablitaurPiece->self, 9.5+0.5s cast, single-target
    TonzeSwipe1000Long = 48192, // Helper->self, 10.0s cast, range 60 width 60 rect
    TonzeSwing1111Visual = 48193, // 4C5F->self, 7.0+1.0s cast, single-target
    TonzeSwing1111 = 48194, // Helper->self, 8.0s cast, range 23 circle
    TonzeSwing1111VisualLong = 48195, // YoungerTablitaurPiece->self, 13.0+1.0s cast, single-target
    TonzeSwing1111Long = 48196, // Helper->self, 14.0s cast, range 23 circle

    TonzeStomp10Teleport = 48197, // YoungerTablitaurPiece->location, 6.3+0.7s cast, single-target
    Shockwave = 48199, // Helper->self, 7.0s cast, range ?-60 donut
    TonzeStomp10 = 48198, // Helper->self, 7.0s cast, range 5 circle

    TonzeStomp10TeleportLong = 48200, // 4C5F->location, 9.8+0.7s cast, single-target
    ShockwaveLong = 48202, // Helper->self, 10.5s cast, range ?-60 donut
    TonzeStomp10Long = 48201, // Helper->self, 10.5s cast, range 5 circle

    // TODO when does this end?
    EndlessSwingBoss = 48205, // 4C5F->self, 5.0+1.0s cast, single-target
    EndlessSwing = 48206, // Helper->4C5F, 6.0s cast, range 8 circle
    EndlessSwing1 = 48207, // Helper->4C5F, no cast, range 8 circle

    EndlessSwipesYounger = 48208, // YoungerTablitaurPiece->self, 5.0s cast, single-target
    EndlessSwipesElder = 48209, // ElderTablitaurPiece->self, 5.0s cast, single-target
    EndlessSwipes1 = 48210, // Helper->self, 5.2s cast, range 40 60.000-degree cone
    EndlessSwipes2 = 48211, // YoungerTablitaurPiece->self, no cast, single-target
    EndlessSwipes3 = 48212, // Helper->self, 0.5s cast, range 40 60-degree cone

    RallyingCheer = 48203, // YoungerTablitaurPiece->ElderTablitaurPiece, 7.0s cast, single-target
    TonzeSlash100 = 48204, // ElderTablitaurPiece->self/player, 9.0s cast, range 65 width 8 rect

    // Enrage - if one dies before the other too early
    DisorientingGroan = 48213, // YoungerTablitaurPiece->self, no cast, single-target
    EndlessSlashes = 48214, // YoungerTablitaurPiece->self, 9.0s cast, single-target
    EndlessSlashes1 = 48216, // Helper->self, no cast, range 80 width 70 rect
    EndlessSlashes2 = 48215, // YoungerTablitaurPiece->self, no cast, single-target
}

public enum IconID : uint
{
    TankBuster = 412, // player->self
    TurnRight = 167, // YoungerTablitaurPiece->self
    TurnLeft = 168, // ElderTablitaurPiece->self
}

public enum TetherID : uint
{
    KnockbackTether = 54, // YoungerTablitaurPiece/4C5F->4E00
    TankBusterTether = 260, // YoungerTablitaurPiece->ElderTablitaurPiece
    UnknownTether = 17, // 4C5F->player - most likely the target tether for the spin attack that follows the player around
    _Gen_Tether_chn_dark001f = 1, // ElderTablitaurPiece->player

}

sealed class Hint(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(Actor actor, GlobalHints hints)
    {
        hints.Add("Kill both at the same time!");
    }
}

sealed class TonzeSwipe1000 : Components.SimpleAOEGroups
{
    public TonzeSwipe1000(ModuleBase module) : base(module, [(uint)AID.TonzeSwipe1000, (uint)AID.TonzeSwipe1000Long], new AOEShapeRect(60.0f, 30.0f),
        expectedNumCasters: 2)
    {
        MaxDangerColor = 1;
        MaxRisky = 1;
    }
}

sealed class TonzeSwing1111 : Components.SimpleAOEGroups
{
    public TonzeSwing1111(ModuleBase module) : base(module, [(uint)AID.TonzeSwing1111, (uint)AID.TonzeSwing1111Long], 23.0f, expectedNumCasters: 2)
    {
        MaxDangerColor = 1;
        MaxRisky = 1;
    }
}

// The generic knockback guards only against being shoved into a zone, not off the floor, and BossmodReborn's gives no hint
// at all. Saar, 2026-10-25: the first Shockwave left him at (138.1,12.4), 5y from the second's source (135,15), and the
// dodge called that safe; the second threw him to (142.5,-8.6), off the 40y square: Bleeding twice. Each pending shove
// forbids the ground it would throw you off from, tested a yalm inside the walls (the test is yes or no, so the dodge's
// margin cannot apply), and is held a second past its cast, because the push lands later (see Ymir's Tsunami).
sealed class Shockwave(ModuleBase module) : Components.SimpleKnockbackGroups(module, [(uint)AID.Shockwave, (uint)AID.ShockwaveLong], 20.0f)
{
    private const float HalfSize = 19.0f;
    private readonly List<(WPos origin, DateTime until)> held = [];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        base.OnCastStarted(caster, spell);
        SortHelpers.SortKnockbacksByActivation(Casters);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.Shockwave or (uint)AID.ShockwaveLong)
        {
            held.Add((spell.LocXZ != default ? spell.LocXZ : caster.Position, World.FutureTime(1d)));
        }
        base.OnCastFinished(caster, spell);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        foreach (var k in Casters)
        {
            if (!IsImmune(slot, k.Activation))
            {
                hints.AddForbiddenZone(new SDKnockbackInAABBRectAwayFromOrigin(Module.Center, k.Origin, Distance, HalfSize, HalfSize), k.Activation);
            }
        }
        held.RemoveAll(h => h.until <= World.CurrentTime);
        foreach (var (origin, until) in held)
        {
            if (!IsImmune(slot, until))
            {
                hints.AddForbiddenZone(new SDKnockbackInAABBRectAwayFromOrigin(Module.Center, origin, Distance, HalfSize, HalfSize), World.CurrentTime);
            }
        }
    }
}

sealed class TonzeStomp10 : Components.SimpleAOEGroups
{
    public TonzeStomp10(ModuleBase module) : base(module, [(uint)AID.TonzeStomp10, (uint)AID.TonzeStomp10Long], 5.0f, expectedNumCasters: 2)
    {
        MaxDangerColor = 1;
        MaxRisky = 1;
    }
}

// TODO change shape to arc cap
// Ticks about once a second (EndlessSwing1, no cast) around the Elder, and the Elder walks after its target at 2.5-2.9y/s,
// so a circle where it stands is a yalm or three short by the next tick. Korha, 2026-10-25: parked 8.6-9.0y from the
// Elder, just past an 8y circle and its margin, she was hit twice, 279 each, and died. Drawn as the circle carried a tick's
// walk ahead along the Elder's facing; with that, she was inside it at every tick from 103.4s.
sealed class EndlessSwing(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.EndlessSwing)   // named for the validator; OnEventCast below does the counting
{
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeCapsule shape = new(8.0f, 3.0f);
    private Actor? spellSource;
    private readonly EndlessSwipes? endlessSwipes = module.FindComponent<EndlessSwipes>();
    // Over once it stops ticking: the first tick comes 7.0-7.2s after the cast starts, then every 1.2s or less (seven
    // pulls, 2026-10-25). A swing cut short was left up for good: Saar, 21:45, the Elder died 3s into the Younger's
    // swing, the Younger enraged (Disorienting Groan) and never swung, and the circle covered its whole melee ring for the
    // rest of the fight, so the dodge kept him 18y off it.
    private DateTime quietAfter;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.EndlessSwingBoss)
        {
            // A new swing starts its own count, and is drawn even if the swipes' caster died in an earlier one: that flag
            // was never reset, so after a bull fell mid-swipes every later Endless Swing went undrawn.
            aoes.Clear();
            aoes.Add(new(shape, spell.LocXZ, spell.Rotation));
            spellSource = caster;
            NumCasts = 0;
            quietAfter = World.FutureTime(10d);
            if (endlessSwipes != null)
                endlessSwipes.isCasterDead = false;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.EndlessSwing1)
            quietAfter = World.FutureTime(3d);
        else if (spell.Action.ID == (uint)AID.DisorientingGroan)
            aoes.Clear();
        if (spell.Action.ID is (uint)AID.EndlessSwipes1 or (uint)AID.EndlessSwipes3)
        {
            NumCasts++;

            if (NumCasts == 13)
            {
                aoes.Clear();
            }
        }
    }

    public override void Update()
    {
        // If the caster of swipes dies, then we have to clear any left over aoes otherwise they will not get removed - it can be either boss
        if (endlessSwipes != null && endlessSwipes.isCasterDead || aoes.Count != 0 && World.CurrentTime > quietAfter)
        {
            aoes.Clear();
            return;
        }

        var count = aoes.Count;
        if (count == 0 || spellSource == null)
        {
            return;
        }

        var nextAOEs = CollectionsMarshal.AsSpan(aoes);
        ref var aoe = ref nextAOEs[0];
        aoe.Origin = spellSource.Position;
        aoe.Rotation = spellSource.Rotation;
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(aoes);
}

sealed class EndlessSwipes(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private ActorCastInfo? spellInfo;
    private Actor? source;
    private Angle increment = default;
    private readonly AOEShapeCone shape = new(40f, 30f.Degrees());
    public bool isCasterDead = false;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        increment = iconID switch
        {
            (uint)IconID.TurnLeft => 30.0f.Degrees(),
            (uint)IconID.TurnRight => -30.0f.Degrees(),
            _ => default
        };

        InitIfReady();
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        // The spell for the rotation and starting locXZ
        if (spell.Action.ID == (uint)AID.EndlessSwipes1)
        {
            spellInfo = spell;
            InitIfReady();
        }

        // The spell for which boss is actually performing the spell - it can be either one
        if (spell.Action.ID is (uint)AID.EndlessSwipesYounger or (uint)AID.EndlessSwipesElder)
        {
            source = caster;
            InitIfReady();
        }
    }

    private void InitIfReady()
    {
        if (spellInfo != null && increment != default && source != null)
        {
            Sequences.Add(new(shape, spellInfo.LocXZ, spellInfo.Rotation, increment, Module.CastFinishAt(spellInfo), 1.5d, 13, 3, actorID: source.InstanceID));
            spellInfo = null;
            increment = default;
            source = null;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.EndlessSwipes1 or (uint)AID.EndlessSwipes3)
        {
            if (Sequences.Count > 0)
            {
                AdvanceSequence(0, World.CurrentTime);
            }
        }
    }

    // If the caster of swipes dies, then we have to clear any left over aoes otherwise they will not get removed - it can be either boss
    public override void Update()
    {
        base.Update();

        if (Sequences.Count == 0)
        {
            return;
        }

        var target = World.Actors.Find(Sequences[0].ActorID);
        if (target == null || target.IsDead)
        {
            Sequences.Clear();
            isCasterDead = true;
        }
    }
}

// The Elder throws it. Asked when the icon comes, not when the component is built: BossmodReborn's
// Enemies(...)[0] here threw whenever the module started before the Elder was in the actor table, and the whole module
// failed to load (2026-10-22: "Index was out of range" from TonzeSlash100's constructor).
sealed class TonzeSlash100(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeRect(65.0f, 4.0f), (uint)IconID.TankBuster, (uint)AID.TonzeSlash100,
    9.1d, tankbuster: true)
{
    private readonly List<Actor> elders = module.Enemies((uint)OID.ElderTablitaurPiece);

    public override Actor? BaitSource(Actor target) => this.elders.Count > 0 ? this.elders[0] : null;
}

sealed class YoungerTablitaurPieceStates : StateMachineBuilder
{
    public YoungerTablitaurPieceStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<TonzeSwipe1000>()
            .ActivateOnEnter<TonzeSwing1111>()
            .ActivateOnEnter<Shockwave>()
            .ActivateOnEnter<TonzeStomp10>()
            .ActivateOnEnter<EndlessSwipes>()
            .ActivateOnEnter<EndlessSwing>()
            .ActivateOnEnter<TonzeSlash100>()
            .Raw.Update = () => AllDeadOrDestroyed(YoungerTablitaurPiece.Bosses);
    }
}

[ModuleInfo(CFCID = 1089u, NameID = 14556u, PrimaryActorOID = (uint)OID.YoungerTablitaurPiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
public sealed class YoungerTablitaurPiece : ModuleBase
{
    public static readonly uint[] Bosses = [(uint)OID.YoungerTablitaurPiece, (uint)OID.ElderTablitaurPiece];

    public YoungerTablitaurPiece(WorldState ws, Actor primary) : base(ws, primary, new(120f, 0f), new ArenaBoundsRect(20f, 20f))
    {
        ActivateComponent<Hint>();
    }

    // Kill both together: one falling too early sets the survivor off (Disorienting Groan, then Endless Slashes over the
    // whole floor). Korha, 2026-10-25 01:36: the Younger died with the Elder on 72% and Disorienting Groan came 0.2s later.
    // Daedalus keeps to one target, so every pull was lopsided (21/97, 5/59, 0/74 percent). The fuller bull is the priority,
    // handed over only once the other is BalanceSlack fuller, so the rotation is not flipped on every hit.
    private Actor? balanceOn;
    private const float BalanceSlack = 0.02f;

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var elders = this.Enemies((uint)OID.ElderTablitaurPiece);
        var elder = elders.Count > 0 ? elders[0] : null;
        var younger = this.PrimaryActor;
        if (elder == null)
            return;
        // With one bull down the other is the fight, and nothing else names it: the dodge walks to a ranked target or the
        // module's primary (the Younger, dead), and Daedalus retargets only within 25y. Saar, 2026-10-25: the Younger fell
        // at 80s, the Elder walked to (120,-22) past the north edge, and he stood 29y away for 81s.
        if (younger.IsDeadOrDestroyed != elder.IsDeadOrDestroyed)
        {
            hints.SetPriority(younger.IsDeadOrDestroyed ? elder : younger, 1);
            return;
        }
        if (younger.IsDeadOrDestroyed)
            return;
        static float Left(Actor a) => a.HPMP.MaxHP > 0 && a.HPMP.CurHP > 0 ? (float)a.HPMP.CurHP / a.HPMP.MaxHP : 1f; // 0 on a live bull: not synced yet
        var y = Left(younger);
        var e = Left(elder);
        if (this.balanceOn == null || (this.balanceOn == younger ? e - y > BalanceSlack : y - e > BalanceSlack))
            this.balanceOn = y >= e ? younger : elder;
        hints.SetPriority(this.balanceOn, 1);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(this, Bosses);
    }
}
