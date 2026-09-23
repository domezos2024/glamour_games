using System.Runtime.CompilerServices;
namespace GlamourGames;
static class Log
{
    static readonly object L = new();
    static readonly string Path = System.IO.Path.Combine(Save.Dir, "log.txt");
    static StreamWriter w;
    public static void I(string m, [CallerFilePath] string f = "", [CallerMemberName] string fn = "")
    {
        lock (L)
        {
            try
            {
                w ??= new StreamWriter(new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
                w.WriteLine($"{DateTime.Now:HH:mm:ss.fff} T{Environment.CurrentManagedThreadId} {System.IO.Path.GetFileNameWithoutExtension(f)}.{fn}: {m}");
            }
            catch { }
        }
    }
}
