using SkiaSharp;
using Silk.NET.Input;
namespace GlamourGames;
class ConnectFour : Scene
{
    public override string Title => "Vier Gewinnt";
    public override SKColor Acc1 => C.Blue; public override SKColor Acc2 => C.Magenta;
    const int Rows = 6, Cols = 7; const float CS = 104, BX = 800 - CS * 3.5f, BY = 168;
    class Disc { public int Col, Row, P; public float Y, Vy; public bool Settled, Landed_; public float Pulse; }
    int[] b = new int[42]; readonly List<Disc> discs = new(); int cur = 1, over; int[] score = new int[2]; List<int> winCells; float winT; bool busy; int hoverCol = -1; string status = "";
    static float CX(int c) => BX + c * CS + CS / 2; static float CY(int r) => BY + r * CS + CS / 2;
    public override void Enter() { base.Enter(); Ui.Add(new Button(40, 700, 260, 62, "Neue Runde", C.Green, NextRound, 24)); Ui.Add(new Button(40, 780, 260, 62, "Punkte zurück", C.Purple, NewMatch, 22)); NewMatch(); }
    int starter = 1;
    void NextRound() { starter = 3 - starter; Reset(); }
    void NewMatch() { score = new int[2]; starter = 1; Reset(); CoinToss.Start(this, f => { starter = f + 1; Reset(); }); }
    void Reset() { b = new int[42]; discs.Clear(); cur = starter; over = 0; winCells = null; busy = false; winT = 0; status = $"{Pl.Name(cur - 1)} ist dran"; Modal = null; parade = null; resShown = false; }
    public override bool WantsHand => hoverCol >= 0;
    public override void MouseMove(float x, float y) { hoverCol = -1; if (over != 0 || busy || Modal != null) return; if (x > BX && x < BX + Cols * CS && y > BY - 100 && y < BY + Rows * CS + 30) hoverCol = (int)((x - BX) / CS); }
    public override void MouseUp(float x, float y) { if (parade != null && parade.T > 1.5f) { FireRes(); return; } if (hoverCol >= 0) Drop(hoverCol); }
    public override void KeyDown(Key k)
    {
        if (k >= Key.Number1 && k <= Key.Number7) Drop(k - Key.Number1);
        else if (k >= Key.Keypad1 && k <= Key.Keypad7) Drop(k - Key.Keypad1);
    }
    void Drop(int col)
    {
        if (over != 0 || busy) return; int row = -1; for (int r = Rows - 1; r >= 0; r--) if (b[r * Cols + col] == 0) { row = r; break; }
        if (row < 0) { Sfx.Play(S.NoMatch, .5f); App.Shake(4); return; }
        b[row * Cols + col] = cur; busy = true; discs.Add(new Disc { Col = col, Row = row, P = cur, Y = BY - CS, Vy = 0 }); Sfx.Play(S.Turn, .4f);
    }
    void Landed(Disc d)
    {
        Sfx.Play(S.Drop); Fx.Spark(CX(d.Col), CY(d.Row) + CS / 2 - 8, d.P == 1 ? C.Cyan : C.Pink, 10, 180);
        var w = Check(d.Row, d.Col, d.P);
        if (w != null)
        {
            over = d.P; winCells = w; score[d.P - 1]++; status = $"{Pl.Name(d.P - 1)} GEWINNT!"; Sfx.Play(S.Win); App.Flash(d.P == 1 ? C.Cyan : C.Pink, .3f); App.Shake(9);
            foreach (var i in w) Fx.Burst(CX(i % Cols), CY(i / Cols), 30, new[] { d.P == 1 ? C.Cyan : C.Pink, C.White, C.Gold }, 460);
foreach (var i in w) { int ii = i; Tm.After(.15f * (Array.IndexOf(w.ToArray(), ii) + 1), () => { Fx.Explosion(CX(ii % Cols), CY(ii / Cols), .6f); Sfx.Play(S.Boom, .3f, 1.3f); }); }
            int wp = d.P; Tm.After(2.2f, () => { if (over != wp) return; parade = new DiceParade(this, wp - 1, new[] { C.Cyan, C.Pink }, new[] { Pl.Name(0), Pl.Name(1) }, new[] { score[0], score[1] }, PKind.Disc, 1.35f); showRes = () => Result($"{Pl.Name(wp - 1)} gewinnt!", $"Stand: {score[0]} : {score[1]}", wp == 1 ? C.Cyan : C.Pink, ("Nächste Runde", C.Green, NextRound), ("Menü", C.Purple, () => App.Go(new Menu()))); });
        }
        else if (b.All(v => v != 0)) { over = 3; status = "UNENTSCHIEDEN!"; Sfx.Play(S.Lose); Tm.After(1.2f, () => Result("Unentschieden", $"Stand: {score[0]} : {score[1]}", C.Gold, ("Nochmal", C.Green, NextRound), ("Menü", C.Purple, () => App.Go(new Menu())))); }
        else { cur = 3 - cur; status = $"{Pl.Name(cur - 1)} ist dran"; }
        busy = false;
    }
    List<int> Check(int row, int col, int p)
    {
        foreach (var (dr, dc) in new[] { (0, 1), (1, 0), (1, 1), (1, -1) })
        {
            var cells = new List<int> { row * Cols + col };
            for (int s = -1; s <= 1; s += 2) for (int d = 1; d < 4; d++) { int r = row + dr * d * s, c = col + dc * d * s; if (r < 0 || r >= Rows || c < 0 || c >= Cols || b[r * Cols + c] != p) break; cells.Add(r * Cols + c); }
            if (cells.Count >= 4) return cells;
        }
        return null;
    }
    DiceParade parade; bool resShown; Action showRes;
    public override void DebugWin() { Modal = null; parade = new DiceParade(this, 0, new[] { C.Cyan, C.Pink }, new[] { "Spieler 1", "Spieler 2" }, new[] { 200, 150 }, PKind.Disc, 1.35f); }
    void FireRes() { if (resShown) return; resShown = true; showRes?.Invoke(); }
    public override void Update(float dt)
    {
        parade?.Update(dt); if (parade != null && parade.T > parade.Total) FireRes();
        foreach (var d in discs)
        {
            if (d.Settled) { d.Pulse += dt; continue; }
            d.Vy += 3200 * dt; d.Y += d.Vy * dt; float ty = CY(d.Row);
            if (d.Y >= ty) { d.Y = ty; if (d.Vy > 260) { d.Vy = -d.Vy * .32f; if (!d.Landed_) { d.Landed_ = true; Landed(d); } } else { d.Vy = 0; d.Settled = true; } }
        }
        if (winCells != null) winT += dt;
    }
    public override void Draw(SKCanvas c) { DrawBoard(c); parade?.Draw(c); }
    void DrawBoard(SKCanvas c)
    {
        W.PlayerBox(c, Gfx.R(40, 140, 260, 210), Pl.Name(0), score[0].ToString(), C.Cyan, over == 0 && cur == 1, Time, "Siege");
        W.PlayerBox(c, Gfx.R(40, 380, 260, 210), Pl.Name(1), score[1].ToString(), C.Pink, over == 0 && cur == 2, Time, "Siege");
        Gfx.Text(c, "Tippe eine Spalte an", 170, 640, 20, C.Dim, Al.C, false);
        if (hoverCol >= 0) { var col = cur == 1 ? C.Cyan : C.Pink; Gfx.Rect(c, Gfx.R(BX + hoverCol * CS + 4, BY, CS - 8, Rows * CS), 14, col.A(.10f)); Gfx.Ball(c, CX(hoverCol), BY - CS * .55f + MathF.Sin(Time * 6) * 4, CS * .4f, col); }
        c.Save(); foreach (var d in discs) DrawDisc(c, d); c.Restore();
        var frame = Gfx.R(BX - 16, BY - 6, Cols * CS + 32, Rows * CS + 30);
        Gfx.Glow(c, frame, 26, C.Blue, 22, .5f);
        using var path = new SKPath { FillType = SKPathFillType.EvenOdd }; path.AddRoundRect(new SKRoundRect(frame, 26));
        for (int r = 0; r < Rows; r++) for (int cc = 0; cc < Cols; cc++) path.AddCircle(CX(cc), CY(r), CS * .4f);
        using var sh = SKShader.CreateLinearGradient(new(frame.Left, frame.Top), new(frame.Right, frame.Bottom), new[] { new SKColor(50, 90, 255), new SKColor(20, 40, 170), new SKColor(10, 20, 100) }, null, SKShaderTileMode.Clamp);
        var p = Gfx.Fill(SKColors.White); p.Shader = sh; c.DrawPath(path, p);
        for (int r = 0; r < Rows; r++) for (int cc = 0; cc < Cols; cc++) { c.DrawCircle(CX(cc), CY(r), CS * .4f, Gfx.Line(new SKColor(120, 170, 255, 120), 3)); c.DrawCircle(CX(cc), CY(r) + 2, CS * .41f, Gfx.Line(new SKColor(0, 0, 40, 130), 3)); }
        Gfx.Stroke(c, frame, 26, new SKColor(140, 180, 255), 3);
        if (winCells != null)
        {
            foreach (var i in winCells) { float k = .5f + .5f * MathF.Sin(winT * 9); var col = over == 1 ? C.Cyan : C.Pink; c.DrawCircle(CX(i % Cols), CY(i / Cols), CS * .43f, Gfx.Line(SKColors.White.A(.6f + .4f * k), 6)); Gfx.Radial(c, CX(i % Cols), CY(i / Cols), CS * .8f, col, .5f * k); }
        }
        Gfx.Text(c, status, 800, 122, 38, over == 1 ? C.Cyan : over == 2 ? C.Pink : over == 3 ? C.Gold : SKColors.White, Al.C, true, 8);
    }
    static void DrawDisc(SKCanvas c, Disc d)
    {
        var col = d.P == 1 ? C.Cyan : C.Pink; float r = CS * .4f, x = CX(d.Col);
        Gfx.Radial(c, x, d.Y, r * 1.8f, col, .35f);
        Gfx.Ball(c, x, d.Y, r, col); c.DrawCircle(x, d.Y, r * .68f, Gfx.Line(col.Light(.4f).A(.55f), 3)); c.DrawCircle(x, d.Y, r, Gfx.Line(col.Light(.5f), 2));
    }
}
