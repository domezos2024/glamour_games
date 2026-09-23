using SkiaSharp;
namespace GlamourGames;
class TicTacToe : Scene
{
    public override string Title => "Tic Tac Toe";
    public override SKColor Acc1 => C.Cyan; public override SKColor Acc2 => C.Pink;
    static readonly int[][] Lines = { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, new[] { 0, 4, 8 }, new[] { 2, 4, 6 } };
    int[] b = new int[9]; float[] pt = new float[9]; int cur = 1, starter = 2, gameIdx; int[] rw = new int[3]; int[] wins = new int[3]; int[] points = new int[3];
    int[] dots = { -1, -1, -1 }; int[] win; bool over, roundEnd; string status = ""; float winT; int hover = -1; bool lockIn;
    const float CS = 180, BX = 800 - CS * 1.5f, BY = 190;
    static SKRect Cell(int i) => Gfx.R(BX + (i % 3) * CS, BY + (i / 3) * CS, CS, CS);
    public override void Enter() { base.Enter(); Ui.Add(new Button(40, 780, 260, 62, "Punkte zurück", C.Purple, () => { points = new int[3]; wins = new int[3]; StartMatch(); }, 22)); StartMatch(); }
    void StartMatch() { NewRound(); CoinToss.Start(this, f => { starter = 3 - (f + 1); NewRound(); }); }
    void NewRound() { rw = new int[3]; dots = new[] { -1, -1, -1 }; gameIdx = 0; starter = 3 - starter; NewGame(); }
    void NewGame() { b = new int[9]; pt = new float[9]; win = null; over = false; lockIn = false; winT = 0; cur = gameIdx % 2 == 0 ? starter : 3 - starter; status = $"{Pl.Name(cur - 1)} ist dran"; }
    public override bool WantsHand => hover >= 0;
    public override void MouseMove(float x, float y) { hover = -1; if (over || Modal != null) return; for (int i = 0; i < 9; i++) if (Cell(i).Contains(x, y) && b[i] == 0) hover = i; }
    public override void MouseUp(float x, float y) { if (hover >= 0) Place(hover); }
    void Place(int i)
    {
        if (over || b[i] != 0 || lockIn) return;
        b[i] = cur; Sfx.Play(cur == 1 ? S.PlaceX : S.PlaceO); var r = Cell(i); Fx.Burst(r.MidX, r.MidY, 14, new[] { cur == 1 ? C.Cyan : C.Pink }, 200);
        win = Lines.FirstOrDefault(l => b[l[0]] != 0 && b[l[0]] == b[l[1]] && b[l[1]] == b[l[2]]);
        if (win != null)
        {
            over = true; wins[cur]++; status = $"{Pl.Name(cur - 1)} gewinnt dieses Spiel!"; Sfx.Play(S.Win); App.Flash(cur == 1 ? C.Cyan : C.Pink, .3f); App.Shake(8);
            foreach (var k in win) { var q = Cell(k); Fx.Burst(q.MidX, q.MidY, 40, new[] { cur == 1 ? C.Cyan : C.Pink, C.White }, 420); Fx.Explosion(q.MidX, q.MidY, .5f); } Fx.Lightning(Cell(win[1]).MidX, -20, Cell(win[1]).MidX, Cell(win[1]).MidY, cur == 1 ? C.Cyan : C.Pink); Celebrate(cur == 1 ? C.Cyan : C.Pink, 2.5f, .5f, $"{Pl.Name(cur - 1)} gewinnt!"); Sfx.Play(S.Boom, .5f);
            int w = cur; Tm.After(1.7f, () => Advance(w));
        }
        else if (Full() || EarlyDraw())
        {
            over = true; wins[0]++; status = "UNENTSCHIEDEN - zählt nicht für die Runde"; Sfx.Play(S.Lose); Tm.After(1.5f, () => Advance(0));
        }
        else { cur = 3 - cur; status = $"{Pl.Name(cur - 1)} ist dran"; }
    }
    bool Full() => b.All(v => v != 0);
    bool EarlyDraw() => Lines.All(l => { var v = new[] { b[l[0]], b[l[1]], b[l[2]] }; return v.Contains(1) && v.Contains(2); });
    void Advance(int w)
    {
        if (w > 0) rw[w]++; dots[gameIdx] = w; gameIdx++;
        if (rw[1] >= 2 || rw[2] >= 2 || gameIdx >= 3) FinishRound(); else NewGame();
    }
    void FinishRound()
    {
        int w = rw[1] > rw[2] ? 1 : rw[2] > rw[1] ? 2 : 0; roundEnd = true;
        if (w > 0)
        {
            points[w] += 8; Sfx.Play(S.Big); Celebrate(w == 1 ? C.Cyan : C.Pink, 5, 1.1f); App.Shake(10);
            Action show = () => Result($"{Pl.Name(w - 1)} gewinnt die RUNDE!", $"Ergebnis {rw[1]}:{rw[2]}  -  +8 Punkte", w == 1 ? C.Cyan : C.Pink, ("Nächste Runde", C.Green, () => { roundEnd = false; NewRound(); }), ("Menü", C.Purple, () => App.Go(new Menu())));
            Tm.After(3f, () => { if (Save.IsHigh("hs_ttt", points[w])) NameEntry("hs_ttt", points[w], "NEUER HIGHSCORE!", show); else show(); });
        }
        else Result("Runde unentschieden", $"Ergebnis {rw[1]}:{rw[2]}", C.Gold, ("Nächste Runde", C.Green, () => { roundEnd = false; NewRound(); }), ("Menü", C.Purple, () => App.Go(new Menu())));
    }
    public override void Update(float dt)
    {
        for (int i = 0; i < 9; i++) if (b[i] != 0) pt[i] = Math.Min(1, pt[i] + dt * 3.2f);
        if (win != null) winT += dt;
    }
    public override void Draw(SKCanvas c)
    {
        W.PlayerBox(c, Gfx.R(40, 130, 260, 200), Pl.Name(0), points[1].ToString(), C.Cyan, !over && cur == 1, Time, $"Siege {wins[1]}");
        W.PlayerBox(c, Gfx.R(40, 360, 260, 200), Pl.Name(1), points[2].ToString(), C.Pink, !over && cur == 2, Time, $"Siege {wins[2]}");
        Gfx.Text(c, $"Unentschieden: {wins[0]}", 170, 600, 24, C.Dim, Al.C, false); Gfx.Text(c, "Rundenpunkte", 170, 640, 22, C.Dim, Al.C, false);
        HighscoreList(c, "hs_ttt", 1320, 150, 250, C.Cyan);
        for (int i = 0; i < 3; i++)
        {
            float x = 800 + (i - 1) * 60, y = 150; var col = dots[i] == 1 ? C.Cyan : dots[i] == 2 ? C.Pink : dots[i] == 0 ? C.Gold : C.Dim.A(.4f);
            c.DrawCircle(x, y, 16, Gfx.Fill(col.A(dots[i] >= 0 ? 1 : .25f))); c.DrawCircle(x, y, 16, Gfx.Line(col, 2.5f)); if (i == gameIdx && !roundEnd) Gfx.Glow(c, Gfx.Ctr(x, y, 32, 32), 16, C.White, 6, .5f + .4f * MathF.Sin(Time * 5));
        }
        var br = Gfx.R(BX - 14, BY - 14, CS * 3 + 28, CS * 3 + 28); Gfx.Glow(c, br, 28, C.Cyan, 20, .25f); W.Panel(c, br, C.Cyan, 28);
        var lp = Gfx.Line(C.Cyan.A(.85f), 6); var gl = Gfx.Line(C.Cyan.A(.5f), 12); gl.MaskFilter = Gfx.Blur(8);
        for (int k = 1; k < 3; k++)
        {
            c.DrawLine(BX + k * CS, BY + 14, BX + k * CS, BY + 3 * CS - 14, gl); c.DrawLine(BX + k * CS, BY + 14, BX + k * CS, BY + 3 * CS - 14, Gfx.Line(C.Cyan.A(.85f), 6));
            c.DrawLine(BX + 14, BY + k * CS, BX + 3 * CS - 14, BY + k * CS, gl); c.DrawLine(BX + 14, BY + k * CS, BX + 3 * CS - 14, BY + k * CS, Gfx.Line(C.Cyan.A(.85f), 6));
        }
        for (int i = 0; i < 9; i++)
        {
            var r = Cell(i); if (i == hover) { Gfx.Rect(c, Gfx.Inflate(r, -12), 16, (cur == 1 ? C.Cyan : C.Pink).A(.12f)); DrawMark(c, cur, r.MidX, r.MidY, 1, .25f); }
            if (b[i] != 0) DrawMark(c, b[i], r.MidX, r.MidY, Ease.OutCubic(pt[i]), 1, win != null && win.Contains(i) ? .5f + .5f * MathF.Sin(winT * 10) : 0);
        }
        if (win != null)
        {
            var a = Cell(win[0]); var z = Cell(win[2]); float p = Ease.OutCubic(winT / .5f); var col = (b[win[0]] == 1 ? C.Cyan : C.Pink);
            var e = new SKPoint(a.MidX + (z.MidX - a.MidX) * p, a.MidY + (z.MidY - a.MidY) * p); var g = Gfx.Line(col, 26); g.MaskFilter = Gfx.Blur(14); c.DrawLine(a.MidX, a.MidY, e.X, e.Y, g); c.DrawLine(a.MidX, a.MidY, e.X, e.Y, Gfx.Line(SKColors.White, 7));
        }
        Gfx.Text(c, status, 800, 838, 34, over && win != null ? (b[win[0]] == 1 ? C.Cyan : C.Pink) : SKColors.White, Al.C, true, 8);
    }
    static void DrawMark(SKCanvas c, int who, float cx, float cy, float p, float alpha = 1, float glow = 0)
    {
        var col = who == 1 ? C.Cyan : C.Pink; float s = 52;
        c.Save(); c.Translate(cx, cy); float sc = 1 + glow * .08f; c.Scale(sc, sc);
        if (who == 1)
        {
            float p1 = Ease.Clamp(p * 2), p2 = Ease.Clamp(p * 2 - 1);
            foreach (var pass in new[] { 0, 1 })
            {
                var pa = pass == 0 ? Gfx.Line(col.A(alpha * .9f), 24) : Gfx.Line(SKColors.White.A(alpha), 9); if (pass == 0) pa.MaskFilter = Gfx.Blur(10);
                c.DrawLine(-s, -s, -s + 2 * s * p1, -s + 2 * s * p1, pa);
                if (p2 > 0) { var pb = pass == 0 ? Gfx.Line(col.A(alpha * .9f), 24) : Gfx.Line(SKColors.White.A(alpha), 9); if (pass == 0) pb.MaskFilter = Gfx.Blur(10); c.DrawLine(s, -s, s - 2 * s * p2, -s + 2 * s * p2, pb); }
            }
        }
        else
        {
            foreach (var pass in new[] { 0, 1 })
            {
                var pa = pass == 0 ? Gfx.Line(col.A(alpha * .9f), 24) : Gfx.Line(SKColors.White.A(alpha), 9); if (pass == 0) pa.MaskFilter = Gfx.Blur(10);
                using var path = new SKPath(); path.AddArc(new SKRect(-s, -s, s, s), -90, 360 * Math.Max(.001f, p)); c.DrawPath(path, pa);
            }
        }
        c.Restore();
    }
}
