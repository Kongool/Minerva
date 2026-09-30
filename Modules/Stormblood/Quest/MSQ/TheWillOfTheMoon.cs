// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;


namespace Minerva.Stormblood.Quest.MSQ.TheWillOfTheMoon;

public enum OID : uint
{
    Boss = 0x24A0,
    Magnai = 0x24A1,
    KhunShavar = 0x252F, // R1.82
    Hien = 0x24A3,
    Daidukul = 0x24A2, // R0.5
    TheScaleOfTheFather = 0x2532, // R1.0
    Helper = 0x233C
}

public enum AID : uint
{
    DispellingWind = 13223, // Boss->self, 3.0s cast, range 40+R width 8 rect
    Epigraph = 13225, // 252D->self, 3.0s cast, range 45+R width 8 rect
    WhisperOfLivesPast = 13226, // 252E->self, 3.5s cast, range -12 donut
    AncientBlizzard = 13227, // 252F->self, 3.0s cast, range 40+R 45-degree cone
    Tornado = 13228, // 252F->location, 5.0s cast, range 6 circle
    Epigraph2 = 13222, // 2530->self, 3.0s cast, range 45+R width 8 rect
    FlatlandFury = 13244, // 2532->self, 17.0s cast, range 10 circle
    FlatlandFuryEnrage = 13329, // 249F->self, 25.0s cast, range 10 circle
    ViolentEarth = 13236, // 233C->location, 3.0s cast, range 6 circle
    WindChisel = 13518, // 233C->self, 2.0s cast, range 34+R 20-degree cone
    TranquilAnnihilation = 13233, // _Gen_DaidukulTheMirthful->24A3, 15.0s cast, single-target
    CrushWeapon = 13234, // Hien->249F, 4.7s cast, single-target: breaks one enrage staff
}

public enum SID : uint
{
    Invincibility = 775 // none->Boss, extra=0x0
}

class DispellingWind(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DispellingWind, new AOEShapeRect(40f, 4f));
class Epigraph(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Epigraph, new AOEShapeRect(45f, 4f));
class Whisper(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WhisperOfLivesPast, new AOEShapeDonut(6f, 12f));
class Blizzard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientBlizzard, new AOEShapeCone(40f, 22.5f.Degrees()));
class Tornado(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Tornado, 6f);
class Epigraph1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Epigraph2, new AOEShapeRect(45f, 4f));

public class FlatlandFury(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlatlandFury, 10f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // All nine circles cover the arena, so nothing is forbidden yet: kill the centre staff, and the middle is safe --
        // the ring's circles stop 4y short of it. BossmodReborn forced the nearest staff; the user, 2026-09-29: "the
        // early staffs need to target the center one to make a safe spot". Daedalus reads the priority list, not the
        // forced target, so the centre staff also goes above Magnai (1).
        var casters = ActiveCasters;
        if (casters.Length == 9)
        {
            var centre = 0ul;
            var best = float.MaxValue;
            foreach (ref readonly var c in casters)
            {
                var d = (c.Origin - Module.Center).Length();
                if (d < best)
                {
                    best = d;
                    centre = c.ActorID;
                }
            }
            if (World.Actors.Find(centre) is { } staff)
            {
                hints.ForcedTarget = staff;
                hints.SetPriority(staff, 2);
            }
        }
        else
            base.AddAIHints(slot, actor, assignment, hints);
    }
}

public class FlatlandFuryEnrage(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlatlandFuryEnrage, 10f)
{
    // The staff Hien breaks with Crush Weapon is the way out: its circle never lands. His cast names it about 13s before
    // the fury resolves; the staff only dies 3.7s before, too late to walk there from across the arena (2026-09-29:
    // "hien makes a safe spot near the end ... need to make sure that one move there").
    private ulong broken;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        base.OnCastStarted(caster, cast);
        if (cast.Action.ID == (uint)AID.CrushWeapon)
            broken = cast.TargetID;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var casters = ActiveCasters;
        var named = false;
        foreach (ref readonly var c in casters)
            named |= c.ActorID == broken;

        // all nine still up and none named: they cover the arena, and there is nowhere to aim for yet
        if (!named && casters.Length >= 9)
            return;
        foreach (ref readonly var c in casters)
            if (c.ActorID != broken)
                hints.AddForbiddenZone(c);
    }
}

public class ViolentEarth(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ViolentEarth, 6f);
public class WindChisel(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WindChisel, new AOEShapeCone(34f, 10f.Degrees()));

public class Scales(ModuleBase module) : Components.Adds(module, (uint)OID.TheScaleOfTheFather);

// Y'shtola, whom you play against Magnai, ported from BossmodReborn's AutoYshtola. Hien falling fails the duty, so he
// is healed first, and to a higher floor while Daidukul casts Tranquil Annihilation on him (2026-09-29, Khun Shavar:
// he fell to 15% under Tomahawk and then that).
class AutoYshtola(ModuleBase module, WorldState ws) : QuestBattle.UnmanagedRotation(ws, 25f)
{
    private const uint AeroIIStatus = 144; // her Aero II applies White Mage's DoT

    protected override void Exec(Actor? primaryTarget)
    {
        var magnai = module.GetActor((uint)OID.Magnai);
        var hien = module.GetActor((uint)OID.Hien);
        var daidukul = module.GetActor((uint)OID.Daidukul);

        // MaxHP 0 is HP not yet known: the recording had none for Hien until 30s after he appeared, and read as 0 it
        // asked for Cure II on a full-health Hien all that time
        if (hien is { IsDead: false } && hien.HPMP.MaxHP > 0)
        {
            var hienMinHP = daidukul?.CastInfo?.Action.ID == (uint)AID.TranquilAnnihilation ? 28000 : 10000;
            if (hien.PendingHPRaw < hienMinHP)
            {
                if (Player.DistanceToHitbox(hien) > 25f)
                    Hints.ForcedMovement = Player.DirectionTo(hien);
                UseAction(Roleplay.AID.CureIISeventhDawn, hien);
            }

            // Hien's Crush Weapon breaks the staff that makes the safe spot; stay by him
            if (hien.CastInfo?.Action.ID == (uint)AID.CrushWeapon)
                Hints.GoalZones.Add(AIHints.GoalSingleTarget(hien.Position, 2f, 5f));
        }

        if (magnai is { IsDead: false } && StatusDetails(magnai, AeroIIStatus, Player.InstanceID).Left < 4.6f)
            UseAction(Roleplay.AID.AeroIISeventhDawn, magnai);

        UseAction(Roleplay.AID.StoneIVSeventhDawn, primaryTarget);

        if (Player.HPMP.CurMP < 5000)
            UseAction(Roleplay.AID.Aetherwell, Player);
    }
}

class YshtolaAI(ModuleBase module) : QuestBattle.RotationModule<AutoYshtola>(module);
class P1Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            if (e.Actor.FindStatus((uint)SID.Invincibility) != null)
                e.Priority = AIHints.Enemy.PriorityInvincible;

            // they do very little damage and sadu will raise them after a short delay, no point in attacking
            if (e.Actor.OID == (uint)OID.KhunShavar)
                e.Priority = AIHints.Enemy.PriorityPointless;
        }
    }
}

class P2Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID == (uint)OID.Magnai ? 1 : 0;
        }
    }
}

class SaduHeavensflameStates : StateMachineBuilder
{
    public SaduHeavensflameStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<P1Hints>()
            .ActivateOnEnter<DispellingWind>()
            .ActivateOnEnter<Epigraph>()
            .ActivateOnEnter<Whisper>()
            .ActivateOnEnter<Blizzard>()
            .ActivateOnEnter<Tornado>()
            .ActivateOnEnter<Epigraph1>()
            .Raw.Update = () => module.Enemies((uint)OID.Magnai).Count != 0;
        TrivialPhase(1)
            .ActivateOnEnter<P2Hints>()
            .ActivateOnEnter<Scales>()
            .ActivateOnEnter<FlatlandFury>()
            .ActivateOnEnter<FlatlandFuryEnrage>()
            .ActivateOnEnter<ViolentEarth>()
            .ActivateOnEnter<WindChisel>()
            // last: it picks its target from the priorities the components above have set (the centre staff)
            .ActivateOnEnter<YshtolaAI>()
            .OnEnter(() =>
            {
                // the module's centre, not the radar's: the ported Arena.Center moved only the drawing, and the
                // dodge kept the first arena, 48y away (Magnai, 2026-09-29: "it tries to run you out of the arena")
                module.Center = new(-186.5f, 550.5f);
            })
            .Raw.Update = () => module.Raid.Player()?.IsDeadOrDestroyed ?? true;
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68683u, NameID = 6152u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class SaduHeavensflame(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-223f, 519f), new ArenaBoundsCircle(20u))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(World.Actors.Where(x => !x.IsAlly));
    }
}
