using Android.Content;
using Android.Views;
using SkiaSharp.Views.Android;

namespace GlamourGames.Android;

public interface IGameSurface { void ResumeGame(); void PauseGame(); }

public sealed class GameView : SKCanvasView, IGameSurface
{
    long lastNs;
    readonly FpsMeter fps = new("Canvas");
    volatile bool running = true;

    public GameView(Context context) : base(context)
    {
        IgnorePixelScaling = false;
        PaintSurface += OnPaintSurface;
        Touch += HandleTouch;
        lastNs = Java.Lang.JavaSystem.NanoTime();
    }

    public void ResumeGame()
    {
        running = true;
        lastNs = Java.Lang.JavaSystem.NanoTime();
        Invalidate();
    }

    public void PauseGame() => running = false;

    void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        long now = Java.Lang.JavaSystem.NanoTime();
        float dt = (now - lastNs) / 1_000_000_000f;
        lastNs = now;
        if (!running) dt = 0;
        App.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height, dt);
        fps.Frame();
        if (running) PostInvalidateDelayed(16);
    }

    void HandleTouch(object? sender, TouchEventArgs e)
    {
        var ev = e.Event;
        if (ev == null) return;
        float x = ev.GetX();
        float y = ev.GetY();
        switch (ev.ActionMasked)
        {
            case MotionEventActions.Down:
            case MotionEventActions.PointerDown:
                App.TouchDown(x, y); break;
            case MotionEventActions.Move:
                App.TouchMove(x, y); break;
            case MotionEventActions.Up:
            case MotionEventActions.PointerUp:
            case MotionEventActions.Cancel:
                App.TouchUp(x, y); break;
        }
        e.Handled = true;
    }
}