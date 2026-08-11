using System.Globalization;
using System.Text;

namespace Minerva;

/// <summary>
/// Reads back the tokens an <see cref="OperationOutput"/> produced for one line: whitespace-split,
/// with double-quoted strings (spaces/escapes) honoured. Typed <c>Next*</c> accessors mirror the
/// <c>Emit</c> overloads so op reconstruction reads fields in the same order they were written.
/// </summary>
public sealed class OpTokenReader
{
    private readonly List<string> tokens;
    private int pos;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public OpTokenReader(string line, int start = 0)
    {
        this.tokens = Tokenize(line, start);
    }

    public string FourCC => this.tokens.Count > 0 ? this.tokens[0] : "";
    public bool HasMore => this.pos < this.tokens.Count;

    /// <summary>Skip the leading 4-char tag; call once before reading fields.</summary>
    public OpTokenReader SkipTag()
    {
        this.pos = 1;
        return this;
    }

    public string NextString() => this.tokens[this.pos++];
    public int NextInt() => int.Parse(this.tokens[this.pos++], Inv);
    public bool NextBool() => this.tokens[this.pos++] == "1";
    public uint NextU32() => uint.Parse(this.tokens[this.pos++], Inv);
    public ulong NextU64() => ulong.Parse(this.tokens[this.pos++], Inv);
    public uint NextHex32() => uint.Parse(this.tokens[this.pos++], NumberStyles.HexNumber, Inv);
    public ulong NextHex64() => ulong.Parse(this.tokens[this.pos++], NumberStyles.HexNumber, Inv);
    public float NextFloat() => float.Parse(this.tokens[this.pos++], Inv);

    /// <summary>An angle stored as degrees; returned in radians.</summary>
    public Angle NextAngleDeg() => (this.NextFloat()).Degrees();

    /// <summary>A <c>(x,y,z,w)</c> vector token.</summary>
    public Vector4 NextVec4()
    {
        var t = this.tokens[this.pos++].Trim('(', ')');
        var p = t.Split(',');
        return new Vector4(float.Parse(p[0], Inv), float.Parse(p[1], Inv), float.Parse(p[2], Inv), float.Parse(p[3], Inv));
    }

    // whitespace tokenizer honouring "quoted strings" with \" and \\ escapes
    private static List<string> Tokenize(string line, int start)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        var have = false; // whether sb holds a (possibly empty) token

        for (var i = start; i < line.Length; ++i)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '\\' && i + 1 < line.Length)
                    sb.Append(line[++i]);
                else if (c == '"')
                    inQuotes = false;
                else
                    sb.Append(c);
            }
            else if (c == '"')
            {
                inQuotes = true;
                have = true;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (have)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    have = false;
                }
            }
            else
            {
                sb.Append(c);
                have = true;
            }
        }
        if (have)
            result.Add(sb.ToString());
        return result;
    }
}
