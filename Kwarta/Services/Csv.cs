namespace Kwarta.Services;

public static class Csv
{
    /// <summary>One CSV line, quoting fields that contain commas, quotes or line breaks.</summary>
    public static string Row(params IEnumerable<string> fields) => string.Join(",", fields.Select(Escape));

    static string Escape(string field) =>
        field.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{field.Replace("\"", "\"\"")}\"" : field;
}
