using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// Shared by runtime loading and the editor import/export workflow.
public static class LocalizationCsv
{
    public sealed class Row
    {
        public string Key;
        public string Korean;
        public string English;
        public string Context;
    }

    private static readonly Regex Placeholder = new Regex(@"\{([^{}]+)\}");

    public static List<Row> Parse(string csv)
    {
        List<List<string>> records = ReadRecords((csv ?? string.Empty).TrimStart('\uFEFF'));
        if (records.Count == 0) throw new FormatException("Translation CSV is empty.");
        List<string> headers = records[0];
        int key = headers.IndexOf("Key"), ko = headers.IndexOf("ko"), en = headers.IndexOf("en");
        int context = headers.IndexOf("Context");
        if (key < 0 || ko < 0 || en < 0 || headers.Distinct().Count() != headers.Count)
            throw new FormatException("CSV requires unique Key, ko and en headers (Context is optional).");
        var rows = new List<Row>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 1; i < records.Count; i++)
        {
            List<string> record = records[i];
            if (record.All(string.IsNullOrEmpty)) continue;
            if (record.Count != headers.Count) throw new FormatException($"CSV record {i + 1}: column count mismatch.");
            string id = record[key].Trim();
            if (id.Length == 0 || !keys.Add(id)) throw new FormatException($"CSV record {i + 1}: blank or duplicate key '{id}'.");
            if (!string.IsNullOrWhiteSpace(record[en]) && !TokensMatch(record[ko], record[en]))
                throw new FormatException($"CSV key '{id}': ko/en placeholders differ.");
            rows.Add(new Row { Key = id, Korean = record[ko], English = record[en], Context = context < 0 ? "" : record[context] });
        }
        return rows;
    }

    public static bool TokensMatch(string source, string translated)
    {
        var tokens = new HashSet<string>(Placeholder.Matches(source ?? "").Cast<Match>().Select(m => m.Groups[1].Value));
        return tokens.SetEquals(Placeholder.Matches(translated ?? "").Cast<Match>().Select(m => m.Groups[1].Value));
    }

    public static string Write(IEnumerable<Row> rows)
    {
        var text = new StringBuilder("Key,ko,en,Context\r\n");
        foreach (Row row in rows)
        {
            text.Append(Escape(row.Key)).Append(',').Append(Escape(row.Korean)).Append(',')
                .Append(Escape(row.English)).Append(',').Append(Escape(row.Context)).Append("\r\n");
        }
        return text.ToString();
    }

    private static string Escape(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";

    private static List<List<string>> ReadRecords(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        bool quoted = false, closed = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else { quoted = false; closed = true; }
                }
                else field.Append(c);
                continue;
            }
            if (c == ',' || c == '\r' || c == '\n')
            {
                record.Add(field.ToString()); field.Clear(); closed = false;
                if (c != ',')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    records.Add(record); record = new List<string>();
                }
            }
            else if (c == '"')
            {
                if (closed || field.Length != 0) throw new FormatException("Unexpected quote in CSV field.");
                quoted = true;
            }
            else
            {
                if (closed) throw new FormatException("Unexpected character after quoted CSV field.");
                field.Append(c);
            }
        }
        if (quoted) throw new FormatException("Unclosed quoted CSV field.");
        if (field.Length != 0 || record.Count != 0 || closed)
        {
            record.Add(field.ToString()); records.Add(record);
        }
        return records;
    }
}
