using SkiaSharp;
namespace GlamourGames;
class Button
{
    public SKRect R; public string Text; public string Sub; public SKColor Col = C.Cyan; public Action Click; public bool Enabled = true, Visible = true, Selected; public float Size = 26;
    public Spring Hov = new(0) { K = 320, D = 24 }; SKPaint layerP; public float Press, Pulse, Alpha = 1; public bool Round;
    public Func<SKCanvas, SKRect, float, bool> Custom;
    public Button() { }
    public Button(float x, float y, float w, float h, string t, SKColor col, Action a, float size = 26) { R = Gfx.R(x, y, w, h); Text = t; Col = col; Click = a; Size = size; }
    public bool Hit(float x, float y) => Visible && Enabled && R.Contains(x, y);
    public const float MinTouch = 104;
    public bool HitPad(float x, float y)
    {
        if (!Visible || !Enabled) return false;
        float px = Math.Max(0, (MinTouch - R.Width) / 2), py = Math.Max(0, (MinTouch - R.Height) / 2);
        return x >= R.Left - px && x <= R.Right + px && y >= R.Top - py && y <= R.Bottom + py;
    }
    public static Button Pick(IList<Button> items, float x, float y)
    {
        for (int i = items.Count - 1; i >= 0; i--) if (items[i].Hit(x, y)) return items[i];
        Button best = null; float bd = float.MaxValue;
        foreach (var b in items) if (b.HitPad(x, y)) { float d = (b.R.MidX - x) * (b.R.MidX - x) + (b.R.MidY - y) * (b.R.MidY - y); if (d < bd) { bd = d; best = b; } }
        return best;
    }
    public void Update(float dt, bool hot, bool down)
    {
        Hov.Target = hot && Enabled ? 1 : 0; Hov.Update(dt);
        Press = Ease.Lerp(Press, down && hot ? 1 : 0, Math.Min(1, dt * 30)); Pulse += dt;
    }
    public void Draw(SKCanvas c)
    {
        if (!Visible) return;
        if (Alpha < 1) { layerP ??= new SKPaint(); layerP.Color = SKColors.White.A(Alpha); c.SaveLayer(layerP); }
        float sc = 1 + Hov.V * .045f - Press * .06f; var col = Enabled ? Col : new SKColor(90, 85, 110);
        c.Save(); c.Translate(R.MidX, R.MidY); c.Scale(sc, sc); c.Translate(-R.MidX, -R.MidY);
        float rad = Round ? R.Height / 2 : Math.Min(16, R.Height / 2.2f);
        float pulse = Selected ? .5f + .5f * MathF.Sin(Pulse * 5) : 0;
        if (Enabled) Gfx.Glow(c, R, rad, col, 12 + Hov.V * 8, .35f + Hov.V * .35f + pulse * .3f);
        Gfx.RectGrad(c, R, rad, col.Dark(.42f + Hov.V * .15f + pulse * .1f), col.Dark(.16f + Hov.V * .08f));
        var hl = new SKRect(R.Left + 3, R.Top + 2, R.Right - 3, R.Top + R.Height * .5f);
        Gfx.RectGrad(c, hl, rad - 2, SKColors.White.A(.16f), SKColors.White.A(0));
        Gfx.Stroke(c, R, rad, col.A(Enabled ? .7f + Hov.V * .3f + pulse * .3f : .5f), Selected ? 3.5f : 2);
        if (Custom != null) Custom(c, R, Hov.V);
        else if (Sub == null) Gfx.Text(c, Text, R.MidX, R.MidY, Size, Enabled ? SKColors.White : C.Dim, Al.C, true, Enabled ? 3 * Hov.V : 0);
        else { Gfx.Text(c, Text, R.MidX, R.MidY - Size * .32f, Size, SKColors.White); Gfx.Text(c, Sub, R.MidX, R.MidY + Size * .72f, Size * .55f, col.Light(.4f), Al.C, false); }
        c.Restore();
        if (Alpha < 1) c.Restore();
    }
}
class Ui
{
    public readonly List<Button> Items = new(); Button hot, down;
    public bool Hot => hot != null;
    public Button Add(Button b) { Items.Add(b); return b; }
    public void Clear() { Items.Clear(); hot = down = null; }
    public bool Move(float x, float y)
    {
        Button h = Button.Pick(Items, x, y);
        if (h != hot && h != null) Sfx.Play(S.Hover, .25f);
        hot = h; return h != null;
    }
    public bool Down(float x, float y) { Move(x, y); down = hot; return down != null; }
    public bool Up(float x, float y)
    {
        Move(x, y); var d = down; down = null;
        if (d != null && d == hot) { Sfx.Play(S.Click, .6f); Haptics.Tap(); d.Click?.Invoke(); return true; }
        return false;
    }
    public void Update(float dt) { for (int i = 0; i < Items.Count; i++) { var b = Items[i]; b.Update(dt, b == hot, b == down); } }
    public void Draw(SKCanvas c) { foreach (var b in Items) b.Draw(c); }
}
class Modal
{
    public string Title, Sub; public SKColor Col = C.Gold; public readonly List<Button> Btns = new(); Button hot, down;
    public bool Input, Keep, AllowEmpty; public string Text = ""; public int Max = 5; public Action<string> Submit; public Action Cancel;
    public float T; public float W = 760, H = 420; public float CY => Input ? 131 : 450;
    public List<string> Lines = new();
    public Action<SKCanvas, SKRect> Extra;
    public void Update(float dt) { T += dt; foreach (var b in Btns) b.Update(dt, b == hot, b == down); }
    void Layout()
    {
        float n = Btns.Count, bw = Math.Min(260, (W - 60 - (n - 1) * 20) / Math.Max(1, n)), x0 = 800 - (n * bw + (n - 1) * 20) / 2, y = CY + H / 2 - (Input ? 78 : 100);
        for (int i = 0; i < Btns.Count; i++) Btns[i].R = Gfx.R(x0 + i * (bw + 20), y, bw, 64);
    }
    public void Draw(SKCanvas c)
    {
        Layout(); float a = Ease.OutCubic(T / .25f), sc = .8f + .2f * Ease.OutBack(T / .4f);
        c.DrawRect(-2000, -2000, 5600, 4900, Gfx.Fill(SKColors.Black.A(.62f * a)));
        c.Save(); c.Translate(800, CY); c.Scale(sc, sc); c.Translate(-800, -CY);
        var r = Gfx.Ctr(800, CY, W, H);
        Gfx.Glow(c, r, 28, Col, 26, .6f * a); Gfx.RectGrad(c, r, 28, new SKColor(38, 16, 72, 250), new SKColor(12, 4, 28, 252)); Gfx.Stroke(c, r, 28, Col.A(.9f), 3);
        Gfx.Text(c, Title, 800, r.Top + (Input ? 44 : 70), Input ? 44 : 54, Col, Al.C, true, 14, true);
        float y = r.Top + (Input ? 92 : 140);
        if (Sub != null) { Gfx.Text(c, Sub, 800, y, Input ? 26 : 28, SKColors.White, Al.C, false); y += Input ? 34 : 46; }
        foreach (var l in Lines) { Gfx.Text(c, l, 800, y, 24, C.Dim, Al.C, false); y += 36; }
        if (Input)
        {
            var ir = FieldRect; Gfx.Rect(c, ir, 14, SKColors.Black.A(.5f)); Gfx.Stroke(c, ir, 14, C.Cyan, 2.5f);
            string s = Text + (((int)(T * 2)) % 2 == 0 ? "|" : ""); Gfx.Text(c, s, 800, ir.MidY, 40, SKColors.White, Al.C, true, 6);
        }
        Extra?.Invoke(c, r);
        foreach (var b in Btns) b.Draw(c);
        c.Restore();
    }
    public SKRect FieldRect => Gfx.Ctr(800, CY - H / 2 + 150, 380, 62);
    public void Move(float x, float y) { Layout(); hot = Button.Pick(Btns, x, y); }
    public void MDown(float x, float y) { Move(x, y); down = hot; }
    public void MUp(float x, float y) { Move(x, y); var d = down; down = null; if (d != null && d == hot) { Sfx.Play(S.Click, .6f); Haptics.Tap(); d.Click?.Invoke(); } }
    public void Char(char ch) { if (Input && Text.Length < Max && (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || (ch == ' ' && Text.Length > 0))) Text += Keep ? ch : char.ToUpper(ch); }
    public void Back() { if (Input && Text.Length > 0) Text = Text[..^1]; }
    public void Enter() { if (Input) Submit?.Invoke(Text.Trim().Length == 0 && !AllowEmpty ? "ANON" : Text.Trim()); else Btns.FirstOrDefault(b => b.Enabled)?.Click?.Invoke(); }
}
static class W
{
    public static void PlayerBox(SKCanvas c, SKRect r, string name, string big, SKColor col, bool active, float t, string sub = null)
    {
        float pulse = active ? .5f + .5f * MathF.Sin(t * 5) : 0;
        if (active) Gfx.Glow(c, r, 20, col, 18, .45f + .35f * pulse);
        Gfx.RectGrad(c, r, 20, col.Dark(active ? .38f : .16f), new SKColor(10, 3, 24));
        Gfx.Stroke(c, r, 20, col.A(active ? 1 : .4f), active ? 3.5f : 2);
        Gfx.Text(c, name, r.MidX, r.Top + 30, 26, active ? SKColors.White : C.Dim, Al.C, true, active ? 5 : 0);
        Gfx.Text(c, big, r.MidX, r.MidY + 8, Math.Min(72, r.Height * .5f), col.Light(active ? .5f : .1f), Al.C, true, active ? 12 : 0);
        if (sub != null) Gfx.Text(c, sub, r.MidX, r.Bottom - 26, 20, C.Dim, Al.C, false);
    }
    public static void Panel(SKCanvas c, SKRect r, SKColor col, float rad = 22)
    {
        Gfx.RectGrad(c, r, rad, new SKColor(28, 12, 56, 235), new SKColor(10, 4, 24, 240)); Gfx.Stroke(c, r, rad, col.A(.6f), 2);
    }
}
