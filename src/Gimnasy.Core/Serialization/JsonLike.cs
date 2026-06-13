using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Gimnasy.Core.Serialization;

/// <summary>
/// A small, deterministic writer/reader for the engine's JSON-like asset
/// syntax. Output is valid JSON (so any tool can read it), and on read we
/// allow comments and trailing commas so files stay hand-editable — this is
/// the "JSON-подобный синтаксис" used for <c>.scen</c> and <c>.material</c>.
/// </summary>
public static class JsonLike
{
    private static readonly JsonDocumentOptions ReadOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static JsonDocument Parse(string text) => JsonDocument.Parse(text, ReadOptions);

    public static string Write(object? value, string? headerComment = null)
    {
        var sb = new StringBuilder();
        if (headerComment is not null)
            sb.Append("// ").Append(headerComment).Append('\n');
        WriteValue(sb, value, 0);
        sb.Append('\n');
        return sb.ToString();
    }

    private static void WriteValue(StringBuilder sb, object? value, int indent)
    {
        switch (value)
        {
            case null: sb.Append("null"); break;
            case bool b: sb.Append(b ? "true" : "false"); break;
            case string s: WriteString(sb, s); break;
            case double d: sb.Append(FormatNumber(d)); break;
            case float f: sb.Append(FormatNumber(f)); break;
            case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
            case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
            case IDictionary<string, object?> map: WriteObject(sb, map, indent); break;
            case System.Collections.IEnumerable list: WriteArray(sb, list, indent); break;
            default: WriteString(sb, value.ToString() ?? ""); break;
        }
    }

    private static void WriteObject(StringBuilder sb, IDictionary<string, object?> map, int indent)
    {
        if (map.Count == 0) { sb.Append("{}"); return; }
        sb.Append("{\n");
        int n = 0;
        foreach (var kv in map)
        {
            Indent(sb, indent + 1);
            WriteString(sb, kv.Key);
            sb.Append(": ");
            WriteValue(sb, kv.Value, indent + 1);
            if (++n < map.Count) sb.Append(',');
            sb.Append('\n');
        }
        Indent(sb, indent);
        sb.Append('}');
    }

    private static void WriteArray(StringBuilder sb, System.Collections.IEnumerable list, int indent)
    {
        var items = list.Cast<object?>().ToList();
        // Numeric arrays (vectors/colors) stay on a single line for readability.
        bool inline = items.All(x => x is null or double or float or int or long or bool);
        if (items.Count == 0) { sb.Append("[]"); return; }
        if (inline)
        {
            sb.Append('[');
            for (int i = 0; i < items.Count; i++)
            {
                WriteValue(sb, items[i], indent);
                if (i < items.Count - 1) sb.Append(", ");
            }
            sb.Append(']');
            return;
        }
        sb.Append("[\n");
        for (int i = 0; i < items.Count; i++)
        {
            Indent(sb, indent + 1);
            WriteValue(sb, items[i], indent + 1);
            if (i < items.Count - 1) sb.Append(',');
            sb.Append('\n');
        }
        Indent(sb, indent);
        sb.Append(']');
    }

    private static void WriteString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        sb.Append('"');
    }

    private static string FormatNumber(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) return "0";
        // Round-trippable, but drop the trailing ".0" noise for whole numbers.
        if (d == System.Math.Floor(d) && System.Math.Abs(d) < 1e15)
            return ((long)d).ToString(CultureInfo.InvariantCulture);
        return d.ToString("R", CultureInfo.InvariantCulture);
    }

    private static void Indent(StringBuilder sb, int level) => sb.Append(' ', level * 2);
}
