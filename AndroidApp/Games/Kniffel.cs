using SkiaSharp;
using Silk.NET.Input;
namespace GlamourGames;
class Kniffel : Scene
{

    public override string Title => "Kniffel";
    public override SKColor Acc1 => C.Red; public override SKColor Acc2 => C.Orange;
    static readonly (string key, string label, bool upper)[] Cats = {
        ("1", "Einsen", true), ("2", "Zweien", true), ("3", "Dreien", true), ("4", "Vieren", true), ("5", "Fünfen", true), ("6", "Sechsen", true), ("bonus", "Bonus (63: +35)", true),
        ("3k", "3er Pasch", false), ("4k", "4er Pasch", false), ("fh", "Full House", false), ("ss", "Kleine Straße", false), ("ls", "Große Straße", false), ("yz", "Kniffel", false), ("ch", "Chance", false) };
    class Die { public int V = 1; public bool Held; public float T = 1, Dur = 1, Spins, Rz, Bounce, Jit; public float[] Axis = { 1, 0, 0 }; public Spring Lift = new(0) { K = 300, D = 22 }; }
    Die[] dice = Enumerable.Range(0, 5).Select(_ => new Die()).ToArray();
    Dictionary<string, int>[] card = { new(), new() }; int cur, rolls; bool over, rolling; Button roll, newBtn; DiceParade parade; bool resShown; int hoverRow = -1, hoverDie = -1;
    const float TX = 850, TY = 78, RH = 50, LW = 290, CW = 210;
    static readonly float[] Tilt = DieMul();
    static float[] DieMul() => Die3D.Mul(Die3D.RX(.5f), Die3D.RY(-.42f));
    public override void Enter()
    {
        base.Enter(); roll = Ui.Add(new Button(200, 660, 460, 96, "WÜRFELN", C.Red, Roll, 42)); newBtn = Ui.Add(new Button(40, 780, 260, 62, "Neues Spiel", C.Purple, NewMatch, 24)); NewMatch();
    }
    public override void DebugWin() { Modal = null; parade = new DiceParade(this, 0, new[] { C.Cyan, C.Pink }, new[] { "Spieler 1", "Spieler 2" }, new[] { 200, 150 }); }
    void NewMatch() { NewGame(); CoinToss.Start(this, f => cur = f); }
    void NewGame() { selRow = -1; card = new Dictionary<string, int>[] { new(), new() }; cur = 0; rolls = 0; over = false; Modal = null; parade = null; resShown = false; Fx.Clear(); foreach (var d in dice) { d.Held = false; d.V = 1; d.T = 1; } }
    int Calc(string k, int[] v)
    {
        // Defensive: ensure values are in 1..6 and work with distinct sorted values
        var cnt = new int[7]; int sum = 0; var vals = new List<int>();
        foreach (var x in v)
        {
            if (x >= 1 && x <= 6)
            {
                cnt[x]++;
                sum += x;
                vals.Add(x);
            }
            else
            {
                // ignore invalid die values
            }
        }
        var u = vals.Distinct().OrderBy(x => x).ToList();
        bool seq(int len)
        {
            if (u.Count < len) return false;
            for (int i = 0; i <= u.Count - len; i++)
            {
                bool ok = true;
                for (int j = 1; j < len; j++) if (u[i + j] - u[i + j - 1] != 1) { ok = false; break; }
                if (ok) return true;
            }
            return false;
        }
        return k switch
        {
            "1" or "2" or "3" or "4" or "5" or "6" => cnt[int.Parse(k)] * int.Parse(k),
            "3k" => cnt.Any(x => x >= 3) ? sum : 0,
            "4k" => cnt.Any(x => x >= 4) ? sum : 0,
            // Full House: strictly 3 of one number and 2 of another (5 of a kind is Kniffel, NOT Full House!)
            "fh" => (cnt.Any(x => x == 3) && cnt.Any(x => x == 2)) ? 25 : 0,
            "ss" => seq(4) ? 30 : 0, "ls" => seq(5) ? 40 : 0, "yz" => cnt.Any(x => x == 5) ? 50 : 0, "ch" => sum, _ => 0
        };
    }
    int selRow = -1;
    int Total(int p) => card[p].Values.Sum();
    int UpperSum(int p) => Cats.Where(c => c.upper && c.key != "bonus" && card[p].ContainsKey(c.key)).Sum(c => card[p][c.key]);
    void Roll()
    {
        if (selRow >= 0 && !over && !rolling) { int r0 = selRow; selRow = -1; Score(r0); return; }
        if (rolls >= 3 || over || rolling) return;
        selRow = -1; rolls++; rolling = true; Sfx.Play(S.Dice);
        for (int i = 0; i < 5; i++)
        {
            var d = dice[i]; if (d.Held) continue;
            d.V = Random.Shared.Next(1, 7); d.T = 0; d.Dur = .9f + i * .09f; d.Spins = 2 + Random.Shared.Next(3); d.Rz = Random.Shared.Next(4) * MathF.PI / 2 + (Random.Shared.NextSingle() - .5f) * .3f; d.Bounce = 90 + Random.Shared.Next(60); d.Jit = (Random.Shared.NextSingle() - .5f) * 30;
            var a = new[] { Random.Shared.NextSingle() - .5f, Random.Shared.NextSingle() - .5f, Random.Shared.NextSingle() - .5f + .2f }; d.Axis = a;
        }
    }
    public override bool WantsHand => hoverRow >= 0 || hoverDie >= 0;
    static SKRect DieRect(int i) => Gfx.Ctr(130 + i * 145, 400, 128, 128);
    static SKRect RowRect(int r) => Gfx.R(TX, TY + 48 + r * RH, LW + 2 * CW, RH);
    public override void MouseMove(float x, float y)
    {
        hoverRow = -1; hoverDie = -1; if (over || Modal != null) return;
        for (int i = 0; i < 5; i++) if (Gfx.Inflate(DieRect(i), 10).Contains(x, y)) hoverDie = i;
        if (rolls > 0 && !rolling) for (int r = 0; r < Cats.Length; r++) if (Cats[r].key != "bonus" && !card[cur].ContainsKey(Cats[r].key) && RowRect(r).Contains(x, y)) hoverRow = r;
    }
    public override void MouseUp(float x, float y)
    {
        if (parade != null && parade.T > 2 && !resShown) { ShowResult(); return; }
        if (hoverDie >= 0 && rolls > 0 && !rolling) { dice[hoverDie].Held = !dice[hoverDie].Held; Sfx.Play(S.Take, .6f); }
        else if (hoverRow >= 0) { selRow = selRow == hoverRow ? -1 : hoverRow; Sfx.Play(S.Take, .6f); }
    }
    public override void KeyDown(Key k)
    {
        if (k == Key.Space || k == Key.Enter) Roll();
        else if (k >= Key.Number1 && k <= Key.Number5 && rolls > 0 && !rolling) { var d = dice[k - Key.Number1]; d.Held = !d.Held; Sfx.Play(S.Take, .6f); }
    }
    void Score(int r)
    {
        var key = Cats[r].key; int v = Calc(key, dice.Select(d => d.V).ToArray()); card[cur][key] = v; Sfx.Play(v > 0 ? S.Match : S.NoMatch);
        var rr = RowRect(r); float cx = TX + LW + CW * cur + CW / 2; if (v > 0) { Fx.Burst(cx, rr.MidY, 24, null, 300); Pop("+" + v, cx, rr.MidY, C.Gold, 40); } if (key == "yz" && v == 50) { Celebrate(C.Gold, 6, 1.4f); for (int i = 0; i < 5; i++) { var dr = DieRect(i); int kk = i; Tm.After(kk * .18f, () => { Fx.Lightning(dr.MidX + 40, -20, dr.MidX, dr.MidY, C.Cyan); Fx.Explosion(dr.MidX, dr.MidY, .8f); Sfx.Play(S.Boom, .5f, 1.1f); }); } Pop("KNIFFEL!!!", 430, 330, C.Gold, 96); App.Shake(20); }
        else if (v > 0 && (key == "ls" || key == "ss" || key == "fh" || key == "4k")) { Fx.Lightning(cx, -20, cx, rr.MidY, C.Gold); Fx.Shockwave(cx, rr.MidY, C.Gold, 200, .6f); Sfx.Play(S.Sparkle); Fx.Petals(cx, rr.MidY, 18, new[] { C.Pink, C.Gold, C.Cyan }, 320); Pop(key == "ls" ? "GROSSE STRASSE!" : key == "ss" ? "KLEINE STRASSE!" : key == "fh" ? "FULL HOUSE!" : "VIERLING!", 430, 330, C.Gold, 56); App.Shake(8); }
        else if (v == 0) { Fx.Smoke(cx, rr.MidY, 6, 12, 40, 1.4f); Pop("0", cx, rr.MidY, C.Dim, 40); }
        if (UpperSum(cur) >= 63 && !card[cur].ContainsKey("bonus")) { card[cur]["bonus"] = 35; Pop("BONUS +35", 1200, 130, C.Green, 46); Sfx.Play(S.Win); }
        else if (Cats.Where(c => c.upper && c.key != "bonus").All(c => card[cur].ContainsKey(c.key)) && !card[cur].ContainsKey("bonus")) { card[cur]["bonus"] = 0; }
        if (card[0].Count(kv => kv.Key != "bonus") == 13 && card[1].Count(kv => kv.Key != "bonus") == 13) { End(); return; }
        cur = 1 - cur; rolls = 0; foreach (var d in dice) { d.Held = false; d.T = 1; d.V = 1; }
    }
    void End()
    {
        over = true; int a = Total(0), b = Total(1); int w = a > b ? 0 : b > a ? 1 : -1; Sfx.Play(S.Win);
        parade = new DiceParade(this, w, new[] { C.Cyan, C.Pink }, new[] { Pl.Name(0), Pl.Name(1) }, new[] { a, b }); resWin = w; resA = a; resB = b;
        Tm.After(13.2f, ShowResult);
    }
    int resWin, resA, resB;
    void ShowResult()
    {
        if (resShown) return; resShown = true; int w = resWin;
        Result(w < 0 ? "UNENTSCHIEDEN" : $"{Pl.Name(w)} gewinnt!", $"{resA}  :  {resB}", w == 0 ? C.Cyan : w == 1 ? C.Pink : C.Gold, ("Nochmal", C.Green, NewMatch), ("Menü", C.Purple, () => App.Go(new Menu())));
    }
    public override void Update(float dt)
    {
        parade?.Update(dt); newBtn.Visible = parade == null; rolling = false;
        for (int i = 0; i < 5; i++)
        {
            var d = dice[i]; if (d.T < d.Dur) { d.T += dt; rolling = true; if (d.T >= d.Dur) { d.T = d.Dur; var r = DieRect(i); Fx.Spark(r.MidX, r.Bottom, C.Orange, 10, 160); Fx.Smoke(r.MidX, r.Bottom - 10, 2, 8, 20, .8f); Sfx.Play(S.Stop, .35f, 1.2f); } }
            d.Lift.Target = d.Held ? -26 : 0; d.Lift.Update(dt);
        }
        roll.Visible = parade == null; roll.Enabled = (rolls < 3 || selRow >= 0) && !over && !rolling; roll.Size = selRow >= 0 ? 30 : 42; roll.Col = selRow >= 0 ? C.Green : C.Red; roll.Text = selRow >= 0 ? $"EINTRAGEN: {Cats[selRow].label} (+{Calc(Cats[selRow].key, dice.Select(d => d.V).ToArray())})" : rolls == 0 ? "WÜRFELN" : rolls < 3 ? $"NOCHMAL ({3 - rolls})" : "Kategorie wählen";
    }
    public override void Draw(SKCanvas c)
    {
        W.PlayerBox(c, Gfx.R(40, 130, 260, 120), Pl.Name(0), Total(0).ToString(), C.Cyan, cur == 0 && !over, Time);
        W.PlayerBox(c, Gfx.R(320, 130, 260, 120), Pl.Name(1), Total(1).ToString(), C.Pink, cur == 1 && !over, Time);
        Gfx.Text(c, $"Runde {Math.Min(13, card[cur].Count(k => k.Key != "bonus") + 1)} / 13", 610, 190, 26, C.Dim, Al.L, false);
        for (int i = 0; i < 3; i++) { float x = 610 + Gfx.TW("Würfe übrig", 20, false) + 18 + i * 26; c.DrawCircle(x, 235, 10, Gfx.Fill(i < 3 - rolls ? C.Gold : C.Dim.A(.25f))); if (i < 3 - rolls) Gfx.Radial(c, x, 235, 22, C.Gold, .5f); }
        Gfx.Text(c, "Würfe übrig", 610, 235, 20, C.Dim, Al.L, false);
        var tray = Gfx.R(40, 270, 780, 260); Gfx.RectGrad(c, tray, 30, new SKColor(14, 70, 46), new SKColor(6, 30, 22)); Gfx.Stroke(c, tray, 30, new SKColor(120, 70, 30), 8); Gfx.Stroke(c, Gfx.Inflate(tray, -8), 24, C.Green.A(.3f), 2);
        Gfx.Text(c, selRow >= 0 ? "Zeile antippen = abwählen, grüner Knopf = eintragen" : "Tippe auf Würfel, um sie zu halten", 430, 560, 22, C.Dim, Al.C, false);
        for (int i = 0; i < 5; i++)
        {
            var d = dice[i]; var r = DieRect(i); float t = Ease.Clamp(d.T / d.Dur), e = Ease.OutCubic(t);
            var fc = Die3D.Mul(Die3D.RZ(d.Rz), Die3D.Face(d.V));
            var fin = Die3D.Mul(Tilt, fc);
            float[] m = d.T >= d.Dur ? fin : Die3D.Mul(Tilt, Die3D.Mul(Die3D.RAxis(d.Axis[0], d.Axis[1], d.Axis[2], d.Spins * MathF.Tau * (1 - e)), fc));
            float by = d.T >= d.Dur ? 0 : MathF.Abs(MathF.Sin(t * MathF.PI * 3)) * d.Bounce * (1 - t) * (1 - t), jx = d.T >= d.Dur ? 0 : d.Jit * (1 - e);
            float sz = 112 * (1 + (d.Held ? .04f : 0));
            if (d.Held) { Gfx.Radial(c, r.MidX, r.MidY + d.Lift.V, 110, C.Green, .35f); }
            Die3D.Draw(c, r.MidX + jx, r.MidY - by + d.Lift.V, sz, m, d.Held ? new SKColor(255, 250, 230) : new SKColor(246, 236, 214), new SKColor(30, 18, 30), false);
            if (d.Held) Gfx.Text(c, "GEHALTEN", r.MidX, r.Bottom + 34, 20, C.Green, Al.C, true, 6); else if (rolls > 0) Gfx.Text(c, "halten", r.MidX, r.Bottom + 34, 18, C.Dim.A(hoverDie == i ? 1 : .5f), Al.C, false);
            if (i == hoverDie && rolls > 0) Gfx.Glow(c, Gfx.Inflate(r, -6), 20, C.Green, 10, .6f);
        }
        DrawTable(c); parade?.Draw(c);
    }
    void DrawTable(SKCanvas c)
    {
        var frame = Gfx.R(TX - 12, TY - 10, LW + 2 * CW + 24, 48 + Cats.Length * RH + 60); W.Panel(c, frame, C.Red);
        for (int p = 0; p < 2; p++) { var hr = Gfx.R(TX + LW + p * CW + 6, TY, CW - 12, 40); var col = p == 0 ? C.Cyan : C.Pink; Gfx.Rect(c, hr, 12, col.A(cur == p && !over ? .35f : .12f)); if (cur == p && !over) Gfx.Glow(c, hr, 12, col, 8, .6f); Gfx.Text(c, Pl.Name(p), hr.MidX, hr.MidY, 24, col.Light(.4f)); }
        int[] cand = new int[Cats.Length]; bool can = rolls > 0 && !over;
        for (int r = 0; r < Cats.Length; r++)
        {
            var rr = RowRect(r); var (key, label, upper) = Cats[r]; bool hv = hoverRow == r || selRow == r;
            if (r % 2 == 0) Gfx.Rect(c, Gfx.Inflate(rr, -2), 8, SKColors.White.A(.035f));
            if (hv) { Gfx.Rect(c, Gfx.Inflate(rr, -2), 8, C.Gold.A(selRow == r ? .32f : .16f)); Gfx.Stroke(c, Gfx.Inflate(rr, -2), 8, C.Gold, selRow == r ? 4 : 2); }
            if (r == 6 || r == 7) c.DrawLine(rr.Left + 10, rr.Top, rr.Right - 10, rr.Top, Gfx.Line(C.Red.A(.5f), 2));
            Gfx.Text(c, label, rr.Left + 16, rr.MidY, 26, upper ? SKColors.White : C.Orange.Light(.5f), Al.L, false);
            if (key == "bonus") Gfx.Text(c, $"{UpperSum(cur)}/63", rr.Left + LW - 16, rr.MidY, 18, C.Dim, Al.R, false);
            for (int p = 0; p < 2; p++)
            {
                float cx = TX + LW + p * CW + CW / 2;
                if (card[p].TryGetValue(key, out var v)) Gfx.Text(c, v.ToString(), cx, rr.MidY, 30, v == 0 ? C.Dim : SKColors.White, Al.C, true, v > 0 ? 3 : 0);
                else if (key == "bonus")
                {
                    int us = UpperSum(p);
                    Gfx.Text(c, $"{us}/63", cx, rr.MidY, 20, us >= 63 ? C.Green : C.Dim.A(.75f), Al.C, false);
                }
                else if (can && p == cur && key != "bonus") { int pv = Calc(key, dice.Select(d => d.V).ToArray()); float pu = .5f + .5f * MathF.Sin(Time * 5 + r); Gfx.Text(c, pv.ToString(), cx, rr.MidY, 30, (pv > 0 ? C.Gold : C.Dim).A(hv ? 1 : .55f + .3f * pu), Al.C, true, hv ? 8 : 0); }
            }
        }
        float sy = TY + 48 + Cats.Length * RH + 30; c.DrawLine(TX, sy - 24, TX + LW + 2 * CW, sy - 24, Gfx.Line(C.Gold.A(.6f), 2));
        Gfx.Text(c, "SUMME", TX + 16, sy, 28, C.Gold, Al.L, true, 6); for (int p = 0; p < 2; p++) Gfx.Text(c, Total(p).ToString(), TX + LW + p * CW + CW / 2, sy, 34, p == 0 ? C.Cyan : C.Pink, Al.C, true, 8);

        // debug overlay removed
    }
}
