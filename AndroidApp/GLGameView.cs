using Android.Content;
using Android.Views;
using SkiaSharp;
using Android.Runtime;
using Android.Text;
using Android.Views.InputMethods;
using SkiaSharp.Views.Android;

namespace GlamourGames.Android;

public sealed class GLGameView : SKGLSurfaceView, IGameSurface
{
    volatile bool running = true;
    long lastNs = Java.Lang.JavaSystem.NanoTime();
    readonly FpsMeter fps = new("GL");

    public GLGameView(Context context) : base(context)
    {
        PaintSurface += OnPaint;
        Touch += HandleTouch;
        Focusable = true; FocusableInTouchMode = true;
    }

    public override bool OnCheckIsTextEditor() => true;

    public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
    {
        if (keyCode == Keycode.Del) { TouchQueue.Post(() => App.Key(Silk.NET.Input.Key.Backspace)); return true; }
        if (keyCode == Keycode.Enter) { TouchQueue.Post(() => App.Key(Silk.NET.Input.Key.Enter)); return true; }
        int u = e.UnicodeChar; if (u > 32) { TouchQueue.Post(() => App.TypeChar((char)u)); return true; }
        return base.OnKeyDown(keyCode, e);
    }

    public override IInputConnection OnCreateInputConnection(EditorInfo outAttrs)
    {
        outAttrs.InputType = InputTypes.ClassText | InputTypes.TextVariationVisiblePassword | InputTypes.TextFlagNoSuggestions | InputTypes.TextFlagCapWords;
        outAttrs.ImeOptions = ImeFlags.NoExtractUi | ImeFlags.NoFullscreen | (ImeFlags)(int)ImeAction.Done;
        return new KbConn(this);
    }

    sealed class KbConn : BaseInputConnection
    {
        public KbConn(View v) : base(v, false) { }
        public override bool CommitText(Java.Lang.ICharSequence text, int newCursorPosition)
        {
            var t = text?.ToString(); if (!string.IsNullOrEmpty(t)) TouchQueue.Post(() => { foreach (var ch in t) App.TypeChar(ch); });
            return true;
        }
        public override bool SetComposingText(Java.Lang.ICharSequence text, int newCursorPosition) => CommitText(text, newCursorPosition);
        public override bool DeleteSurroundingText(int beforeLength, int afterLength) { TouchQueue.Post(() => { for (int i = 0; i < Math.Max(1, beforeLength); i++) App.Key(Silk.NET.Input.Key.Backspace); }); return true; }
        public override bool PerformEditorAction(ImeAction actionCode) { TouchQueue.Post(() => App.Key(Silk.NET.Input.Key.Enter)); return true; }
        public override bool SendKeyEvent(KeyEvent e)
        {
            if (e.Action != KeyEventActions.Down) return true;
            if (e.KeyCode == Keycode.Del) TouchQueue.Post(() => App.Key(Silk.NET.Input.Key.Backspace));
            else if (e.KeyCode == Keycode.Enter) TouchQueue.Post(() => App.Key(Silk.NET.Input.Key.Enter));
            else { int u = e.UnicodeChar; if (u > 0) TouchQueue.Post(() => App.TypeChar((char)u)); }
            return true;
        }
    }

    public void ResumeGame()
    {
        running = true;
    }

    public void PauseGame()
    {
        running = false;
    }

    void HandleTouch(object sender, TouchEventArgs e)
    {
        var ev = e.Event;
        if (ev == null) return;
        float x = ev.GetX(), y = ev.GetY();
        switch (ev.ActionMasked)
        {
            case MotionEventActions.Down:
            case MotionEventActions.PointerDown:
                TouchQueue.Post(() => App.TouchDown(x, y)); break;
            case MotionEventActions.Move:
                TouchQueue.Post(() => App.TouchMove(x, y)); break;
            case MotionEventActions.Up:
            case MotionEventActions.PointerUp:
            case MotionEventActions.Cancel:
                TouchQueue.Post(() => App.TouchUp(x, y)); break;
        }
        e.Handled = true;
    }

    void OnPaint(object sender, SKPaintGLSurfaceEventArgs e)
    {
        long now = Java.Lang.JavaSystem.NanoTime();
        float dt = running ? (now - lastNs) / 1_000_000_000f : 0;
        lastNs = now;
        TouchQueue.Drain();
        App.Draw(e.Surface.Canvas, e.BackendRenderTarget.Width, e.BackendRenderTarget.Height, dt);
        fps.Frame();
    }
}
