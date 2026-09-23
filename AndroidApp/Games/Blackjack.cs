using SkiaSharp;
using Silk.NET.Input;
namespace GlamourGames;
class Blackjack : Scene
{
    public override string Title => "Black Jack";
    public override SKColor Acc1 => C.Pink; public override SKColor Acc2 => C.Green;
    class Card { public string Rank; public int Suit; public float X, Y, SX, SY, T; public Spring Flip = new(0) { K = 170, D = 20 }; public bool Up; public float Rot; }
    class Hand { public List<Card> Cards = new(); public string Status = "waiting", Result; public long Bet = 10, Credits = 1000; public bool Doubled; }
    Hand[] pl = { new(), new() }; List<Card> dealer = new(); bool revealed; string phase = "betting"; int turn = -1; List<(string r, int s)> shoe = new(); string msg = "";
    Button bDeal, bHit, bStand, bDouble, bRefill; Button[] bm = new Button[2], bp = new Button[2];
    const float CWd = 108, Off = 46; static readonly float[] PX = { 420, 1180 }; const float DY = 250, PY = 450; static readonly SKPoint Shoe = new(1330, 190);
    public override void Enter()
    {
        base.Enter(); for (int i = 0; i < 2; i++) { pl[i].Credits = Save.Int("bj_c" + i, 1000); int k = i; float cx = PX[i]; bm[i] = Ui.Add(new Button(cx + 60, 700, 70, 46, "-10", C.Purple, () => Bet(k, -10), 22)); bp[i] = Ui.Add(new Button(cx + 140, 700, 70, 46, "+10", C.Purple, () => Bet(k, 10), 22)); }
        bDeal = Ui.Add(new Button(650, 795, 300, 70, "AUSTEILEN", C.Green, () => Co.Start(Deal()), 32));
        bHit = Ui.Add(new Button(440, 795, 220, 70, "KARTE", C.Cyan, () => Co.Start(Hit()), 30)); bStand = Ui.Add(new Button(690, 795, 220, 70, "HALTEN", C.Orange, Stand, 30)); bDouble = Ui.Add(new Button(940, 795, 220, 70, "VERDOPPELN", C.Pink, () => Co.Start(Double()), 26));
        bRefill = Ui.Add(new Button(1180, 795, 250, 60, "Guthaben auffüllen", C.Gold, () => { foreach (var h in pl) if (h.Credits < 10) { h.Credits = 1000; } Persist(); Sfx.Play(S.Coin); }, 20));
        Ui.Add(new Button(40, 790, 230, 56, "Neues Spiel", C.Purple, AskNew, 22));
        msg = "Setzt eure Einsätze und drückt AUSTEILEN!";
    }
    void AskNew()
    {
        Result("Neues Spiel?", "Guthaben und Kartenstapel werden zurückgesetzt", C.Pink, ("Ja, neu starten", C.Green, () => { Save.Set("bj_c0", 1000); Save.Set("bj_c1", 1000); App.Go(new Blackjack()); }), ("Abbrechen", C.Dim, null));
    }
    void Persist() { for (int i = 0; i < 2; i++) Save.Set("bj_c" + i, pl[i].Credits); }
    void Bet(int i, int d) { if (phase != "betting") return; pl[i].Bet = Math.Clamp(pl[i].Bet + d, 10, Math.Max(10, pl[i].Credits)); Sfx.Play(S.Chip); }
    static int Val(List<Card> cs)
    {
        int t = 0, a = 0; foreach (var c in cs) { if (c.Rank == "A") { a++; t += 11; } else if (c.Rank is "J" or "Q" or "K") t += 10; else t += int.Parse(c.Rank); }
        while (t > 21 && a > 0) { t -= 10; a--; } return t;
    }
    static bool IsBJ(List<Card> cs) => cs.Count == 2 && Val(cs) == 21;
    void BuildShoe() { shoe.Clear(); for (int d = 0; d < 4; d++) for (int s = 0; s < 4; s++) foreach (var r in CardArt.Ranks) shoe.Add((r, s)); shoe = shoe.OrderBy(_ => Random.Shared.Next()).ToList(); }
    IEnumerator<object> DealTo(List<Card> hand, bool up)
    {
        var (r, s) = shoe[^1]; shoe.RemoveAt(shoe.Count - 1); var c = new Card { Rank = r, Suit = s, X = Shoe.X, Y = Shoe.Y, SX = Shoe.X, SY = Shoe.Y, Up = up, Rot = 20 }; hand.Add(c); c.Flip.Target = up ? 1 : 0; Sfx.Play(S.Deal); yield return .34f;
    }
    IEnumerator<object> Deal()
    {
        if (phase != "betting") yield break;
        foreach (var h in pl) if (h.Bet > h.Credits || h.Bet < 10) { msg = "Einsatz übersteigt Guthaben!"; yield break; }
        phase = "dealing"; msg = "Karten werden ausgeteilt ..."; revealed = false; BuildShoe(); dealer.Clear();
        foreach (var h in pl) { h.Cards.Clear(); h.Status = "playing"; h.Result = null; h.Doubled = false; h.Credits -= h.Bet; } Persist();
        yield return DealTo(pl[0].Cards, true); yield return DealTo(pl[1].Cards, true); yield return DealTo(dealer, true);
        yield return DealTo(pl[0].Cards, true); yield return DealTo(pl[1].Cards, true); yield return DealTo(dealer, false);
        foreach (var (h, i) in pl.Select((h, i) => (h, i))) if (IsBJ(h.Cards)) { h.Status = "blackjack"; Sfx.Play(S.Big); Fx.Burst(PX[i], PY, 40, null, 350); Pop("BLACKJACK!", PX[i], PY - 130, C.Gold, 46); }
        Advance();
    }
    void Advance()
    {
        if (pl[0].Status == "playing") { phase = "p0"; turn = 0; msg = Pl.Name(0) + " ist am Zug"; }
        else if (pl[1].Status == "playing") { phase = "p1"; turn = 1; msg = Pl.Name(1) + " ist am Zug"; }
        else { phase = "dealer"; turn = -1; Co.Start(DealerCo()); }
        if (turn >= 0) Sfx.Play(S.Turn, .5f);
    }
    IEnumerator<object> Hit()
    {
        if (turn < 0 || phase == "busy") yield break; int i = turn; string ph = phase; phase = "busy"; yield return DealTo(pl[i].Cards, true); phase = ph;
        int v = Val(pl[i].Cards); if (v > 21) { pl[i].Status = "bust"; Sfx.Play(S.Lose); App.Shake(6); Pop("BUST!", PX[i], PY - 130, C.Red, 46); yield return .6f; Advance(); } else if (v == 21) { pl[i].Status = "stood"; yield return .3f; Advance(); }
    }
    void Stand() { if (turn < 0 || phase == "busy") return; pl[turn].Status = "stood"; Advance(); }
    IEnumerator<object> Double()
    {
        if (turn < 0 || phase == "busy") yield break; int i = turn; var h = pl[i]; if (h.Cards.Count != 2 || h.Credits < h.Bet) yield break;
        string ph = phase; phase = "busy"; h.Credits -= h.Bet; h.Bet *= 2; h.Doubled = true; Persist(); Sfx.Play(S.Chip); yield return DealTo(h.Cards, true); phase = ph;
        h.Status = Val(h.Cards) > 21 ? "bust" : "stood"; if (h.Status == "bust") { Sfx.Play(S.Lose); Pop("BUST!", PX[i], PY - 130, C.Red, 46); } yield return .6f; Advance();
    }
    IEnumerator<object> DealerCo()
    {
        msg = "Bank deckt auf ..."; yield return .5f; dealer[1].Flip.Target = 1; revealed = true; Sfx.Play(S.Flip); yield return .8f;
        if (!pl.All(p => p.Status == "bust")) while (Val(dealer) < 17) { msg = "Bank zieht ..."; yield return DealTo(dealer, true); yield return .35f; }
        Resolve();
    }
    void Resolve()
    {
        int d = Val(dealer); bool dBust = d > 21, dBJ = IsBJ(dealer); bool anyWin = false;
        for (int i = 0; i < 2; i++)
        {
            var p = pl[i]; string r; int v = Val(p.Cards);
            if (p.Status == "bust") r = "lose"; else if (p.Status == "blackjack") r = dBJ ? "push" : "blackjack"; else if (dBJ) r = "lose"; else if (dBust || v > d) r = "win"; else if (v < d) r = "lose"; else r = "push";
            p.Result = r; if (r == "win") p.Credits += p.Bet * 2; else if (r == "blackjack") p.Credits += (long)Math.Round(p.Bet * 2.5); else if (r == "push") p.Credits += p.Bet;
            if (r is "win" or "blackjack") { anyWin = true; Fx.Burst(PX[i], PY, 60, null, 420); }
        }
        Persist(); phase = "payout"; turn = -1; msg = "Runde beendet - neue Runde starten!";
        if (anyWin) { Sfx.Play(S.Win); App.Flash(C.Gold, .25f); Celebrate(C.Gold, 3, .7f); } else Sfx.Play(S.Lose);
    }
    void NewRound() { phase = "betting"; foreach (var h in pl) { h.Cards.Clear(); h.Status = "waiting"; h.Result = null; h.Bet = Math.Clamp(h.Bet, 10, Math.Max(10, h.Credits)); } dealer.Clear(); revealed = false; msg = "Setzt eure Einsätze und drückt AUSTEILEN!"; }
    public override void KeyDown(Key k)
    {
        if (phase == "betting" && (k == Key.Space || k == Key.Enter)) Co.Start(Deal()); else if (phase == "payout" && (k == Key.Space || k == Key.Enter)) NewRound();
        else if (k == Key.H) Co.Start(Hit()); else if (k == Key.S) Stand(); else if (k == Key.D) Co.Start(Double());
    }
    Card Target(List<Card> hand, int i, float cx, float cy) { float n = hand.Count; return null; }
    public override void Update(float dt)
    {
        bool bet = phase == "betting", pt = phase is "p0" or "p1";
        bDeal.Visible = bet || phase == "payout"; bDeal.Text = bet ? "AUSTEILEN" : "NEUE RUNDE"; bDeal.Click = bet ? () => Co.Start(Deal()) : NewRound; bDeal.Col = bet ? C.Green : C.Cyan;
        bHit.Visible = bStand.Visible = bDouble.Visible = pt; if (pt) { bDouble.Enabled = pl[turn].Cards.Count == 2 && pl[turn].Credits >= pl[turn].Bet; bHit.Text = $"KARTE (H)"; }
        for (int i = 0; i < 2; i++) { bm[i].Visible = bp[i].Visible = bet; }
        bRefill.Visible = phase is "betting" or "payout" && pl.Any(p => p.Credits < 10);
        Place(dealer, 800, DY, dt); Place(pl[0].Cards, PX[0], PY, dt); Place(pl[1].Cards, PX[1], PY, dt);
    }
    void Place(List<Card> hand, float cx, float cy, float dt)
    {
        int n = hand.Count; float off = Math.Min(Off, 320f / Math.Max(1, n));
        for (int i = 0; i < n; i++)
        {
            var c = hand[i]; float tx = cx + (i - (n - 1) / 2f) * off, ty = cy - (i % 2) * 0; c.Flip.Update(dt);
            if (c.T < 1) { c.T += dt / .4f; float e = Ease.OutCubic(c.T); c.X = c.SX + (tx - c.SX) * e; c.Y = c.SY + (ty - c.SY) * e - MathF.Sin(e * MathF.PI) * 60; c.Rot = Ease.Lerp(20, 0, e); }
            else { c.X += (tx - c.X) * Math.Min(1, dt * 12); c.Y += (ty - c.Y) * Math.Min(1, dt * 12); }
        }
    }
    public override void Draw(SKCanvas c)
    {
        var table = Gfx.R(120, 96, 1360, 570); Gfx.Glow(c, table, 285, C.Gold, 20, .35f);
        Gfx.RectGrad(c, table, 285, new SKColor(110, 62, 20), new SKColor(50, 24, 6)); var felt = Gfx.Inflate(table, -16);
        using (var sh = SKShader.CreateRadialGradient(new(800, 380), 700, new[] { new SKColor(20, 120, 70), new SKColor(6, 52, 34) }, null, SKShaderTileMode.Clamp)) { var p = Gfx.Fill(SKColors.White); p.Shader = sh; c.DrawRoundRect(felt, 270, 270, p); }
        Gfx.Stroke(c, felt, 270, C.Gold.A(.6f), 3); Gfx.Stroke(c, Gfx.Inflate(felt, -18), 252, C.Gold.A(.22f), 2);
        Gfx.Text(c, "BLACKJACK PAYS 3 : 2", 800, 440, 40, C.Gold.A(.5f), Al.C, true, 0, true); Gfx.Text(c, "Bank zieht bis 17 und bleibt bei 17", 800, 480, 20, SKColors.White.A(.35f), Al.C, false);
        Gfx.Text(c, msg, 800, 118, 26, C.Yellow.Light(.3f), Al.C, true, 6);
        var sh2 = Gfx.Fill(SKColors.Black.A(.5f)); sh2.MaskFilter = Gfx.Blur(8); c.DrawRoundRect(Gfx.Ctr(Shoe.X + 4, Shoe.Y + 8, 120, 160), 12, 12, sh2); for (int k = 0; k < 4; k++) CardArt.Card(c, Shoe.X + k * 2, Shoe.Y - k * 2, 110, "", 0, 0, 0);
        Gfx.Text(c, $"{shoe.Count} Karten", Shoe.X, Shoe.Y + 110, 18, C.Dim, Al.C, false);
        Gfx.Text(c, "BANK", 800, DY - 100, 22, C.Dim, Al.C, true, 4); DrawHand(c, dealer, -1);
        for (int i = 0; i < 2; i++) DrawPlayer(c, i);
    }
    void DrawHand(SKCanvas c, List<Card> hand, int pi)
    {
        foreach (var cd in hand) CardArt.Card(c, cd.X, cd.Y, CWd, cd.Rank, cd.Suit, cd.Flip.V, cd.Rot, 0, false);
        if (hand.Count > 0)
        {
            bool dl = pi < 0; var vis = dl && !revealed ? hand.Take(1).ToList() : hand; int v = Val(vis); float cx = hand.Average(x => x.X), y = (dl ? DY : PY) + 118;
            if (hand.All(x => x.T >= 1)) { var r = Gfx.Ctr(cx, y, 84, 40); Gfx.Rect(c, r, 20, SKColors.Black.A(.55f)); Gfx.Stroke(c, r, 20, v > 21 ? C.Red : v == 21 ? C.Gold : C.Cyan, 2); Gfx.Text(c, v.ToString() + (dl && !revealed ? "+" : ""), r.MidX, r.MidY, 26, v > 21 ? C.Red : v == 21 ? C.Gold : SKColors.White); }
        }
    }
    void DrawPlayer(SKCanvas c, int i)
    {
        var p = pl[i]; float cx = PX[i]; bool act = turn == i; var col = i == 0 ? C.Cyan : C.Pink;
        DrawHand(c, p.Cards, i);
        var box = Gfx.Ctr(cx, 708, 440, 112); if (act) Gfx.Glow(c, box, 20, col, 16, .5f + .3f * MathF.Sin(Time * 5)); W.Panel(c, box, col);
        Gfx.Text(c, Pl.Name(i) + (act ? "  -  am Zug" : ""), cx - 200, 676, 24, act ? col.Light(.4f) : SKColors.White, Al.L, true, act ? 6 : 0);
        Gfx.Text(c, $"Guthaben: {p.Credits}", cx - 200, 712, 22, C.Gold, Al.L, false); Gfx.Text(c, $"Einsatz: {p.Bet}", cx - 200, 744, 22, SKColors.White, Al.L, true);
        if (p.Cards.Count > 0 || phase == "betting") { var chip = new SKPoint(cx - 205 + 0, 0); }
        if (p.Result != null)
        {
            var (t, cc) = p.Result switch { "win" => ("GEWONNEN!", C.Green), "lose" => ("VERLOREN", C.Red), "push" => ("PUSH", C.Gold), _ => ("BLACKJACK 3:2", C.Gold) };
            float a = Ease.OutBack((Time * 3) % 1000); var rr = Gfx.Ctr(cx, PY - 140, 300, 50); Gfx.Rect(c, rr, 25, cc.Dark(.3f).A(.9f)); Gfx.Stroke(c, rr, 25, cc, 3); Gfx.Text(c, t, rr.MidX, rr.MidY, 28, cc.Light(.4f), Al.C, true, 8);
        }
    }
}
