using System.Numerics;
using SkiaSharp;

namespace GlamourGames;

static class App
{
    public const float VW = 1600, VH = 900;
    public const string Version = "1.8.1", Credit = "erstellt von Michael Bergfeld @ DoMeZos-Ware 2026";
    public static float VX0, VX1, VY0, VY1, MX, MY;
    static float scale = 1, ox, oy;
    static Scene? cur, pending;
    static float fade, flash, shake, toastT, fps, fpsAcc;
    static int fpsN;
    static SKColor flashCol = SKColors.White;
    static string toast = "";
    static bool initialized;

    public static Scene? Current => cur;
    public static int StartGame = -1, TestModal;
    public static Action<bool> Keyboard; static bool kbWant;
    public static void TypeChar(char ch) { cur?.Modal?.Char(ch); }

    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;
        Log.I("Android load");
        Haptics.Init();
        Sfx.Init();
        cur = StartGame >= 0 && StartGame < Registry.All.Count ? Registry.All[StartGame].Make() : new Menu();
        cur.Enter();
        if (TestModal == 2) cur.DebugWin();
        if (TestModal == 1) cur.NameEntry("snake", 100, "Neuer Rekord!", null);
        fade = 1;
    }

    public static void Go(Scene s)
    {
        if (pending != null) return;
        pending = s;
        Log.I("go " + s.GetType().Name);
    }

    public static void Flash(SKColor c, float a = .5f) { flash = a; flashCol = c; }
    public static void Shake(float a = 14) { shake = Math.Max(shake, a); }
    public static void Toast(string t) { toast = t; toastT = 2.4f; }

    public static void Resize(int width, int height)
    {
        scale = Math.Min(width / VW, height / VH);
        ox = (width - VW * scale) / 2f;
        oy = (height - VH * scale) / 2f;
        VX0 = -ox / scale; VX1 = (width - ox) / scale;
        VY0 = -oy / scale; VY1 = (height - oy) / scale;
    }

    static Vector2 ToV(float x, float y)
        => new((x - ox) / scale, (y - oy) / scale);

    public static void TouchMove(float x, float y)
    {
        var v = ToV(x, y); MX = v.X; MY = v.Y;
        if (cur == null) return;
        if (cur.Modal != null) cur.Modal.Move(MX, MY);
        else { cur.Ui.Move(MX, MY); cur.MouseMove(MX, MY); }
    }

    public static void TouchDown(float x, float y)
    {
        TouchMove(x, y);
        if (cur == null || pending != null) return;
        if (cur.Modal != null) { if (cur.Modal.Input && Gfx.Inflate(cur.Modal.FieldRect, 20).Contains(MX, MY)) Keyboard?.Invoke(true); cur.Modal.MDown(MX, MY); return; }
        if (OnMute(MX, MY)) { Sfx.ToggleMute(); return; }
        if (OnMusic(MX, MY)) { Toast(Sfx.NextTrack()); Sfx.Play(S.Chip); return; }
        if (!cur.Ui.Down(MX, MY)) cur.MouseDown(MX, MY);
    }

    public static void TouchUp(float x, float y)
    {
        TouchMove(x, y);
        if (cur == null || pending != null) return;
        if (cur.Modal != null) { cur.Modal.MUp(MX, MY); return; }
        if (!cur.Ui.Up(MX, MY)) cur.MouseUp(MX, MY);
    }

    public static void Back()
    {
        if (cur == null) return;
        if (cur.Modal != null) { cur.Modal.Cancel?.Invoke(); return; }
        if (!cur.Escape()) { if (cur is Menu) return; Go(new Menu()); }
    }

    public static void Key(Silk.NET.Input.Key k)
    {
        if (cur == null) return;
        if (cur.Modal != null)
        {
            if (k == Silk.NET.Input.Key.Enter || k == Silk.NET.Input.Key.KeypadEnter) cur.Modal.Enter();
            else if (k == Silk.NET.Input.Key.Backspace) cur.Modal.Back();
            else if (k == Silk.NET.Input.Key.Escape) cur.Modal.Cancel?.Invoke();
            return;
        }
        if (k == Silk.NET.Input.Key.M) { Sfx.ToggleMute(); return; }
        if (k == Silk.NET.Input.Key.N) { Toast(Sfx.NextTrack()); return; }
        if (k == Silk.NET.Input.Key.Escape) { Back(); return; }
        cur.KeyDown(k);
    }

    static bool OnMusic(float x, float y) => cur != null && cur.Chrome && x > 1425 && x < 1515 && y > 0 && y < 104;
    static bool OnMute(float x, float y) => cur != null && cur.Chrome && x > 1516 && x < 1600 && y > 0 && y < 104;

    public static void Draw(SKCanvas c, int width, int height, float dt)
    {
        Initialize();
        Resize(width, height);
        bool kw = cur?.Modal?.Input == true; if (kw != kbWant) { kbWant = kw; Keyboard?.Invoke(kw); }
        dt = Math.Clamp(dt, 0, .05f);

        if (pending != null)
        {
            fade += dt / .16f;
            if (fade >= 1)
            {
                fade = 1;
                cur?.Leave();
                cur = pending;
                pending = null;
                cur.Enter();
            }
        }
        else if (fade > 0) fade = Math.Max(0, fade - dt / .3f);

        shake = Math.Max(0, shake - dt * 40);
        c.Clear(C.Bg);
        c.Save();
        c.Translate(ox, oy);
        c.Scale(scale);
        if (shake > 0)
            c.Translate((Random.Shared.NextSingle() - .5f) * shake, (Random.Shared.NextSingle() - .5f) * shake);

        cur?.BaseUpdate(dt);
        cur?.BaseDraw(c);

        if (cur?.Chrome == true) DrawMute(c);

        if (flash > 0)
        {
            c.DrawRect(VX0, VY0, VX1 - VX0, VY1 - VY0, Gfx.Fill(flashCol.A(flash)));
            flash = Math.Max(0, flash - dt * 1.8f);
        }

        if (toastT > 0)
        {
            toastT -= dt;
            float a = Ease.Clamp(Math.Min(toastT, .3f) / .3f);
            var r = Gfx.Ctr(800, 840 - (1 - a) * 20, Gfx.TW(toast, 26) + 60, 52);
            Gfx.Rect(c, r, 26, C.Panel.A(.92f * a));
            Gfx.Stroke(c, r, 26, C.Cyan.A(a), 2);
            Gfx.Text(c, toast, r.MidX, r.MidY, 26, SKColors.White.A(a));
        }

        fpsAcc += dt; fpsN++;
        if (fpsAcc > .5f) { fps = fpsN / fpsAcc; fpsAcc = 0; fpsN = 0; }
        c.Restore();

        if (fade > 0)
        {
            c.DrawRect(0, 0, width, height, Gfx.Fill(SKColors.Black.A(Ease.InCubic(fade))));
        }
    }

    static void DrawMusic(SKCanvas c)
    {
        bool h = OnMusic(MX, MY), on = Sfx.Track >= 0 && !Sfx.Muted;
        var r = Gfx.Ctr(1480, 50, 52, 52);
        Gfx.Rect(c, r, 26, C.Panel.A(h ? .95f : .7f));
        Gfx.Stroke(c, r, 26, (on ? C.Pink : C.Dim).A(h ? 1 : .7f), 2);
        var col = on ? SKColors.White : C.Dim;
        c.DrawCircle(1474, 61, 5.5f, Gfx.Fill(col));
        c.DrawCircle(1489, 58, 5.5f, Gfx.Fill(col));
        c.DrawLine(1479, 60, 1479, 38, Gfx.Line(col, 3));
        c.DrawLine(1494, 57, 1494, 35, Gfx.Line(col, 3));
        c.DrawLine(1479, 38, 1494, 35, Gfx.Line(col, 4));
        if (Sfx.Track < 0) c.DrawLine(1462, 66, 1498, 34, Gfx.Line(C.Red, 3));
        else Gfx.Text(c, (Sfx.Track + 1).ToString(), 1480, 74, 15, on ? C.Pink.Light(.4f) : C.Dim);
    }

    static void DrawMute(SKCanvas c)
    {
        DrawMusic(c);
        bool h = OnMute(MX, MY); var r = Gfx.Ctr(1550, 50, 52, 52);
        Gfx.Rect(c, r, 26, C.Panel.A(h ? .95f : .7f));
        Gfx.Stroke(c, r, 26, (Sfx.Muted ? C.Dim : C.Cyan).A(h ? 1 : .7f), 2);
        var col = Sfx.Muted ? C.Dim : SKColors.White;
        using var p = new SKPath();
        p.MoveTo(1537, 44); p.LineTo(1544, 44); p.LineTo(1553, 37);
        p.LineTo(1553, 63); p.LineTo(1544, 56); p.LineTo(1537, 56); p.Close();
        c.DrawPath(p, Gfx.Fill(col));
        if (Sfx.Muted)
        {
            c.DrawLine(1560, 43, 1570, 57, Gfx.Line(C.Red, 3));
            c.DrawLine(1570, 43, 1560, 57, Gfx.Line(C.Red, 3));
        }
    }
}