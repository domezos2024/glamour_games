namespace GlamourGames.Android;

sealed class FpsMeter
{
    readonly string tag;
    long start = Java.Lang.JavaSystem.NanoTime();
    int frames;
    long worst;
    long prev;
    long alloc0 = GC.GetTotalAllocatedBytes();
    int gc0 = GC.CollectionCount(0);

    public FpsMeter(string tag) => this.tag = tag;

    bool first = true;
    public void Frame()
    {
        if (first) { first = false; global::Android.Util.Log.Info("GGFps", tag + " erstesBildMs=" + (global::Android.OS.SystemClock.ElapsedRealtime() - global::Android.OS.Process.StartElapsedRealtime)); }
        long now = Java.Lang.JavaSystem.NanoTime();
        if (prev != 0 && now - prev > worst) worst = now - prev;
        prev = now;
        frames++;
        long el = now - start;
        if (el < 2_000_000_000L) return;
        global::Android.Util.Log.Info("GGFps", $"{tag} fps={frames * 1e9 / el:F1} worstFrameMs={worst / 1e6:F1} allocKBps={(GC.GetTotalAllocatedBytes() - alloc0) / 1024.0 * 1e9 / el:F0} gc0={GC.CollectionCount(0) - gc0} parts={App.Current?.Fx.Count}");
        alloc0 = GC.GetTotalAllocatedBytes();
        gc0 = GC.CollectionCount(0);
        start = now;
        frames = 0;
        worst = 0;
    }
}
