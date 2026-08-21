using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Minerva;

namespace Minerva.Validate;

/// <summary>
/// One AOE as both engines describe it, normalised so Minerva's and BMR's can be compared. The two have
/// separate shape hierarchies with different <c>ToString</c> formats, so the descriptor is built from the
/// shape's class name plus its numeric fields rather than from text.
/// </summary>
internal readonly record struct AoeSample(string Kind, float[] Dims, float OriginX, float OriginZ, float RotationDeg, bool Risky)
{
    public string Describe()
        => $"{this.Kind}({string.Join(",", this.Dims.Select(d => d.ToString("f1", CultureInfo.InvariantCulture)))}) " +
           $"@({this.OriginX.ToString("f1", CultureInfo.InvariantCulture)},{this.OriginZ.ToString("f1", CultureInfo.InvariantCulture)}) " +
           $"rot {this.RotationDeg.ToString("f0", CultureInfo.InvariantCulture)}";

    /// <summary>Same shape family, same dimensions, same place — within tolerance.</summary>
    public bool Matches(in AoeSample other, float posTol, float rotTol, float dimTol)
        => this.Kind == other.Kind
        && MathF.Abs(this.OriginX - other.OriginX) <= posTol
        && MathF.Abs(this.OriginZ - other.OriginZ) <= posTol
        && AngleClose(this.RotationDeg, other.RotationDeg, rotTol)
        && this.Dims.Length == other.Dims.Length
        && this.Dims.Zip(other.Dims).All(p => MathF.Abs(p.First - p.Second) <= dimTol);

    private static bool AngleClose(float a, float b, float tol)
    {
        var d = MathF.Abs(a - b) % 360f;
        return MathF.Min(d, 360f - d) <= tol;
    }

    /// <summary>Shape family, with each engine's naming prefix stripped so the two line up.</summary>
    public static string KindOf(object shape)
    {
        var n = shape.GetType().Name;
        return n.StartsWith("AOEShape", StringComparison.Ordinal) ? n["AOEShape".Length..] : n;
    }

    /// <summary>
    /// The shape's numeric parameters in a canonical order per family, looked up <b>by name</b> rather
    /// than by declaration order — the two engines happen to declare these in the same order today, but
    /// nothing enforces that, and a silent reorder would turn into phantom disagreements. Angles are
    /// emitted in degrees. Unknown families fall back to declaration order.
    /// </summary>
    private static readonly Dictionary<string, string[]> CanonicalDims = new()
    {
        ["Circle"] = ["Radius"],
        ["Donut"] = ["InnerRadius", "OuterRadius"],
        ["Cone"] = ["Radius", "HalfAngle", "DirectionOffset"],
        ["Rect"] = ["LenFront|LengthFront", "HalfWidth", "LenBack|LengthBack", "DirectionOffset"],
        ["DonutSector"] = ["InnerRadius", "OuterRadius", "HalfAngle", "DirectionOffset"],
        ["Cross"] = ["Length", "HalfWidth", "DirectionOffset"],
        ["Capsule"] = ["Radius", "Length", "DirectionOffset"],
    };

    public static float[] DimsOf(object shape)
    {
        var t = shape.GetType();
        if (CanonicalDims.TryGetValue(KindOf(shape), out var names))
        {
            var dims = new List<float>(names.Length);
            foreach (var spec in names)
            {
                float? v = null;
                foreach (var alias in spec.Split('|'))
                    if (Numeric(t.GetField(alias)?.GetValue(shape)) is { } f) { v = f; break; }
                dims.Add(v ?? 0f);
            }
            return [.. dims];
        }

        // unknown family (custom polygons and the like): declaration order, best effort
        var fallback = new List<float>();
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            if (Numeric(f.GetValue(shape)) is { } v)
                fallback.Add(v);
        return [.. fallback];
    }

    /// <summary>A float, or an Angle flattened to degrees; null for anything else.</summary>
    private static float? Numeric(object? value) => value switch
    {
        float f => f,
        null => null,
        var a when a.GetType().Name == "Angle" => a.GetType().GetProperty("Deg")?.GetValue(a) as float?,
        _ => null,
    };

    /// <summary>
    /// Rebuild a Minerva shape from the normalised descriptor. Both sides are rendered through this, so a
    /// visual difference in the viewer is a real difference — not one engine's renderer against the
    /// other's. Returns null for families that carry no reconstructable dimensions (custom polygons).
    /// </summary>
    public AOEShape? Rebuild() => this.Kind switch
    {
        "Circle" when this.Dims.Length >= 1 => new AOEShapeCircle(this.Dims[0]),
        "Donut" when this.Dims.Length >= 2 => new AOEShapeDonut(this.Dims[0], this.Dims[1]),
        "Cone" when this.Dims.Length >= 3 => new AOEShapeCone(this.Dims[0], this.Dims[1].Degrees(), this.Dims[2].Degrees()),
        "Rect" when this.Dims.Length >= 4 => new AOEShapeRect(this.Dims[0], this.Dims[1], this.Dims[2], this.Dims[3].Degrees()),
        "DonutSector" when this.Dims.Length >= 4 => new AOEShapeDonutSector(this.Dims[0], this.Dims[1], this.Dims[2].Degrees(), this.Dims[3].Degrees()),
        _ => null,
    };

    /// <summary>World-space outline for drawing, or an empty list when the shape can't be rebuilt.</summary>
    public IReadOnlyList<WPos> Contour()
        => this.Rebuild()?.Contour(new WPos(this.OriginX, this.OriginZ), this.RotationDeg.Degrees()) ?? [];

    /// <summary>Sample Minerva's active AOEs for the local player.</summary>
    public static List<AoeSample> FromMinerva(ModuleBase module, Actor pc)
    {
        var result = new List<AoeSample>();
        foreach (var c in module.Components.OfType<Minerva.Components.GenericAOEs>())
            foreach (ref readonly var aoe in c.ActiveAOEs(0, pc))
                result.Add(new AoeSample(KindOf(aoe.Shape), DimsOf(aoe.Shape), aoe.Origin.X, aoe.Origin.Z, aoe.Rotation.Deg, aoe.Risky));
        return result;
    }

    /// <summary>
    /// Sample BMR's active AOEs by reading the lists its components hold them in.
    /// <para>The obvious approach — reflecting <c>ActiveAOEs</c> — cannot work: it returns
    /// <c>ReadOnlySpan&lt;AOEInstance&gt;</c>, and a span is a ref struct that reflection refuses to box, so
    /// every Invoke throws and every component yields nothing. That failure is silent, which made the
    /// comparison report a healthy-looking agreement figure while BMR contributed no shapes whatsoever.</para>
    /// <para>Reading the backing lists sidesteps it. The lists are the same instances ActiveAOEs returns a
    /// window onto, so positions and shapes match; what is lost is the per-call filtering a few components
    /// apply (a MaxCasts cap, or colouring), which can make this slightly more generous than what BMR would
    /// actually draw. For "do the two engines see the same mechanic" that is the right trade.</para>
    /// </summary>
    public static List<AoeSample> FromBmr(object module, object? pcActor)
    {
        var result = new List<AoeSample>();
        if (module.GetType().GetField("Components")?.GetValue(module) is not IEnumerable components)
            return result;

        foreach (var c in components)
        {
            foreach (var f in c.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.GetValue(c) is not IEnumerable list || !IsAoeList(f.FieldType))
                    continue;
                foreach (var aoe in list)
                    Add(result, aoe);
            }
        }

        return result;

        static bool IsAoeList(Type t)
            => t.IsGenericType && t.GetGenericArguments() is [{ Name: "AOEInstance" }];

        static void Add(List<AoeSample> into, object? aoe)
        {
            if (aoe?.GetType().GetField("Shape")?.GetValue(aoe) is not { } shape)
                return;
            var t = aoe.GetType();
            var origin = t.GetField("Origin")?.GetValue(aoe);
            var rot = t.GetField("Rotation")?.GetValue(aoe);
            var risky = t.GetField("Risky")?.GetValue(aoe) as bool? ?? true;
            into.Add(new AoeSample(KindOf(shape), DimsOf(shape),
                (float)(origin?.GetType().GetField("X")?.GetValue(origin) ?? 0f),
                (float)(origin?.GetType().GetField("Z")?.GetValue(origin) ?? 0f),
                (float)(rot?.GetType().GetProperty("Deg")?.GetValue(rot) ?? 0f),
                risky));
        }
    }
}
