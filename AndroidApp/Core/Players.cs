namespace GlamourGames;
static class Pl
{
    public static string Name(int i) { var s = Save.Str("pname" + i, "").Trim(); return s.Length == 0 ? "Spieler " + (i + 1) : s; }
    public static void SetName(int i, string n) => Save.Set("pname" + i, n.Trim());
}
