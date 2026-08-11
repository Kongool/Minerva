using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Minerva;

/// <summary>
/// Records a live <see cref="WorldState"/> to a text log by subscribing to <see cref="WorldState.Modified"/>
/// and writing one line per operation: <c>&lt;timestampTicks&gt; &lt;serialized-op&gt;</c>. Because every op
/// already serializes itself (<see cref="WorldState.Operation.Write"/>), recording is just capturing the
/// stream — and <see cref="ReplayParser"/> replays it back offline for analysis / generation.
/// </summary>
public sealed class ReplayRecorder : IDisposable
{
    public const string Magic = "MINERVA-REPLAY";
    public const int Version = 1;

    private readonly WorldState ws;
    private readonly TextWriter writer;
    private readonly EventSubscription subscription;
    private readonly bool excludeOtherPlayers;
    private readonly ulong localPlayerId;
    private readonly HashSet<ulong> dropped = []; // actor ids whose ops we're skipping (other players/pets)
    private int opCount;

    public int OpCount => this.opCount;

    public ReplayRecorder(WorldState ws, TextWriter writer, bool excludeOtherPlayers = false, ulong localPlayerId = 0)
    {
        this.ws = ws;
        this.writer = writer;
        this.excludeOtherPlayers = excludeOtherPlayers;
        this.localPlayerId = localPlayerId;

        // header: magic, version, QPF, game version — lets the parser reconstruct the world shell
        writer.WriteLine($"{Magic} {Version} {ws.QPF} {Quote(ws.GameVersion)}");

        // snapshot current state first, so a mid-fight recording is self-contained
        foreach (var op in ws.CompareToInitial())
            this.Record(op);

        this.subscription = ws.Modified.Subscribe(this.Record);
    }

    // gate each op through the player filter before writing it
    private void Record(WorldState.Operation op)
    {
        if (this.excludeOtherPlayers)
        {
            if (op is ActorState.OpCreate create && !this.KeepActor(create.InstanceID, create.Type))
            {
                this.dropped.Add(create.InstanceID);
                return;
            }
            if (op is ActorState.Operation actorOp && this.dropped.Contains(actorOp.InstanceID))
            {
                if (op is ActorState.OpDestroy)
                    this.dropped.Remove(actorOp.InstanceID);
                return;
            }
        }
        this.Write(op);
    }

    // keep enemies/helpers/objects always; for players & pets keep only the POV player and party members
    private bool KeepActor(ulong instanceID, ActorType type)
    {
        if (type is not (ActorType.Player or ActorType.Pet or ActorType.Chocobo or ActorType.Companion))
            return true;
        if (instanceID == this.localPlayerId)
            return true;
        foreach (var slot in this.ws.Party.Slots)
            if (slot.InstanceID == instanceID)
                return true;
        return false;
    }

    private void Write(WorldState.Operation op)
    {
        var o = new OperationOutput();
        op.Write(o);
        this.writer.Write(op.Timestamp.Ticks.ToString(CultureInfo.InvariantCulture));
        this.writer.Write(' ');
        this.writer.WriteLine(o.ToString());
        this.opCount++;
    }

    private static string Quote(string s) => s.IndexOf(' ') < 0 && s.Length > 0 ? s : $"\"{s}\"";

    public void Dispose()
    {
        this.subscription.Dispose();
        this.writer.Flush();
    }
}
