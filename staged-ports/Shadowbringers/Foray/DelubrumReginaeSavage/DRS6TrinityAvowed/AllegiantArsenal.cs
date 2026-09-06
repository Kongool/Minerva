// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS6TrinityAvowed;

sealed class AllegiantArsenal(ModuleBase module) : Components.GenericAOEs(module)
{
    public enum Order { Unknown, SwordSecond, BowSecond, StaffSecond, StaffSwordBow, BowSwordStaff, SwordBowStaff, StaffBowSword, SwordStaffBow, BowStaffSword }

    public Order Mechanics;
    private AOEInstance[] _aoe = [];
    public bool Active => _aoe.Length != 0;

    private static readonly AOEShapeCone cone = new(70f, 135f.Degrees());
    private static readonly AOEShapeCircle circle = new(10f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.AllegiantArsenalSword:
                Activate(cone, Mechanics switch
                {
                    Order.Unknown => Order.SwordSecond,
                    Order.BowSecond => Order.StaffBowSword,
                    Order.StaffSecond => Order.BowStaffSword,
                    _ => Order.Unknown
                }, 180f.Degrees());
                break;
            case (uint)AID.AllegiantArsenalBow:
                Activate(cone, Mechanics switch
                {
                    Order.Unknown => Order.BowSecond,
                    Order.SwordSecond => Order.StaffSwordBow,
                    Order.StaffSecond => Order.SwordStaffBow,
                    _ => Order.Unknown
                });
                break;
            case (uint)AID.AllegiantArsenalStaff:
                Activate(circle, Mechanics switch
                {
                    Order.Unknown => Order.StaffSecond,
                    Order.SwordSecond => Order.BowSwordStaff,
                    Order.BowSecond => Order.SwordBowStaff,
                    _ => Order.Unknown
                });
                break;
        }
        void Activate(AOEShape shape, Order newOrder, Angle offset = default)
        {
            _aoe = [new(shape, spell.LocXZ, spell.Rotation + offset, Module.CastFinishAt(spell, 5.2d))];
            if (newOrder != Order.Unknown)
            {
                Mechanics = newOrder;
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.InfernalSlash or (uint)AID.Flashvane or (uint)AID.FuryOfBozja)
        {
            _aoe = [];
        }
    }
}
