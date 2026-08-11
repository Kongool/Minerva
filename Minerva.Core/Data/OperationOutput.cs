using System.Globalization;
using System.Text;

namespace Minerva;

/// <summary>
/// Fluent sink an <see cref="WorldState.Operation"/> serializes itself into. Each op emits a
/// 4-char tag followed by its fields. Phase 1 provides a human-readable text form used for
/// debug logging and the self-test; Phase 4 (Replay) will add a compact binary sink behind
/// the same fluent surface. Keeping serialization on the op — rather than in a giant switch —
/// is what makes the state stream round-trippable.
/// </summary>
public sealed class OperationOutput
{
    private readonly StringBuilder sb = new();
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public OperationOutput Tag(string fourCC)
    {
        if (this.sb.Length > 0)
            this.sb.Append(' ');
        this.sb.Append(fourCC);
        return this;
    }

    public OperationOutput Emit(string? v) => this.Token(Quote(v ?? ""));
    public OperationOutput Emit(bool v) => this.Token(v ? "1" : "0");
    public OperationOutput Emit(int v) => this.Token(v.ToString(Inv));
    public OperationOutput Emit(uint v, string? format = null) => this.Token(v.ToString(format, Inv));
    public OperationOutput Emit(ulong v, string? format = null) => this.Token(v.ToString(format, Inv));
    public OperationOutput Emit(float v, string format = "f3") => this.Token(v.ToString(format, Inv));
    public OperationOutput Emit(WPos v) => this.Token($"[{v.X.ToString("f3", Inv)},{v.Z.ToString("f3", Inv)}]");
    public OperationOutput Emit(WDir v) => this.Token($"({v.X.ToString("f3", Inv)},{v.Z.ToString("f3", Inv)})");
    public OperationOutput Emit(Angle v) => this.Token(v.Deg.ToString("f3", Inv));
    public OperationOutput Emit(Vector4 v) => this.Token($"({v.X.ToString("f3", Inv)},{v.Y.ToString("f3", Inv)},{v.Z.ToString("f3", Inv)},{v.W.ToString("f3", Inv)})");

    private OperationOutput Token(string s)
    {
        this.sb.Append(' ').Append(s);
        return this;
    }

    // strings are the only token that can contain spaces (names, RSV values); quote when needed so
    // the replay parser can tokenize on whitespace. "" for empty; internal quotes/backslashes escaped.
    private static string Quote(string s)
    {
        if (s.Length > 0 && s.IndexOfAny([' ', '"', '\\', '\t']) < 0)
            return s;
        var b = new StringBuilder(s.Length + 2);
        b.Append('"');
        foreach (var ch in s)
        {
            if (ch is '"' or '\\')
                b.Append('\\');
            b.Append(ch);
        }
        b.Append('"');
        return b.ToString();
    }

    public override string ToString() => this.sb.ToString();
}
