using SkiaSharp;
using Silk.NET.Input;
namespace GlamourGames;
class Poker : Scene
{
    public override string Title => "Texas Hold'em";
    public override SKColor Acc1 => C.Magenta; public override SKColor Acc2 => C.Cyan;
    static readonly (int sb, int bb)[] Blinds = { (10, 20), (20, 40), (30, 60), (50, 100), (75, 150), (100, 200), (150, 300), (200, 400), (300, 600), (500, 1000) };
    const int PerLevel = 8, Start = 1000;
    static readonly string[] Cat = { "Höchste Karte", "Paar", "Zwei Paare", "Drilling", "Straße", "Flush", "Full House", "Vierling", "Straight Flush" };
    struct Cd { public int R, S; }
    class P { public string Name; public bool Human; public int Chips, Bet, Total; public List<Cd> Hole = new(); public bool Folded, AllIn, Out, Acted, CanRaise; public string LastAct = "", HandName = ""; public Spring[] Flip = { new(0) { K = 170, D = 20 }, new(0) { K = 170, D = 20 } }; }
    P[] pl; List<Cd> deck = new(), board = new(); int button, handNo, curBet, lastRaise, toAct = -1, sbI, bbI, sb = 10, bb = 20; string street = ""; bool inHand, gameOver, showAll; int revealed = -1; List<int> winners = new(); float handT;
    (string act, int to)? pending; (string t, string s)? overlay; bool overlayDone; readonly List<string> log = new(); string msg = ""; Button bNext, bNew, bFold, bCall, bRaise, bMin, bHalf, bPot, bAll, bShow;
    readonly Spring[] bflip = Enumerable.Range(0, 5).Select(_ => new Spring(0) { K = 170, D = 20 }).ToArray(); readonly float[] bt = new float[5];
    int sliderMin, sliderMax, sliderVal; bool drag; static readonly SKRect Track = Gfx.R(850, 748, 300, 14);
    static readonly SKPoint[] Seat = { new(340, 545), new(1060, 545), new(700, 215) };
    static readonly Random Rng = new();
    public override void Enter()
    {
        base.Enter();
        bNew = Ui.Add(new Button(40, 790, 230, 56, "Neues Spiel", C.Purple, () => NewGame(), 22)); bNext = Ui.Add(new Button(560, 785, 280, 72, "Nächste Hand", C.Green, () => { Co.Start(HandCo()); }, 30));
        bFold = Ui.Add(new Button(330, 792, 190, 66, "FOLD", C.Red, () => pending = ("fold", 0), 28)); bCall = Ui.Add(new Button(540, 792, 250, 66, "CHECK", C.Cyan, () => pending = ("call", 0), 28)); bRaise = Ui.Add(new Button(810, 792, 300, 66, "RAISE", C.Gold, () => pending = ("raise", sliderVal), 26));
        bMin = Ui.Add(new Button(330, 738, 90, 40, "Min", C.Purple, () => Quick("min"), 18)); bHalf = Ui.Add(new Button(430, 738, 100, 40, "½ Pot", C.Purple, () => Quick("half"), 18)); bPot = Ui.Add(new Button(540, 738, 90, 40, "Pot", C.Purple, () => Quick("pot"), 18)); bAll = Ui.Add(new Button(640, 738, 110, 40, "All-In", C.Orange, () => Quick("all"), 18));
        bShow = Ui.Add(new Button(600, 600, 400, 80, "Karten zeigen", C.Green, () => overlayDone = true, 32) { Visible = false });
        NewGame();
    }
    void NewGame()
    {
        Co.Clear(); pending = null; overlay = null; pl = new[] { Mk(Pl.Name(0), true), Mk(Pl.Name(1), true), Mk("Computer", false) }; button = Rng.Next(3); handNo = 0; board.Clear(); inHand = false; gameOver = false; revealed = -1; winners.Clear(); showAll = false; toAct = -1; log.Clear(); Modal = null;
        Lg($"Neues Spiel - jeder startet mit {Start} Chips."); msg = "Neues Spiel! \"Nächste Hand\" drücken.";
    }
    static P Mk(string n, bool h) => new() { Name = n, Human = h, Chips = Start };
    void Lg(string t) { log.Insert(0, t); if (log.Count > 40) log.RemoveAt(40); }
    static string RS(int r) => r switch { 11 => "J", 12 => "Q", 13 => "K", 14 => "A", _ => r.ToString() };
    static string CT(Cd c) => RS(c.R) + "♠♥♦♣"[c.S];
    int Next(int i, Func<P, bool> f) { for (int k = 1; k <= 3; k++) { int j = (i + k) % 3; if (f(pl[j])) return j; } return -1; }
    int Pot => pl.Sum(p => p.Total); List<P> ActiveIn => pl.Where(p => !p.Out && !p.Folded).ToList();
    List<Cd> NewDeck() { var d = new List<Cd>(); for (int s = 0; s < 4; s++) for (int r = 2; r <= 14; r++) d.Add(new Cd { R = r, S = s }); return d.OrderBy(_ => Rng.Next()).ToList(); }
    Cd Draw() { var c = deck[^1]; deck.RemoveAt(deck.Count - 1); return c; }
    // ---- Bewertung ----
    static int StraightHigh(bool[] has) { for (int h = 14; h >= 5; h--) { bool ok = true; for (int k = 0; k < 5; k++) { int r = h - k == 1 ? 14 : h - k; if (!has[r]) { ok = false; break; } } if (ok) return h; } return 0; }
    static (long score, int cat, string name) Eval(IEnumerable<Cd> cards)
    {
        var rc = new int[15]; var sc = new List<int>[4] { new(), new(), new(), new() }; var has = new bool[15];
        foreach (var c in cards) { rc[c.R]++; sc[c.S].Add(c.R); has[c.R] = true; }
        int[] res = null; int fs = Array.FindIndex(sc, a => a.Count >= 5);
        if (fs >= 0) { var fh = new bool[15]; sc[fs].ForEach(r => fh[r] = true); int sf = StraightHigh(fh); if (sf > 0) res = new[] { 8, sf }; }
        var quads = new List<int>(); var trips = new List<int>(); var pairs = new List<int>();
        for (int r = 14; r >= 2; r--) { if (rc[r] == 4) quads.Add(r); else if (rc[r] == 3) trips.Add(r); else if (rc[r] == 2) pairs.Add(r); }
        int[] Kick(int[] ex, int n) { var o = new List<int>(); for (int r = 14; r >= 2 && o.Count < n; r--) if (rc[r] > 0 && !ex.Contains(r)) o.Add(r); return o.ToArray(); }
        if (res == null && quads.Count > 0) res = new[] { 7, quads[0] }.Concat(Kick(new[] { quads[0] }, 1)).ToArray();
        if (res == null && trips.Count > 0 && (trips.Count > 1 || pairs.Count > 0)) { int pp = Math.Max(trips.Count > 1 ? trips[1] : 0, pairs.Count > 0 ? pairs[0] : 0); res = new[] { 6, trips[0], pp }; }
        if (res == null && fs >= 0) res = new[] { 5 }.Concat(sc[fs].OrderByDescending(x => x).Take(5)).ToArray();
        if (res == null) { int st = StraightHigh(has); if (st > 0) res = new[] { 4, st }; }
        if (res == null && trips.Count > 0) res = new[] { 3, trips[0] }.Concat(Kick(new[] { trips[0] }, 2)).ToArray();
        if (res == null && pairs.Count >= 2) res = new[] { 2, pairs[0], pairs[1] }.Concat(Kick(new[] { pairs[0], pairs[1] }, 1)).ToArray();
        if (res == null && pairs.Count == 1) res = new[] { 1, pairs[0] }.Concat(Kick(new[] { pairs[0] }, 3)).ToArray();
        res ??= new[] { 0 }.Concat(Kick(new int[0], 5)).ToArray();
        long score = 0; for (int i = 0; i < 6; i++) score = score * 15 + (i < res.Length ? res[i] : 0);
        string name = Cat[res[0]];
        if (res[0] == 8 && res[1] == 14) name = "Royal Flush"; else if (res[0] == 1) name = "Paar " + RS(res[1]); else if (res[0] == 2) name = $"Zwei Paare {RS(res[1])}/{RS(res[2])}"; else if (res[0] == 3) name = "Drilling " + RS(res[1]);
        else if (res[0] == 7) name = "Vierling " + RS(res[1]); else if (res[0] == 6) name = $"Full House {RS(res[1])}/{RS(res[2])}"; else if (res[0] == 4) name = "Straße bis " + RS(res[1]); else if (res[0] == 0) name = "Höchste Karte " + RS(res[1]);
        return (score, res[0], name);
    }
    double Equity(List<Cd> hole, List<Cd> brd, int nOpp, int iters)
    {
        var used = new HashSet<int>(hole.Concat(brd).Select(c => c.R * 4 + c.S)); var rest = new List<Cd>(); for (int s = 0; s < 4; s++) for (int r = 2; r <= 14; r++) if (!used.Contains(r * 4 + s)) rest.Add(new Cd { R = r, S = s });
        double win = 0;
        for (int it = 0; it < iters; it++)
        {
            int need = 5 - brd.Count + nOpp * 2; for (int i = 0; i < need; i++) { int j = i + Rng.Next(rest.Count - i); (rest[i], rest[j]) = (rest[j], rest[i]); }
            var b = brd.Concat(rest.Take(5 - brd.Count)).ToList(); long my = Eval(hole.Concat(b)).score; int off = 5 - brd.Count, ties = 0; bool lose = false;
            for (int o = 0; o < nOpp; o++) { long sc = Eval(new[] { rest[off], rest[off + 1] }.Concat(b)).score; off += 2; if (sc > my) { lose = true; break; } if (sc == my) ties++; }
            if (!lose) win += ties > 0 ? 1.0 / (ties + 1) : 1;
        }
        return win / iters;
    }
    // ---- Regeln ----
    (int toCall, int minTo, int maxTo, bool canRaise) Legal(int i)
    {
        var p = pl[i]; int toCall = Math.Max(0, curBet - p.Bet), maxTo = p.Bet + p.Chips, minTo = curBet == 0 ? Math.Min(maxTo, bb) : Math.Min(maxTo, curBet + lastRaise);
        int others = pl.Where((q, j) => j != i && !q.Out && !q.Folded && !q.AllIn).Count(); return (Math.Min(toCall, p.Chips), minTo, maxTo, p.CanRaise && maxTo > curBet && others > 0);
    }
    void Post(int i, int amt, string label) { var p = pl[i]; int a = Math.Min(amt, p.Chips); p.Chips -= a; p.Bet += a; p.Total += a; if (p.Chips == 0) p.AllIn = true; p.LastAct = $"{label} {a}"; }
    bool RoundDone()
    {
        var live = ActiveIn; if (live.Count <= 1) return true;
        foreach (var p in live.Where(p => !p.AllIn)) if (!p.Acted || p.Bet < curBet) return false; return true;
    }
    void Apply(int i, string act, int to)
    {
        var p = pl[i]; var L = Legal(i);
        if (act == "fold") { p.Folded = true; p.LastAct = "Fold"; Lg($"{p.Name}: Fold"); }
        else if (act == "call") { int a = L.toCall; p.Chips -= a; p.Bet += a; p.Total += a; if (p.Chips == 0) p.AllIn = true; p.LastAct = a == 0 ? "Check" : p.AllIn ? "All-In " + p.Bet : "Call " + a; Lg($"{p.Name}: {p.LastAct}"); Sfx.Play(a == 0 ? S.Tick : S.Chip); }
        else
        {
            to = Math.Max(L.minTo, Math.Min(L.maxTo, to)); int add = to - p.Bet; p.Chips -= add; p.Bet = to; p.Total += add; if (p.Chips == 0) p.AllIn = true;
            int raiseSize = to - curBet; bool full = raiseSize >= lastRaise || (curBet == 0 && to >= bb); bool wasBet = curBet == 0;
            if (to > curBet) { for (int j = 0; j < 3; j++) { var q = pl[j]; if (j != i && !q.Folded && !q.AllIn && !q.Out) { if (!full && q.Acted) q.CanRaise = false; else if (full) q.CanRaise = true; q.Acted = false; } } if (full) lastRaise = raiseSize; curBet = to; }
            p.LastAct = (p.AllIn ? "All-In " : wasBet ? "Bet " : "Raise auf ") + to; Lg($"{p.Name}: {p.LastAct}"); Sfx.Play(S.Chip); Sfx.Play(S.Chip, .7f, 1.2f);
        }
        p.Acted = true; toAct = Next(i, q => !q.Out && !q.Folded && !q.AllIn); if (toAct < 0) toAct = i;
    }
    void AiAct(int i)
    {
        var p = pl[i]; var L = Legal(i); int nOpp = Math.Max(1, ActiveIn.Count - 1); double eq = Equity(p.Hole, board, nOpp, board.Count > 0 ? 350 : 250); int pot = Pot; double potOdds = L.toCall / (double)(pot + L.toCall == 0 ? 1 : pot + L.toCall), r = Rng.NextDouble();
        int BetSize(double f) => curBet + Math.Max(bb, (int)Math.Round((pot + L.toCall) * f)); double fair = 1.0 / (nOpp + 1), edge = eq - fair; string act = "call"; int to = 0;
        if (L.toCall == 0)
        {
            if (edge > .28 && L.canRaise) { act = "raise"; to = r < .25 ? (eq > .85 ? L.maxTo : BetSize(.75)) : BetSize(.5 + Rng.NextDouble() * .4); }
            else if (edge > .1 && L.canRaise && r < .55) { act = "raise"; to = BetSize(.4 + Rng.NextDouble() * .3); }
            else if (L.canRaise && r < .09) { act = "raise"; to = BetSize(.5); }
        }
        else
        {
            if (edge > .32 && L.canRaise && r < .7) { act = "raise"; to = eq > .85 && r < .3 ? L.maxTo : BetSize(.7 + Rng.NextDouble() * .5); }
            else if (eq >= potOdds + .04 || (edge > .05 && L.toCall <= bb * 2)) act = "call";
            else if (L.canRaise && r < .04) { act = "raise"; to = BetSize(.8); }
            else if (eq >= potOdds - .05 && r < .3) act = "call"; else act = "fold";
        }
        if (act == "raise") { int st = bb >= 20 ? 10 : 1; to = Math.Max(L.minTo, Math.Min(L.maxTo, (int)Math.Round(to / (double)st) * st)); if (to <= curBet) act = "call"; }
        if (act == "fold" && L.toCall == 0) act = "call"; Apply(i, act, to);
    }
    // ---- Ablauf ----
    IEnumerator<object> HandCo()
    {
        if (inHand) yield break; foreach (var p in pl) if (p.Chips <= 0) p.Out = true; var alive = pl.Where(p => p.Chips > 0).ToList(); if (alive.Count < 2) { GameOver(); yield break; }
        handNo++; int lvl = Math.Min(Blinds.Length - 1, (handNo - 1) / PerLevel); (sb, bb) = Blinds[lvl]; if ((handNo - 1) % PerLevel == 0 && handNo > 1) Lg($"Blinds steigen auf {sb}/{bb}.");
        foreach (var p in pl) { p.Hole.Clear(); p.Bet = p.Total = 0; p.Folded = p.Out; p.AllIn = p.Acted = false; p.CanRaise = true; p.LastAct = p.HandName = ""; p.Flip[0].Snap(0); p.Flip[1].Snap(0); }
        button = Next(button, p => !p.Out); bool heads = alive.Count == 2; sbI = heads ? button : Next(button, p => !p.Out); bbI = Next(sbI, p => !p.Out);
        deck = NewDeck(); board.Clear(); for (int k = 0; k < 5; k++) { bflip[k].Snap(0); bt[k] = 0; } winners.Clear(); revealed = -1; showAll = false; inHand = true; street = "preflop"; handT = 0;
        Post(sbI, sb, "SB"); Post(bbI, bb, "BB"); curBet = bb; lastRaise = bb; for (int k = 0; k < 2; k++) foreach (var p in pl) if (!p.Out) p.Hole.Add(Draw());
        Lg($"- Hand {handNo} - Blinds {sb}/{bb} - Dealer: {pl[button].Name}"); Sfx.Play(S.Deal); Sfx.Play(S.Chip); toAct = Next(bbI, p => !p.Out && !p.Folded && !p.AllIn); msg = $"Hand {handNo} - Blinds {sb}/{bb}"; yield return .9f;
        bool skip = false;
        while (true)
        {
            if (ActiveIn.Count <= 1) { EndFold(); yield break; }
            if (skip || RoundDone())
            {
                skip = false; foreach (var p in pl) { p.Bet = 0; p.Acted = false; p.CanRaise = true; if (!p.Folded && !p.AllIn) p.LastAct = ""; } curBet = 0; lastRaise = bb;
                if (street == "river") { Showdown(); yield break; }
                if (street == "preflop") { draw(3); street = "flop"; } else if (street == "flop") { draw(1); street = "turn"; } else { draw(1); street = "river"; }
                Lg($"{street.ToUpper()}: {string.Join(" ", board.Select(CT))}"); Sfx.Play(S.Flip);
                if (ActiveIn.Count(p => !p.AllIn) <= 1) { showAll = true; yield return 1.3f; skip = true; continue; }
                toAct = Next(button, p => !p.Out && !p.Folded && !p.AllIn); yield return .5f; continue;
            }
            if (toAct < 0 || pl[toAct].Folded || pl[toAct].AllIn || pl[toAct].Out) toAct = Next(toAct < 0 ? button : toAct, p => !p.Out && !p.Folded && !p.AllIn);
            var cur = pl[toAct];
            if (cur.Human)
            {
                var humans = pl.Where(q => q.Human && !q.Out && !q.Folded).ToList();
                if (humans.Count > 1 || revealed != toAct) { revealed = -1; overlay = (cur.Name + " ist am Zug", "Gerät übergeben - dann Karten zeigen"); overlayDone = false; msg = ""; yield return (Func<bool>)(() => overlayDone); overlay = null; revealed = toAct; }
                SetupRaise(); pending = null; msg = $"{cur.Name} ist dran - zu zahlen: {Legal(toAct).toCall}"; Sfx.Play(S.Turn, .5f);
                yield return (Func<bool>)(() => pending != null); var (a, to) = pending.Value; pending = null; Apply(toAct, a, to);
            }
            else { revealed = -1; msg = "Computer überlegt ..."; yield return .7f + (float)Rng.NextDouble() * .7f; AiAct(toAct); }
            yield return .35f;
        }
        void draw(int n) { deck.RemoveAt(deck.Count - 1); for (int k = 0; k < n; k++) board.Add(Draw()); }
    }
    void SetupRaise() { var L = Legal(toAct); sliderMin = L.minTo; sliderMax = L.maxTo; sliderVal = L.minTo; }
    void Quick(string k)
    {
        if (toAct < 0 || !pl[toAct].Human || !inHand) return; var L = Legal(toAct); int pot = Pot + L.toCall, to = k switch { "min" => L.minTo, "all" => L.maxTo, "half" => curBet + (int)Math.Round(pot / 2.0), _ => curBet + pot };
        sliderVal = Math.Clamp(to, L.minTo, L.maxTo); Sfx.Play(S.Chip);
    }
    void EndFold()
    {
        var w = ActiveIn[0]; int pot = Pot; w.Chips += pot; winners = new() { Array.IndexOf(pl, w) }; Lg($"{w.Name} gewinnt {pot} (alle anderen gefoldet)."); msg = $"{w.Name} gewinnt {pot}!"; Finish();
    }
    void Showdown()
    {
        showAll = true; street = "showdown"; var live = ActiveIn; foreach (var p in live) p.HandName = Eval(p.Hole.Concat(board)).name;
        var levels = pl.Select(p => p.Total).Where(c => c > 0).Distinct().OrderBy(x => x).ToList(); int prev = 0; var pots = new List<(int amt, List<int> elig)>();
        foreach (var lv in levels)
        {
            int amt = 0; var elig = new List<int>(); for (int j = 0; j < 3; j++) { var p = pl[j]; int c = Math.Min(p.Total, lv) - Math.Min(p.Total, prev); if (c > 0) amt += c; if (!p.Folded && !p.Out && p.Total >= lv) elig.Add(j); }
            if (amt > 0) { if (elig.Count > 0) pots.Add((amt, elig)); else if (pots.Count > 0) pots[^1] = (pots[^1].amt + amt, pots[^1].elig); }
            prev = lv;
        }
        var scores = new Dictionary<int, long>(); foreach (var p in live) scores[Array.IndexOf(pl, p)] = Eval(p.Hole.Concat(board)).score; var msgs = new List<string>(); var ws = new HashSet<int>();
        for (int k = 0; k < pots.Count; k++)
        {
            var pt = pots[k]; long best = pt.elig.Max(j => scores.GetValueOrDefault(j, -1)); var w = pt.elig.Where(j => scores.GetValueOrDefault(j, -1) == best).ToList(); int share = pt.amt / w.Count, rest = pt.amt - share * w.Count;
            foreach (var j in w) { pl[j].Chips += share; ws.Add(j); } if (rest > 0) pl[new[] { 1, 2, 3 }.Select(d => (button + d) % 3).First(j => w.Contains(j))].Chips += rest;
            string label = pots.Count > 1 ? (k == 0 ? "Hauptpot" : $"Side-Pot {k}") : "Pot"; msgs.Add($"{label} {pt.amt}: {string.Join(" & ", w.Select(j => $"{pl[j].Name} ({pl[j].HandName})"))}");
        }
        winners = ws.ToList(); foreach (var p in live) Lg($"{p.Name}: {string.Join(" ", p.Hole.Select(CT))} - {p.HandName}"); msgs.ForEach(Lg); msg = string.Join(" - ", msgs); Finish();
    }
    void Finish()
    {
        inHand = false; toAct = -1; foreach (var p in pl) { p.Total = 0; p.Bet = 0; }
        if (winners.Any(j => pl[j].Human)) { Sfx.Play(S.Win); Celebrate(C.Gold, 3, .7f); } else Sfx.Play(S.Lose);
        foreach (var j in winners) { var s = Seat[j]; Fx.Burst(s.X, s.Y, 40, new[] { C.Gold, C.Yellow, C.White }, 380); Pop("GEWINNER", s.X, s.Y - 100, C.Gold, 40); }
        foreach (var p in pl) if (p.Chips <= 0 && !p.Out) { p.Out = true; Lg($"{p.Name} ist ausgeschieden."); }
        if (pl.Count(p => !p.Out) < 2) Tm.After(1.4f, GameOver);
    }
    void GameOver()
    {
        var w = pl.FirstOrDefault(p => !p.Out); gameOver = true; Sfx.Play(S.Big); Celebrate(w != null && w.Human ? C.Gold : C.Red, 5, 1.2f, $"{w?.Name} gewinnt!"); Lg($"TURNIERSIEG: {w?.Name}");
        Tm.After(3f, () => Result($"{w?.Name} gewinnt das Turnier!", $"nach {handNo} Händen", w != null && w.Human ? C.Gold : C.Red, ("Neues Spiel", C.Green, NewGame), ("Menü", C.Purple, () => App.Go(new Menu()))));
    }
    // ---- Eingabe ----
    bool HumanTurn => inHand && toAct >= 0 && pl[toAct].Human && pending == null && overlay == null && pl[toAct] == pl[toAct] && waitingHuman;
    bool waitingHuman => Co.Busy && revealed == toAct && toAct >= 0;
    public override void MouseDown(float x, float y) { if (HumanTurn && Gfx.Inflate(Track, 52).Contains(x, y)) { drag = true; SetSlider(x); } }
    public override void MouseMove(float x, float y) { if (drag) SetSlider(x); }
    public override void MouseUp(float x, float y) { drag = false; }
    void SetSlider(float x)
    {
        float t = Ease.Clamp((x - Track.Left) / Track.Width); int v = (int)Math.Round(sliderMin + (sliderMax - sliderMin) * t); int st = bb >= 20 ? 10 : 1; if (v > sliderMin && v < sliderMax) v = (int)Math.Round(v / (double)st) * st; sliderVal = Math.Clamp(v, sliderMin, sliderMax);
    }
    public override void KeyDown(Key k)
    {
        if (!HumanTurn) { if (k == Key.Space && bNext.Visible) Co.Start(HandCo()); return; }
        if (k == Key.F) pending = ("fold", 0); else if (k == Key.C || k == Key.Space) pending = ("call", 0);
    }
    public override void Update(float dt)
    {
        handT += dt; bool ht = HumanTurn; var L = ht ? Legal(toAct) : default;
        bNext.Visible = !inHand && !gameOver && !Co.Busy && overlay == null; bShow.Visible = overlay != null;
        foreach (var b in new[] { bFold, bCall, bRaise, bMin, bHalf, bPot, bAll }) b.Visible = ht;
        if (ht)
        {
            var p = pl[toAct]; bCall.Text = L.toCall == 0 ? "CHECK" : L.toCall >= p.Chips ? $"ALL-IN {p.Chips}" : $"CALL {L.toCall}"; bool ok = L.canRaise && L.maxTo > L.toCall + p.Bet;
            bRaise.Visible = bMin.Visible = bHalf.Visible = bPot.Visible = bAll.Visible = ok; bRaise.Text = curBet == 0 ? $"BET {sliderVal}" : $"RAISE AUF {sliderVal}";
        }
        for (int i = 0; i < 3; i++) for (int k = 0; k < 2; k++) { bool show = pl[i].Hole.Count > k && ((showAll && !pl[i].Folded) || (pl[i].Human && revealed == i)); pl[i].Flip[k].Target = show ? 1 : 0; pl[i].Flip[k].Update(dt); }
        for (int k = 0; k < 5; k++) { bflip[k].Target = k < board.Count ? 1 : 0; bflip[k].Update(dt); if (k < board.Count) bt[k] = Math.Min(1, bt[k] + dt * 3.5f); }
    }
    // ---- Zeichnen ----
    public override void Draw(SKCanvas c)
    {
        var table = Gfx.R(120, 100, 1160, 590); Gfx.Glow(c, table, 290, C.Magenta, 18, .3f); Gfx.RectGrad(c, table, 290, new SKColor(96, 40, 92), new SKColor(40, 14, 42)); var felt = Gfx.Inflate(table, -16);
        using (var sh = SKShader.CreateRadialGradient(new(700, 395), 640, new[] { new SKColor(60, 20, 110), new SKColor(20, 6, 48) }, null, SKShaderTileMode.Clamp)) { var p = Gfx.Fill(SKColors.White); p.Shader = sh; c.DrawRoundRect(felt, 275, 275, p); }
        Gfx.Stroke(c, felt, 275, C.Magenta.A(.6f), 3); Gfx.Stroke(c, Gfx.Inflate(felt, -18), 257, C.Magenta.A(.2f), 2);
        Gfx.Text(c, msg, 700, 118, 24, C.Yellow.Light(.3f), Al.C, true, 6);
        Gfx.Text(c, $"POT  {Pot}", 700, 300, 34, C.Gold, Al.C, true, 10); Gfx.Text(c, $"Hand {handNo}  -  Blinds {sb}/{bb}", 700, 268, 18, C.Dim, Al.C, false);
        for (int k = 0; k < 5; k++)
        {
            float x = 700 + (k - 2) * 108, y = 400; if (k >= board.Count) { Gfx.Stroke(c, Gfx.Ctr(x, y, 92, 129), 10, SKColors.White.A(.15f), 2); continue; }
            float e = Ease.OutBack(bt[k]); c.Save(); c.Translate(x, y); c.Scale(e, e); CardArt.Card(c, 0, 0, 92, RS(board[k].R), board[k].S, bflip[k].V, 0, 0, winners.Count > 0 && Hl(board[k])); c.Restore();
        }
        for (int i = 0; i < 3; i++) DrawSeat(c, i);
        DrawLog(c);
        if (HumanTurn) DrawActionBar(c);
        if (overlay != null) DrawOverlay(c);
    }
    bool Hl(Cd cd) => false;
    static List<string> Wrap(string t, float w, float size)
    {
        var res = new List<string>(); var cur = "";
        foreach (var wd in t.Split(' ')) { var tr = cur.Length == 0 ? wd : cur + " " + wd; if (cur.Length > 0 && Gfx.TW(tr, size, false) > w) { res.Add(cur); cur = wd; } else cur = tr; }
        if (cur.Length > 0) res.Add(cur); return res;
    }
    void DrawSeat(SKCanvas c, int i)
    {
        var p = pl[i]; var s = Seat[i]; bool turn = inHand && toAct == i && !p.Folded; bool win = winners.Contains(i); var col = i == 0 ? C.Cyan : i == 1 ? C.Pink : C.Green; float dim = p.Out ? .3f : p.Folded ? .5f : 1;
        var box = Gfx.Ctr(s.X, s.Y + (i == 2 ? 10 : 24), 330, 200); c.Save(); if (dim < 1) { using var lp = new SKPaint { Color = SKColors.White.WithAlpha((byte)(255 * dim)) }; c.SaveLayer(lp); }
        if (turn || win) Gfx.Glow(c, box, 22, win ? C.Gold : col, 18, .55f + .3f * MathF.Sin(Time * 6)); W.Panel(c, box, win ? C.Gold : col);
        Gfx.Text(c, p.Name + (p.Human ? "" : " (KI)") + (inHand && i == sbI ? "  SB" : "") + (inHand && i == bbI ? "  BB" : ""), s.X, box.Top + 26, 22, turn ? col.Light(.5f) : SKColors.White, Al.C, true, turn ? 6 : 0);
        for (int k = 0; k < 2; k++)
        {
            float x = s.X + (k - .5f) * 92, y = box.Top + 100; if (p.Hole.Count <= k) { Gfx.Stroke(c, Gfx.Ctr(x, y, 84, 118), 8, SKColors.White.A(.12f), 2); continue; }
            float a = Ease.OutBack((handT - k * .15f - i * .08f) / .4f); c.Save(); c.Translate(x, y); c.Scale(a, a); CardArt.Card(c, 0, 0, 84, RS(p.Hole[k].R), p.Hole[k].S, p.Flip[k].V, k == 0 ? -5 : 5, 0, false); c.Restore();
        }
        Gfx.Text(c, p.Out ? "ausgeschieden" : $"{p.Chips} Chips", s.X, box.Bottom - 26, 22, C.Gold, Al.C, true, 4);
        string st = p.HandName.Length > 0 && showAll ? p.HandName : p.AllIn ? "ALL-IN" : p.LastAct; if (st.Length > 0) Gfx.Text(c, st, s.X, box.Bottom + 20, 20, p.Folded ? C.Dim : C.Yellow.Light(.3f), Al.C, false);
        c.Restore();
        if (i == button && !p.Out) { var d = new SKPoint(box.Left + 20, box.Top - 8); c.DrawCircle(d.X, d.Y, 18, Gfx.Fill(SKColors.White)); c.DrawCircle(d.X, d.Y, 18, Gfx.Line(C.Gold, 3)); Gfx.Text(c, "D", d.X, d.Y, 22, SKColors.Black, Al.C); }
        if (p.Bet > 0)
        {
            float bx = 700 + (s.X - 700) * .55f, by = i == 2 ? 330 : 500 - 10; if (i == 2) { bx = 700; by = 330; }
            if (i == 2) by = 335; for (int k = 0; k < Math.Min(5, 1 + p.Bet / 100); k++) Chip(c, bx + 90 * 0 + (i == 2 ? 190 : 0), by - k * 5 + (i == 2 ? -50 : 0), col);
            Gfx.Text(c, p.Bet.ToString(), bx + (i == 2 ? 190 : 0), by + 22 + (i == 2 ? -50 : 0), 20, SKColors.White, Al.C, true, 4);
        }
    }
    static void Chip(SKCanvas c, float x, float y, SKColor col)
    {
        c.DrawOval(x, y + 3, 22, 9, Gfx.Fill(col.Dark(.4f))); c.DrawOval(x, y, 22, 9, Gfx.Fill(col)); c.DrawOval(x, y, 22, 9, Gfx.Line(SKColors.White.A(.8f), 1.5f)); c.DrawOval(x, y, 12, 5, Gfx.Line(SKColors.White.A(.6f), 1.2f));
    }
    void DrawLog(SKCanvas c)
    {
        var r = Gfx.R(1310, 100, 270, 590); W.Panel(c, r, C.Magenta); Gfx.Text(c, "VERLAUF", r.MidX, r.Top + 26, 22, C.Magenta.Light(.4f), Al.C, true, 4);
        c.Save(); c.ClipRect(Gfx.R(r.Left + 8, r.Top + 46, r.Width - 16, r.Height - 56));
        float ly = r.Top + 66; for (int i = 0; i < log.Count && ly < r.Bottom - 20; i++)
            foreach (var ln in Wrap(log[i], r.Width - 28, 16)) { Gfx.Text(c, ln, r.Left + 14, ly, 16, i == 0 ? SKColors.White : C.Dim.A(Math.Max(.4f, 1 - i * .04f)), Al.L, false); ly += 26; }
        c.Restore();
    }
    void DrawActionBar(SKCanvas c)
    {
        var p = pl[toAct]; string hint = board.Count == 0 ? "Starthand " + string.Join(" ", p.Hole.Select(CT)) : Eval(p.Hole.Concat(board)).name;
        var bar = Gfx.R(310, 718, 860, 156); W.Panel(c, bar, C.Gold, 22); Gfx.Text(c, $"{p.Name} - {hint}", 740, 706, 22, C.Gold.Light(.3f), Al.C, true, 5);
        if (Legal(toAct).canRaise) { Gfx.Rect(c, Track, 7, SKColors.White.A(.15f)); float t = sliderMax == sliderMin ? 1 : (sliderVal - sliderMin) / (float)(sliderMax - sliderMin); Gfx.Rect(c, Gfx.R(Track.Left, Track.Top, Track.Width * t, Track.Height), 7, C.Gold); Gfx.Ball(c, Track.Left + Track.Width * t, Track.MidY, 15, C.Gold);
            { float kx = Track.Left + Track.Width * t; Gfx.Radial(c, kx, Track.MidY, 34, C.Gold, .5f); c.DrawCircle(kx, Track.MidY, 20, Gfx.Fill(C.Gold)); c.DrawCircle(kx, Track.MidY, 20, Gfx.Line(SKColors.White, 3)); }
            Gfx.Text(c, (sliderVal >= sliderMax ? "All-In " : "") + sliderVal, 1140, 726, 20, C.Gold, Al.R, true); }
    }
    void DrawOverlay(SKCanvas c)
    {
        c.DrawRect(-2000, -2000, 5600, 4900, Gfx.Fill(new SKColor(6, 1, 15, 235))); var r = Gfx.Ctr(800, 450, 900, 420); Gfx.Glow(c, r, 30, C.Magenta, 26, .5f); W.Panel(c, r, C.Magenta, 30);
        Gfx.Text(c, overlay.Value.t, 800, 330, 60, C.Magenta.Light(.4f), Al.C, true, 20, true); Gfx.Text(c, overlay.Value.s, 800, 430, 28, SKColors.White, Al.C, false); Gfx.Text(c, "Die anderen Spieler schauen weg!", 800, 480, 22, C.Dim, Al.C, false);
    }
}
