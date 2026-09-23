using SkiaSharp;
namespace GlamourGames;
class Nim : Scene
{
    public override string Title => "Nim";
    public override SKColor Acc1 => C.Orange; public override SKColor Acc2 => C.Gold;
    int[] piles = { 3, 5, 7 }; int cur = 1; int[] score = new int[2]; bool over; int hp = -1, hs = -1; readonly float[][] pop = new float[3][];
    class Fly { public float X, Y, Vx, Vy, Rot, Vr, T; public SKColor Col; }
    readonly List<Fly> fly = new(); string status = "";
    const float PX0 = 470, PDX = 330, BASE = 770, RW = 220, RHt = 52, GAP = 10;
    static float PX(int p) => PX0 + p * PDX;
    static SKRect Rod(int p, int k) => Gfx.Ctr(PX(p), BASE - RHt / 2 - k * (RHt + GAP), RW, RHt);
    public override void Enter() { base.Enter(); Ui.Add(new Button(40, 700, 260, 62, "Neue Runde", C.Green, NextRound, 24)); Ui.Add(new Button(40, 780, 260, 62, "Punkte zurück", C.Purple, NewMatch, 22)); NewMatch(); }
    int starter = 1;
    void NextRound() { starter = 3 - starter; Reset(); }
    void NewMatch() { score = new int[2]; starter = 1; Reset(); CoinToss.Start(this, f => { starter = f + 1; Reset(); }); }
    void Reset() { piles = new[] { 3, 5, 7 }; cur = starter; over = false; fly.Clear(); status = $"{Pl.Name(cur - 1)} ist dran"; Modal = null; for (int i = 0; i < 3; i++) pop[i] = Enumerable.Range(0, 7).Select(k => -k * .08f - i * .1f).ToArray(); }
    public override bool WantsHand => hp >= 0;
    public override void MouseMove(float x, float y)
    {
        hp = hs = -1; if (over || Modal != null) return;
        for (int p = 0; p < 3; p++) for (int k = 0; k < piles[p]; k++) if (Gfx.Inflate(Rod(p, k), 6).Contains(x, y)) { hp = p; hs = k; }
    }
    public override void MouseUp(float x, float y)
    {
        if (hp < 0) return; int p = hp, k = hs, n = piles[p] - k;
        for (int j = k; j < piles[p]; j++) { var r = Rod(p, j); fly.Add(new Fly { X = r.MidX, Y = r.MidY, Vx = (Random.Shared.NextSingle() - .5f) * 500, Vy = -300 - Random.Shared.NextSingle() * 300, Vr = (Random.Shared.NextSingle() - .5f) * 8, Col = C.Gold }); Fx.Burst(r.MidX, r.MidY, 12, new[] { C.Gold, C.Orange, C.Yellow }, 260); }
        piles[p] -= n; Sfx.Play(S.Take); App.Shake(4 + n); hp = hs = -1;
        if (piles.All(v => v == 0)) { over = true; score[cur - 1]++; status = $"{Pl.Name(cur - 1)} GEWINNT!"; Sfx.Play(S.Win); Celebrate(cur == 1 ? C.Cyan : C.Pink, 4, 1f, $"{Pl.Name(cur - 1)} gewinnt!"); App.Flash(C.Gold, .3f); int w = cur; Tm.After(3.4f, () => Result($"{Pl.Name(w - 1)} gewinnt!", $"Wer den letzten Stab nimmt, gewinnt.  Stand {score[0]} : {score[1]}", w == 1 ? C.Cyan : C.Pink, ("Nochmal", C.Green, NextRound), ("Menü", C.Purple, () => App.Go(new Menu())))); }
        else { cur = 3 - cur; status = $"{Pl.Name(cur - 1)} ist dran"; }
    }
    public override void Update(float dt)
    {
        for (int p = 0; p < 3; p++) for (int k = 0; k < 7; k++) pop[p][k] = Math.Min(1, pop[p][k] + dt * 3);
        for (int i = fly.Count - 1; i >= 0; i--) { var f = fly[i]; f.T += dt; f.Vy += 1800 * dt; f.X += f.Vx * dt; f.Y += f.Vy * dt; f.Rot += f.Vr * dt; if (f.T > 1.1f) fly.RemoveAt(i); }
    }
    public override void Draw(SKCanvas c)
    {
        W.PlayerBox(c, Gfx.R(40, 140, 260, 200), Pl.Name(0), score[0].ToString(), C.Cyan, !over && cur == 1, Time, "Siege");
        W.PlayerBox(c, Gfx.R(40, 370, 260, 200), Pl.Name(1), score[1].ToString(), C.Pink, !over && cur == 2, Time, "Siege");
        Gfx.Text(c, "Tippe einen Stab: er und alle darüber werden genommen.", 800, 120, 26, C.Dim, Al.C, false);
        for (int p = 0; p < 3; p++)
        {
            float x = PX(p); var plate = Gfx.Ctr(x, BASE + 30, RW + 60, 30); Gfx.GlowFill(c, plate, 14, C.Orange, 14, .3f); Gfx.RectGrad(c, plate, 12, new SKColor(80, 50, 20), new SKColor(30, 16, 8)); Gfx.Stroke(c, plate, 12, C.Orange.A(.7f), 2);
            Gfx.Text(c, $"Stapel {p + 1}", x, BASE + 80, 26, C.Dim, Al.C, true); Gfx.Text(c, piles[p].ToString(), x, BASE + 30, 22, C.Gold, Al.C, true, 4);
            for (int k = 0; k < piles[p]; k++)
            {
                float a = Ease.OutBack(pop[p][k]); if (pop[p][k] <= 0) continue; bool hl = hp == p && k >= hs; var r = Rod(p, k);
                c.Save(); c.Translate(r.MidX, r.MidY); c.Scale(a, a); c.Translate(-r.MidX, -r.MidY);
                var col = hl ? C.Red.Mix(C.Orange, .4f) : C.Gold; Gfx.Glow(c, r, 22, col, hl ? 16 : 8, hl ? .9f : .35f);
                Gfx.RectGrad(c, r, 22, col.Light(.45f), col.Dark(.55f)); Gfx.RectGrad(c, Gfx.R(r.Left + 8, r.Top + 4, r.Width - 16, r.Height * .35f), 10, SKColors.White.A(.5f), SKColors.White.A(0)); Gfx.Stroke(c, r, 22, col.Light(.6f), 2);
                for (int q = 0; q < 3; q++) c.DrawCircle(r.Left + 34 + q * 8, r.MidY, 2, Gfx.Fill(col.Dark(.4f)));
                c.Restore();
            }
            if (hp == p) { var r0 = Rod(p, hs); Gfx.Text(c, $"Nehme {piles[p] - hs}", x + RW / 2 + 70, r0.MidY, 26, C.Red.Light(.4f), Al.L, true, 8); }
        }
        foreach (var f in fly) { float a = 1 - Ease.Clamp(f.T / 1.1f); c.Save(); c.Translate(f.X, f.Y); c.RotateDegrees(f.Rot * 57.3f); var r = Gfx.Ctr(0, 0, RW, RHt); Gfx.RectGrad(c, r, 22, f.Col.Light(.4f).A(a), f.Col.Dark(.5f).A(a)); c.Restore(); }
        Gfx.Text(c, status, 800, 172, 36, over ? C.Gold : (cur == 1 ? C.Cyan : C.Pink), Al.C, true, 8);
    }
}
