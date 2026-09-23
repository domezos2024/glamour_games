using SkiaSharp;
namespace GlamourGames;
static class Bloom
{
    struct Fl { public float x, h, size, ph, delay; public int type, ci; }
    struct Pe { public float x, y, sp, sw, ph, size; public int ci; }
    struct Ff { public float x, y, fx, fy, ph, size; }
    static readonly Random R = new(11);
    static readonly Fl[] fl = Enumerable.Range(0, 30).Select(i => new Fl { x = (i + (float)R.NextDouble() * .8f) / 30f, h = 60 + (float)R.NextDouble() * 110, size = 16 + (float)R.NextDouble() * 20, ph = (float)R.NextDouble() * 6.3f, delay = (float)R.NextDouble() * 1.6f, type = R.Next(3), ci = R.Next(6) }).ToArray();
    static readonly Pe[] pe = Enumerable.Range(0, 28).Select(_ => new Pe { x = (float)R.NextDouble(), y = (float)R.NextDouble(), sp = 22 + (float)R.NextDouble() * 34, sw = 20 + (float)R.NextDouble() * 40, ph = (float)R.NextDouble() * 6.3f, size = 5 + (float)R.NextDouble() * 6, ci = R.Next(6) }).ToArray();
    static readonly Ff[] ff = Enumerable.Range(0, 22).Select(_ => new Ff { x = (float)R.NextDouble(), y = .35f + (float)R.NextDouble() * .6f, fx = .1f + (float)R.NextDouble() * .3f, fy = .1f + (float)R.NextDouble() * .4f, ph = (float)R.NextDouble() * 6.3f, size = 2 + (float)R.NextDouble() * 2.5f }).ToArray();
    static readonly float[] blades = Enumerable.Range(0, 150).Select(_ => (float)R.NextDouble()).ToArray();
    static SKColor Pal(int i, SKColor a1, SKColor a2) => i switch { 0 => a1, 1 => a2, 2 => C.Pink, 3 => C.Gold, 4 => C.Purple, _ => C.Magenta };
    static readonly SKPath stemP = new();
    static SKPath hillPath; static SKShader hillSh; static (float, float, float) hillKey;
    static readonly Dictionary<(float, uint), SKShader> auroraSh = new();
    public static void Draw(SKCanvas c, float t, SKColor a1, SKColor a2, float x0, float x1, float y0, float y1)
    {
        float w = x1 - x0, h = y1 - y0, grow = Ease.OutCubic(t / 1.8f);
        DrawAurora(c, t, a1, a2, x0, w, y0);
        if (hillPath == null || hillKey != (x0, x1, y1))
        {
            hillPath?.Dispose(); hillSh?.Dispose(); hillKey = (x0, x1, y1); hillPath = new SKPath();
            hillPath.MoveTo(x0, y1); for (float x = 0; x <= w + 20; x += 20) hillPath.LineTo(x0 + x, y1 - 46 - 20 * MathF.Sin(x * .006f + 1)); hillPath.LineTo(x1, y1); hillPath.Close();
            hillSh = SKShader.CreateLinearGradient(new(0, y1 - 90), new(0, y1), new[] { new SKColor(30, 8, 60, 0), new SKColor(18, 4, 40, 200) }, null, SKShaderTileMode.Clamp);
        }
        { var p = Gfx.Fill(SKColors.White); p.Shader = hillSh; c.DrawPath(hillPath, p); }
        for (int i = 0; i < blades.Length; i++)
        {
            float bx = x0 + blades[i] * w, bh = (18 + blades[(i * 7) % blades.Length] * 34) * grow, sway = MathF.Sin(t * 1.3f + bx * .02f) * 6, gy = y1 - 30 - 20 * MathF.Sin((bx - x0) * .006f + 1);
            c.DrawLine(bx, gy + 30, bx + sway, gy + 30 - bh, Gfx.Line(C.Green.Dark(.35f).A(.5f), 2.2f));
        }
        foreach (var f in fl)
        {
            float g = Ease.OutBack(Ease.Clamp((t - f.delay) / 1.4f)), pulse = 1 + .07f * MathF.Sin(t * 1.6f + f.ph);
            float bx = x0 + f.x * w, gy = y1 - 26 - 20 * MathF.Sin((bx - x0) * .006f + 1), hh = f.h * g, sway = MathF.Sin(t * 1.1f + f.ph) * 9 * g;
            var col = Pal(f.ci, a1, a2); var stem = C.Green.Dark(.45f);
            var sp = stemP; sp.Reset(); sp.MoveTo(bx, gy + 26); sp.QuadTo(bx + sway * .3f, gy + 26 - hh * .5f, bx + sway, gy + 26 - hh);
            c.DrawPath(sp, Gfx.Line(stem.A(.75f), 3.5f));
            float lx = bx + sway * .35f, ly = gy + 26 - hh * .4f; c.Save(); c.Translate(lx, ly); c.RotateDegrees(-35 + sway * 2); c.DrawOval(14 * g, 0, 15 * g, 5 * g, Gfx.Fill(stem.Light(.15f).A(.7f))); c.Restore();
            Head(c, bx + sway, gy + 26 - hh, f.size * g * pulse, col, f.type, t + f.ph);
        }
        foreach (var p in pe)
        {
            float py = y0 + (p.y * h + t * p.sp) % h, px = x0 + ((p.x * w + MathF.Sin(t * .8f + p.ph) * p.sw + t * 10) % w + w) % w, a = .55f;
            c.Save(); c.Translate(px, py); c.RotateDegrees(t * 60 + p.ph * 50); c.Scale(1, MathF.Abs(MathF.Cos(t * 1.8f + p.ph)) * .7f + .3f); c.DrawOval(0, 0, p.size * .55f, p.size, Gfx.Fill(Pal(p.ci, a1, a2).Light(.25f).A(a))); c.Restore();
        }
        foreach (var f in ff)
        {
            float fx = x0 + ((f.x + .06f * MathF.Sin(t * f.fx + f.ph)) % 1f + 1) % 1 * w, fy = y0 + (f.y + .05f * MathF.Cos(t * f.fy + f.ph)) * h, a = .5f + .5f * MathF.Sin(t * 2.2f + f.ph);
            Gfx.Radial(c, fx, fy, f.size * 6, C.Yellow, a * .35f); c.DrawCircle(fx, fy, f.size * .6f, Gfx.Fill(SKColors.White.A(.4f + a * .5f)));
        }
    }
    static void DrawAurora(SKCanvas c, float t, SKColor a1, SKColor a2, float x0, float w, float y0)
    {
        for (int k = 0; k < 3; k++)
        {
            using var p = new SKPath(); float baseY = y0 + 70 + k * 28; p.MoveTo(x0, baseY);
            for (float x = 0; x <= w; x += 40) p.LineTo(x0 + x, baseY + MathF.Sin(x * .004f + t * (.25f + k * .07f) + k * 2) * 34 + MathF.Sin(x * .011f - t * .4f) * 12);
            for (float x = w; x >= 0; x -= 40) p.LineTo(x0 + x, baseY + 90 + MathF.Sin(x * .005f + t * .3f + k) * 30);
            p.Close(); var col = k == 1 ? a2 : k == 2 ? C.Purple : a1;
            if (!auroraSh.TryGetValue((baseY, (uint)col), out var sh)) auroraSh[(baseY, (uint)col)] = sh = SKShader.CreateLinearGradient(new(0, baseY - 30), new(0, baseY + 120), new[] { col.A(0), col.A(.10f), col.A(0) }, new[] { 0f, .35f, 1f }, SKShaderTileMode.Clamp);
            var pt = Gfx.Fill(SKColors.White); pt.Shader = sh; pt.BlendMode = SKBlendMode.Plus; c.DrawPath(p, pt);
        }
    }
    static void Head(SKCanvas c, float x, float y, float r, SKColor col, int type, float t)
    {
        if (r < 1) return; Gfx.Radial(c, x, y, r * 2.4f, col, .28f);
        c.Save(); c.Translate(x, y); c.RotateDegrees(MathF.Sin(t * .5f) * 8);
        if (type == 0) { for (int k = 0; k < 6; k++) { c.Save(); c.RotateDegrees(k * 60); c.DrawOval(0, -r * .62f, r * .34f, r * .62f, Gfx.Fill(col.Light(.08f * (k % 2)).A(.85f))); c.Restore(); } c.DrawCircle(0, 0, r * .3f, Gfx.Fill(C.Gold)); }
        else if (type == 1) { using var tp = new SKPath(); tp.MoveTo(-r * .7f, -r * .3f); tp.LineTo(-r * .45f, -r * 1.05f); tp.LineTo(-r * .15f, -r * .55f); tp.LineTo(0, -r * 1.2f); tp.LineTo(r * .15f, -r * .55f); tp.LineTo(r * .45f, -r * 1.05f); tp.LineTo(r * .7f, -r * .3f); tp.QuadTo(0, r * .55f, -r * .7f, -r * .3f); tp.Close(); c.DrawPath(tp, Gfx.Fill(col.A(.9f))); c.DrawPath(tp, Gfx.Line(col.Light(.5f).A(.7f), 1.5f)); }
        else { for (int k = 0; k < 11; k++) { c.Save(); c.RotateDegrees(k * 32.7f); c.DrawOval(0, -r * .7f, r * .14f, r * .5f, Gfx.Fill(SKColors.White.Mix(col, .35f).A(.85f))); c.Restore(); } c.DrawCircle(0, 0, r * .26f, Gfx.Fill(C.Yellow)); }
        c.Restore();
    }
}
