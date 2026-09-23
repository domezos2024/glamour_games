using Android.Media;
using System.Collections.Concurrent;

namespace GlamourGames;

public enum S { Click, Hover, Flip, Match, NoMatch, PlaceX, PlaceO, Win, Lose, Drop, Hit, Miss, Sunk, Eat, Die, Dice, Deal, Coin, Spin, Stop, Big, Chip, Take, Turn, Tick, Splash, Blub, Boom, Crackle, Thunder, Zap, Launch, FwPop, Cheer, Clap, Step, Shake, Sparkle, Fanfare, Creak, Gurgle, Whoosh, Pop, Boing, Gong }

static class Sfx
{
    const int SR = 44100;
    static AudioTrack? musicTrack;
    static readonly object Gate = new();
    static readonly object MusicGate = new();
    static readonly Random R = new(7);
    static readonly Dictionary<S, float[]> bank = new();
    public static bool Muted, MusicOff, SfxOff;
    public static float Vol = .5f, MusicVol = .8f;
    public static float SfxVol => Vol;
    public static int Track = 0;
    public static readonly string[] TrackNames = { "Glamour", "Oase", "Casino-Groove", "Arcade", "Mystik" };

    public static void Init()
    {
        try
        {
            Muted = Save.Int("muted", 0) != 0;
            Track = Save.Int("music", 0); MusicOff = Save.Int("musicoff", 0) != 0; SfxOff = Save.Int("sfxoff", 0) != 0; Vol = Save.Int("sfxvol", 50) / 100f; MusicVol = Save.Int("musicvol", 80) / 100f;
            Build();
            StartMusic();
        }
        catch (Exception e) { Log.I("audio init " + e.Message); }
    }

    public static void ToggleMute()
    {
        Muted = !Muted;
        Save.Set("muted", Muted ? 1 : 0);
        lock (MusicGate)
        {
            if (Muted) musicTrack?.Pause();
            else if (Track >= 0 && !MusicOff) musicTrack?.Play();
        }
    }

    public static void AppPause() { lock (MusicGate) { try { musicTrack?.Pause(); } catch { } } }
    public static void AppResume() { lock (MusicGate) { try { if (!Muted && !MusicOff && Track >= 0) musicTrack?.Play(); } catch { } } }

    static void Feel(S s)
    {
        switch (s)
        {
            case S.Hit: case S.Sunk: Haptics.Hit(); break;
            case S.Win: case S.Big: case S.Fanfare: Haptics.Win(); break;
            case S.Lose: case S.Die: Haptics.Lose(); break;
            case S.Dice: case S.Shake: case S.Match: case S.Coin: Haptics.Tap(); break;
        }
    }
    const int Slots = 10, SlotBytes = 262144;
    static readonly AudioTrack?[] pool = new AudioTrack?[Slots];
    static int slot;
    static AudioTrack MakeSlot() => new AudioTrack.Builder()
        .SetAudioAttributes(new AudioAttributes.Builder().SetUsage(AudioUsageKind.Game).SetContentType(AudioContentType.Sonification).Build())
        .SetAudioFormat(new AudioFormat.Builder().SetEncoding(Encoding.PcmFloat).SetSampleRate(SR).SetChannelMask(ChannelOut.Stereo).Build())
        .SetBufferSizeInBytes(SlotBytes).SetTransferMode(AudioTrackMode.Stream).Build();
    public static void Play(S s, float vol = 1, float pitch = 1)
    {
        Feel(s);
        if (Muted || SfxOff) return;
        try
        {
            if (!bank.TryGetValue(s, out var data)) return;
            int n = Math.Min(data.Length, SlotBytes / sizeof(float));
            lock (Gate)
            {
                int i = slot = (slot + 1) % Slots;
                var t = pool[i] ??= MakeSlot();
                try { t.SetStartThresholdInFrames(1); } catch { }
                t.Stop(); t.Flush();
                t.SetVolume(Math.Clamp(Vol * vol * 1.4f, 0, 1));
                t.SetPlaybackRate((int)(SR * Math.Clamp(pitch, .5f, 2f)));
                t.Play();
                t.Write(data, 0, n, WriteMode.NonBlocking);
            }
        }
        catch (Exception e) { Log.I("sfx " + s + " " + e.Message); }
    }

    public static string NextTrack()
    {
        Track = Track >= TrackNames.Length - 1 ? -1 : Track + 1;
        Save.Set("music", Track);
        lock (MusicGate)
        {
            StopMusic();
            if (!Muted && !MusicOff && Track >= 0) StartMusic();
        }
        return Track < 0 ? "Musik aus" : "Musik: " + TrackNames[Track];
    }

    static void StopMusic()
    {
        try { musicTrack?.Stop(); musicTrack?.Release(); } catch { }
        musicTrack = null;
    }

    public static void SetTrack(int t) { Track = t; Save.Set("music", Track); lock (MusicGate) { StopMusic(); if (!Muted && !MusicOff && Track >= 0) StartMusic(); } }
    public static void SetMusicOff(bool off) { MusicOff = off; Save.Set("musicoff", off ? 1 : 0); lock (MusicGate) { StopMusic(); if (!Muted && !MusicOff && Track >= 0) StartMusic(); } }
    public static void SetMusicVol(float v) { MusicVol = Math.Clamp(v, 0, 1); Save.Set("musicvol", (int)Math.Round(MusicVol * 100)); lock (MusicGate) { try { musicTrack?.SetVolume(MusicVol); } catch { } } }
    public static void SetSfxVol(float v) { Vol = Math.Clamp(v, 0, 1); Save.Set("sfxvol", (int)Math.Round(Vol * 100)); }
    public static void SetSfxOff(bool off) { SfxOff = off; Save.Set("sfxoff", off ? 1 : 0); }
    static void StartMusic()
    {
        if (Muted || MusicOff || Track < 0) return;
        try
        {
            var data = MakeMusicLoop(Track, 0);
            musicTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder().SetUsage(AudioUsageKind.Game).SetContentType(AudioContentType.Music).Build())
                .SetAudioFormat(new AudioFormat.Builder().SetEncoding(Encoding.PcmFloat).SetSampleRate(SR).SetChannelMask(ChannelOut.Stereo).Build())
                .SetBufferSizeInBytes(Math.Max(16384, data.Length * sizeof(float)))
                .SetTransferMode(AudioTrackMode.Static).Build();
            musicTrack.Write(data, 0, data.Length, WriteMode.Blocking);
            musicTrack.SetLoopPoints(0, data.Length / 2, -1);
            musicTrack.SetVolume(MusicVol); musicTrack.Play();
        }
        catch (Exception e) { Log.I("music start " + e.Message); }
    }


    static double Saw(double p) { p -= Math.Floor(p); return p * 2 - 1; }
    static double Sq(double p) { p -= Math.Floor(p); return p < .5 ? 1 : -1; }
    static double Tri(double p) { p -= Math.Floor(p); return 4 * Math.Abs(p - .5) - 1; }
    static double Env(double lt, double k) => Math.Min(1, lt * 200) * Math.Exp(-lt * k);

    static float[] MakeMusicLoop(int style, float unused)
    {
        double[][] chords = style switch
        {
            1 => new[] { new[] { 1d, 1.2, 1.5 }, new[] { 1.335, 1.6, 2 }, new[] { 1.125, 1.335, 1.68 }, new[] { 1.5, 1.782, 2.25 } },
            2 => new[] { new[] { 1d, 1.26, 1.5, 1.78 }, new[] { 1.335, 1.68, 2, 2.38 }, new[] { 1.5, 1.89, 2.25, 2.67 }, new[] { 1.335, 1.68, 2, 2.38 } },
            3 => new[] { new[] { 1d, 1.26, 1.5 }, new[] { 1.19, 1.5, 1.78 }, new[] { 1.335, 1.68, 2 }, new[] { 1.5, 1.89, 2.25 } },
            4 => new[] { new[] { 1d, 1.2, 1.44 }, new[] { 1d, 1.189, 1.5 }, new[] { 0.944, 1.125, 1.41 }, new[] { 1d, 1.2, 1.5 } },
            _ => new[] { new[] { 1d, 1.26, 1.5 }, new[] { 1.335, 1.68, 2 }, new[] { 1.5, 1.89, 2.25 }, new[] { 1.122, 1.335, 1.68 } }
        };
        double[] baseHz = { 196, 220, 110, 174.61, 130.81 };
        double[] bpms = { 104, 84, 118, 140, 72 };
        double b0 = baseHz[style], beat = 60d / bpms[style];
        int frames = (int)(SR * beat * 16);
        var o = new float[frames * 2];
        var rnd = new Random(1000 + style * 97);
        var mel = new int[64]; for (int i = 0; i < mel.Length; i++) mel[i] = rnd.Next(0, 6);
        var hat = new float[frames]; for (int i = 0; i < frames; i++) hat[i] = (float)(rnd.NextDouble() * 2 - 1);
        double step = style == 3 ? beat / 4 : style == 4 ? beat * 2 : beat / 2;
        for (int i = 0; i < frames; i++)
        {
            double t = i / (double)SR;
            int ci = (int)(t / (beat * 4)) % 4; var ch = chords[ci];
            double root = b0 * ch[0];
            double lt = t % step; int si = (int)(t / step);
            double tb = t % beat;
            double L, Rr;
            switch (style)
            {
                case 0:
                {
                    double pad = 0; foreach (var r in ch) pad += Math.Sin(Math.Tau * b0 * r * t) + .3 * Math.Sin(Math.Tau * b0 * r * 2 * t);
                    double f = b0 * 2 * ch[mel[si % 64] % ch.Length] * (mel[si % 64] >= 3 ? 2 : 1);
                    double m = Tri(f * t) * Env(lt, 6) * .10;
                    double bass = Math.Sin(Math.Tau * root * .5 * t) * Env(tb, 3) * .16;
                    double sp = Math.Sin(Math.Tau * root * 4 * t) * Env(t % (beat * 2), 9) * .02;
                    L = pad * .03 + m + bass + sp; Rr = pad * .03 + m * .8 + bass + sp;
                    break;
                }
                case 1:
                {
                    double f = b0 * 2 * ch[mel[si % 64] % ch.Length] * (mel[si % 64] % 2 == 0 ? 1 : 1.5);
                    double vib = 1 + .006 * Math.Sin(Math.Tau * 5 * t);
                    double fl = (Math.Sin(Math.Tau * f * vib * t) + .2 * Math.Sin(Math.Tau * f * 2 * vib * t)) * Env(lt, 1.6) * .10;
                    double dr = (Math.Sin(Math.Tau * root * t) + .5 * Math.Sin(Math.Tau * root * 1.5 * t)) * .06 * (.7 + .3 * Math.Sin(t * .9));
                    L = dr + fl; Rr = dr * .9 + fl * 1.1;
                    break;
                }
                case 2:
                {
                    double[] walk = { 1, 1.26, 1.5, 1.26 };
                    double bf = root * .5 * walk[(int)(t / beat) % 4];
                    double bass = Sq(bf * t) * Env(tb, 5) * .10 + Math.Sin(Math.Tau * bf * t) * Env(tb, 4) * .10;
                    double kick = Math.Sin(Math.Tau * (50 + 90 * Math.Exp(-tb * 30)) * t) * Math.Exp(-tb * 9) * .28;
                    double hh = hat[i] * Env((tb + beat / 2) % beat, 60) * .07;
                    double st = 0; int bi = (int)(t / beat) % 4;
                    if (bi == 1 || bi == 3) foreach (var r in ch) st += Math.Sin(Math.Tau * b0 * 2 * r * t);
                    st *= Env(tb, 7) * .03;
                    L = bass + kick + hh + st; Rr = bass + kick + hh * .6 + st * 1.2;
                    break;
                }
                case 3:
                {
                    double f = b0 * 2 * ch[si % ch.Length] * ((si / ch.Length) % 2 == 0 ? 1 : 2);
                    double ar = Sq(f * t) * Env(lt, 9) * .07;
                    double bf = root * .5 * (((int)(t / (beat / 2)) % 2) == 0 ? 1 : 2);
                    double bass = Tri(bf * t) * .12 * Env(t % (beat / 2), 4);
                    bool odd = (int)(t / beat) % 2 == 1;
                    double sn = odd ? hat[i] * Math.Exp(-tb * 25) * .12 : 0;
                    double kk = !odd ? Math.Sin(Math.Tau * (60 + 80 * Math.Exp(-tb * 40)) * t) * Math.Exp(-tb * 14) * .2 : 0;
                    L = ar + bass + sn + kk; Rr = ar * .8 + bass + sn + kk;
                    break;
                }
                default:
                {
                    double pad = 0; foreach (var r in ch) pad += Saw(b0 * r * t * 1.003) + Saw(b0 * r * t * .997);
                    double lp = pad * .012 * (.6 + .4 * Math.Sin(t * .45));
                    double drone = Math.Sin(Math.Tau * b0 * .5 * t) * .12;
                    double bell = Math.Sin(Math.Tau * b0 * 4 * ch[mel[si % 64] % ch.Length] * t) * Env(lt, 2.2) * (mel[si % 64] < 3 ? .09 : 0);
                    L = lp + drone + bell; Rr = lp * 1.1 + drone + bell * .6;
                    break;
                }
            }
            o[i * 2] = (float)Math.Clamp(L, -.9, .9); o[i * 2 + 1] = (float)Math.Clamp(Rr, -.9, .9);
        }
        int fade = SR / 200;
        for (int i = 0; i < fade; i++) { float g = i / (float)fade; o[i * 2] *= g; o[i * 2 + 1] *= g; o[(frames - 1 - i) * 2] *= g; o[(frames - 1 - i) * 2 + 1] *= g; }
        return o;
    }

    static float[] Noise(float dur, float decay = 12, float lp = 1) { float y = 0; return Gen(dur, (t, u) => { y += lp * ((float)R.NextDouble() * 2 - 1 - y); return y; }, decay); }
    static float[] Mix(params float[][] a) { var o = new float[a.Max(x => x.Length)]; foreach (var x in a) for (int i = 0; i < x.Length; i++) o[i] += x[i]; return o; }
    static float[] Gap(float sec) => new float[(int)(sec * SR)];
    static float[] Cat(params float[][] a) => a.SelectMany(x => x).ToArray();
    static float[] Ring(float f, float dur, float decay) => Gen(dur, (t, u) => MathF.Sin(MathF.Tau * f * t) + .5f * MathF.Sin(MathF.Tau * f * 2.76f * t) + .3f * MathF.Sin(MathF.Tau * f * 5.4f * t), decay);
    static float[] Arp(float decay, float note, params float[] fs) => Cat(fs.Select(f => Tone(f, note, decay)).ToArray());

    static void Build()
    {
        foreach (S s in Enum.GetValues<S>()) bank[s] = Tone(260 + (int)s * 17, .1f, 7);
        bank[S.Click] = Mix(Tone(1200, .04f, 40), Noise(.02f, 60));
        bank[S.Hover] = Tone(700, .04f, 30);
        bank[S.Flip] = Cat(Noise(.05f, 30, .5f), Sweep(500, 900, .08f, 12));
        bank[S.Match] = Arp(9, .09f, 660, 880, 1320);
        bank[S.NoMatch] = Cat(Tone(300, .1f, 8), Tone(220, .16f, 8));
        bank[S.PlaceX] = Mix(Tone(520, .1f, 14), Noise(.03f, 40));
        bank[S.PlaceO] = Mix(Tone(780, .12f, 12), Noise(.03f, 40));
        bank[S.Win] = Arp(5, .12f, 523, 659, 784, 1047);
        bank[S.Lose] = Cat(Sweep(400, 200, .2f, 5), Sweep(300, 120, .3f, 4));
        bank[S.Drop] = Sweep(700, 150, .18f, 10);
        bank[S.Hit] = Mix(Noise(.25f, 10, .4f), Tone(90, .25f, 9));
        bank[S.Miss] = Mix(Noise(.3f, 8, .15f), Sweep(500, 250, .3f, 6));
        bank[S.Sunk] = Mix(Noise(.5f, 5, .3f), Tone(70, .5f, 4), Sweep(400, 60, .5f, 5));
        bank[S.Eat] = Cat(Sweep(400, 900, .06f, 14), Sweep(500, 1100, .06f, 14));
        bank[S.Die] = Sweep(600, 80, .5f, 4);
        bank[S.Dice] = Cat(Noise(.03f, 60), Gap(.03f), Noise(.03f, 50), Gap(.04f), Noise(.03f, 45), Gap(.05f), Noise(.04f, 30));
        bank[S.Deal] = Noise(.07f, 30, .6f);
        bank[S.Coin] = Cat(Ring(1760, .06f, 25), Ring(2350, .35f, 7));
        bank[S.Spin] = Sweep(180, 900, .35f, 4);
        bank[S.Stop] = Mix(Tone(160, .12f, 12), Noise(.04f, 40));
        bank[S.Big] = Arp(4, .1f, 523, 659, 784, 1047, 1319, 1568);
        bank[S.Chip] = Mix(Ring(1400, .12f, 25), Noise(.02f, 60));
        bank[S.Take] = Sweep(400, 1000, .12f, 12);
        bank[S.Turn] = Cat(Tone(660, .06f, 12), Tone(880, .08f, 10));
        bank[S.Tick] = Tone(1500, .02f, 90);
        bank[S.Splash] = Mix(Noise(.4f, 6, .25f), Sweep(800, 300, .3f, 8));
        bank[S.Blub] = Cat(Sweep(250, 550, .09f, 10), Sweep(300, 650, .07f, 12));
        bank[S.Boom] = Mix(Noise(.55f, 5, .12f), Tone(55, .55f, 4), Sweep(140, 40, .5f, 5));
        bank[S.Crackle] = Cat(Noise(.02f, 50), Gap(.02f), Noise(.02f, 50), Gap(.03f), Noise(.03f, 40), Gap(.02f), Noise(.02f, 50));
        bank[S.Thunder] = Mix(Noise(.6f, 3, .06f), Tone(45, .6f, 3));
        bank[S.Zap] = Mix(Sweep(2000, 200, .18f, 10), Noise(.1f, 20));
        bank[S.Launch] = Mix(Sweep(300, 1800, .5f, 3), Noise(.5f, 4, .3f));
        bank[S.FwPop] = Mix(Noise(.2f, 14, .6f), Tone(150, .2f, 12));
        bank[S.Cheer] = Mix(Noise(.6f, 3, .35f), Sweep(500, 700, .6f, 3));
        bank[S.Clap] = Cat(Noise(.03f, 60), Gap(.02f), Noise(.04f, 45));
        bank[S.Step] = Mix(Noise(.03f, 60, .3f), Tone(120, .04f, 40));
        bank[S.Shake] = Cat(Noise(.05f, 25, .7f), Gap(.02f), Noise(.05f, 25, .7f), Gap(.02f), Noise(.06f, 20, .7f));
        bank[S.Sparkle] = Arp(9, .06f, 2093, 2637, 3136, 2637, 3520);
        bank[S.Fanfare] = Cat(Tone(523, .1f, 4), Tone(523, .1f, 4), Tone(523, .1f, 4), Tone(659, .16f, 3), Tone(784, .3f, 2.5f));
        bank[S.Creak] = Sweep(200, 130, .4f, 3);
        bank[S.Gurgle] = Cat(Sweep(200, 450, .08f, 8), Sweep(250, 500, .08f, 8), Sweep(200, 420, .1f, 8));
        bank[S.Whoosh] = Gen(.4f, (t, u) => ((float)R.NextDouble() * 2 - 1) * MathF.Sin(MathF.PI * u), 0);
        bank[S.Pop] = Sweep(300, 900, .05f, 30);
        bank[S.Boing] = Gen(.35f, (t, u) => MathF.Sin(MathF.Tau * (300 + 140 * MathF.Sin(t * 40)) * t), 8);
        bank[S.Gong] = Ring(110, .55f, 3.5f);
        foreach (var k in bank.Keys.ToList()) { var d = bank[k]; float pk = d.Max(Math.Abs); if (pk > 0) { float g = .6f / pk; for (int i = 0; i < d.Length; i++) d[i] *= g; } bank[k] = Stereo(d); }
    }
    static float[] Stereo(float[] m) { var o = new float[m.Length * 2]; for (int i = 0; i < m.Length; i++) o[i * 2] = o[i * 2 + 1] = m[i]; return o; }
    static float[] Gen(float dur, Func<float, float, float> f, float decay = 6)
    {
        int n = Math.Max(1, (int)(dur * SR)); var b = new float[n];
        for (int i = 0; i < n; i++) { float t = i / (float)SR, u = i / (float)Math.Max(1, n - 1); b[i] = f(t, u) * MathF.Exp(-decay * t); }
        return b;
    }
    static float[] Tone(float f, float dur, float decay = 5) => Gen(dur, (t, u) => MathF.Sin(MathF.Tau * f * t), decay);
    static float[] Sweep(float f0, float f1, float dur, float decay = 4) { float ph = 0; return Gen(dur, (t, u) => { ph += MathF.Tau * (f0 + (f1 - f0) * u) / SR; return MathF.Sin(ph); }, decay); }
}
