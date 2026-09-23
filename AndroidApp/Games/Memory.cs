using SkiaSharp;
namespace GlamourGames;
class MemoryGame : Scene
{
    public override string Title => "Memory";
    public override SKColor Acc1 => C.Purple; public override SKColor Acc2 => C.Pink;
    static readonly string[] Sym = { "diamond", "car", "heart", "crown", "star", "clover", "butterfly", "paw", "rose", "wolf", "moon", "gamepad", "sun", "palms", "infinity", "notes", "coffee", "wings", "unicorn", "dragonfly" };
    class Card { public int Sym; public Spring Flip = new(0) { K = 190, D = 20 }; public bool Matched; public float MatchT, Hov; public Spring Lift = new(0); }
    Card[] cards; readonly List<int> up = new(); bool locked; int cur, first; int[] score = new int[2]; int matched;
    bool toss = true; int choice = -1; float tossT = -1; int tossRes; bool tossDone; Button bHead, bTail, bStart, bNew; float coinAng; float coinLift;
    int over = -1;
    const float CW = 104, CH = 128, GAP = 12, X0 = (1600 - (8 * CW + 7 * GAP)) / 2, Y0 = 128;
    static SKRect Rc(int i) => Gfx.R(X0 + (i % 8) * (CW + GAP), Y0 + (i / 8) * (CH + GAP), CW, CH);
    public override void Enter()
    {
        base.Enter();
        bHead = Ui.Add(new Button(560, 690, 230, 90, "KOPF", C.Gold, () => Pick(0), 32));
        bTail = Ui.Add(new Button(810, 690, 230, 90, "ZAHL", C.Gold, () => Pick(1), 32));
        bStart = Ui.Add(new Button(600, 740, 400, 84, "Spiel starten", C.Green, StartGame, 34) { Visible = false });
        bNew = Ui.Add(new Button(50, 780, 250, 62, "Neues Spiel", C.Purple, () => ToToss(), 24) { Visible = false });
        ToToss();
    }
    void ToToss()
    {
        toss = true; Modal = null; cards = null; Tm.Clear();
        bHead.Visible = bTail.Visible = false; bStart.Visible = false; bNew.Visible = false;
        CoinToss.Start(this, f => { first = f; StartGame(); });
    }
    void Pick(int c)
    {
        if (choice >= 0) return; choice = c; tossRes = Random.Shared.Next(2); tossT = 0; bHead.Visible = bTail.Visible = false; Sfx.Play(S.Chip);
    }
    void StartGame()
    {
        toss = false; bStart.Visible = false; bNew.Visible = true; score = new int[2]; matched = 0; up.Clear(); locked = false; cur = first;
        var pairs = Sym.Concat(Sym).OrderBy(_ => Random.Shared.Next()).ToArray();
        cards = pairs.Select(s => new Card { Sym = Array.IndexOf(Sym, s) }).ToArray();
        for (int i = 0; i < cards.Length; i++) { cards[i].Lift.V = -400 - i * 20; cards[i].Lift.Target = 0; }
    }
    public override bool WantsHand => !toss && over >= 0;
    public override void MouseMove(float x, float y) { over = -1; if (cards == null || toss || Modal != null) return; for (int i = 0; i < 40; i++) if (Rc(i).Contains(x, y)) over = i; }
    public override void MouseUp(float x, float y)
    {
        if (toss || locked || over < 0) return; var c = cards[over];
        if (c.Matched || up.Contains(over)) return;
        c.Flip.Target = 1; up.Add(over); Sfx.Play(S.Flip);
        if (up.Count == 2) Check();
    }
    void Check()
    {
        locked = true; int a = up[0], b = up[1];
        if (cards[a].Sym == cards[b].Sym)
            Tm.After(.55f, () =>
            {
                cards[a].Matched = cards[b].Matched = true; score[cur] += 8; matched += 2; up.Clear(); locked = false; Sfx.Play(S.Match);
                foreach (var i in new[] { a, b }) { var r = Rc(i); Fx.Burst(r.MidX, r.MidY, 34, null, 380); Fx.Ring(r.MidX, r.MidY, C.Gold, 18, 240); }
                var r0 = Rc(a); Pop("+8", r0.MidX, r0.Top, C.Gold, 46); App.Flash(C.Gold, .18f);
                if (matched == 40) Tm.After(.7f, End);
            });
        else
            Tm.After(1.0f, () => { Sfx.Play(S.NoMatch); cards[a].Flip.Target = 0; cards[b].Flip.Target = 0; up.Clear(); cur = 1 - cur; locked = false; Sfx.Play(S.Turn, .5f); });
    }
    void End()
    {
        int best = Math.Max(score[0], score[1]); int w = score[0] > score[1] ? 0 : score[1] > score[0] ? 1 : -1;
        Celebrate(w == 0 ? C.Cyan : w == 1 ? C.Pink : C.Gold, 5, 1.1f, w < 0 ? "Unentschieden!" : $"{Pl.Name(w)} gewinnt!"); Sfx.Play(S.Win); App.Shake(10);
        Action show = () => Result(w < 0 ? "UNENTSCHIEDEN!" : $"{Pl.Name(w)} gewinnt!", $"{Pl.Name(0)}: {score[0]} Pkt   |   {Pl.Name(1)}: {score[1]} Pkt", w == 0 ? C.Cyan : w == 1 ? C.Pink : C.Gold, ("Nochmal", C.Green, () => ToToss()), ("Menü", C.Purple, () => App.Go(new Menu())));
        Tm.After(3f, () => { if (Save.IsHigh("hs_mem", best)) NameEntry("hs_mem", best, "NEUER HIGHSCORE!", show); else show(); });
    }
    public override void Update(float dt)
    {
        if (toss) return;
        if (false)
        {
            if (tossT >= 0 && !tossDone)
            {
                tossT += dt; float p = Ease.Clamp(tossT / 2.4f); coinAng = Ease.OutCubic(p) * MathF.PI * (10 + (tossRes == 0 ? 0 : 1)); coinLift = MathF.Sin(p * MathF.PI) * 300;
                if (p >= 1) { tossDone = true; coinAng = MathF.PI * (10 + (tossRes == 0 ? 0 : 1)); coinLift = 0; first = tossRes == choice ? 0 : 1; bStart.Visible = true; Sfx.Play(S.Coin); Fx.Burst(800, 470, 50, new[] { C.Gold, C.Yellow }, 380); Pop("Spieler " + (first + 1) + " beginnt!", 800, 300, C.Gold, 54); }
            }
            return;
        }
        if (cards == null) return;
        foreach (var c in cards) { c.Flip.Update(dt); c.Lift.Update(dt); if (c.Matched) c.MatchT += dt; }
        for (int i = 0; i < 40; i++) cards[i].Hov = Ease.Lerp(cards[i].Hov, i == over && !cards[i].Matched ? 1 : 0, Math.Min(1, dt * 14));
    }
    public override void Draw(SKCanvas c)
    {
        if (toss) { DrawToss(c); return; }
        W.PlayerBox(c, Gfx.R(40, 140, 260, 210), Pl.Name(0), score[0].ToString(), C.Cyan, cur == 0 && !locked || cur == 0, Time, "Punkte");
        W.PlayerBox(c, Gfx.R(40, 380, 260, 210), Pl.Name(1), score[1].ToString(), C.Pink, cur == 1, Time, "Punkte");
        Gfx.Text(c, $"Paare übrig: {20 - matched / 2}", 170, 640, 26, C.Dim, Al.C, false);
        Gfx.Text(c, $"{Pl.Name(cur)} ist dran", 170, 690, 26, cur == 0 ? C.Cyan : C.Pink, Al.C, true, 6);
        HighscoreList(c, "hs_mem", 1320, 150, 250, C.Purple);
        for (int i = 0; i < 40; i++) DrawCard(c, i);
    }
    void DrawToss(SKCanvas c)
    {
        Gfx.Text(c, "Münzwurf", 800, 150, 64, C.Gold, Al.C, true, 20, true);
        Gfx.Text(c, choice < 0 ? "Spieler 1 wählt: Kopf oder Zahl? Gewinnt Spieler 1 den Wurf, beginnt er." : $"Spieler 1 hat {(choice == 0 ? "KOPF" : "ZAHL")} gewählt ...", 800, 215, 26, C.Dim, Al.C, false);
        Gfx.Radial(c, 800, 470, 260, C.Gold, .12f + .05f * MathF.Sin(Time * 2));
        Coin3D.Draw(c, 800, 470, 130, coinAng, coinLift);
        if (tossDone) Gfx.Text(c, tossRes == 0 ? "KOPF" : "ZAHL", 800, 650, 46, C.Gold, Al.C, true, 14);
        HighscoreList(c, "hs_mem", 1320, 150, 250, C.Purple);
    }
    void DrawCard(SKCanvas c, int i)
    {
        var cd = cards[i]; var r = Rc(i); float f = cd.Flip.V, sx = MathF.Abs(MathF.Cos(f * MathF.PI)), lift = cd.Hov * 8 + (f > .05f && f < .95f ? MathF.Sin(f * MathF.PI) * 16 : 0);
        float sc = 1 + cd.Hov * .04f + (cd.Matched ? .03f * MathF.Sin(cd.MatchT * 4) : 0);
        if (cd.Lift.V < -300) return;
        c.Save(); c.Translate(r.MidX, r.MidY - lift + cd.Lift.V); c.Scale(Math.Max(sx, .02f) * sc, sc);
        var rr = Gfx.Ctr(0, 0, CW, CH); bool face = f > .5f; var hue = SKColor.FromHsv(cd.Sym * 18, 70, 100);
        var sh = Gfx.Fill(SKColors.Black.A(.4f)); sh.MaskFilter = Gfx.Blur(8); c.DrawRoundRect(Gfx.Ctr(3, 8 + lift, CW, CH), 14, 14, sh);
        if (!face)
        {
            Gfx.Glow(c, rr, 14, C.Purple, 8, .25f + cd.Hov * .5f);
            Gfx.RectGrad(c, rr, 14, new SKColor(80, 30, 150), new SKColor(28, 8, 70)); Gfx.Stroke(c, rr, 14, C.Purple.Light(.3f), 2.5f + cd.Hov * 1.5f);
            var inner = Gfx.Inflate(rr, -10); Gfx.Stroke(c, inner, 8, C.Gold.A(.4f), 1.5f);
            var p = Gfx.Line(C.Gold.A(.22f), 1.2f); for (float k = -CH; k < CW + CH; k += 16) { c.Save(); c.ClipRoundRect(new SKRoundRect(inner, 8), SKClipOperation.Intersect, true); c.DrawLine(-CW / 2 + k, -CH / 2, -CW / 2 + k + CH, CH / 2, p); c.DrawLine(-CW / 2 + k, CH / 2, -CW / 2 + k + CH, -CH / 2, p); c.Restore(); }
            Gfx.Suit(c, 2, 0, 0, 18, C.Gold.A(.85f));
        }
        else
        {
            Gfx.Glow(c, rr, 14, cd.Matched ? C.Gold : hue, 12, cd.Matched ? .9f : .55f);
            Gfx.RectGrad(c, rr, 14, hue.Dark(.55f), hue.Dark(.16f)); Gfx.RectGrad(c, new SKRect(rr.Left + 3, rr.Top + 3, rr.Right - 3, rr.MidY), 12, SKColors.White.A(.12f), SKColors.White.A(0));
            Gfx.Stroke(c, rr, 14, cd.Matched ? C.Gold : hue, cd.Matched ? 4 : 2.5f);
            Gfx.Radial(c, 0, 0, 60, hue, .35f);
            Gfx.Image(c, Assets.Img(Sym[cd.Sym]), Gfx.Ctr(0, 0, CW * .8f, CW * .8f));
        }
        c.Restore();
    }
}
