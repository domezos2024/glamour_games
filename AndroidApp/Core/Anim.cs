namespace GlamourGames;
static class Ease
{
    public static float Clamp(float t) => t < 0 ? 0 : t > 1 ? 1 : t;
    public static float OutCubic(float t) { t = Clamp(t); return 1 - MathF.Pow(1 - t, 3); }
    public static float InCubic(float t) { t = Clamp(t); return t * t * t; }
    public static float InOutCubic(float t) { t = Clamp(t); return t < .5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2; }
    public static float OutBack(float t) { t = Clamp(t); const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2); }
    public static float OutQuad(float t) { t = Clamp(t); return 1 - (1 - t) * (1 - t); }
    public static float OutBounce(float t)
    {
        t = Clamp(t); const float n = 7.5625f, d = 2.75f;
        if (t < 1 / d) return n * t * t;
        if (t < 2 / d) { t -= 1.5f / d; return n * t * t + .75f; }
        if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + .9375f; }
        t -= 2.625f / d; return n * t * t + .984375f;
    }
    public static float OutElastic(float t) { t = Clamp(t); if (t == 0 || t == 1) return t; return MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - .75f) * (2 * MathF.PI / 3)) + 1; }
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
class Spring
{
    public float V, Target, Vel, K = 260, D = 22;
    public Spring(float v = 0) { V = Target = v; }
    public void Snap(float v) { V = Target = v; Vel = 0; }
    public void Update(float dt)
    {
        int n = Math.Max(1, (int)MathF.Ceiling(dt / .008f)); float h = dt / n;
        for (int i = 0; i < n; i++) { Vel += ((Target - V) * K - Vel * D) * h; V += Vel * h; }
    }
}
class Tween
{
    public float T, Dur, Delay; public bool Done => T >= Dur + Delay;
    public Tween(float dur, float delay = 0) { Dur = dur; Delay = delay; }
    public float P => Ease.Clamp((T - Delay) / Dur);
    public void Update(float dt) { if (T < Dur + Delay) T += dt; }
    public void Reset() { T = 0; }
}
class Timers
{
    readonly List<(float t, Action a)> l = new();
    public void After(float sec, Action a) => l.Add((sec, a));
    public void Clear() => l.Clear();
    public void Update(float dt)
    {
        if (l.Count == 0) return;
        for (int i = 0; i < l.Count; i++) l[i] = (l[i].t - dt, l[i].a);
        var due = l.Where(x => x.t <= 0).ToList();
        l.RemoveAll(x => x.t <= 0);
        foreach (var d in due) d.a();
    }
}
class Coro
{
    class Run { public Stack<IEnumerator<object>> St = new(); public float Wait; public Func<bool> Until; }
    readonly List<Run> l = new();
    public void Start(IEnumerator<object> e) { var r = new Run(); r.St.Push(e); l.Add(r); }
    public void Clear() => l.Clear();
    public bool Busy => l.Count > 0;
    public void Update(float dt)
    {
        for (int i = 0; i < l.Count; i++)
        {
            var r = l[i];
            if (r.Wait > 0) { r.Wait -= dt; if (r.Wait > 0) continue; }
            if (r.Until != null) { if (!r.Until()) continue; r.Until = null; }
            while (r.St.Count > 0)
            {
                var e = r.St.Peek();
                if (!e.MoveNext()) { r.St.Pop(); continue; }
                var y = e.Current;
                if (y is IEnumerator<object> n) { r.St.Push(n); continue; }
                if (y is float f) r.Wait = f; else if (y is Func<bool> fn) r.Until = fn;
                break;
            }
            if (r.St.Count == 0) l.RemoveAt(i--);
        }
    }
}
