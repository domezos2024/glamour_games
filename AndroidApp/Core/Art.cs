using SkiaSharp;
namespace GlamourGames;
static class CardArt
{
    public static readonly string[] Ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    public static SKColor SuitCol(int s) => s == 1 || s == 2 ? new SKColor(214, 30, 60) : new SKColor(24, 20, 40);
    public static void Face(SKCanvas c, SKRect r, string rank, int suit, bool hl = false)
    {
        float w = r.Width, rad = w * .09f;
        Gfx.RectGrad(c, r, rad, new SKColor(255, 255, 255), new SKColor(226, 220, 240));
        Gfx.Stroke(c, r, rad, hl ? C.Gold : new SKColor(120, 100, 160), hl ? 4 : 1.5f);
        var col = SuitCol(suit); float fs = w * (rank == "10" ? .27f : .3f);
        Gfx.Text(c, rank, r.Left + w * .2f, r.Top + w * .26f, fs, col, Al.C);
        Gfx.Suit(c, suit, r.Left + w * .2f, r.Top + w * .5f, w * .09f, col);
        Gfx.Suit(c, suit, r.MidX, r.MidY + w * .05f, w * .27f, col);
        c.Save(); c.RotateDegrees(180, r.MidX, r.MidY);
        Gfx.Text(c, rank, r.Left + w * .2f, r.Top + w * .26f, fs, col, Al.C); Gfx.Suit(c, suit, r.Left + w * .2f, r.Top + w * .5f, w * .09f, col); c.Restore();
        if (hl) Gfx.Glow(c, r, rad, C.Gold, 10, .9f);
    }
    public static void Back(SKCanvas c, SKRect r)
    {
        float rad = r.Width * .09f;
        Gfx.RectGrad(c, r, rad, new SKColor(70, 20, 140), new SKColor(28, 6, 70)); Gfx.Stroke(c, r, rad, C.Gold, 2.5f);
        var inner = Gfx.Inflate(r, -r.Width * .09f); c.Save(); c.ClipRoundRect(new SKRoundRect(inner, rad * .5f), SKClipOperation.Intersect, true);
        var p = Gfx.Line(C.Gold.A(.35f), 1.5f); float s = r.Width * .16f;
        for (float k = -r.Height; k < r.Width + r.Height; k += s) { c.DrawLine(inner.Left + k, inner.Top, inner.Left + k + inner.Height, inner.Bottom, p); c.DrawLine(inner.Left + k, inner.Bottom, inner.Left + k + inner.Height, inner.Top, p); }
        c.Restore(); Gfx.Stroke(c, inner, rad * .5f, C.Gold.A(.8f), 2);
        Gfx.Suit(c, 2, r.MidX, r.MidY, r.Width * .16f, C.Gold.A(.9f));
    }
    public static void Card(SKCanvas c, float cx, float cy, float w, string rank, int suit, float flip, float rot = 0, float lift = 0, bool hl = false)
    {
        float h = w * 1.4f, sx = MathF.Abs(MathF.Cos(flip * MathF.PI)); bool face = flip > .5f;
        c.Save(); c.Translate(cx, cy - lift); c.RotateDegrees(rot);
        var sh = Gfx.Fill(SKColors.Black.A(.45f)); sh.MaskFilter = Gfx.Blur(10 + lift * .1f); c.DrawRoundRect(Gfx.Ctr(6 + lift * .15f, 10 + lift * .3f, w * sx, h), 12, 12, sh);
        c.Scale(Math.Max(sx, .02f), 1);
        var r = Gfx.Ctr(0, 0, w, h);
        if (face) Face(c, r, rank, suit, hl); else Back(c, r);
        c.Restore();
    }
}
static class Die3D
{
    // Hard-coded mapping from face index -> pip value to ensure visual pips match internal die values.
    // If rendering orientation changes, adjust this array accordingly.
    static readonly int[] faceVal = { 1, 6, 3, 4, 2, 5 };
    static readonly float[][] nrm = { new float[] { 0, 0, 1 }, new float[] { 0, 0, -1 }, new float[] { 1, 0, 0 }, new float[] { -1, 0, 0 }, new float[] { 0, 1, 0 }, new float[] { 0, -1, 0 } };
    static readonly float[][] uax = { new float[] { 1, 0, 0 }, new float[] { -1, 0, 0 }, new float[] { 0, 0, -1 }, new float[] { 0, 0, 1 }, new float[] { 1, 0, 0 }, new float[] { 1, 0, 0 } };
    static readonly float[][] vax = { new float[] { 0, 1, 0 }, new float[] { 0, 1, 0 }, new float[] { 0, 1, 0 }, new float[] { 0, 1, 0 }, new float[] { 0, 0, -1 }, new float[] { 0, 0, 1 } };
    public static float[] Mul(float[] a, float[] b)
    {
        var r = new float[9]; for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) r[i * 3 + j] = a[i * 3] * b[j] + a[i * 3 + 1] * b[3 + j] + a[i * 3 + 2] * b[6 + j]; return r;
    }
    public static float[] RX(float a) { float c = MathF.Cos(a), s = MathF.Sin(a); return new float[] { 1, 0, 0, 0, c, -s, 0, s, c }; }
    public static float[] RY(float a) { float c = MathF.Cos(a), s = MathF.Sin(a); return new float[] { c, 0, s, 0, 1, 0, -s, 0, c }; }
    public static float[] RZ(float a) { float c = MathF.Cos(a), s = MathF.Sin(a); return new float[] { c, -s, 0, s, c, 0, 0, 0, 1 }; }
    public static float[] RAxis(float x, float y, float z, float a)
    {
        float l = MathF.Sqrt(x * x + y * y + z * z); x /= l; y /= l; z /= l; float c = MathF.Cos(a), s = MathF.Sin(a), t = 1 - c;
        return new float[] { t * x * x + c, t * x * y - s * z, t * x * z + s * y, t * x * y + s * z, t * y * y + c, t * y * z - s * x, t * x * z - s * y, t * y * z + s * x, t * z * z + c };
    }
    public static float[] Face(int v)
    {
        return v switch { 1 => RX(0), 6 => RX(MathF.PI), 3 => RY(-MathF.PI / 2), 4 => RY(MathF.PI / 2), 2 => RX(MathF.PI / 2), _ => RX(-MathF.PI / 2) };
    }
    public static SKMatrix Homog(SKPoint p0, SKPoint p1, SKPoint p2, SKPoint p3)
    {
        float dx1 = p1.X - p2.X, dx2 = p3.X - p2.X, dx3 = p0.X - p1.X + p2.X - p3.X, dy1 = p1.Y - p2.Y, dy2 = p3.Y - p2.Y, dy3 = p0.Y - p1.Y + p2.Y - p3.Y;
        float den = dx1 * dy2 - dx2 * dy1, g = den == 0 ? 0 : (dx3 * dy2 - dx2 * dy3) / den, h = den == 0 ? 0 : (dx1 * dy3 - dx3 * dy1) / den;
        return new SKMatrix(p1.X - p0.X + g * p1.X, p3.X - p0.X + h * p3.X, p0.X, p1.Y - p0.Y + g * p1.Y, p3.Y - p0.Y + h * p3.Y, p0.Y, g, h, 1);
    }
    static readonly (float, float)[][] pips = {
        new[]{(.5f,.5f)}, new[]{(.27f,.27f),(.73f,.73f)}, new[]{(.27f,.27f),(.5f,.5f),(.73f,.73f)}, new[]{(.27f,.27f),(.73f,.27f),(.27f,.73f),(.73f,.73f)},
        new[]{(.27f,.27f),(.73f,.27f),(.5f,.5f),(.27f,.73f),(.73f,.73f)}, new[]{(.27f,.27f),(.73f,.27f),(.27f,.5f),(.73f,.5f),(.27f,.73f),(.73f,.73f)} };
    public static void Draw(SKCanvas c, float cx, float cy, float size, float[] m, SKColor body, SKColor pip, bool glow = false)
    {
        float f = 5.5f; var pts = new SKPoint[8]; var zs = new float[8]; int k = 0;
        var vs = new float[8][]; for (int i = 0; i < 8; i++) vs[i] = new float[] { (i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1 };
        SKPoint P(float[] v, out float z)
        {
            float x = m[0] * v[0] + m[1] * v[1] + m[2] * v[2], y = m[3] * v[0] + m[4] * v[1] + m[5] * v[2]; z = m[6] * v[0] + m[7] * v[1] + m[8] * v[2];
            float k2 = f / (f - z); return new SKPoint(cx + x * k2 * size * .5f, cy - y * k2 * size * .5f);
        }
        if (glow) Gfx.Radial(c, cx, cy, size * 1.1f, body, .5f);
        var sh = Gfx.Fill(SKColors.Black.A(.4f)); sh.MaskFilter = Gfx.Blur(size * .12f); c.DrawOval(cx + size * .08f, cy + size * .62f, size * .55f, size * .14f, sh);
        var order = Enumerable.Range(0, 6).Select(i => (i, z: m[6] * nrm[i][0] + m[7] * nrm[i][1] + m[8] * nrm[i][2])).Where(t => t.z > .001f).OrderBy(t => t.z);
        foreach (var (i, z) in order)
        {
            var n = nrm[i]; var u = uax[i]; var v = vax[i];
            float[] corner(float a, float b) => new float[] { n[0] + u[0] * a + v[0] * b, n[1] + u[1] * a + v[1] * b, n[2] + u[2] * a + v[2] * b };
            var q0 = P(corner(-1, 1), out _); var q1 = P(corner(1, 1), out _); var q2 = P(corner(1, -1), out _); var q3 = P(corner(-1, -1), out _);
            var hm = Homog(q0, q1, q2, q3);
            float lx = m[0] * n[0] + m[1] * n[1] + m[2] * n[2], ly = m[3] * n[0] + m[4] * n[1] + m[5] * n[2];
            float shade = Math.Clamp(.55f + .25f * z + .2f * (-lx * .3f + ly * .8f), .35f, 1.05f);
            c.Save(); c.Concat(in hm);
            var r = new SKRect(0, 0, 1, 1); float rad = .16f;
            using var sh2 = SKShader.CreateLinearGradient(new(0, 0), new(1, 1), new[] { body.Dark(Math.Min(1, shade * 1.05f)), body.Dark(shade * .78f) }, null, SKShaderTileMode.Clamp);
            var p = Gfx.Fill(SKColors.White); p.Shader = sh2; c.DrawRoundRect(r, rad, rad, p);
            var pl = Gfx.Line(SKColors.White.A(.35f * shade), .02f); c.DrawRoundRect(new SKRect(.02f, .02f, .98f, .98f), rad, rad, pl);
            foreach (var (px, py) in pips[faceVal[i] - 1])
            {
                c.DrawCircle(px, py, .105f, Gfx.Fill(SKColors.Black.A(.35f))); c.DrawCircle(px, py, .095f, Gfx.Fill(pip.Dark(Math.Min(1, shade + .2f))));
                c.DrawCircle(px - .025f, py - .03f, .03f, Gfx.Fill(SKColors.White.A(.35f)));
            }
            c.Restore();
        }
    }
}
static class Coin3D
{
    public static void Draw(SKCanvas c, float cx, float cy, float r, float angle, float lift = 0)
    {
        float cs = MathF.Cos(angle), sn = MathF.Abs(MathF.Sin(angle)); bool head = cs >= 0; float sy = Math.Max(.03f, MathF.Abs(cs)), th = r * .16f;
        var sh = Gfx.Fill(SKColors.Black.A(.4f - lift * .0015f)); sh.MaskFilter = Gfx.Blur(14); c.DrawOval(cx, cy + r * 1.05f + lift * .3f, r * (.85f - lift * .001f), r * .2f, sh);
        cy -= lift;
        for (int i = 8; i >= 0; i--)
        {
            float off = (i / 8f - .5f) * th * 2 * sn * (head ? 1 : -1); var col = C.Gold.Dark(.55f + .25f * (1 - i / 8f));
            c.DrawOval(cx, cy + off, r, r * sy, Gfx.Fill(col));
        }
        c.Save(); c.Translate(cx, cy); c.Scale(1, sy);
        using var clip = new SKPath(); clip.AddCircle(0, 0, r * .96f); c.ClipPath(clip, SKClipOperation.Intersect, true);
        c.DrawCircle(0, 0, r, Gfx.Fill(C.Gold.Dark(.6f)));
        Gfx.Image(c, Assets.Img(head ? "coin_H" : "coin_T"), Gfx.Ctr(0, 0, r * 2, r * 2));
        c.Restore();
        c.Save(); c.Translate(cx, cy); c.Scale(1, sy); c.DrawCircle(0, 0, r * .97f, Gfx.Line(C.Gold.Light(.5f).A(.8f), 3)); c.Restore();
        float gl = .5f + .5f * MathF.Sin(angle * 2); c.DrawOval(cx - r * .3f, cy - r * .35f * sy, r * .5f, r * .18f * sy, Gfx.Fill(SKColors.White.A(.12f * gl * sy)));
    }
}
