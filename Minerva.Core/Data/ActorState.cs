namespace Minerva;

/// <summary>
/// The set of actors currently in the world. Every mutation goes through a nested
/// <see cref="Operation"/>, which both applies the change and fires a typed event so
/// downstream systems (radar, modules, generator) react without polling.
/// </summary>
public sealed class ActorState : IEnumerable<Actor>
{
    private const ulong InvalidEntityID = 0xE0000000;

    public readonly Dictionary<ulong, Actor> Actors = [];

    public IEnumerator<Actor> GetEnumerator() => this.Actors.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.Actors.Values.GetEnumerator();

    public Actor? Find(ulong instanceID) => instanceID is not 0 and not InvalidEntityID ? this.Actors.GetValueOrDefault(instanceID) : null;

    // --- per-change events ---
    public readonly Event<Actor> Added = new();
    public readonly Event<Actor> Removed = new();
    public readonly Event<Actor> Renamed = new();
    public readonly Event<Actor> Moved = new();
    public readonly Event<Actor> SizeChanged = new();
    public readonly Event<Actor> HPMPChanged = new();
    public readonly Event<Actor> IsTargetableChanged = new();
    public readonly Event<Actor> IsDeadChanged = new();
    public readonly Event<Actor> InCombatChanged = new();
    public readonly Event<Actor> TargetChanged = new();
    public readonly Event<Actor> Tethered = new();
    public readonly Event<Actor> Untethered = new();
    public readonly Event<Actor> CastStarted = new();
    public readonly Event<Actor, ActorCastInfo> CastFinished = new(); // carries the cast that just ended
    public readonly Event<Actor, ActorCastEvent> CastEvent = new();
    public readonly Event<Actor, int> StatusGain = new(); // (actor, slot index)
    public readonly Event<Actor, int> StatusLose = new();
    public readonly Event<Actor, ActorIconEvent> IconAppeared = new();
    public readonly Event<Actor, ActorVFXEvent> VFXAppeared = new();

    /// <summary>Advance per-frame state: roll position history and progress active casts.</summary>
    public void Tick(in FrameState frame)
    {
        foreach (var act in this.Actors.Values)
        {
            act.PrevPosRot = act.PosRot;
            if (act.CastInfo is { } cast)
                cast.ElapsedTime = MathF.Min(cast.ElapsedTime + frame.Duration, cast.TotalTime);
        }
    }

    /// <summary>Emit the operations that would recreate the current actor set from empty (for snapshots/replays).</summary>
    public List<WorldState.Operation> CompareToInitial()
    {
        List<WorldState.Operation> ops = [];
        foreach (var a in this.Actors.Values)
        {
            ops.Add(new OpCreate(a.InstanceID, a.OID, a.SpawnIndex, a.Name, a.NameID, a.Type, a.PosRot, a.HitboxRadius, a.HPMP, a.IsTargetable, a.IsAlly, a.OwnerID));
            if (a.HPMP != default)
                ops.Add(new OpHPMP(a.InstanceID, a.HPMP));
            if (a.IsDead)
                ops.Add(new OpDead(a.InstanceID, true));
            if (a.InCombat)
                ops.Add(new OpCombat(a.InstanceID, true));
            if (a.TargetID != default)
                ops.Add(new OpTarget(a.InstanceID, a.TargetID));
            if (a.Tether.ID != default)
                ops.Add(new OpTether(a.InstanceID, a.Tether));
            if (a.CastInfo != null)
                ops.Add(new OpCastInfo(a.InstanceID, a.CastInfo));
            for (var i = 0; i < Actor.NumStatuses; ++i)
                if (a.Statuses[i].ID != default)
                    ops.Add(new OpStatus(a.InstanceID, i, a.Statuses[i]));
        }
        return ops;
    }

    /// <summary>Base for actor-scoped operations: resolves the target actor before applying.</summary>
    public abstract class Operation(ulong instanceID) : WorldState.Operation
    {
        public readonly ulong InstanceID = instanceID;

        protected abstract void ExecActor(WorldState ws, Actor actor);
        protected override void Exec(WorldState ws)
        {
            if (ws.Actors.Actors.TryGetValue(this.InstanceID, out var actor))
                this.ExecActor(ws, actor);
        }
    }

    public sealed class OpCreate(
        ulong instanceID, uint oid, int spawnIndex, string name, uint nameID, ActorType type,
        Vector4 posRot, float hitboxRadius, ActorHPMP hpmp, bool isTargetable, bool isAlly, ulong ownerID)
        : Operation(instanceID)
    {
        public readonly uint OID = oid;
        public readonly int SpawnIndex = spawnIndex;
        public readonly string Name = name;
        public readonly uint NameID = nameID;
        public readonly ActorType Type = type;
        public readonly Vector4 PosRot = posRot;
        public readonly float HitboxRadius = hitboxRadius;
        public readonly ActorHPMP HPMP = hpmp;
        public readonly bool IsTargetable = isTargetable;
        public readonly bool IsAlly = isAlly;
        public readonly ulong OwnerID = ownerID;

        protected override void ExecActor(WorldState ws, Actor actor) { }
        protected override void Exec(WorldState ws)
        {
            var actor = ws.Actors.Actors[this.InstanceID] = new Actor(this.InstanceID, this.OID, this.SpawnIndex, this.Name, this.NameID, this.Type, this.PosRot, this.HitboxRadius, this.HPMP, this.IsTargetable, this.IsAlly, this.OwnerID);
            ws.Actors.Added.Fire(actor);
        }
        public override void Write(OperationOutput o) => o.Tag("ACT+").Emit(this.InstanceID, "X").Emit(this.OID, "X").Emit(this.SpawnIndex).Emit(this.Name).Emit(this.NameID).Emit((uint)this.Type, "X4").Emit(this.PosRot).Emit(this.HitboxRadius).Emit(this.IsTargetable).Emit(this.IsAlly).Emit(this.OwnerID, "X");
    }

    public sealed class OpDestroy(ulong instanceID) : Operation(instanceID)
    {
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            actor.IsDestroyed = true;
            if (actor.InCombat) { actor.InCombat = false; ws.Actors.InCombatChanged.Fire(actor); }
            if (actor.Tether.Target != default) { ws.Actors.Untethered.Fire(actor); actor.Tether = default; }
            if (actor.CastInfo != null) { ws.Actors.CastFinished.Fire(actor, actor.CastInfo); actor.CastInfo = null; }
            for (var i = 0; i < Actor.NumStatuses; ++i)
                if (actor.Statuses[i].ID != default) { ws.Actors.StatusLose.Fire(actor, i); actor.Statuses[i] = default; }
            ws.Actors.Removed.Fire(actor);
            ws.Actors.Actors.Remove(this.InstanceID);
        }
        public override void Write(OperationOutput o) => o.Tag("ACT-").Emit(this.InstanceID, "X");
    }

    public sealed class OpRename(ulong instanceID, string name, uint nameID) : Operation(instanceID)
    {
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            actor.Name = name;
            actor.NameID = nameID;
            ws.Actors.Renamed.Fire(actor);
        }
        public override void Write(OperationOutput o) => o.Tag("NAME").Emit(this.InstanceID, "X").Emit(name).Emit(nameID);
    }

    public sealed class OpMove(ulong instanceID, Vector4 posRot) : Operation(instanceID)
    {
        public readonly Vector4 PosRot = posRot;
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            actor.PosRot = this.PosRot;
            ws.Actors.Moved.Fire(actor);
        }
        public override void Write(OperationOutput o) => o.Tag("MOVE").Emit(this.InstanceID, "X").Emit(this.PosRot);
    }

    public sealed class OpSizeChange(ulong instanceID, float hitboxRadius) : Operation(instanceID)
    {
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            actor.HitboxRadius = hitboxRadius;
            ws.Actors.SizeChanged.Fire(actor);
        }
        public override void Write(OperationOutput o) => o.Tag("ACSZ").Emit(this.InstanceID, "X").Emit(hitboxRadius);
    }

    public sealed class OpHPMP(ulong instanceID, ActorHPMP hpmp) : Operation(instanceID)
    {
        public readonly ActorHPMP HPMP = hpmp;
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            actor.HPMP = this.HPMP;
            ws.Actors.HPMPChanged.Fire(actor);
        }
        public override void Write(OperationOutput o) => o.Tag("HP  ").Emit(this.InstanceID, "X").Emit(this.HPMP.CurHP).Emit(this.HPMP.MaxHP).Emit(this.HPMP.Shield).Emit(this.HPMP.CurMP).Emit(this.HPMP.MaxMP);
    }

    public sealed class OpTargetable(ulong instanceID, bool value) : Operation(instanceID)
    {
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            actor.IsTargetable = value;
            ws.Actors.IsTargetableChanged.Fire(actor);
        }
        public override void Write(OperationOutput o) => o.Tag(value ? "ATG+" : "ATG-").Emit(this.InstanceID, "X");
    }

    public sealed class OpDead(ulong instanceID, bool value) : Operation(instanceID)
    {
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            actor.IsDead = value;
            ws.Actors.IsDeadChanged.Fire(actor);
        }
        public override void Write(OperationOutput o) => o.Tag(value ? "DIE+" : "DIE-").Emit(this.InstanceID, "X");
    }

    public sealed class OpCombat(ulong instanceID, bool value) : Operation(instanceID)
    {
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            actor.InCombat = value;
            ws.Actors.InCombatChanged.Fire(actor);
        }
        public override void Write(OperationOutput o) => o.Tag(value ? "COM+" : "COM-").Emit(this.InstanceID, "X");
    }

    public sealed class OpTarget(ulong instanceID, ulong value) : Operation(instanceID)
    {
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            actor.TargetID = value;
            ws.Actors.TargetChanged.Fire(actor);
        }
        public override void Write(OperationOutput o) => o.Tag("TARG").Emit(this.InstanceID, "X").Emit(value, "X");
    }

    public sealed class OpTether(ulong instanceID, ActorTetherInfo value) : Operation(instanceID)
    {
        public readonly ActorTetherInfo Value = value;
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            if (actor.Tether.Target != default)
                ws.Actors.Untethered.Fire(actor);
            actor.Tether = this.Value;
            if (this.Value.Target != default)
                ws.Actors.Tethered.Fire(actor);
        }
        public override void Write(OperationOutput o) => o.Tag("TETH").Emit(this.InstanceID, "X").Emit(this.Value.ID).Emit(this.Value.Target, "X");
    }

    public sealed class OpCastInfo(ulong instanceID, ActorCastInfo? value) : Operation(instanceID)
    {
        public readonly ActorCastInfo? Value = value;
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            var prev = actor.CastInfo;
            actor.CastInfo = this.Value;
            if (this.Value != null)
                ws.Actors.CastStarted.Fire(actor);
            else if (prev != null)
                ws.Actors.CastFinished.Fire(actor, prev);
        }
        public override void Write(OperationOutput o)
        {
            if (this.Value is { } c)
                o.Tag("CST+").Emit(this.InstanceID, "X").Emit(c.Action.ID, "X").Emit(c.TargetID, "X").Emit(c.Rotation).Emit(c.ElapsedTime).Emit(c.TotalTime);
            else
                o.Tag("CST-").Emit(this.InstanceID, "X");
        }
    }

    /// <summary>Cast resolved (snapshot). Event-only — no persistent actor state changes.</summary>
    public sealed class OpCastEvent(ulong instanceID, ActorCastEvent value) : Operation(instanceID)
    {
        public readonly ActorCastEvent Value = value;
        protected override void ExecActor(WorldState ws, Actor actor) => ws.Actors.CastEvent.Fire(actor, this.Value);
        public override void Write(OperationOutput o) => o.Tag("CST!").Emit(this.InstanceID, "X").Emit(this.Value.Action.ID, "X").Emit(this.Value.MainTargetID, "X").Emit(this.Value.Rotation).Emit(this.Value.GlobalSequence);
    }

    public sealed class OpStatus(ulong instanceID, int index, ActorStatus value) : Operation(instanceID)
    {
        public readonly int Index = index;
        public readonly ActorStatus Value = value;
        protected override void ExecActor(WorldState ws, Actor actor)
        {
            var had = actor.Statuses[this.Index].ID != default;
            // fire StatusLose while the lost status is still in the slot, so handlers reading it by
            // ref see the details (matches BMR); StatusGain fires after the new status is written.
            if (this.Value.ID == default && had)
                ws.Actors.StatusLose.Fire(actor, this.Index);
            actor.Statuses[this.Index] = this.Value;
            if (this.Value.ID != default)
                ws.Actors.StatusGain.Fire(actor, this.Index);
        }
        public override void Write(OperationOutput o)
        {
            if (this.Value.ID != default)
                o.Tag("STA+").Emit(this.InstanceID, "X").Emit(this.Index).Emit(this.Value.ID).Emit((uint)this.Value.Extra, "X4").Emit(this.Value.SourceID, "X");
            else
                o.Tag("STA-").Emit(this.InstanceID, "X").Emit(this.Index);
        }
    }

    /// <summary>Overhead marker icon appeared on an actor. Event-only.</summary>
    public sealed class OpIcon(ulong instanceID, uint iconID, ulong targetID) : Operation(instanceID)
    {
        protected override void ExecActor(WorldState ws, Actor actor) => ws.Actors.IconAppeared.Fire(actor, new ActorIconEvent(iconID, targetID));
        public override void Write(OperationOutput o) => o.Tag("ICON").Emit(this.InstanceID, "X").Emit(iconID).Emit(targetID, "X");
    }

    /// <summary>A targeted VFX played on an actor. Event-only.</summary>
    public sealed class OpVFX(ulong instanceID, uint vfxID, ulong targetID) : Operation(instanceID)
    {
        protected override void ExecActor(WorldState ws, Actor actor) => ws.Actors.VFXAppeared.Fire(actor, new ActorVFXEvent(vfxID, targetID));
        public override void Write(OperationOutput o) => o.Tag("VFX ").Emit(this.InstanceID, "X").Emit(vfxID).Emit(targetID, "X");
    }
}

/// <summary>Payload for an overhead icon event.</summary>
public readonly struct ActorIconEvent(uint iconID, ulong targetID)
{
    public readonly uint IconID = iconID;
    public readonly ulong TargetID = targetID;
}

/// <summary>Payload for a targeted-VFX event.</summary>
public readonly struct ActorVFXEvent(uint vfxID, ulong targetID)
{
    public readonly uint VFXID = vfxID;
    public readonly ulong TargetID = targetID;
}
