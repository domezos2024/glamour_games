using SkiaSharp;
using Silk.NET.Input;
namespace GlamourGames;
class Battleship : Scene
{
    public override string Title => "Schiffe Versenken";
    public override SKColor Acc1 => C.Blue; public override SKColor Acc2 => C.Cyan;
    enum Ph { Setup, Pass, Attack, Over }
    class Ship { public int Size; public string Name; public List<int> Cells = new(); public bool Horiz = true, Sunk; }
    static readonly (int, string)[] Def = { (4, "Schlachtschiff"), (3, "Kreuzer"), (3, "Kreuzer"), (2, "Zerstörer") };
    const int N = 8;
    Ph ph = Ph.Setup; int cur, sel = -1; bool horiz = true; List<Ship>[] fleet = new List<Ship>[2]; HashSet<int>[] shots = { new(), new() }; int[] score = new int[2];
    readonly List<Button> shipBtns = new(); string status = ""; int hover = -1; int passTo; Action passAfter; bool busy; readonly Dictionary<int, float> mark = new(); readonly Dictionary<Ship, float> sink = new(); readonly Dictionary<Ship, bool> sunkFx = new();
    const float GS = 76, GX = 150, GY = 190, MS = 40, MX0 = 1080, MY0 = 250;
    static SKRect Cell(int i, float gx = GX, float gy = GY, float s = GS) => Gfx.R(gx + (i % N) * s, gy + (i / N) * s, s, s);
    public override void Enter() { base.Enter(); NewMatch(); }
    int starter;
    void NewMatch() { score = new int[2]; starter = 0; NewGame(); CoinToss.Start(this, f => starter = f); }
    void NextRound() { starter = 1 - starter; NewGame(); }
    void NewGame() { parade = null; resShown = false; fleet = new[] { Mk(), Mk() }; shots = new[] { new HashSet<int>(), new HashSet<int>() }; cur = 0; ph = Ph.Setup; sel = -1; horiz = true; mark.Clear(); sink.Clear(); sunkFx.Clear(); Fx.Clear(); Modal = null; busy = false; BuildUi(); status = $"{Pl.Name(0)}: Schiffe platzieren"; }
    static List<Ship> Mk() => Def.Select(d => new Ship { Size = d.Item1, Name = d.Item2 }).ToList();
    void BuildUi()
    {
        Ui.Clear(); Ui.Add(Back); shipBtns.Clear();
        if (ph == Ph.Setup)
        {
            var f = fleet[cur];
            for (int i = 0; i < f.Count; i++) { int k = i; var b = Ui.Add(new Button(1020, 190 + i * 84, 420, 68, $"{f[i].Name} ({f[i].Size})", f[i].Cells.Count > 0 ? C.Green : C.Cyan, () => { sel = k; horiz = f[k].Horiz; }, 26)); shipBtns.Add(b); }
            Ui.Add(new Button(1020, 550, 200, 62, "Drehen", C.Purple, () => horiz = !horiz, 24));
            Ui.Add(new Button(1240, 550, 200, 62, "Zufällig", C.Orange, Randomize, 24));
            Ui.Add(new Button(1020, 640, 420, 76, "Fertig", C.Green, Done, 32) { Enabled = fleet[cur].All(s => s.Cells.Count > 0) });
        }
        else if (ph == Ph.Pass) Ui.Add(new Button(600, 610, 400, 90, "Bereit", C.Green, () => { var a = passAfter; passAfter = null; cur = passTo; a?.Invoke(); }, 36));
        else if (ph == Ph.Attack) Ui.Add(new Button(1020, 760, 420, 62, "Aufgeben / Neues Spiel", C.Red, NewMatch, 22));
    }
    bool Occ(int p, int cell, Ship except = null) => fleet[p].Any(s => s != except && s.Cells.Contains(cell));
    List<int> Footprint(int cell, int size, bool h)
    {
        int r = cell / N, c = cell % N; var l = new List<int>();
        for (int k = 0; k < size; k++) { int rr = h ? r : r + k, cc = h ? c + k : c; if (rr >= N || cc >= N) return null; l.Add(rr * N + cc); }
        return l;
    }
    bool CanPlace(int cell, Ship s) { var fp = Footprint(cell, s.Size, horiz); return fp != null && fp.All(x => !Occ(cur, x, s)); }
    void Place(int cell)
    {
        if (sel < 0) { var s = fleet[cur].FirstOrDefault(x => x.Cells.Contains(cell)); if (s != null) { sel = fleet[cur].IndexOf(s); horiz = s.Horiz; s.Cells.Clear(); Sfx.Play(S.Take); BuildUi(); } return; }
        var sh = fleet[cur][sel];
        if (!CanPlace(cell, sh)) { App.Toast(Footprint(cell, sh.Size, horiz) == null ? "Schiff passt nicht aufs Feld" : "Position belegt"); Sfx.Play(S.NoMatch, .5f); return; }
        sh.Cells = Footprint(cell, sh.Size, horiz); sh.Horiz = horiz; Sfx.Play(S.Drop); var r = Cell(cell); Fx.Ring(r.MidX, r.MidY, C.Cyan, 20, 200);
        sel = fleet[cur].FindIndex(x => x.Cells.Count == 0); BuildUi();
    }
    void Randomize()
    {
        foreach (var s in fleet[cur]) s.Cells.Clear();
        foreach (var s in fleet[cur]) for (int t = 0; t < 500; t++) { bool h = Random.Shared.Next(2) == 0; var fp = Footprint(Random.Shared.Next(64), s.Size, h); if (fp != null && fp.All(x => !Occ(cur, x))) { s.Cells = fp; s.Horiz = h; break; } }
        sel = -1; Sfx.Play(S.Dice, .7f); BuildUi();
    }
    void Done()
    {
        if (cur == 0) ToPass(1, () => { ph = Ph.Setup; sel = -1; status = $"{Pl.Name(1)}: Schiffe platzieren"; BuildUi(); });
        else ToPass(starter, () => { ph = Ph.Attack; status = $"{Pl.Name(starter)} - Feuer frei!"; BuildUi(); });
    }
    void ToPass(int to, Action after) { passTo = to; passAfter = after; ph = Ph.Pass; hover = -1; BuildUi(); Sfx.Play(S.Turn); }
    public override bool WantsHand => hover >= 0;
    public override void MouseMove(float x, float y)
    {
        hover = -1; if (Modal != null || busy) return;
        if (ph == Ph.Setup || ph == Ph.Attack) for (int i = 0; i < 64; i++) if (Cell(i).Contains(x, y)) hover = i;
    }
    public override void KeyDown(Key k) { if (k == Key.R && ph == Ph.Setup) horiz = !horiz; }
    public override void Wheel(float d) { if (ph == Ph.Setup) horiz = !horiz; }
    public override void MouseUp(float x, float y)
    {
        if (parade != null && parade.T > 1.5f) { FireRes(); return; }
        if (hover < 0) return;
        if (ph == Ph.Setup) Place(hover); else if (ph == Ph.Attack) Shoot(hover);
    }
    void Shoot(int cell)
    {
        if (busy || shots[cur].Contains(cell)) return;
        int p = cur, o = 1 - p; shots[p].Add(cell); mark[cell + p * 100] = 0; var r = Cell(cell);
        var hit = fleet[o].FirstOrDefault(s => s.Cells.Contains(cell));
        if (hit != null)
        {
            Sfx.Play(S.Hit); Sfx.Play(S.Boom, .7f, .9f + Random.Shared.NextSingle() * .3f); Tm.After(.25f, () => Sfx.Play(S.Crackle, .4f)); App.Shake(11); App.Flash(C.Orange, .22f); Fx.Explosion(r.MidX, r.MidY, 1f); Fx.Burst(r.MidX, r.MidY, 24, new[] { C.Orange, C.Yellow, C.Red }, 380, 0, 200); Pop("BUMM!", r.MidX, r.MidY - 30, C.Yellow, 44);
            if (hit.Cells.All(shots[p].Contains))
            {
                hit.Sunk = true; sink[hit] = 0; Sfx.Play(S.Sunk); Sfx.Play(S.Creak, .8f); Tm.After(.7f, () => Sfx.Play(S.Gurgle, .8f)); App.Flash(C.Orange, .35f); App.Shake(16);
                for (int ci = 0; ci < hit.Cells.Count; ci++) { var q = Cell(hit.Cells[ci]); Tm.After(ci * .2f, () => { Fx.Explosion(q.MidX, q.MidY, 1.3f); Sfx.Play(S.Boom, .6f, .8f + Random.Shared.NextSingle() * .4f); App.Shake(9); }); }
                Tm.After(.9f, () => Fx.Lightning(r.MidX, -20, r.MidX, r.MidY, C.Orange));
                Pop("VERSENKT!", 480, 140, C.Orange, 60);
                if (fleet[o].All(s => s.Sunk)) { Win(p); return; }
                status = "VERSENKT! Nochmal!";
            }
            else status = "TREFFER! Nochmal!";
        }
        else
        {
            Sfx.Play(S.Miss, .5f); Sfx.Play(S.Splash, .9f, .9f + Random.Shared.NextSingle() * .3f); Fx.Splash(r.MidX, r.MidY, 1.1f); Tm.After(.28f, () => { Sfx.Play(S.Blub, .7f); Fx.Bubbles(r.MidX, r.MidY, 6, 14); Pop("BLUBB!", r.MidX, r.MidY - 26, C.Blue.Light(.55f), 34); }); status = "Wasser - Wechsel"; busy = true;
            Tm.After(1.5f, () => { busy = false; ToPass(o, () => { ph = Ph.Attack; status = $"{Pl.Name(o)} - Feuer frei!"; BuildUi(); }); });
        }
    }
    DiceParade parade; bool resShown; Action showRes;
    public override void DebugWin() { Modal = null; parade = new DiceParade(this, 1, new[] { C.Cyan, C.Pink }, new[] { "Spieler 1", "Spieler 2" }, new[] { 200, 150 }, PKind.Sailor, 1.35f); }
    void FireRes() { if (resShown) return; resShown = true; showRes?.Invoke(); }
    void Win(int p)
    {
        ph = Ph.Over; score[p]++; Sfx.Play(S.Big); App.Shake(12); busy = true;
        Tm.After(2.2f, () =>
        {
            if (ph != Ph.Over) return;
            parade = new DiceParade(this, p, new[] { C.Cyan, C.Pink }, new[] { Pl.Name(0), Pl.Name(1) }, new[] { score[0], score[1] }, PKind.Sailor, 1.35f);
            showRes = () => { busy = false; Result($"{Pl.Name(p)} GEWINNT!", $"Alle Schiffe von {Pl.Name(1 - p)} versenkt", p == 0 ? C.Cyan : C.Pink, new List<string> { $"Stand: {score[0]} : {score[1]}" }, ("Nochmal", C.Green, NextRound), ("Menü", C.Purple, () => App.Go(new Menu()))); };
        });
    }
    public override void Update(float dt)
    {
        parade?.Update(dt); if (parade != null && parade.T > parade.Total) FireRes();
        foreach (var k in mark.Keys.ToList()) mark[k] += dt;
        foreach (var sh in sink.Keys.ToList())
        {
            float st = sink[sh] + dt; sink[sh] = st;
            if (st < 3.2f) { var r0 = Cell(sh.Cells[0]); var r1 = Cell(sh.Cells[^1]); float mx = (r0.MidX + r1.MidX) / 2, my = (r0.MidY + r1.MidY) / 2; if (Random.Shared.NextSingle() < dt * 14) Fx.Bubbles(mx + (Random.Shared.NextSingle() - .5f) * sh.Size * GS * (sh.Horiz ? .8f : .1f), my + (Random.Shared.NextSingle() - .5f) * sh.Size * GS * (sh.Horiz ? .1f : .8f), 1, 10); if (Random.Shared.NextSingle() < dt * 3) Fx.Ripple(mx, my, 60 + 30 * sh.Size); if (st < 1.8f && Random.Shared.NextSingle() < dt * 5) Fx.Smoke(mx, my, 1, 16, 55, 1.6f); }
            else if (!sunkFx.ContainsKey(sh)) { sunkFx[sh] = true; var r0 = Cell(sh.Cells[0]); var r1 = Cell(sh.Cells[^1]); float mx = (r0.MidX + r1.MidX) / 2, my = (r0.MidY + r1.MidY) / 2; Fx.Splash(mx, my, 1.8f); Fx.Bubbles(mx, my, 14, 30); Sfx.Play(S.Splash, 1f, .7f); Sfx.Play(S.Blub, .8f, .7f); Pop("BLUBB... BLUBB...", mx, my - 40, C.Blue.Light(.6f), 34); }
        }
        if (ph == Ph.Attack)
        {
            int o2 = 1 - cur; bool any = false;
            foreach (var i in shots[cur]) { var sh = fleet[o2].FirstOrDefault(x => x.Cells.Contains(i)); if (sh == null) continue; if (sh.Sunk && (!sink.TryGetValue(sh, out var st2) || st2 > 2.2f)) continue; any = true; var r = Cell(i); if (Random.Shared.NextSingle() < dt * 2.6f) Fx.Smoke(r.MidX, r.MidY - 8, 1, 12, 55, 1.6f); if (Random.Shared.NextSingle() < dt * 9) Fx.Flames(r.MidX, r.MidY + 8, 1, 9); }
            if (any && Random.Shared.NextSingle() < dt * .8f) Sfx.Play(S.Crackle, .18f);
        }
        if (Random.Shared.NextSingle() < dt * 8 && ph == Ph.Attack) { var r = Cell(Random.Shared.Next(64)); Fx.Spark(r.MidX, r.MidY, C.Cyan.A(.5f), 1, 12); }
    }
    public override void Draw(SKCanvas c) { DrawBoard(c); parade?.Draw(c); }
    void DrawBoard(SKCanvas c)
    {
        if (ph == Ph.Pass) { DrawPass(c); return; }
        Gfx.Text(c, status, 800, 108, 34, ph == Ph.Attack ? C.Cyan : C.Gold, Al.C, true, 10);
        if (ph == Ph.Setup) DrawSetup(c); else DrawAttack(c);
    }
    void DrawPass(SKCanvas c)
    {
        var r = Gfx.Ctr(800, 450, 900, 420); Gfx.Glow(c, r, 30, C.Cyan, 26, .5f); W.Panel(c, r, C.Cyan, 30);
        Gfx.Text(c, Pl.Name(passTo), 800, 330, 84, passTo == 0 ? C.Cyan : C.Pink, Al.C, true, 24, true);
        Gfx.Text(c, "Gerät übergeben - der andere Spieler schaut weg!", 800, 430, 30, SKColors.White, Al.C, false);
        Gfx.Text(c, "Erst wenn du bereit bist, auf \"Bereit\" tippen.", 800, 480, 24, C.Dim, Al.C, false);
    }
    void Water(SKCanvas c, float gx, float gy, float s, bool labels)
    {
        var r = Gfx.R(gx - 8, gy - 8, N * s + 16, N * s + 16); Gfx.Glow(c, r, 14, C.Cyan, 16, .35f);
        Gfx.RectGrad(c, r, 14, new SKColor(0, 60, 100), new SKColor(0, 12, 34)); c.Save(); c.ClipRoundRect(new SKRoundRect(r, 14), SKClipOperation.Intersect, true);
        for (int k = 0; k < 9; k++) { float y = gy + k * s * N / 8 - 8; var wp = new SKPath(); wp.MoveTo(gx - 10, y); for (float x = gx - 10; x < gx + N * s + 10; x += 12) wp.LineTo(x, y + MathF.Sin(x * .03f + Time * 1.4f + k) * 4); c.DrawPath(wp, Gfx.Line(C.Cyan.A(.06f), 2)); wp.Dispose(); }
        c.Restore(); Gfx.Stroke(c, r, 14, C.Cyan.A(.8f), 2.5f);
        var gl = Gfx.Line(C.Cyan.A(.22f), 1.5f); for (int k = 0; k <= N; k++) { c.DrawLine(gx + k * s, gy, gx + k * s, gy + N * s, gl); c.DrawLine(gx, gy + k * s, gx + N * s, gy + k * s, gl); }
        if (labels) for (int k = 0; k < N; k++) { Gfx.Text(c, ((char)('A' + k)).ToString(), gx + k * s + s / 2, gy - 22, 20, C.Cyan.Light(.4f)); Gfx.Text(c, (k + 1).ToString(), gx - 24, gy + k * s + s / 2, 20, C.Cyan.Light(.4f)); }
    }
    static void Hull(SKCanvas c, float x, float y, int len, float s, bool horiz, float alpha, float red, float t)
    {
        float L = len * s, W = s;
        c.Save(); if (horiz) c.Translate(x, y); else { c.Translate(x + s, y); c.RotateDegrees(90); }
        var sh = Gfx.Fill(SKColors.Black.A(.4f * alpha)); sh.MaskFilter = Gfx.Blur(5); c.DrawRoundRect(Gfx.R(4, W * .3f, L - 6, W * .6f), 8, 8, sh);
        using var hp = new SKPath(); hp.MoveTo(W * .1f, W * .18f); hp.LineTo(L - W * .55f, W * .12f); hp.CubicTo(L - W * .1f, W * .2f, L - W * .05f, W * .4f, L - W * .03f, W * .5f);
        hp.CubicTo(L - W * .05f, W * .6f, L - W * .1f, W * .8f, L - W * .55f, W * .88f); hp.LineTo(W * .1f, W * .82f); hp.Close();
        using var sh1 = SKShader.CreateLinearGradient(new(0, W * .1f), new(0, W * .9f), new[] { new SKColor(170, 190, 215).Mix(C.Red, red * .55f), new SKColor(80, 96, 124).Mix(C.Red, red * .55f) }, null, SKShaderTileMode.Clamp);
        var p = Gfx.Fill(SKColors.White.A(alpha)); p.Shader = sh1; c.DrawPath(hp, p); c.DrawPath(hp, Gfx.Line(new SKColor(30, 40, 60, (byte)(255 * alpha)), 2));
        var deck = Gfx.R(W * .22f, W * .3f, L - W * .95f, W * .4f); Gfx.Rect(c, deck, 5, new SKColor(120, 135, 160, (byte)(255 * alpha)).Mix(C.Red, red * .55f));
        int towers = Math.Max(1, len - 1);
        for (int k = 0; k < towers; k++) { float tx = W * .35f + k * (L - W * 1.3f) / Math.Max(1, towers - 1 == 0 ? 1 : towers - 1); if (towers == 1) tx = L * .4f; var tr = Gfx.Ctr(tx, W * .5f, W * .3f, W * .3f); Gfx.RectGrad(c, tr, 4, new SKColor(220, 230, 245, (byte)(255 * alpha)), new SKColor(100, 115, 140, (byte)(255 * alpha))); if (k % 2 == 0) c.DrawLine(tx, W * .5f, tx + W * .28f, W * .5f, Gfx.Line(new SKColor(40, 50, 70, (byte)(255 * alpha)), 3.5f)); }
        c.Restore();
    }
    void DrawFire(SKCanvas c, float cx, float cy, float s, float t, int seed)
    {
        float f = .75f + .25f * MathF.Sin(t * 14 + seed);
        Gfx.Radial(c, cx, cy, s * .9f, C.Orange, .8f * f); Gfx.Radial(c, cx, cy - s * .08f, s * .5f, C.Yellow, .9f); Gfx.Ball(c, cx, cy, s * .12f, C.White);
        using var fl = new SKPath(); fl.MoveTo(cx - s * .2f, cy + s * .2f); fl.CubicTo(cx - s * .3f, cy - s * .1f, cx - s * .05f, cy - s * .2f, cx, cy - s * (.4f + .08f * MathF.Sin(t * 11 + seed))); fl.CubicTo(cx + s * .05f, cy - s * .2f, cx + s * .3f, cy - s * .1f, cx + s * .2f, cy + s * .2f); fl.Close();
        var pp = Gfx.Fill(C.Orange.A(.85f)); pp.BlendMode = SKBlendMode.Plus; c.DrawPath(fl, pp);
    }
    void DrawSetup(SKCanvas c)
    {
        Water(c, GX, GY, GS, true);
        foreach (var s in fleet[cur]) if (s.Cells.Count > 0) { var r = Cell(s.Cells[0]); Hull(c, r.Left + 3, r.Top + 3, s.Size, GS - 6, s.Horiz, 1, 0, Time); }
        if (hover >= 0 && sel >= 0)
        {
            var sh = fleet[cur][sel]; bool ok = CanPlace(hover, sh); var fp = Footprint(hover, sh.Size, horiz) ?? new List<int> { hover };
            foreach (var i in fp) { var r = Cell(i); Gfx.Rect(c, Gfx.Inflate(r, -3), 8, (ok ? C.Green : C.Red).A(.35f)); }
            if (ok) { var r = Cell(hover); Hull(c, r.Left + 3, r.Top + 3, sh.Size, GS - 6, horiz, .6f, 0, Time); }
        }
        var pr = Gfx.R(1000, 130, 460, 620); Gfx.Text(c, $"Flotte von {Pl.Name(cur)}", 1230, 158, 28, cur == 0 ? C.Cyan : C.Pink, Al.C, true, 8);
        for (int i = 0; i < shipBtns.Count; i++) { shipBtns[i].Selected = sel == i; shipBtns[i].Col = fleet[cur][i].Cells.Count > 0 ? C.Green : C.Cyan; }
        Gfx.Text(c, sel >= 0 ? $"Ausgewählt: {fleet[cur][sel].Name} - {(horiz ? "waagerecht" : "senkrecht")}" : "Wähle ein Schiff (oder tippe ein platziertes zum Verschieben)", 1230, 528, 20, C.Dim, Al.C, false);
    }
    void DrawAttack(SKCanvas c)
    {
        int p = cur, o = 1 - p;
        Water(c, GX, GY, GS, true);
        foreach (var s in fleet[o]) if (s.Sunk)
            {
                var r = Cell(s.Cells[0]); float st = sink.TryGetValue(s, out var q) ? q : 9;
                if (st >= 3.2f) { Hull(c, r.Left + 3, r.Top + 3, s.Size, GS - 6, s.Horiz, .55f, 1, Time); continue; }
                float u = Ease.InOutCubic(st / 3.2f), L = s.Size * (GS - 6), cw = s.Horiz ? L : GS - 6, ch = s.Horiz ? GS - 6 : L, cx = r.Left + 3 + cw / 2, cy = r.Top + 3 + ch / 2;
                c.Save(); c.Translate(cx + MathF.Sin(st * 9) * 2 * (1 - u), cy + 34 * u); c.RotateDegrees((s.Horiz ? 1 : -1) * 24 * u + MathF.Sin(st * 5) * 2); c.Scale(1 - .25f * u); c.Translate(-cx, -cy);
                Hull(c, r.Left + 3, r.Top + 3, s.Size, GS - 6, s.Horiz, 1 - .5f * u, 1, Time); c.Restore();
                var wr = Gfx.R(cx - cw / 2 - 6, cy - ch / 2 - 6, cw + 12, ch + 40); Gfx.Rect(c, wr, 14, new SKColor(0, 60, 110).A(.8f * u));
                for (int k = 0; k < 3; k++) { float ph2 = (st * .8f + k * .33f) % 1; c.DrawOval(cx, cy + 10, cw * (.3f + .5f * ph2), ch * (.2f + .35f * ph2), Gfx.Line(SKColors.White.A(.5f * (1 - ph2)), 2.5f)); }
            }
        for (int i = 0; i < 64; i++)
        {
            var r = Cell(i);
            if (shots[p].Contains(i))
            {
                float t = mark.TryGetValue(i + p * 100, out var mt) ? mt : 9; bool isHit = fleet[o].Any(s => s.Cells.Contains(i));
                if (isHit)
                {
                    var hs = fleet[o].First(s => s.Cells.Contains(i)); float fs = 1; if (hs.Sunk) fs = 1 - Ease.Clamp((sink.TryGetValue(hs, out var sq) ? sq : 9) / 2.2f);
                    if (fs > .03f) DrawFire(c, r.MidX, r.MidY, GS * fs, Time, i); else c.DrawCircle(r.MidX, r.MidY, 14, Gfx.Fill(SKColors.Black.A(.35f)));
                }
                else
                {
                    c.DrawCircle(r.MidX, r.MidY, 7, Gfx.Fill(SKColors.White.A(.75f))); c.DrawCircle(r.MidX, r.MidY, 9 + 20 * Ease.OutCubic(t / 1f), Gfx.Line(SKColors.White.A(.6f * (1 - Ease.Clamp(t))), 3));
                    for (int k = 0; k < 2; k++) { float ph2 = (Time * .55f + k * .5f + i * .13f) % 1; c.DrawOval(r.MidX, r.MidY, 10 + 24 * ph2, (10 + 24 * ph2) * .42f, Gfx.Line(new SKColor(200, 235, 255).A(.4f * (1 - ph2)), 2)); }
                }
            }
        }
        if (hover >= 0 && !shots[p].Contains(hover) && !busy)
        {
            var r = Cell(hover); Gfx.Rect(c, Gfx.Inflate(r, -2), 6, C.Red.A(.18f)); float cr = 22 + MathF.Sin(Time * 6) * 2; var cp = Gfx.Line(C.Red, 3);
            c.DrawCircle(r.MidX, r.MidY, cr, cp); c.DrawLine(r.MidX - cr - 8, r.MidY, r.MidX + cr + 8, r.MidY, cp); c.DrawLine(r.MidX, r.MidY - cr - 8, r.MidX, r.MidY + cr + 8, cp);
        }
        int left = fleet[o].Count(s => !s.Sunk); Gfx.Text(c, $"Feindliche Schiffe übrig: {left}", 1240, 640, 26, SKColors.White, Al.C, true, 4);
        Gfx.Text(c, $"Stand: {score[0]} : {score[1]}", 1240, 690, 26, C.Gold, Al.C, true, 6);
        Gfx.Text(c, $"Treffer von {Pl.Name(p)}: {shots[p].Count(i => fleet[o].Any(s => s.Cells.Contains(i)))}   Schüsse: {shots[p].Count}", 1240, 590, 24, C.Cyan.Light(.3f), Al.C, false);
    }
}
