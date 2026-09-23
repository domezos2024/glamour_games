using Android.Content;
using Android.OS;
namespace GlamourGames;
static class Haptics
{
    public static bool On = true;
    static Vibrator vib;
    static Vibrator V => vib ??= (Vibrator)global::Android.App.Application.Context.GetSystemService(Context.VibratorService);
    public static void Init() { On = Save.Int("haptics", 1) != 0; }
    public static void Set(bool on) { On = on; Save.Set("haptics", on ? 1 : 0); }
    static void Buzz(long ms, int amp = -1) { if (!On) return; try { var v = V; if (v == null || !v.HasVibrator) return; v.Vibrate(VibrationEffect.CreateOneShot(ms, amp < 0 ? VibrationEffect.DefaultAmplitude : amp)); } catch { } }
    static void Pattern(long[] p, int[] a) { if (!On) return; try { var v = V; if (v == null || !v.HasVibrator) return; v.Vibrate(VibrationEffect.CreateWaveform(p, a, -1)); } catch { } }
    public static void Tap() => Buzz(12, 60);
    public static void Hit() => Buzz(40, 140);
    public static void Toss() => Pattern(new long[] { 0, 25, 60, 25, 60, 40 }, new[] { 0, 90, 0, 90, 0, 140 });
    public static void Win() => Pattern(new long[] { 0, 60, 70, 60, 70, 120 }, new[] { 0, 120, 0, 160, 0, 220 });
    public static void Lose() => Buzz(90, 70);
}
