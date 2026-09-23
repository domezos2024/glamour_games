using System.Text.Json;
namespace GlamourGames;
static class Save
{
    public static readonly string Dir = Path.Combine(global::Android.App.Application.Context.FilesDir?.AbsolutePath ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GlamourGames");
    static readonly string File_ = Path.Combine(Dir, "save.json");
    static Dictionary<string, string> d = new();
    static Save()
    {
        Directory.CreateDirectory(Dir);
        try { if (File.Exists(File_)) d = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(File_)) ?? new(); } catch { d = new(); }
    }
    public static int Int(string k, int def) => d.TryGetValue(k, out var v) && int.TryParse(v, out var i) ? i : def;
    public static string Str(string k, string def = "") => d.TryGetValue(k, out var v) ? v : def;
    public static void Set(string k, object v) { d[k] = Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture); try { File.WriteAllText(File_, JsonSerializer.Serialize(d)); } catch { } }
    public static List<(string name, int score)> Scores(string k)
    {
        var l = new List<(string, int)>();
        foreach (var e in Str(k).Split(';', StringSplitOptions.RemoveEmptyEntries)) { var p = e.Split('|'); if (p.Length == 2 && int.TryParse(p[1], out var s)) l.Add((p[0], s)); }
        return l;
    }
    public static bool IsHigh(string k, int s) { var l = Scores(k); return s > 0 && (l.Count < 10 || s > l[^1].score); }
    public static void AddScore(string k, string n, int s)
    {
        var l = Scores(k); l.Add((n, s)); l = l.OrderByDescending(x => x.score).Take(10).ToList();
        Set(k, string.Join(';', l.Select(x => x.name.Replace(";", "").Replace("|", "") + "|" + x.score)));
    }
}
