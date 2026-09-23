using SkiaSharp;
using Silk.NET.Input;
namespace GlamourGames;
class SnakeGame : Scene
{
    public override string Title => "Snake";
    public override SKColor Acc1 => C.Green; public override SKColor Acc2 => C.Yellow;
    const int N = 16; const float CS = 44, BX = 800 - N * CS / 2, BY = 128;
    List<(int x, int y)> body = new(), prev = new(); (int x, int y) dir = (1, 0), next = (1, 0), food; int score, best; float speed = .17f, acc; bool running, dead, started; float deadT, eatT; readonly Queue<(int, int)> turns = new();
    public override void Enter()
    {
        base.Enter(); best = Save.Int("sn_best", 0);
        Ui.Add(new Button(40, 700, 260, 62, "Neustart", C.Green, Reset, 24)); Ui.Add(new Button(40, 780, 260, 62, "Start / Pause", C.Cyan, Toggle, 24));
        (string t, float x, float y, Key k)[] pad = { ("▲", 1360, 560, Key.Up), ("▼", 1360, 780, Key.Down), ("◀", 1250, 670, Key.Left), ("▶", 1470, 670, Key.Right) };
        foreach (var (t, x, y, k) in pad) { var kk = k; Ui.Add(new Button(x, y, 110, 110, t, C.Green, () => KeyDown(kk), 46) { Alpha = .6f }); }
        Reset();
    }
    void Reset() { body = new() { (4, 8), (3, 8), (2, 8) }; prev = body.ToList(); dir = next = (1, 0); turns.Clear(); score = 0; speed = .17f; acc = 0; running = false; dead = false; started = false; Modal = null; PlaceFood(); }
    void Toggle() { if (dead) { Reset(); return; } running = !running; started = true; Sfx.Play(S.Turn); }
    void PlaceFood() { do food = (Random.Shared.Next(N), Random.Shared.Next(N)); while (body.Contains(food)); }
    public override void KeyDown(Key k)
    {
        (int, int)? d = k switch { Key.Up or Key.W => (0, -1), Key.Down or Key.S => (0, 1), Key.Left or Key.A => (-1, 0), Key.Right or Key.D => (1, 0), _ => null };
        if (k == Key.Space || k == Key.Enter) { Toggle(); return; }
        if (d == null || dead) return;
        if (!running && !started) { running = true; started = true; }
        (int x, int y) last = turns.Count > 0 ? turns.Last() : dir; if (turns.Count < 3 && !(d.Value.Item1 == -last.x && d.Value.Item2 == -last.y) && d.Value != last) turns.Enqueue(d.Value);
    }
    SKPoint sw; bool swActive;
    public override void MouseDown(float x, float y) { sw = new(x, y); swActive = true; }
    public override void MouseUp(float x, float y) { swActive = false; }
    public override void MouseMove(float x, float y)
    {
        if (!swActive) return;
        float dx = x - sw.X, dy = y - sw.Y;
        if (MathF.Abs(dx) < 40 && MathF.Abs(dy) < 40) return;
        sw = new(x, y);
        KeyDown(MathF.Abs(dx) > MathF.Abs(dy) ? (dx > 0 ? Key.Right : Key.Left) : (dy > 0 ? Key.Down : Key.Up));
    }
    void Tick()
    {
        if (turns.Count > 0) next = turns.Dequeue(); dir = next; prev = body.ToList();
        var h = body[0]; var nh = (x: h.x + dir.x, y: h.y + dir.y);
        if (nh.x < 0 || nh.x >= N || nh.y < 0 || nh.y >= N || body.Take(body.Count - 1).Contains(nh)) { Die(); return; }
        body.Insert(0, nh);
        if (nh == food)
        {
            score++; eatT = 0; Sfx.Play(S.Eat); speed = Math.Max(.095f, .17f - score * .003f); prev.Add(prev[^1]); var fc = P(food); Fx.Burst(fc.X, fc.Y, 24, new[] { C.Yellow, C.Green, C.White }, 260); Pop("+1", fc.X, fc.Y - 20, C.Yellow, 34); PlaceFood();
        }
        else body.RemoveAt(body.Count - 1);
        Sfx.Play(S.Tick, .12f);
    }
    void Die()
    {
        dead = true; running = false; deadT = 0; Sfx.Play(S.Die); App.Shake(16); App.Flash(C.Red, .35f); var hp = P(body[0]); Fx.Burst(hp.X, hp.Y, 60, new[] { C.Green, C.Red, C.White }, 500); Fx.Explosion(hp.X, hp.Y, 1f); Sfx.Play(S.Boom, .6f);
        bool nb = score > best; if (nb) { best = score; Save.Set("sn_best", best); }
        Tm.After(.9f, () => { if (nb) { Celebrate(C.Gold, 4, 1f); Sfx.Play(S.Big); } Result(nb ? "NEUER BESTWERT!" : "GAME OVER", $"Länge {score + 3}  -  Punkte {score}", nb ? C.Gold : C.Red, new List<string> { $"Bestwert: {best}" }, ("Nochmal", C.Green, Reset), ("Menü", C.Purple, () => App.Go(new Menu()))); });
    }
    static SKPoint P((int x, int y) c) => new(BX + c.x * CS + CS / 2, BY + c.y * CS + CS / 2);
    public override void Update(float dt)
    {
        eatT += dt; if (dead) { deadT += dt; return; }
        if (running) { acc += dt; while (acc >= speed && running) { acc -= speed; Tick(); } }
        if (running && Random.Shared.NextSingle() < dt * 30) { var h = Pos(0); Fx.Spark(h.X, h.Y, C.Green, 1, 30); }
    }
    SKPoint Pos(int i)
    {
        float a = running ? Ease.Clamp(acc / speed) : 1; var to = P(body[i]); var from = P(i < prev.Count ? prev[i] : prev[^1]); if (!running) from = to;
        return new(from.X + (to.X - from.X) * a, from.Y + (to.Y - from.Y) * a);
    }
    public override void Draw(SKCanvas c)
    {
        W.PlayerBox(c, Gfx.R(40, 140, 260, 200), "Punkte", score.ToString(), C.Green, running, Time, $"Länge {body.Count}");
        W.PlayerBox(c, Gfx.R(40, 370, 260, 200), "Bestwert", best.ToString(), C.Gold, false, Time);
        Gfx.Text(c, $"Tempo {(.17f / speed * 100):0}%", 170, 620, 24, C.Dim, Al.C, false);
        var fr = Gfx.R(BX - 10, BY - 10, N * CS + 20, N * CS + 20); Gfx.Glow(c, fr, 16, C.Green, 20, .45f); Gfx.RectGrad(c, fr, 16, new SKColor(4, 22, 14), new SKColor(2, 8, 10)); Gfx.Stroke(c, fr, 16, C.Green.A(.9f), 3);
        var gl = Gfx.Line(C.Green.A(.10f), 1.2f); for (int k = 0; k <= N; k++) { c.DrawLine(BX + k * CS, BY, BX + k * CS, BY + N * CS, gl); c.DrawLine(BX, BY + k * CS, BX + N * CS, BY + k * CS, gl); }
        for (int i = 0; i < N * N; i += 1) if ((i / N + i % N) % 2 == 0) c.DrawRect(BX + (i % N) * CS, BY + (i / N) * CS, CS, CS, Gfx.Fill(C.Green.A(.025f)));
        var fp = P(food); float pl = 1 + .12f * MathF.Sin(Time * 6);
        Gfx.Radial(c, fp.X, fp.Y, CS * 1.2f, C.Yellow, .45f); Gfx.Ball(c, fp.X, fp.Y, CS * .32f * pl, C.Yellow.Mix(C.Orange, .3f)); Gfx.Star(c, fp.X + MathF.Cos(Time * 3) * CS * .5f, fp.Y + MathF.Sin(Time * 3) * CS * .5f, 5, C.White.A(.8f), Time);
        if (dead && deadT > .2f) { }
        int n = body.Count; var pts = new SKPoint[n]; for (int i = 0; i < n; i++) pts[i] = Pos(i);
        float fade = dead ? Math.Max(0, 1 - deadT * .8f) : 1;
        for (int i = n - 1; i >= 0; i--)
        {
            float t = i / (float)Math.Max(1, n - 1); float r = CS * (.46f - .17f * t); var col = C.Green.Mix(new SKColor(0, 120, 90), t * .8f);
            for (int s = 3; s >= 0; s--)
            {
                var q = s == 0 ? pts[i] : i + 1 < n ? new SKPoint(pts[i].X + (pts[i + 1].X - pts[i].X) * s / 4f, pts[i].Y + (pts[i + 1].Y - pts[i].Y) * s / 4f) : pts[i];
                if (s == 0 || i + 1 < n) { Gfx.Ball(c, q.X, q.Y, r, col.A(fade)); }
            }
            if (i % 3 == 0) Gfx.Radial(c, pts[i].X, pts[i].Y, r * 2, C.Green, .18f * fade);
        }
        var h = pts[0]; float ex = dir.x, ey = dir.y, px = -ey, py = ex; float er = CS * .11f;
        foreach (var sd in new[] { -1, 1 }) { float ox = h.X + ex * CS * .12f + px * sd * CS * .2f, oy = h.Y + ey * CS * .12f + py * sd * CS * .2f; c.DrawCircle(ox, oy, er * 1.4f, Gfx.Fill(SKColors.White)); c.DrawCircle(ox + ex * er * .5f, oy + ey * er * .5f, er * .8f, Gfx.Fill(SKColors.Black)); }
        if (!started && !dead) { Gfx.Rect(c, Gfx.Ctr(800, 460, 640, 130), 24, SKColors.Black.A(.6f)); Gfx.Text(c, "Wische oder nutze das Kreuz", 800, 435, 34, SKColors.White, Al.C, true, 6); Gfx.Text(c, "Start / Pause: Knopf links", 800, 485, 26, C.Dim, Al.C, false); }
        else if (!running && !dead) Gfx.Text(c, "PAUSE", 800, 460, 80, C.Yellow, Al.C, true, 20);
    }
}
