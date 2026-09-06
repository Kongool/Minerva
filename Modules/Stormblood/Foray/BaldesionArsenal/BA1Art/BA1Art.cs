// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.BaldesionArsenal.BA1Art;

sealed class Thricecull(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Thricecull);
sealed class AcallamNaSenorach(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AcallamNaSenorach);
sealed class DefilersDeserts(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DefilersDeserts, new AOEShapeRect(35.5f, 4f));
sealed class Pitfall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pitfall, 20f);
sealed class LegendaryGeasAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LegendaryGeas, 8f);

sealed class DefilersDesertsPredict(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCross cross = new(35.5f, 4f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        void AddAOE(Angle angle) => _aoes.Add(new(cross, spell.LocXZ, angle, Module.CastFinishAt(spell, 6.9d)));
        var id = spell.Action.ID;
        if (id == (uint)AID.LegendaryGeas)
        {
            AddAOE(45f.Degrees());
            AddAOE(default);
        }
        else if (id == (uint)AID.DefilersDeserts)
        {
            _aoes.Clear();
        }
    }
}

sealed class LegendaryGeasStay(ModuleBase module) : Components.StayMove(module)
{
    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (actor.OID == (uint)OID.ShadowLinksHelper && actor.Position.AlmostEqual(new(-135f, 750f), 1f))
        {
            if (state == 0x00010002u)
            {
                Array.Fill(PlayerStates, new(Requirement.Stay2, World.CurrentTime, 1));
            }
            else if (state == 0x00040008u)
            {
                Array.Clear(PlayerStates);
            }
        }
    }
}

sealed class GloryUnearthed(ModuleBase module) : Components.StandardChasingAOEs(module, 10f, (uint)AID.GloryUnearthedFirst, (uint)AID.GloryUnearthedRest, 6.5f, 1.5d, 5, true, (uint)IconID.GloryUnearthed);
sealed class PiercingDark(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.PiercingDark, 6f);

[ModuleInfo(Group = ModuleGroup.BaldesionArsenal, GroupID = 639u, CFCID = 639u, NameID = 7968u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class BA1Art(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-128.98f, 748f), 29.5f, 64)], [new Rectangle(new(-129f, 718f), 20, 1.15f), new Rectangle(new(-129f, 778f), 20f, 1.48f),
    new Polygon(new(-123.5f, 778f), 1.7f, 8), new Polygon(new(-134.5f, 778f), 1.7f, 8), new Polygon(new(-123.5f, 718f), 1.5f, 8), new Polygon(new(-134.5f, 718f), 1.5f, 8)]);

    protected override bool CheckPull() => base.CheckPull() && (Center - Raid.Player()!.Position).LengthSq() < 1e4f;
}
