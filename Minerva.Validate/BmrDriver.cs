using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Minerva;

namespace Minerva.Validate;

/// <summary>
/// Walks a Minerva recording, replaying it into a BossmodReborn <c>WorldState</c> and ticking BMR's boss
/// module alongside. Phase 1 of docs/dual-viewer-plan.md — once this holds, both engines can be sampled
/// frame by frame and compared (see <see cref="CompareDriver"/>).
/// </summary>
internal sealed class BmrDriver
{
    /// <summary>Set by --verbose: print per-frame state-machine progress while bridging.</summary>
    public static bool Diagnose;

    public sealed record Result(
        string? ModuleName,
        int OpsRead, int OpsApplied, int Frames,
        int ComponentCount, int MaxComponentCount,
        (string Tag, int Count)[] Unmapped);

    /// <summary>
    /// Translate one Minerva op into BMR's world. Pure translation — no module handling — so the
    /// comparison driver can share it. Anything without a mapping is counted, never dropped silently.
    /// </summary>
    public static void Apply(BmrBridge bridge, WorldState.Operation op)
    {
        switch (op)
        {
            case WorldState.OpFrameStart fs: bridge.FrameStart(fs.Frame.Timestamp, fs.Frame.Index, fs.Frame.Duration); break;
            case WorldState.OpZoneChange zc: bridge.ZoneChange(zc.Zone, zc.CFCID); break;

            case ActorState.OpCreate c:
                bridge.Create(c.InstanceID, c.OID, c.Name, c.NameID, (int)c.Type, c.PosRot, c.HitboxRadius, c.IsTargetable, c.IsAlly, c.OwnerID);
                break;

            case ActorState.OpMove m: bridge.Move(m.InstanceID, m.PosRot); break;
            case ActorState.OpCombat cb: bridge.Combat(cb.InstanceID, cb.Value); break;
            case ActorState.OpTargetable tg: bridge.Targetable(tg.InstanceID, tg.Value); break;
            case ActorState.OpDead d: bridge.Dead(d.InstanceID, d.Value); break;
            case ActorState.OpDestroy de: bridge.Destroy(de.InstanceID); break;
            case ActorState.OpTarget tt: bridge.Target(tt.InstanceID, tt.Value); break;
            case ActorState.OpIcon ic: bridge.Icon(ic.InstanceID, ic.IconID, ic.TargetID); break;
            case ActorState.OpTether th: bridge.Tether(th.InstanceID, th.Value.ID, th.Value.Target); break;
            case ActorState.OpStatus st: bridge.Status(st.InstanceID, st.Index, st.Value.ID, st.Value.Extra, st.Value.ExpireAt, st.Value.SourceID); break;

            case ActorState.OpCastInfo ci:
                if (ci.Value is { } cast)
                    bridge.CastInfo(ci.InstanceID, cast.Action.ID, cast.TargetID, cast.Location, cast.TotalTime, cast.ElapsedTime, cast.Rotation.Rad);
                else
                    bridge.CastClear(ci.InstanceID);
                break;

            case ActorState.OpCastEvent ce:
                bridge.CastEvent(ce.InstanceID, ce.Value.Action.ID, ce.Value.MainTargetID, ce.Value.TargetPos, ce.Value.GlobalSequence, ce.Value.Rotation.Rad);
                break;

            default:
                bridge.NoteUnmapped(op.GetType().Name);
                break;
        }
    }

    /// <summary>
    /// Replay <paramref name="timeline"/> into BMR. The module is created the first time an enemy actor
    /// appears that BMR has a module for, then ticked once per frame — exactly how the live plugin drives
    /// it, which is what makes <c>ActivateOnEnter</c> and time-based state transitions fire.
    /// </summary>
    public static Result Run(ReplayTimeline timeline, BmrHost host, uint? forceOID = null)
    {
        var bridge = new BmrBridge(host.Assembly, timeline.QPF, timeline.GameVersion);
        object? module = null;
        string? moduleName = null;
        MethodInfo? update = null;
        FieldInfo? componentsField = null;
        var frames = 0;
        var maxComponents = 0;

        foreach (var (_, op) in timeline.Ops)
        {
            Apply(bridge, op);

            if (op is WorldState.OpFrameStart)
                frames++;

            if (module == null && op is ActorState.OpCreate c && (forceOID == null || c.OID == forceOID))
                TryCreateModule(c.InstanceID, c.OID);

            // tick the module on frame boundaries, like the plugin's per-frame Update
            if (op is WorldState.OpFrameStart && module != null)
            {
                try { update!.Invoke(module, null); }
                catch (Exception ex) { bridge.NoteUnmapped($"!Update:{(ex.InnerException ?? ex).GetType().Name}"); }
                var n = (componentsField!.GetValue(module) as ICollection)?.Count ?? 0;
                if (n > maxComponents)
                    maxComponents = n;
                if (Diagnose)
                {
                    var sm = Member(module, "StateMachine");
                    var pa = Member(module, "PrimaryActor");
                    Console.WriteLine($"    [frame {frames}] t={bridge.CurrentTime:HH:mm:ss} " +
                        $"phase={Member(sm, "ActivePhaseIndex")} inCombat={Member(pa, "InCombat")} " +
                        $"targetable={Member(pa, "IsTargetable")} components={n}");
                }
            }
        }

        var final = module != null ? (componentsField!.GetValue(module) as ICollection)?.Count ?? 0 : 0;
        return new Result(moduleName, timeline.Ops.Count, bridge.Applied, frames, final, maxComponents,
            bridge.Unmapped.OrderByDescending(kv => kv.Value).Select(kv => (kv.Key, kv.Value)).ToArray());

        // BMR mixes public fields and properties freely; look for either
        static object? Member(object? target, string name)
        {
            if (target == null)
                return null;
            var t = target.GetType();
            return t.GetField(name)?.GetValue(target) ?? t.GetProperty(name)?.GetValue(target);
        }

        void TryCreateModule(ulong instanceID, uint oid)
        {
            if (host.InfoForOID(oid) == null)
                return;
            var primary = bridge.FindActor(instanceID);
            if (primary == null)
                return;
            module = host.CreateModuleForActor(bridge.WorldState, primary);
            if (module == null)
                return;
            var mt = module.GetType();
            moduleName = mt.Name;
            update = mt.GetMethod("Update", Type.EmptyTypes);
            componentsField = mt.GetField("Components");
        }
    }
}
