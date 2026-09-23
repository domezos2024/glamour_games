using SkiaSharp;
namespace GlamourGames;
static class C
{
    public static readonly SKColor Bg = new(6, 1, 15), Cyan = new(0, 255, 242), Pink = new(255, 32, 121), Magenta = new(255, 0, 229), Gold = new(255, 215, 0),
        Purple = new(160, 32, 255), Green = new(0, 255, 106), Yellow = new(250, 255, 0), Orange = new(255, 136, 0), Blue = new(77, 166, 255), Red = new(255, 68, 68),
        White = SKColors.White, Dim = new(170, 160, 200), Panel = new(20, 8, 40);
    public static SKColor A(this SKColor c, float a) => c.WithAlpha((byte)Math.Clamp(a * 255, 0, 255));
    public static SKColor Mix(this SKColor a, SKColor b, float t) => new((byte)(a.Red + (b.Red - a.Red) * t), (byte)(a.Green + (b.Green - a.Green) * t), (byte)(a.Blue + (b.Blue - a.Blue) * t), (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
    public static SKColor Dark(this SKColor c, float f) => new((byte)(c.Red * f), (byte)(c.Green * f), (byte)(c.Blue * f), c.Alpha);
    public static SKColor Light(this SKColor c, float f) => c.Mix(SKColors.White, f);
}
static class Assets
{
    static readonly Dictionary<string, SKImage> imgs = new();
    static readonly object imgLock = new();
    public static void Preload()
    {
        try
        {
            foreach (var f in global::Android.App.Application.Context.Assets.List("") ?? Array.Empty<string>())
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                if (ext == ".png" || ext == ".jpg" || ext == ".webp") Img(Path.GetFileNameWithoutExtension(f));
            }
        }
        catch (Exception e) { Log.I("preload " + e.Message); }
    }
    public static string Dir = "Assets";
    public static SKImage Img(string name)
    {
        lock (imgLock) return ImgLocked(name);
    }
    static SKImage ImgLocked(string name)
    {
        if (imgs.TryGetValue(name, out var i)) return i;
        try
        {
            var files = global::Android.App.Application.Context.Assets.List("") ?? Array.Empty<string>();
            var file = files.FirstOrDefault(x => string.Equals(Path.GetFileNameWithoutExtension(x), name, StringComparison.OrdinalIgnoreCase));
            if (file == null) return null;
            using var s = global::Android.App.Application.Context.Assets.Open(file);
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            i = SKImage.FromEncodedData(ms.ToArray());
        }
        catch (Exception e) { Log.I("asset " + name + ": " + e.Message); i = null; }
        imgs[name] = i;
        return i;
    }
}
enum Al { L, C, R }
static class Gfx
{
    public static readonly SKPaint P = new() { IsAntialias = true };
    static readonly Dictionary<float, SKMaskFilter> blurs = new();
    static readonly Dictionary<(int, bool, bool), SKFont> fonts = new();
    static SKTypeface LoadTf(string file, string family, SKFontStyle st)
    {
        try
        {
            using var a = global::Android.App.Application.Context.Assets.Open("Fonts/" + file);
            using var ms = new MemoryStream(); a.CopyTo(ms);
            var tf = SKTypeface.FromData(SKData.CreateCopy(ms.ToArray()));
            if (tf != null) return tf;
        }
        catch (Exception e) { Log.I("font " + file + ": " + e.Message); }
        return SKTypeface.FromFamilyName(family, st);
    }
    static readonly SKTypeface sans = LoadTf("Selawik-Regular.ttf", "sans-serif", SKFontStyle.Normal), sansB = LoadTf("Selawik-Bold.ttf", "sans-serif", SKFontStyle.Bold),
        serifB = LoadTf("PTSerif-BoldItalic.ttf", "serif", SKFontStyle.BoldItalic), symTf = LoadTf("NotoSansSymbols2.ttf", "sans-serif", SKFontStyle.Normal);
    public static SKMaskFilter Blur(float s) { s = MathF.Round(s * 2) / 2; if (!blurs.TryGetValue(s, out var m)) blurs[s] = m = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s); return m; }
    static float Bump(float s) { if (s >= 32) return s; float b = s * 1.15f; return s >= 16 && b < 24 ? 24 : b; }
    public static SKFont Font(float size, bool bold = true, bool serif = false)
    {
        size = Bump(size);
        var k = ((int)MathF.Round(size * 2), bold, serif);
        if (!fonts.TryGetValue(k, out var f)) fonts[k] = f = new SKFont(serif ? serifB : bold ? sansB : sans, size) { Subpixel = true, Edging = SKFontEdging.SubpixelAntialias };
        return f;
    }
    public static SKPaint Fill(SKColor c) { P.Reset(); P.IsAntialias = true; P.Style = SKPaintStyle.Fill; P.Color = c; return P; }
    public static SKPaint Line(SKColor c, float w) { P.Reset(); P.IsAntialias = true; P.Style = SKPaintStyle.Stroke; P.StrokeWidth = w; P.Color = c; P.StrokeCap = SKStrokeCap.Round; P.StrokeJoin = SKStrokeJoin.Round; return P; }
    public static SKRect R(float x, float y, float w, float h) => new(x, y, x + w, y + h);
    public static SKRect Ctr(float cx, float cy, float w, float h) => new(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2);
    public static SKRect Inflate(SKRect r, float d) => new(r.Left - d, r.Top - d, r.Right + d, r.Bottom + d);
    public static void Rect(SKCanvas c, SKRect r, float rad, SKColor col) => c.DrawRoundRect(r, rad, rad, Fill(col));
    static readonly Dictionary<(float, float, uint, uint), SKShader> gradCache = new();
    public static void RectGrad(SKCanvas c, SKRect r, float rad, SKColor top, SKColor bot)
    {
        var key = (r.Top, r.Bottom, (uint)top, (uint)bot);
        if (!gradCache.TryGetValue(key, out var sh))
        {
            if (gradCache.Count > 400) { foreach (var v in gradCache.Values) v.Dispose(); gradCache.Clear(); }
            gradCache[key] = sh = SKShader.CreateLinearGradient(new(r.Left, r.Top), new(r.Left, r.Bottom), new[] { top, bot }, null, SKShaderTileMode.Clamp);
        }
        var p = Fill(SKColors.White); p.Shader = sh; c.DrawRoundRect(r, rad, rad, p);
    }
    public static void Stroke(SKCanvas c, SKRect r, float rad, SKColor col, float w) => c.DrawRoundRect(r, rad, rad, Line(col, w));
    public static void Glow(SKCanvas c, SKRect r, float rad, SKColor col, float sigma, float a = 1)
    {
        var p = Line(col.A(a), sigma * .8f); p.MaskFilter = Blur(sigma); c.DrawRoundRect(r, rad, rad, p);
    }
    public static void GlowFill(SKCanvas c, SKRect r, float rad, SKColor col, float sigma, float a = 1)
    {
        var p = Fill(col.A(a)); p.MaskFilter = Blur(sigma); c.DrawRoundRect(r, rad, rad, p);
    }
    static readonly Dictionary<(int, uint, byte), SKShader> radCache = new(), ballCache = new();
    static SKShader CachedShader(Dictionary<(int, uint, byte), SKShader> d, (int, uint, byte) k, Func<SKShader> mk)
    {
        if (!d.TryGetValue(k, out var sh)) { if (d.Count > 600) { foreach (var v in d.Values) v.Dispose(); d.Clear(); } d[k] = sh = mk(); }
        return sh;
    }
    public static void Radial(SKCanvas c, float x, float y, float rad, SKColor col, float a = 1)
    {
        var col2 = col.A(a); int rq = Math.Max(1, (int)MathF.Round(rad * 2)); float rr = rq / 2f;
        var sh = CachedShader(radCache, (rq, (uint)col2, 0), () => SKShader.CreateRadialGradient(new(0, 0), rr, new[] { col2, col2.WithAlpha(0) }, null, SKShaderTileMode.Clamp));
        var p = Fill(SKColors.White); p.Shader = sh; c.Save(); c.Translate(x, y); c.DrawCircle(0, 0, rr, p); c.Restore();
    }
    public static void Ball(SKCanvas c, float x, float y, float r, SKColor col)
    {
        int rq = Math.Max(1, (int)MathF.Round(r * 2)); float rr = rq / 2f;
        var sh = CachedShader(ballCache, (rq, (uint)col, 1), () => SKShader.CreateRadialGradient(new(-rr * .35f, -rr * .4f), rr * 1.5f, new[] { col.Light(.75f), col, col.Dark(.4f) }, new[] { 0f, .45f, 1f }, SKShaderTileMode.Clamp));
        var p = Fill(SKColors.White); p.Shader = sh; c.Save(); c.Translate(x, y); c.DrawCircle(0, 0, rr, p); c.Restore();
    }
    static readonly Dictionary<(int, SKTypeface), SKTypeface> fbTf = new();
    static readonly Dictionary<(SKTypeface, float), SKFont> fbFonts = new();
    static SKTypeface Fb(int cp, SKTypeface primary)
    {
        if (cp < 0x2190 || cp == 0xFEFF || primary.ContainsGlyph(cp)) return null;
        if (!fbTf.TryGetValue((cp, primary), out var tf)) fbTf[(cp, primary)] = tf = symTf.ContainsGlyph(cp) ? symTf : serifB.ContainsGlyph(cp) ? serifB : SKFontManager.Default.MatchCharacter(cp);
        return tf;
    }
    static SKFont FbFont(SKTypeface tf, SKFont f) { if (!fbFonts.TryGetValue((tf, f.Size), out var sf)) fbFonts[(tf, f.Size)] = sf = new SKFont(tf, f.Size) { Subpixel = true, Edging = SKFontEdging.SubpixelAntialias }; return sf; }
    static bool HasSuit(string s) { foreach (var ch in s) if (ch >= 0x2190) return true; return false; }
    static IEnumerable<(string, SKFont)> Runs(string s, SKFont f)
    {
        int st = 0; SKTypeface cur = null;
        for (int i = 0; i < s.Length;)
        {
            int cp = char.ConvertToUtf32(s, i), n = char.IsSurrogatePair(s, i) ? 2 : 1; var tf = Fb(cp, f.Typeface);
            if (i > st && tf != cur) { yield return (s[st..i], cur == null ? f : FbFont(cur, f)); st = i; }
            cur = tf; i += n;
        }
        if (st < s.Length) yield return (s[st..], cur == null ? f : FbFont(cur, f));
    }
    public static float TW(string s, float size, bool bold = true, bool serif = false)
    {
        var f = Font(size, bold, serif);
        if (!HasSuit(s)) return f.MeasureText(s);
        float w = 0; foreach (var (t, ft) in Runs(s, f)) w += ft.MeasureText(t); return w;
    }
    static void DrawRuns(SKCanvas c, string s, float x, float by, SKTextAlign a, SKFont f, SKPaint p)
    {
        if (!HasSuit(s)) { c.DrawText(s, x, by, a, f, p); return; }
        float w = 0; foreach (var (t, ft) in Runs(s, f)) w += ft.MeasureText(t);
        float cx = a == SKTextAlign.Center ? x - w / 2 : a == SKTextAlign.Right ? x - w : x;
        foreach (var (t, ft) in Runs(s, f)) { c.DrawText(t, cx, by, SKTextAlign.Left, ft, p); cx += ft.MeasureText(t); }
    }
    public static void Text(SKCanvas c, string s, float x, float y, float size, SKColor col, Al al = Al.C, bool bold = true, float glow = 0, bool serif = false)
    {
        var f = Font(size, bold, serif); var m = f.Metrics; float by = y - (m.Ascent + m.Descent) / 2;
        var a = al == Al.C ? SKTextAlign.Center : al == Al.L ? SKTextAlign.Left : SKTextAlign.Right;
        if (glow > 0) { var g = Fill(col.A(.9f)); g.MaskFilter = Blur(glow); DrawRuns(c, s, x, by, a, f, g); }
        DrawRuns(c, s, x, by, a, f, Fill(col));
    }
    public static void TextOutline(SKCanvas c, string s, float x, float y, float size, SKColor col, float w, bool serif = true)
    {
        var f = Font(size, true, serif); var m = f.Metrics; float by = y - (m.Ascent + m.Descent) / 2; c.DrawText(s, x, by, SKTextAlign.Center, f, Line(col, w));
    }
    static readonly Dictionary<(string, uint, int), SKImage> bannerCache = new();
    public static void BannerText(SKCanvas c, string s, float size, SKColor col, float alpha)
    {
        var key = (s, (uint)col, (int)size);
        if (!bannerCache.TryGetValue(key, out var img))
        {
            if (bannerCache.Count > 12) { foreach (var v in bannerCache.Values) v.Dispose(); bannerCache.Clear(); }
            int w = (int)Math.Ceiling(TW(s, size, true, true) + 200), h = (int)(size * 1.9f);
            using var sf = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)); var cv = sf.Canvas; cv.Clear(SKColors.Transparent);
            Text(cv, s, w / 2f, h / 2f, size, col, Al.C, true, 34, true); Text(cv, s, w / 2f, h / 2f, size, col.Light(.6f), Al.C, true, 0, true);
            bannerCache[key] = img = sf.Snapshot();
        }
        var p = Fill(SKColors.White.A(alpha)); c.DrawImage(img, -img.Width / 2f, -img.Height / 2f, new SKSamplingOptions(SKFilterMode.Linear), p);
    }
    public static void TextShadow(SKCanvas c, string s, float x, float y, float size, SKColor col, Al al = Al.C, bool serif = false)
    {
        Text(c, s, x + 2, y + 3, size, SKColors.Black.A(.6f), al, true, 0, serif); Text(c, s, x, y, size, col, al, true, 0, serif);
    }
    public static void Image(SKCanvas c, SKImage img, SKRect dst, float a = 1)
    {
        if (img == null) return;
        var p = Fill(SKColors.White.A(a));
        c.DrawImage(img, dst, new SKSamplingOptions(SKCubicResampler.Mitchell), p);
    }
    public static SKPath Heart(float cx, float cy, float s)
    {
        var p = new SKPath(); p.MoveTo(cx, cy + s * .9f);
        p.CubicTo(cx - s * 1.5f, cy - s * .1f, cx - s * .8f, cy - s * 1.1f, cx, cy - s * .35f);
        p.CubicTo(cx + s * .8f, cy - s * 1.1f, cx + s * 1.5f, cy - s * .1f, cx, cy + s * .9f); p.Close(); return p;
    }
    public static void Suit(SKCanvas c, int suit, float cx, float cy, float s, SKColor col)
    {
        var p = Fill(col);
        switch (suit)
        {
            case 1: using (var h = Heart(cx, cy, s)) c.DrawPath(h, p); break;
            case 2: { using var d = new SKPath(); d.MoveTo(cx, cy - s); d.LineTo(cx + s * .75f, cy); d.LineTo(cx, cy + s); d.LineTo(cx - s * .75f, cy); d.Close(); c.DrawPath(d, p); break; }
            case 0:
                {
                    using var h = Heart(cx, cy, s); c.Save(); c.Scale(1, -1, cx, cy); c.DrawPath(h, p); c.Restore();
                    using var st = new SKPath(); st.MoveTo(cx, cy + s * .1f); st.LineTo(cx + s * .35f, cy + s); st.LineTo(cx - s * .35f, cy + s); st.Close(); c.DrawPath(st, p); break;
                }
            default:
                {
                    c.DrawCircle(cx, cy - s * .45f, s * .42f, p); c.DrawCircle(cx - s * .48f, cy + s * .2f, s * .42f, p); c.DrawCircle(cx + s * .48f, cy + s * .2f, s * .42f, p);
                    using var st = new SKPath(); st.MoveTo(cx, cy); st.LineTo(cx + s * .3f, cy + s); st.LineTo(cx - s * .3f, cy + s); st.Close(); c.DrawPath(st, p); break;
                }
        }
    }
    public static void Gear(SKCanvas c, float cx, float cy, float r, SKColor col)
    {
        using var p = new SKPath();
        for (int i = 0; i < 16; i++) { float a0 = i * MathF.PI / 8, a1 = a0 + MathF.PI / 16, rr = (i % 2 == 0) ? r : r * .78f; p.LineTo(cx + MathF.Cos(a0) * rr, cy + MathF.Sin(a0) * rr); p.LineTo(cx + MathF.Cos(a1) * rr, cy + MathF.Sin(a1) * rr); }
        p.Close(); c.DrawPath(p, Fill(col)); c.DrawCircle(cx, cy, r * .38f, Fill(C.Panel));
    }
    static SKPath starPath;
    public static void Star(SKCanvas c, float cx, float cy, float r, SKColor col, float rot = 0)
    {
        if (starPath == null)
        {
            starPath = new SKPath();
            for (int i = 0; i < 10; i++) { float a = i * MathF.PI / 5 - MathF.PI / 2, rr = i % 2 == 0 ? 1f : .45f; var pt = new SKPoint(MathF.Cos(a) * rr, MathF.Sin(a) * rr); if (i == 0) starPath.MoveTo(pt); else starPath.LineTo(pt); }
            starPath.Close();
        }
        c.Save(); c.Translate(cx, cy); c.RotateRadians(rot); c.Scale(r, r); c.DrawPath(starPath, Fill(col)); c.Restore();
    }
}
