using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Minerva;

/// <summary>
/// A parsed replay as a time-ordered list of operations, ready to be stepped through for interactive
/// playback. Each op is stamped with the real time of its governing frame (so the snapshot ops that
/// open a recording all share the fight's start time, not the default 0 their own line carries).
/// </summary>
public sealed class ReplayTimeline
{
    public required ulong QPF { get; init; }
    public required string GameVersion { get; init; }
    public required IReadOnlyList<(long Ticks, WorldState.Operation Op)> Ops { get; init; }

    public long StartTicks => this.Ops.Count > 0 ? this.Ops[0].Ticks : 0;
    public long EndTicks => this.Ops.Count > 0 ? this.Ops[^1].Ticks : 0;
    public long DurationTicks => System.Math.Max(0, this.EndTicks - this.StartTicks);
}

/// <summary>
/// Replays a recorded log (see <see cref="ReplayRecorder"/>) back through a fresh
/// <see cref="WorldState"/>: each line is tokenized and reconstructed into the operation that wrote
/// it, then executed — reproducing the exact event stream offline, with no game. Analyzers and the
/// Phase-5 generator attach to the reconstructed world and observe as it plays.
/// </summary>
public sealed class ReplayParser
{
    public WorldState World { get; }

    private ReplayParser(WorldState world) => this.World = world;

    /// <summary>Parse a full log and replay every op through a new world state.</summary>
    public static ReplayParser Replay(TextReader reader, Action<WorldState>? onCreated = null)
    {
        var header = reader.ReadLine() ?? throw new InvalidDataException("empty replay");
        var (qpf, gameVersion) = ParseHeader(header);
        var ws = new WorldState(qpf, gameVersion);
        var parser = new ReplayParser(ws);
        onCreated?.Invoke(ws); // let observers subscribe before playback

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0)
                continue;
            var op = ParseLine(line);
            if (op != null)
                ws.Execute(op);
        }
        return parser;
    }

    /// <summary>
    /// Parse a full log into a stepped <see cref="ReplayTimeline"/> without executing it — the input to
    /// interactive playback. Each op is stamped with its frame's real timestamp (see the type remarks).
    /// </summary>
    public static ReplayTimeline ParseTimeline(TextReader reader)
    {
        var header = reader.ReadLine() ?? throw new InvalidDataException("empty replay");
        var (qpf, gameVersion) = ParseHeader(header);

        var ops = new List<(long, WorldState.Operation)>();
        var frameTicks = 0L; // real time of the frame currently being read
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0)
                continue;
            var sp = line.IndexOf(' ');
            if (sp < 0)
                continue;
            var r = new OpTokenReader(line, sp + 1);
            var fourCC = r.FourCC;
            r.SkipTag();
            var op = Build(fourCC, r);
            if (op == null)
                continue;
            if (op is WorldState.OpFrameStart fs && fs.Frame.Timestamp.Ticks != 0)
                frameTicks = fs.Frame.Timestamp.Ticks; // frames define the clock; other ops inherit it
            ops.Add((frameTicks, op));
        }
        return new ReplayTimeline { QPF = qpf, GameVersion = gameVersion, Ops = ops };
    }

    private static (ulong qpf, string version) ParseHeader(string header)
    {
        var r = new OpTokenReader(header);
        if (r.FourCC != ReplayRecorder.Magic)
            throw new InvalidDataException($"not a Minerva replay (got '{r.FourCC}')");
        r.SkipTag();
        _ = r.NextInt();          // format version
        var qpf = r.NextU64();
        var version = r.HasMore ? r.NextString() : "unknown";
        return (qpf, version);
    }

    private static WorldState.Operation? ParseLine(string line)
    {
        var sp = line.IndexOf(' ');
        if (sp < 0)
            return null;
        _ = long.Parse(line.AsSpan(0, sp), CultureInfo.InvariantCulture); // line timestamp (informational)
        var r = new OpTokenReader(line, sp + 1);
        var fourCC = r.FourCC;
        r.SkipTag();
        return Build(fourCC, r);
    }

    private static WorldState.Operation? Build(string tag, OpTokenReader r) => tag switch
    {
        "FRAM" => BuildFrame(r),
        "ZONE" => new WorldState.OpZoneChange((ushort)r.NextU32(), (ushort)r.NextU32()),
        "ENVC" => new WorldState.OpMapEffect((byte)r.NextHex32(), r.NextHex32()),
        "DIRU" => new WorldState.OpDirectorUpdate(r.NextHex32(), r.NextHex32(), r.NextHex32(), r.NextHex32(), r.NextHex32(), r.NextHex32()),
        "RSV " or "RSV" => new WorldState.OpRSVData(r.NextString(), r.NextString()),
        "PAR " or "PAR" => new PartyState.OpModify(r.NextInt(), new PartyState.Member(r.NextHex64(), r.NextHex64())),

        "ACT+" => BuildCreate(r),
        "ACT-" => new ActorState.OpDestroy(r.NextHex64()),
        "NAME" => new ActorState.OpRename(r.NextHex64(), r.NextString(), r.NextU32()),
        "MOVE" => new ActorState.OpMove(r.NextHex64(), r.NextVec4()),
        "ACSZ" => new ActorState.OpSizeChange(r.NextHex64(), r.NextFloat()),
        "HP  " or "HP" => new ActorState.OpHPMP(r.NextHex64(), new ActorHPMP(r.NextU32(), r.NextU32(), r.NextU32(), r.NextU32(), r.NextU32())),
        "ATG+" => new ActorState.OpTargetable(r.NextHex64(), true),
        "ATG-" => new ActorState.OpTargetable(r.NextHex64(), false),
        "DIE+" => new ActorState.OpDead(r.NextHex64(), true),
        "DIE-" => new ActorState.OpDead(r.NextHex64(), false),
        "COM+" => new ActorState.OpCombat(r.NextHex64(), true),
        "COM-" => new ActorState.OpCombat(r.NextHex64(), false),
        "TARG" => new ActorState.OpTarget(r.NextHex64(), r.NextHex64()),
        "TETH" => new ActorState.OpTether(r.NextHex64(), new ActorTetherInfo(r.NextU32(), r.NextHex64())),
        "CST+" => BuildCastInfo(r),
        "CST-" => new ActorState.OpCastInfo(r.NextHex64(), null),
        "CST!" => BuildCastEvent(r),
        "STA+" => BuildStatusGain(r),
        "STA-" => BuildStatusLose(r),
        "ICON" => new ActorState.OpIcon(r.NextHex64(), r.NextU32(), r.NextHex64()),
        "VFX " or "VFX" => new ActorState.OpVFX(r.NextHex64(), r.NextU32(), r.NextHex64()),
        _ => null, // unknown/ignored op
    };

    private static WorldState.Operation BuildFrame(OpTokenReader r)
    {
        var prevMs = r.NextFloat();
        var tsTicks = (long)r.NextU64();
        var qpc = r.NextU64();
        var index = r.NextU32();
        var duration = r.NextFloat();
        var frame = new FrameState(new DateTime(tsTicks, DateTimeKind.Utc), qpc, index, duration, duration, 1f);
        return new WorldState.OpFrameStart(frame, TimeSpan.FromMilliseconds(prevMs));
    }

    private static WorldState.Operation BuildCreate(OpTokenReader r)
    {
        var id = r.NextHex64();
        var oid = r.NextHex32();
        var spawnIndex = r.NextInt();
        var name = r.NextString();
        var nameID = r.NextU32();
        var type = (ActorType)r.NextHex32();
        var posRot = r.NextVec4();
        var radius = r.NextFloat();
        var targetable = r.NextBool();
        var ally = r.NextBool();
        var owner = r.NextHex64();
        return new ActorState.OpCreate(id, oid, spawnIndex, name, nameID, type, posRot, radius, default, targetable, ally, owner);
    }

    private static WorldState.Operation BuildCastInfo(OpTokenReader r)
    {
        var id = r.NextHex64();
        var cast = new ActorCastInfo
        {
            Action = ActionID.MakeSpell(r.NextHex32()),
            TargetID = r.NextHex64(),
            Rotation = r.NextAngleDeg(),
            ElapsedTime = r.NextFloat(),
            TotalTime = r.NextFloat(),
        };
        return new ActorState.OpCastInfo(id, cast);
    }

    private static WorldState.Operation BuildCastEvent(OpTokenReader r)
    {
        var id = r.NextHex64();
        var action = ActionID.MakeSpell(r.NextHex32());
        var target = r.NextHex64();
        var rotation = r.NextAngleDeg();
        var seq = r.NextU32();
        return new ActorState.OpCastEvent(id, new ActorCastEvent(action, target, rotation, default, seq));
    }

    private static WorldState.Operation BuildStatusGain(OpTokenReader r)
    {
        var id = r.NextHex64();
        var index = r.NextInt();
        var status = new ActorStatus(r.NextU32(), (ushort)r.NextHex32(), default, r.NextHex64());
        return new ActorState.OpStatus(id, index, status);
    }

    private static WorldState.Operation BuildStatusLose(OpTokenReader r)
    {
        var id = r.NextHex64();
        var index = r.NextInt();
        return new ActorState.OpStatus(id, index, default);
    }
}
