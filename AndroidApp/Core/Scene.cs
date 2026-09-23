using SkiaSharp;
using Silk.NET.Input;
namespace GlamourGames;
static class Backdrop
{
    static readonly Random R = new(3);
    static readonly (float x, float y, float z, float ph)[] stars = Enumerable.Range(0, 220).Select(_ => ((float)R.NextDouble(), (float)R.NextDouble(), .2f + (float)R.NextDouble() * .8f, (float)R.NextDouble() * 6.3f)).ToArray();
    static SKShader vgSh; static (float, float, float, float) vgKey;
    public static void Draw(SKCanvas c, float t, SKColor a1, SKColor a2)
    {
        float x0 = App.VX0 - 50, x1 = App.VX1 + 50, y0 = App.VY0 - 50, y1 = App.VY1 + 50, w = x1 - x0, h = y1 - y0;
        c.DrawRect(x0, y0, w, h, Gfx.Fill(C.Bg));
        Gfx.Radial(c, x0 + w * (.2f + .08f * MathF.Sin(t * .23f)), y0 + h * (.15f + .08f * MathF.Cos(t * .31f)), w * .55f, a1, .22f);
        Gfx.Radial(c, x0 + w * (.85f + .07f * MathF.Cos(t * .19f)), y0 + h * (.9f + .06f * MathF.Sin(t * .27f)), w * .5f, a2, .18f);
        Gfx.Radial(c, x0 + w * (.5f + .2f * MathF.Sin(t * .11f)), y0 + h * (.5f + .15f * MathF.Cos(t * .13f)), w * .35f, C.Purple, .09f);
        foreach (var s in stars)
        {
            float px = x0 + ((s.x * w + t * 6 * s.z) % w), py = y0 + ((s.y * h - t * 4 * s.z) % h + h) % h, tw = .5f + .5f * MathF.Sin(t * 1.5f * s.z + s.ph);
            c.DrawCircle(px, py, 0.8f + 1.6f * s.z, Gfx.Fill(SKColors.White.A(.15f + .5f * tw * s.z)));
        }
        if (vgSh == null || vgKey != (x0, y0, w, h)) { vgSh?.Dispose(); vgKey = (x0, y0, w, h); vgSh = SKShader.CreateRadialGradient(new(x0 + w / 2, y0 + h / 2), Math.Max(w, h) * .75f, new[] { SKColors.Transparent, SKColors.Black.A(.55f) }, new[] { .55f, 1f }, SKShaderTileMode.Clamp); }
        var p = Gfx.Fill(SKColors.White); p.Shader = vgSh; c.DrawRect(x0, y0, w, h, p);
        Bloom.Draw(c, t, a1, a2, App.VX0, App.VX1, App.VY0, App.VY1);
    }
}
abstract class Scene
{
    public readonly Ui Ui = new(); public Modal Modal; public readonly Particles Fx = new(); public readonly Timers Tm = new(); public readonly Coro Co = new(); public float Time; float celT = -1, celDur; SKColor celCol;
    public virtual string Title => ""; public virtual SKColor Acc1 => C.Cyan; public virtual SKColor Acc2 => C.Pink; public virtual bool Chrome => true;
    protected Button Back;
    readonly List<(string s, float x, float y, SKColor c, float t, float size)> pops = new();
    public void Pop(string s, float x, float y, SKColor c, float size = 40) => pops.Add((s, x, y, c, 0, size));
    public virtual void Enter() { Log.I("enter " + GetType().Name); if (Chrome) Back = Ui.Add(new Button(24, 14, 260, 84, "<  Menü", C.Purple, () => App.Go(new Menu()), 32)); }
    public virtual void Leave() { Log.I("leave " + GetType().Name); }
    public virtual void Update(float dt) { }
    public abstract void Draw(SKCanvas c);
    public virtual void MouseMove(float x, float y) { }
    public virtual void MouseDown(float x, float y) { }
    public virtual void MouseUp(float x, float y) { }
    public virtual void Wheel(float d) { }
    public virtual void KeyDown(Key k) { }
    public virtual bool Escape() => false;
    public virtual void DebugWin() { }
    public virtual bool WantsHand => false;
    string banner; SKColor bannerCol;
    public void Celebrate(SKColor col, float dur = 5, float power = 1, string banner = null)
    {
        this.banner = banner; bannerCol = col;
        Log.I("celebrate " + GetType().Name); celT = 0; celDur = dur; celCol = col; Sfx.Play(S.Fanfare, .8f); Sfx.Play(S.Cheer, .7f); Tm.After(.6f, () => Sfx.Play(S.Sparkle, .6f));
        var rnd = Random.Shared; var cols = new[] { col, C.Gold, C.Pink, C.Cyan, C.Green, C.Purple };
        Fx.Cannon(-10, 900, -1.05f, (int)(70 * power)); Fx.Cannon(1610, 900, -2.09f, (int)(70 * power)); Sfx.Play(S.Pop, .8f, .8f);
        Fx.Shockwave(800, 450, col, 700, .9f); Fx.Shockwave(800, 450, C.Gold, 500, .7f); App.Flash(col.Light(.5f), .5f); App.Shake(12);
        for (int i = 0; i < (int)(dur * 3.2f * power); i++) { float d = .25f + i * .3f + (float)rnd.NextDouble() * .15f; Tm.After(d, () => Fx.Rocket(200 + rnd.NextSingle() * 1200, 120 + rnd.NextSingle() * 320, cols[rnd.Next(cols.Length)])); }
        for (int i = 0; i < 4; i++) { int k = i; float d = .5f + i * 1.4f * (dur / 5); Tm.After(d, () => { float x = 250 + rnd.NextSingle() * 1100; Fx.Lightning(x + rnd.NextSingle() * 200 - 100, -20, x, 300 + rnd.NextSingle() * 350, k % 2 == 0 ? col.Light(.3f) : C.Gold); Sfx.Play(S.Zap, .35f); App.Shake(8); }); }
        for (int i = 0; i < 3; i++) { float d = 1.2f + i * 1.6f; Tm.After(d, () => { Fx.Cannon(-10, 900, -1.05f, 40); Fx.Cannon(1610, 900, -2.09f, 40); Fx.Petals(800, 500, 30, cols, 500); Sfx.Play(S.Pop, .6f, 1.1f); }); }
    }
    void DrawBanner(SKCanvas c)
    {
        if (banner == null || celT < 0) return;
        float a = 1 - Ease.Clamp((celT - celDur + .8f) / .8f), u = Ease.OutElastic(celT / 1f) * (1 + .03f * MathF.Sin(celT * 7));
        if (a <= 0) return;
        c.Save(); c.Translate(800, 190); c.Scale(u);
        float fs = Math.Min(96, 1300 / Math.Max(6, banner.Length) * 1.7f);
        Gfx.BannerText(c, banner, fs, bannerCol, a); c.Restore();
    }
    void DrawRays(SKCanvas c)
    {
        float a = Ease.Clamp(celT / .5f) * (1 - Ease.Clamp((celT - celDur) / 2f)); if (a <= 0) return;
        c.Save(); c.Translate(800, 450); c.RotateDegrees(celT * 14);
        for (int i = 0; i < 16; i++) { using var p = new SKPath(); float ang = i * MathF.Tau / 8 / 2, w = .11f; p.MoveTo(0, 0); p.LineTo(MathF.Cos(ang - w) * 1800, MathF.Sin(ang - w) * 1800); p.LineTo(MathF.Cos(ang + w) * 1800, MathF.Sin(ang + w) * 1800); p.Close(); var pt = Gfx.Fill((i % 2 == 0 ? celCol : C.Gold).A(.11f * a)); pt.BlendMode = SKBlendMode.Plus; c.DrawPath(p, pt); }
        c.Restore();
    }
    public void BaseUpdate(float dt)
    {
        Time += dt; if (celT >= 0) { celT += dt; if (celT > celDur + 2) celT = -1; } for (int i = pops.Count - 1; i >= 0; i--) { var q = pops[i]; q.t += dt; if (q.t > 1.4f) pops.RemoveAt(i); else pops[i] = q; } Tm.Update(dt); Co.Update(dt); Fx.Update(dt); Ui.Update(dt); Modal?.Update(dt); Update(dt);
    }
    public void BaseDraw(SKCanvas c)
    {
        Backdrop.Draw(c, Time, Acc1, Acc2); if (celT >= 0) DrawRays(c);
        if (Chrome && Title.Length > 0) { Gfx.Text(c, Title, 800, 52, 44, SKColors.White, Al.C, true, 14, true); Gfx.Text(c, Title, 800, 52, 44, Acc1.Light(.55f), Al.C, true, 0, true); }
        Draw(c); Ui.Draw(c); Fx.Draw(c); DrawBanner(c);
        foreach (var q in pops) { float a = 1 - Ease.InCubic((q.t - .8f) / .6f), sc = Ease.OutBack(q.t / .3f); c.Save(); c.Translate(q.x, q.y - q.t * 70); c.Scale(sc, sc); Gfx.Text(c, q.s, 0, 0, q.size, q.c.A(a), Al.C, true, 10); c.Restore(); }
        Modal?.Draw(c);
    }
    public void Result(string title, string sub, SKColor col, params (string, SKColor, Action)[] btns) => Result(title, sub, col, null, btns);
    public void Result(string title, string sub, SKColor col, List<string> lines, params (string, SKColor, Action)[] btns)
    {
        var m = new Modal { Title = title, Sub = sub, Col = col, H = 380 + (lines?.Count ?? 0) * 34 };
        if (lines != null) m.Lines = lines;
        foreach (var (t, cc, a) in btns) m.Btns.Add(new Button { Text = t, Col = cc, Click = () => { Modal = null; a?.Invoke(); }, Size = 26 });
        Modal = m; Sfx.Play(S.Turn);
    }
    public void NameEntry(string key, int score, string title, Action done)
    {
        Fx.Confetti(1600, 90);
        var m = new Modal { Title = title, Sub = $"{score} Punkte - Name eintragen (max. 5 Zeichen)", Col = C.Gold, Input = true, H = 262, Text = Save.Str("lastname", "") };
        m.Submit = n => { Save.Set("lastname", n); Save.AddScore(key, n, score); Modal = null; done?.Invoke(); };
        m.Cancel = () => { Modal = null; done?.Invoke(); };
        m.Btns.Add(new Button { Text = "Speichern", Col = C.Green, Click = () => m.Submit(m.Text.Trim().Length == 0 ? "ANON" : m.Text.Trim()) });
        m.Btns.Add(new Button { Text = "Überspringen", Col = C.Dim, Click = () => m.Cancel() });
        Modal = m;
    }
    public static void HighscoreList(SKCanvas c, string key, float x, float y, float w, SKColor col)
    {
        var l = Save.Scores(key); Gfx.Text(c, "HIGHSCORES", x + w / 2, y, 30, col, Al.C, true, 8, true);
        for (int i = 0; i < 10; i++)
        {
            float yy = y + 44 + i * 32; bool have = i < l.Count; var cc = i == 0 ? C.Gold : i == 1 ? new SKColor(220, 220, 230) : i == 2 ? new SKColor(230, 150, 90) : C.Dim;
            Gfx.Text(c, $"{i + 1}.", x + 12, yy, 22, cc, Al.L); Gfx.Text(c, have ? l[i].name : "-----", x + 56, yy, 22, have ? SKColors.White : C.Dim.A(.4f), Al.L);
            Gfx.Text(c, have ? l[i].score.ToString() : "", x + w - 12, yy, 22, cc, Al.R);
        }
    }
}
