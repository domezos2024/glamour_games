using System.Globalization;
using SkiaSharp;
using Silk.NET.Input;
namespace GlamourGames;
class SlotGame : Scene
{
    public override string Title => "Buch der Pharaonen";
    public override SKColor Acc1 => C.Gold; public override SKColor Acc2 => C.Orange;
    static readonly Dictionary<string, int[]> Pay = new()
    {
        ["EXPLORER"] = new[] { 0, 0, 10, 100, 1000, 5000 }, ["PHARAOH"] = new[] { 0, 0, 5, 40, 400, 2000 }, ["GODDESS"] = new[] { 0, 0, 5, 30, 100, 750 }, ["SCARAB"] = new[] { 0, 0, 5, 30, 100, 750 },
        ["A"] = new[] { 0, 0, 0, 5, 40, 150 }, ["K"] = new[] { 0, 0, 0, 5, 40, 150 }, ["Q"] = new[] { 0, 0, 0, 5, 25, 100 }, ["J"] = new[] { 0, 0, 0, 5, 25, 100 }, ["T"] = new[] { 0, 0, 0, 5, 25, 100 } };
    static readonly int[] Scatter = { 0, 0, 0, 2, 20, 200 };
    static readonly int[][] Lines = { new[] { 1, 1, 1, 1, 1 }, new[] { 0, 0, 0, 0, 0 }, new[] { 2, 2, 2, 2, 2 }, new[] { 0, 1, 2, 1, 0 }, new[] { 2, 1, 0, 1, 2 }, new[] { 1, 2, 2, 2, 1 }, new[] { 1, 0, 0, 0, 1 }, new[] { 2, 2, 1, 0, 0 }, new[] { 0, 0, 1, 2, 2 }, new[] { 2, 1, 1, 1, 0 } };
    static readonly string[] Ids = { "EXPLORER", "PHARAOH", "GODDESS", "SCARAB", "A", "K", "Q", "J", "T", "BOOK" };
    // 10 symbols, 1 Book per 30-stop reel
    static readonly int[][] Counts = {
        new[] { 1, 2, 2, 2, 4, 4, 4, 4, 6, 1 }, new[] { 1, 2, 2, 2, 4, 4, 4, 4, 6, 1 }, new[] { 1, 2, 2, 2, 4, 4, 4, 4, 6, 1 }, new[] { 1, 2, 2, 2, 4, 4, 4, 4, 6, 1 }, new[] { 1, 2, 2, 2, 4, 4, 4, 4, 6, 1 } };
    static readonly string[] Regular = { "EXPLORER", "PHARAOH", "GODDESS", "SCARAB", "A", "K", "Q", "J", "T" };
    static readonly Dictionary<string, string> Names = new() { ["EXPLORER"] = "Abenteurer", ["PHARAOH"] = "Pharao", ["GODDESS"] = "Göttin", ["SCARAB"] = "Skarabäus", ["A"] = "Ass", ["K"] = "König", ["Q"] = "Dame", ["J"] = "Bube", ["T"] = "Zehn", ["BOOK"] = "Buch" };
    static readonly int[] LineBets = { 1, 2, 4, 5, 10, 20, 50, 100, 200, 500, 1000 };
    static readonly SKColor[] LC = { C.Gold, C.Cyan, C.Pink, C.Green, C.Orange, C.Purple, C.Yellow, C.Magenta, C.Blue, C.Red };
    static readonly string[][] Strips = Enumerable.Range(0, 5).Select(i => MakeStrip(Counts[i], (uint)(1234 + i * 7919))).ToArray();
    static readonly CultureInfo De = new("de-DE");
    static string Eu(long c) => (c / 100.0).ToString("N2", De) + " €";
    static string[] MakeStrip(int[] counts, uint seed)
    {
        uint s = seed; double Rnd() { s = unchecked(s * 1664525u + 1013904223u); return s / 4294967296.0; }
        var items = new List<(string id, double key)>();
        for (int k = 0; k < Ids.Length; k++) for (int i = 0; i < counts[k]; i++) items.Add((Ids[k], (i + Rnd() * 0.8 + 0.1) / counts[k]));
        var o = items.OrderBy(x => x.key).Select(x => x.id).ToArray(); int L = o.Length;
        for (int pass = 0; pass < 50; pass++)
        {
            bool ok = true;
            for (int i = 0; i < L; i++) { int j = (i + 1) % L; if (o[i] == o[j]) { int k = (i + 2 + (int)Math.Floor(Rnd() * (L - 3))) % L; (o[j], o[k]) = (o[k], o[j]); ok = false; } }
            if (ok) break;
        }
        return o;
    }
    const float CW = 138, GP = 6, RX = 443, RY = 130;
    long credits; int lbIdx = 3, lines = 10; bool spinning, inFree; int freeSpins; string fsSym; long fsSum, lastWin;
    readonly float[] pos = new float[5], rs = new float[5], re = new float[5], rt = new float[5], rd = new float[5], last = new float[5], bump = new float[5]; readonly bool[] moving = new bool[5];
    string[][] grid = new string[5][]; string msg = ""; List<(int li, string sym, int n, long win)> wins = new(); int showLine = -2; float lineT, prevT; List<int> expReels = new(); float expT = 99; string expSym;
    (string title, string sub, string img)? overlay; float ovT;
    enum GambleMode { Cards, Ladder }
    GambleMode gMode = GambleMode.Ladder;
    int ladderStep;
    static readonly long[] LadderBase = { 0, 15, 30, 60, 120, 240, 400, 800, 1600, 3200, 6400, 14000 };
    int[] stops = new int[5];
    bool fastStopping;

    class Gamble { public bool Active, Busy; public int Steps; public long Amount, Base; public List<(string rank, int suit)> Hist = new(); public string Rank = ""; public int Suit; public Spring Flip = new(0) { K = 200, D = 22 }; }
    Gamble g = new();
    Button bAuto, bAutoSel, bSpin, bRefill, bBetM, bBetP, bLnM, bLnP, bRed, bBlack, bLadderRisk, bHalf, bTake, bTabCards, bTabLadder;
    long LineBet => LineBets[lbIdx]; long TotalBet => LineBet * lines;

    long LadderVal(int step)
    {
        if (step <= 0) return 0;
        step = Math.Clamp(step, 0, LadderBase.Length - 1);
        double factor = Math.Max(1.0, TotalBet / 10.0);
        return (long)Math.Round(LadderBase[step] * factor);
    }

    int FindLadderStep(long amt)
    {
        if (amt <= 0) return 0;
        int best = 1;
        long bestDiff = long.MaxValue;
        for (int i = 1; i < LadderBase.Length; i++)
        {
            long diff = Math.Abs(LadderVal(i) - amt);
            if (diff < bestDiff) { bestDiff = diff; best = i; }
        }
        return best;
    }

    public override void Enter()
    {
        base.Enter(); credits = Save.Int("slot_cents", 10000); var rnd = Random.Shared;
        for (int r = 0; r < 5; r++) { pos[r] = last[r] = rnd.Next(Strips[r].Length); bump[r] = 9; }
        SetGrid();
        bSpin = Ui.Add(new Button(732, 672, 136, 136, "SPIN", C.Gold, () => Spin(), 34) { Round = true });
        bBetM = Ui.Add(new Button(520, 725, 60, 54, "-", C.Purple, () => ChangeBet(-1), 34)); bBetP = Ui.Add(new Button(600, 725, 60, 54, "+", C.Purple, () => ChangeBet(1), 34));
        bLnM = Ui.Add(new Button(940, 725, 60, 54, "-", C.Purple, () => ChangeLines(-1), 34)); bLnP = Ui.Add(new Button(1020, 725, 60, 54, "+", C.Purple, () => ChangeLines(1), 34));
        bAutoSel = Ui.Add(new Button(1098, 725, 60, 54, "25", C.Purple, () => { if (autoOn) return; autoIdx = (autoIdx + 1) % AutoOpts.Length; Sfx.Play(S.Chip); }, 26));
        bAuto = Ui.Add(new Button(1166, 725, 124, 54, "AUTO", C.Cyan, AutoToggle, 20));
        bRefill = Ui.Add(new Button(300, 730, 180, 56, "+100 € Spielgeld", C.Green, () => { credits += 10000; Save.Set("slot_cents", credits); Sfx.Play(S.Coin); msg = "Spielgeld aufgefüllt"; }, 20) { Visible = false });

        bTabCards = Ui.Add(new Button(1205, 116, 170, 36, "♠ KARTEN", C.Purple, () => { gMode = GambleMode.Cards; Sfx.Play(S.Chip); }, 20) { Visible = false });
        bTabLadder = Ui.Add(new Button(1390, 116, 170, 36, "🪜 LEITER", C.Gold, () => { gMode = GambleMode.Ladder; ladderStep = FindLadderStep(g.Amount); g.Amount = LadderVal(ladderStep); Sfx.Play(S.Chip); }, 20) { Visible = false });
        bRed = Ui.Add(new Button(1205, 442, 170, 52, "ROT", C.Red, () => Guess(true), 28) { Visible = false });
        bBlack = Ui.Add(new Button(1390, 442, 170, 52, "SCHWARZ", new SKColor(70, 70, 90), () => Guess(false), 26) { Visible = false });
        bLadderRisk = Ui.Add(new Button(1205, 442, 355, 52, "⬆ RISIKO (1:1)", C.Orange, LadderRisk, 24) { Visible = false });
        bHalf = Ui.Add(new Button(1205, 506, 170, 48, "½ TEILEN", C.Cyan, Half, 22) { Visible = false });
        bTake = Ui.Add(new Button(1390, 506, 170, 48, "NEHMEN", C.Green, () => Collect(false), 24) { Visible = false });
        msg = "Viel Glück! Tippe auf SPIN";
    }
    void SetGrid() { for (int r = 0; r < 5; r++) grid[r] = Enumerable.Range(0, 3).Select(k => Sym(r, (int)MathF.Floor(pos[r]) + k)).ToArray(); }
    static string Sym(int r, int i) { var s = Strips[r]; return s[((i % s.Length) + s.Length) % s.Length]; }
    void ChangeBet(int d) { if (spinning || inFree || g.Active) return; lbIdx = Math.Clamp(lbIdx + d, 0, LineBets.Length - 1); Sfx.Play(S.Chip); }
    void ChangeLines(int d) { if (spinning || inFree || g.Active) return; lines = Math.Clamp(lines + d, 1, 10); prevT = 1.6f; Sfx.Play(S.Chip); }
    static readonly int[] AutoOpts = { 25, 50, 100, -1 }; int autoIdx, autoLeft; float autoWait; bool autoOn;
    string AutoLbl(int n) => n < 0 ? "∞" : n.ToString();
    void AutoToggle()
    {
        if (autoOn) { autoOn = false; msg = "Auto-Spiel gestoppt"; Sfx.Play(S.Click); return; }
        autoOn = true; autoLeft = AutoOpts[autoIdx]; autoWait = 0; msg = $"Auto-Spiel: {AutoLbl(autoLeft)} Durchläufe"; Sfx.Play(S.Chip);
    }
    void AutoTick(float dt, bool act)
    {
        if (!autoOn) return;
        if (!act || g.Busy || spinning) { autoWait = .7f; return; }
        if (inFree) return;
        autoWait -= dt; if (autoWait > 0) return;
        if (g.Active) { Collect(true); autoWait = .5f; return; }
        if (autoLeft == 0 || TotalBet > credits) { autoOn = false; msg = autoLeft == 0 ? "Auto-Spiel beendet" : "Auto-Spiel gestoppt: zu wenig Guthaben"; return; }
        if (autoLeft > 0) autoLeft--; autoWait = .5f; Spin();
    }
    void Spin()
    {
        if (overlay != null) { CloseOv(); return; }
        if (spinning)
        {
            FastStop();
            return;
        }
        if (Co.Busy) return;
        if (g.Active) { if (g.Busy) return; Collect(true); }
        Co.Start(SpinCo());
    }

    void FastStop()
    {
        if (!spinning || fastStopping) return;
        fastStopping = true;
        for (int r = 0; r < 5; r++)
        {
            if (!moving[r]) continue;
            int L = Strips[r].Length;
            float cur = pos[r];
            float curMod = ((cur % L) + L) % L;
            float dist = curMod - stops[r];
            while (dist < 1.5f) dist += L;
            rs[r] = cur;
            re[r] = cur - dist;
            rt[r] = 0;
            rd[r] = 0.06f + r * 0.035f;
        }
        Sfx.Play(S.Stop, .7f, 1.2f);
        msg = "Schnellstopp!";
    }

    IEnumerator<object> SpinCo()
    {
        bool isFree = inFree && freeSpins > 0; long tb = TotalBet, lb = LineBet;
        if (!isFree)
        {
            if (tb > credits) { msg = "Nicht genug Guthaben - Einsatz senken oder Spielgeld auffüllen."; yield break; }
            credits -= tb; Save.Set("slot_cents", credits); lastWin = 0;
        }
        else freeSpins--;
        spinning = true; fastStopping = false; showLine = -2; wins.Clear(); expReels.Clear(); expT = 99;
        msg = isFree ? $"Freispiel ... ({freeSpins} übrig)" : "Walzen drehen ...";
        Sfx.Play(S.Spin);

        // Determine stops purely by independent random selection (pure casino RNG)
        stops = Strips.Select(s => Random.Shared.Next(s.Length)).ToArray();

        for (int r = 0; r < 5; r++)
        {
            int L = Strips[r].Length; float st = pos[r]; float minTravel = L * (2 + r * .6f); int e = stops[r] + L * (int)MathF.Floor((st - minTravel - stops[r]) / L);
            rs[r] = st; re[r] = e; rt[r] = 0; rd[r] = .9f + r * .3f; moving[r] = true;
        }
        yield return (Func<bool>)(() => !moving.Any(m => m));
        spinning = false; fastStopping = false;
        SetGrid(); var gr = grid;
        long lineTotal = 0; wins.Clear();
        for (int li = 0; li < lines; li++)
        {
            var seq = Lines[li].Select((row, r) => gr[r][row]).ToArray(); string bs = seq.FirstOrDefault(x => x != "BOOK"); if (bs == null) continue;
            int n = 0; foreach (var s in seq) { if (s == bs || s == "BOOK") n++; else break; }
            int m = Pay[bs][n]; if (m > 0) { long w = m * lb; lineTotal += w; wins.Add((li, bs, n, w)); }
        }
        int books = gr.Sum(col => col.Count(x => x == "BOOK")); long scWin = Scatter[Math.Min(books, 5)] * tb; long win = lineTotal + scWin;
        if (wins.Count > 0) { Sfx.Play(win >= tb * 10 ? S.Big : S.Match); yield return ShowWins(); }
        if (scWin > 0) { msg = $"{books}× Buch (Scatter) = {Eu(scWin)}"; Sfx.Play(S.Match); yield return .7f; }
        if (isFree && fsSym != null)
        {
            var reels = Enumerable.Range(0, 5).Where(r => gr[r].Contains(fsSym)).ToList(); int m = Pay[fsSym][reels.Count]; long ex = m > 0 ? m * lb * lines : 0;
            if (ex > 0) { msg = $"{Names[fsSym]} expandiert auf {reels.Count} Walzen!"; expReels = reels; expSym = fsSym; expT = 0; Sfx.Play(S.Big); App.Shake(10); yield return 1.7f; win += ex; msg = $"Sondersymbol-Gewinn: {Eu(ex)}"; yield return .5f; expReels.Clear(); expT = 99; }
        }
        lastWin = win;
        if (win >= tb * 20) { Celebrate(C.Gold, 5, 1.2f); for (int k = 0; k < 4; k++) { int kk = k; Tm.After(.3f * kk, () => Fx.Lightning(400 + kk * 270, -20, 400 + kk * 270, 420, C.Gold)); } }
        if (isFree)
        {
            fsSum += win;
            if (books >= 3) { freeSpins += 10; yield return Overlay("+10 FREISPIELE!", $"{books}× Buch - Freispiele verlängert", "BOOK", 2.2f); }
            if (freeSpins > 0) { msg = win > 0 ? $"Gewinn: {Eu(win)}" : "Kein Gewinn"; yield return 1.0f; Co.Start(SpinCo()); yield break; }
            long total = fsSum; inFree = false; fsSym = null; fsSum = 0;
            yield return Overlay("FREISPIELE BEENDET", $"Gesamtgewinn: {Eu(total)}", "BOOK", 3f); lastWin = total;
            if (total > 0) { msg = $"Freispiel-Gewinn: {Eu(total)} - Risiko oder Nehmen"; StartGamble(total); } else msg = "Freispiele ohne Gewinn.";
            yield break;
        }
        if (books >= 3)
        {
            fsSym = Regular[Random.Shared.Next(Regular.Length)]; inFree = true; freeSpins = 10; fsSum = win;
            yield return Overlay("10 FREISPIELE!", $"Sondersymbol: {Names[fsSym]}", fsSym, 3.2f); yield return .4f; Co.Start(SpinCo()); yield break;
        }
        if (win > 0) { msg = $"GEWINN: {Eu(win)} - Risiko oder Nehmen"; StartGamble(win); } else msg = "Leider kein Gewinn - nochmal!";
    }
    IEnumerator<object> ShowWins()
    {
        showLine = -1; yield return .9f; int n = Math.Min(wins.Count, 5);
        for (int i = 0; i < n; i++) { showLine = wins[i].li; msg = $"Linie {wins[i].li + 1}: {wins[i].n}× {Names[wins[i].sym]} = {Eu(wins[i].win)}"; Sfx.Play(S.Chip); yield return .8f; }
        showLine = -1;
    }
    IEnumerator<object> Overlay(string t, string s, string img, float auto)
    {
        overlay = (t, s, img); ovT = 0; Sfx.Play(S.Big); Celebrate(C.Gold, 3, .8f); Tm.After(auto, () => { if (overlay != null && overlay.Value.title == t) CloseOv(); });
        yield return (Func<bool>)(() => overlay == null);
    }
    void CloseOv() { overlay = null; }
    void StartGamble(long amt)
    {
        g.Active = true;
        g.Steps = 0;
        g.Amount = amt;
        g.Base = TotalBet;
        g.Busy = false;
        g.Flip.Snap(0);
        ladderStep = FindLadderStep(amt);
        g.Amount = LadderVal(ladderStep);
    }
    bool CanDouble => g.Active && !g.Busy && g.Amount > 0 && g.Steps < 5 && g.Amount * 2 <= g.Base * 1000;
    bool CanLadderStep => g.Active && !g.Busy && g.Amount > 0 && ladderStep < 11;
    void Guess(bool red)
    {
        if (!CanDouble) return; g.Busy = true; var suits = new[] { 1, 2, 0, 3 }; g.Suit = suits[Random.Shared.Next(4)]; g.Rank = CardArt.Ranks[Random.Shared.Next(13)]; g.Flip.Target = 1; Sfx.Play(S.Flip);
        Tm.After(.7f, () =>
        {
            bool isRed = g.Suit == 1 || g.Suit == 2; g.Hist.Insert(0, (g.Rank, g.Suit)); if (g.Hist.Count > 8) g.Hist.RemoveAt(8);
            if (isRed == red)
            {
                g.Amount *= 2; g.Steps++; ladderStep = FindLadderStep(g.Amount); lastWin = g.Amount; msg = $"{g.Rank} - RICHTIG! Neuer Betrag: {Eu(g.Amount)}"; Sfx.Play(S.Match); Fx.Burst(1380, 290, 40, new[] { C.Gold, C.Green }, 320); g.Busy = false;
                Tm.After(1.0f, () => { if (g.Active && !g.Busy) g.Flip.Target = 0; });
                if (!CanDouble) { msg += " - Limit erreicht, wird gutgeschrieben."; g.Busy = true; Tm.After(1.6f, () => { g.Busy = false; Collect(false); }); }
            }
            else { msg = $"{g.Rank} - FALSCH! {Eu(g.Amount)} verloren."; g.Amount = 0; lastWin = 0; ladderStep = 0; Sfx.Play(S.Die); App.Shake(8); Tm.After(1.6f, () => { g.Busy = false; EndGamble(); }); }
        });
    }

    void LadderRisk()
    {
        if (!CanLadderStep) return;
        g.Busy = true;
        Sfx.Play(S.Chip);
        Tm.After(0.35f, () =>
        {
            if (!g.Active) return;
            bool up = Random.Shared.Next(2) == 0;
            if (up)
            {
                ladderStep = Math.Min(11, ladderStep + 1);
                g.Amount = LadderVal(ladderStep);
                lastWin = g.Amount;
                Sfx.Play(S.Match);
                Fx.Burst(1380, 178 + (11 - ladderStep) * 21 + 10, 26, new[] { C.Gold, C.Green }, 240);
                msg = $"Leiter: {Eu(g.Amount)}! Hochdrücken oder Nehmen";
                g.Busy = false;
                if (ladderStep == 11)
                {
                    Celebrate(C.Gold, 6, 1.4f);
                    Sfx.Play(S.Big);
                    App.Flash(C.Gold, .4f);
                    msg = $"LEITER VOLL: {Eu(g.Amount)}!";
                    g.Busy = true;
                    Tm.After(2.0f, () => { g.Busy = false; Collect(false); });
                }
            }
            else
            {
                if (ladderStep <= 1)
                {
                    ladderStep = 0;
                    g.Amount = 0;
                    lastWin = 0;
                    msg = "Abgestürzt auf 0,00 €!";
                    Sfx.Play(S.Die);
                    App.Shake(8);
                    Tm.After(1.4f, () => { g.Busy = false; EndGamble(); });
                }
                else
                {
                    ladderStep = Math.Max(0, ladderStep - 1);
                    g.Amount = LadderVal(ladderStep);
                    lastWin = g.Amount;
                    if (ladderStep == 0)
                    {
                        msg = "Abgestürzt auf 0,00 €!";
                        Sfx.Play(S.Die);
                        App.Shake(8);
                        Tm.After(1.4f, () => { g.Busy = false; EndGamble(); });
                    }
                    else
                    {
                        msg = $"Abgestürzt auf {Eu(g.Amount)}! Nochmal hochdrücken oder Nehmen";
                        Sfx.Play(S.NoMatch);
                        g.Busy = false;
                    }
                }
            }
        });
    }

    void Half()
    {
        if (!g.Active || g.Busy || g.Amount < 2 || ladderStep <= 1) return;
        long h = g.Amount / 2;
        credits += h;
        Save.Set("slot_cents", credits);
        g.Amount -= h;
        ladderStep = FindLadderStep(g.Amount);
        g.Amount = LadderVal(ladderStep);
        lastWin = g.Amount;
        msg = $"{Eu(h)} gesichert - {Eu(g.Amount)} verbleiben im Risiko.";
        Sfx.Play(S.Coin);
    }
    void Collect(bool silent)
    {
        if (!g.Active || g.Busy) return;
        long a = g.Amount;
        credits += a;
        Save.Set("slot_cents", credits);
        if (!silent && a > 0)
        {
            Sfx.Play(S.Coin);
            msg = $"{Eu(a)} gutgeschrieben.";
            Fx.Burst(400, 690, 30, new[] { C.Gold, C.Yellow }, 300);
        }
        EndGamble();
    }
    void EndGamble()
    {
        g.Active = false;
        g.Amount = 0;
        g.Busy = false;
        ladderStep = 0;
        g.Flip.Snap(0);
    }
    public override void KeyDown(Key k)
    {
        if (overlay != null) { CloseOv(); return; }
        if (k == Key.Space || k == Key.Enter)
        {
            if (spinning) FastStop();
            else if (g.Active)
            {
                if (gMode == GambleMode.Ladder) LadderRisk();
                else Guess(true);
            }
            else Spin();
        }
        else if (k == Key.Up) ChangeBet(1);
        else if (k == Key.Down) ChangeBet(-1);
        else if (k == Key.Right) ChangeLines(1);
        else if (k == Key.Left) ChangeLines(-1);
    }
    public override void MouseUp(float x, float y) { if (overlay != null) CloseOv(); }
    public override void Update(float dt)
    {
        ovT += dt; expT += dt; lineT += dt; prevT = Math.Max(0, prevT - dt); g.Flip.Update(dt);
        for (int r = 0; r < 5; r++)
        {
            last[r] = pos[r]; bump[r] += dt;
            if (moving[r]) { rt[r] += dt; float p = Ease.Clamp(rt[r] / rd[r]); pos[r] = rs[r] + (re[r] - rs[r]) * (1 - MathF.Pow(1 - p, 3)); if (p >= 1) { moving[r] = false; pos[r] = re[r]; bump[r] = 0; Sfx.Play(S.Stop, .5f, 1 + r * .05f); var (x, y) = (RX + r * (CW + GP) + CW / 2, RY + 1.5f * (CW + GP)); Fx.Spark(x, y + 60, C.Gold, 5, 150); } }
        }
        bool act = !spinning && !Co.Busy && overlay == null;
        if (spinning)
        {
            bSpin.Enabled = !fastStopping;
            bSpin.Text = fastStopping ? "..." : "STOPP";
        }
        else
        {
            bSpin.Enabled = act && !g.Busy;
            bSpin.Text = inFree ? "FREI" : g.Active ? "NEHMEN" : "SPIN";
        }
        bool can = act && !inFree && !g.Active;
        bAuto.Text = autoOn ? $"STOP ({AutoLbl(autoLeft)})" : "AUTO"; bAuto.Col = autoOn ? C.Red : C.Cyan; bAuto.Selected = autoOn; bAuto.Enabled = autoOn || can;
        bAutoSel.Text = autoOn ? AutoLbl(autoLeft) : AutoLbl(AutoOpts[autoIdx]); bAutoSel.Size = 22; bAutoSel.Enabled = !autoOn && can;
        AutoTick(dt, act);
        bBetM.Enabled = bBetP.Enabled = bLnM.Enabled = bLnP.Enabled = can;
        bRefill.Visible = credits < TotalBet && can;

        bool gv = g.Active && overlay == null;
        bTabCards.Visible = bTabLadder.Visible = gv;
        bTabCards.Col = gMode == GambleMode.Cards ? C.Gold : C.Purple;
        bTabCards.Selected = gMode == GambleMode.Cards;
        bTabLadder.Col = gMode == GambleMode.Ladder ? C.Gold : C.Purple;
        bTabLadder.Selected = gMode == GambleMode.Ladder;

        if (gMode == GambleMode.Cards)
        {
            bRed.Visible = bBlack.Visible = gv;
            bLadderRisk.Visible = false;
            bRed.Enabled = bBlack.Enabled = CanDouble;
        }
        else
        {
            bRed.Visible = bBlack.Visible = false;
            bLadderRisk.Visible = gv;
            bLadderRisk.Enabled = CanLadderStep;
        }
        bHalf.Visible = bTake.Visible = gv;
        bHalf.Enabled = !g.Busy && g.Amount >= 2 && ladderStep > 1;
        bTake.Enabled = !g.Busy && g.Amount > 0;
    }
    static SKRect Cell(int r, int k) => Gfx.R(RX + r * (CW + GP), RY + k * (CW + GP), CW, CW);
    static SKPoint Ctr(int r, int k) { var c = Cell(r, k); return new(c.MidX, c.MidY); }
    static void DrawCell(SKCanvas c, SKRect r, string sym, float scale = 1, float dim = 0, bool hl = false, float pulse = 0)
    {
        c.Save(); c.Translate(r.MidX, r.MidY); c.Scale(scale, scale); c.Translate(-r.MidX, -r.MidY);
        if (hl) Gfx.Glow(c, r, 14, C.Gold, 12, .7f + .3f * pulse);
        Gfx.RectGrad(c, r, 14, new SKColor(255, 243, 208), new SKColor(212, 172, 100)); Gfx.Stroke(c, r, 14, hl ? C.Gold : new SKColor(120, 80, 20), hl ? 5 : 3);
        Gfx.Image(c, Assets.Img("slot_" + sym), Gfx.Inflate(r, -9)); if (dim > 0) Gfx.Rect(c, r, 14, SKColors.Black.A(dim)); c.Restore();
    }
    public override void Draw(SKCanvas c)
    {
        DrawPaytable(c);
        var frame = Gfx.R(RX - 22, RY - 22, 5 * CW + 4 * GP + 44, 3 * CW + 2 * GP + 44); Gfx.Glow(c, frame, 26, C.Gold, 22, .45f);
        Gfx.RectGrad(c, frame, 26, new SKColor(94, 50, 14), new SKColor(36, 16, 4)); Gfx.Stroke(c, frame, 26, C.Gold, 4); Gfx.Stroke(c, Gfx.Inflate(frame, -8), 20, C.Gold.A(.4f), 2);
        var HL = new HashSet<(int, int)>(); float pu = .5f + .5f * MathF.Sin(Time * 8);
        int shown = showLine; if (showLine == -1) foreach (var w in wins) for (int r = 0; r < w.n; r++) HL.Add((r, Lines[w.li][r])); else if (showLine >= 0) { var w = wins.First(x => x.li == showLine); for (int r = 0; r < w.n; r++) HL.Add((r, Lines[w.li][r])); }
        for (int r = 0; r < 5; r++)
        {
            var col = Gfx.R(RX + r * (CW + GP) - 2, RY - 4, CW + 4, 3 * CW + 2 * GP + 8); c.Save(); c.ClipRoundRect(new SKRoundRect(col, 16), SKClipOperation.Intersect, true);
            Gfx.Rect(c, col, 14, new SKColor(20, 8, 2)); float v = Math.Abs(pos[r] - last[r]) * (CW + GP) / Math.Max(.001f, .016f); float fl = MathF.Floor(pos[r]), fr = pos[r] - fl;
            float off = bump[r] < 1 ? MathF.Sin(bump[r] * 26) * 9 * MathF.Exp(-bump[r] * 9) : 0;
            c.Save(); c.Translate(0, off);
            for (int k = -1; k <= 3; k++)
            {
                var cr = Cell(r, 0); float y = RY + (k - fr) * (CW + GP); var rect = Gfx.R(cr.Left, y, CW, CW); string sym = Sym(r, (int)fl + k); bool hl = !moving[r] && !spinning && k >= 0 && k < 3 && HL.Contains((r, k));
                bool dimIt = !moving[r] && !spinning && HL.Count > 0 && !hl && k >= 0 && k < 3;
                if (v > 1800) { for (int gI = 1; gI <= 3; gI++) { var gr2 = Gfx.R(cr.Left, y - gI * v * .006f, CW, CW); using var lp = new SKPaint { Color = SKColors.White.WithAlpha((byte)(90 / gI)) }; c.SaveLayer(lp); DrawCell(c, gr2, sym); c.Restore(); } }
                DrawCell(c, rect, sym, hl ? 1.05f + .04f * pu : 1, dimIt ? .5f : 0, hl, pu);
            }
            c.Restore(); if (v > 1500) Gfx.RectGrad(c, col, 14, SKColors.Black.A(.15f), SKColors.Black.A(.15f)); c.Restore();
        }
        if (expT < 5) foreach (var r in expReels)
            {
                float e = Ease.OutBack(expT / .5f), h = (3 * CW + 2 * GP) * e; var rect = Gfx.Ctr(RX + r * (CW + GP) + CW / 2, RY + 1.5f * (CW + GP), CW, h);
                Gfx.Glow(c, rect, 14, C.Gold, 22, .9f); Gfx.RectGrad(c, rect, 14, new SKColor(255, 243, 208), new SKColor(212, 172, 100)); Gfx.Stroke(c, rect, 14, C.Gold, 5);
                Gfx.Image(c, Assets.Img("slot_" + expSym), Gfx.Ctr(rect.MidX, rect.MidY, CW - 14, Math.Min(h, CW * 2.4f)));
            }
        if ((showLine == -1 || showLine >= 0 || prevT > 0) && !spinning || showLine != -2)
        {
            var ls = showLine == -1 ? wins.Select(w => w.li).ToList() : showLine >= 0 ? new List<int> { showLine } : prevT > 0 ? Enumerable.Range(0, lines).ToList() : new();
            foreach (var li in ls)
            {
                using var p = new SKPath(); for (int r = 0; r < 5; r++) { var pt = Ctr(r, Lines[li][r]); if (r == 0) p.MoveTo(pt); else p.LineTo(pt); }
                var gl = Gfx.Line(LC[li].A(.9f), 14); gl.MaskFilter = Gfx.Blur(8); c.DrawPath(p, gl); c.DrawPath(p, Gfx.Line(LC[li], 5));
            }
        }
        DrawHud(c); DrawGamble(c);
        if (inFree)
        {
            var b = Gfx.R(RX - 22, 84, 5 * CW + 4 * GP + 44, 40); Gfx.RectGrad(c, b, 14, new SKColor(14, 30, 22, 245), new SKColor(6, 12, 12, 245)); Gfx.Stroke(c, b, 14, C.Green.A(.85f), 2);
            var seg = new (string l, string v, SKColor col)[] { ("FREISPIELE", freeSpins.ToString(), C.Green.Light(.35f)), ("Sondersymbol", Names[fsSym], C.Gold), ("Summe", Eu(fsSum), C.Gold.Light(.3f)) };
            float gap = 44, tw = 0; foreach (var g in seg) tw += Gfx.TW(g.l + "  ", 18, false) + Gfx.TW(g.v, 24); tw += gap * 2; float x = b.MidX - tw / 2;
            foreach (var g in seg) { float w1 = Gfx.TW(g.l + "  ", 18, false); Gfx.Text(c, g.l, x, b.MidY + 1, 18, new SKColor(215, 205, 235), Al.L, false); Gfx.TextShadow(c, g.v, x + w1, b.MidY, 24, g.col, Al.L); x += w1 + Gfx.TW(g.v, 24) + gap; }
        }
        if (overlay != null) DrawOverlay(c);
    }
    void DrawOverlay(SKCanvas c)
    {
        var (t, s, img) = overlay.Value; float a = Ease.OutCubic(ovT / .3f); c.DrawRect(-2000, -2000, 5600, 4900, Gfx.Fill(SKColors.Black.A(.72f * a)));
        Gfx.Radial(c, 800, 400, 420, C.Gold, .5f * a);
        c.Save(); c.Translate(800, 450); float sc = Ease.OutBack(ovT / .5f); c.Scale(sc, sc);
        Gfx.Text(c, t, 0, -250, 84, C.Gold, Al.C, true, 24, true); c.Save(); c.RotateDegrees(MathF.Sin(ovT * 2) * 4); Gfx.Image(c, Assets.Img("slot_" + img), Gfx.Ctr(0, -20, 240, 240)); c.Restore();
        Gfx.Text(c, s, 0, 150, 40, SKColors.White, Al.C, true, 6); Gfx.Text(c, "Tippen zum Fortfahren", 0, 215, 22, C.Dim, Al.C, false); c.Restore();
    }
    void DrawPaytable(SKCanvas c)
    {
        var r = Gfx.R(30, 108, 385, 470); W.Panel(c, r, C.Gold); Gfx.Text(c, "GEWINNTABELLE", r.MidX, r.Top + 26, 24, C.Gold, Al.C, true, 6, true);
        Gfx.Text(c, "3×", 250, r.Top + 62, 18, C.Dim); Gfx.Text(c, "4×", 320, r.Top + 62, 18, C.Dim); Gfx.Text(c, "5×", 390, r.Top + 62, 18, C.Dim);
        long lb = LineBet;
        for (int i = 0; i < Regular.Length; i++)
        {
            float y = r.Top + 96 + i * 38; var id = Regular[i]; var ic = Gfx.Ctr(66, y, 34, 34); Gfx.Rect(c, Gfx.Inflate(ic, 2), 6, new SKColor(255, 240, 200)); Gfx.Image(c, Assets.Img("slot_" + id), ic);
            if (inFree && fsSym == id) Gfx.Glow(c, Gfx.Inflate(ic, 3), 8, C.Green, 8, .9f);
            for (int k = 0; k < 3; k++) { long v = Pay[id][3 + k] * lb; Gfx.Text(c, Short(v), 250 + k * 70, y, 15, i < 2 ? C.Gold.Light(.3f) : SKColors.White, Al.C, false); }
        }
        float by = r.Top + 96 + 9 * 38 + 4; var bi = Gfx.Ctr(66, by, 34, 34); Gfx.Rect(c, Gfx.Inflate(bi, 2), 6, new SKColor(255, 240, 200)); Gfx.Image(c, Assets.Img("slot_BOOK"), bi);
        Gfx.Text(c, "Buch: Joker + Scatter", 236, by - 8, 15, C.Gold, Al.L, false); Gfx.Text(c, "3+ = 10 Freispiele", 236, by + 10, 15, C.Gold.Light(.3f), Al.L, false);
    }
    static string Short(long cents) { double e = cents / 100.0; return e >= 1000 ? (e / 1000).ToString("0.#", De) + "k €" : e.ToString(e < 10 ? "0.00" : "0", De) + " €"; }
    void DrawHud(SKCanvas c)
    {
        var p = Gfx.R(300, 600, 1000, 270); W.Panel(c, p, C.Gold, 26);
        Gfx.Text(c, msg, 800, 626, 24, C.Yellow.Light(.3f), Al.C, true, 4);
        void Lb(string t, float x, string v, SKColor col) { Gfx.Text(c, t, x, 662, 18, C.Dim, Al.C, false); Gfx.Text(c, v, x, 696, 30, col, Al.C, true, 5); }
        Lb("GUTHABEN", 390, Eu(credits), C.Gold); Lb("EINSATZ / LINIE", 590, Eu(LineBet), SKColors.White); Lb("LINIEN", 1010, lines.ToString(), SKColors.White);
        Lb("GEWINN", 1200, Eu(lastWin), lastWin > 0 ? C.Green : C.Dim); Gfx.Text(c, $"Gesamteinsatz {Eu(TotalBet)}", 1200, 808, 20, C.Dim, Al.C, false);
        Gfx.Text(c, "Einsatz und Linien mit  -  und  +  ändern", 800, 852, 16, C.Dim.A(.7f), Al.C, false);
    }
    void DrawGamble(SKCanvas c)
    {
        var r = Gfx.R(1190, 108, 385, 470);
        W.Panel(c, r, g.Active ? (gMode == GambleMode.Ladder ? C.Orange : C.Red) : C.Dim);
        if (!g.Active)
        {
            Gfx.Text(c, "RISIKO", r.MidX, r.MidY - 30, 36, C.Dim.A(.6f), Al.C, true, 0, true);
            Gfx.Text(c, "Nach einem Gewinn:", r.MidX, r.MidY + 15, 20, C.Dim.A(.6f), Al.C, false);
            Gfx.Text(c, "Karten oder Risikoleiter", r.MidX, r.MidY + 42, 20, C.Gold.A(.7f), Al.C, true);
            Gfx.Text(c, "um den Gewinn zu steigern!", r.MidX, r.MidY + 68, 18, C.Dim.A(.6f), Al.C, false);
            return;
        }

        if (gMode == GambleMode.Cards)
        {
            Gfx.Text(c, "KARTENRISIKO", r.MidX, r.Top + 54, 22, C.Red.Light(.3f), Al.C, true, 6, true);
            Gfx.Text(c, Eu(g.Amount), r.MidX, r.Top + 88, 36, C.Gold, Al.C, true, 6);
            Gfx.Text(c, $"Stufe {g.Steps}/5  -  nächste: {Eu(g.Amount * 2)}", r.MidX, r.Top + 118, 18, C.Dim, Al.C, false);
            CardArt.Card(c, r.MidX, 290, 130, g.Rank, g.Suit, g.Flip.V, 0, 0);
            for (int i = 0; i < g.Hist.Count; i++)
            {
                var h = g.Hist[i];
                var cr = Gfx.Ctr(r.Left + 36 + i * 44, 400, 36, 36);
                Gfx.Rect(c, cr, 8, new SKColor(250, 248, 255));
                Gfx.Suit(c, h.suit, cr.MidX, cr.MidY, 9, CardArt.SuitCol(h.suit));
            }
        }
        else
        {
            Gfx.Text(c, "RISIKOLEITER", r.MidX, r.Top + 46, 18, C.Orange.Light(.3f), Al.C, true, 4, true);
            Gfx.Text(c, Eu(g.Amount), r.MidX, r.Top + 74, 30, C.Gold, Al.C, true, 5);

            float lLeft = r.MidX - 110, lRight = r.MidX + 110;
            c.DrawLine(lLeft - 4, 164, lLeft - 4, 432, Gfx.Line(C.Gold.A(.5f), 4));
            c.DrawLine(lRight + 4, 164, lRight + 4, 432, Gfx.Line(C.Gold.A(.5f), 4));

            int stepUp = Math.Min(11, ladderStep + 1);
            int stepDown = Math.Max(0, ladderStep - 1);
            bool flashPhase = ((int)(Time * 8) % 2) == 0;

            for (int i = 11; i >= 0; i--)
            {
                float ry = 178 + (11 - i) * 21;
                var rungRect = Gfx.R(lLeft, ry, 220, 19);
                long val = LadderVal(i);
                bool isCurrent = (i == ladderStep && !g.Busy);
                bool isBlinkUp = (i == stepUp && flashPhase && CanLadderStep && !g.Busy);
                bool isBlinkDown = (i == stepDown && !flashPhase && CanLadderStep && !g.Busy);
                bool isTop = (i == 11);

                if (isCurrent)
                {
                    Gfx.Glow(c, rungRect, 8, C.Green, 10, .8f);
                    Gfx.RectGrad(c, rungRect, 6, C.Green.Light(.4f), C.Green.Dark(.3f));
                    Gfx.Stroke(c, rungRect, 6, SKColors.White, 2);
                    Gfx.Text(c, $"▶  {Eu(val)}  ◀", rungRect.MidX, rungRect.MidY, 15, SKColors.White, Al.C, true, 4);
                }
                else if (isBlinkUp)
                {
                    Gfx.Glow(c, rungRect, 10, C.Gold, 12, .9f);
                    Gfx.RectGrad(c, rungRect, 6, new SKColor(255, 230, 80), new SKColor(210, 150, 20));
                    Gfx.Stroke(c, rungRect, 6, SKColors.White, 2);
                    Gfx.Text(c, isTop ? $"★ {Eu(val)} ★" : Eu(val), rungRect.MidX, rungRect.MidY, 15, new SKColor(40, 20, 0), Al.C, true, 4);
                }
                else if (isBlinkDown)
                {
                    Gfx.Glow(c, rungRect, 8, C.Orange, 10, .8f);
                    Gfx.RectGrad(c, rungRect, 6, new SKColor(255, 140, 40), new SKColor(180, 60, 10));
                    Gfx.Stroke(c, rungRect, 6, SKColors.White, 2);
                    Gfx.Text(c, Eu(val), rungRect.MidX, rungRect.MidY, 15, SKColors.White, Al.C, true, 4);
                }
                else
                {
                    SKColor bg = isTop ? new SKColor(60, 48, 12) : i == 0 ? new SKColor(40, 14, 14) : new SKColor(24, 20, 36);
                    SKColor border = isTop ? C.Gold.A(.5f) : i == 0 ? C.Red.A(.3f) : SKColors.White.A(.12f);
                    SKColor txtCol = isTop ? C.Gold : i == 0 ? C.Red.Light(.3f) : SKColors.White.A(.65f);
                    Gfx.Rect(c, rungRect, 6, bg);
                    Gfx.Stroke(c, rungRect, 6, border, 1);
                    Gfx.Text(c, isTop ? $"★ {Eu(val)} ★" : Eu(val), rungRect.MidX, rungRect.MidY, 14, txtCol, Al.C, isTop);
                }
            }
        }
    }
}
