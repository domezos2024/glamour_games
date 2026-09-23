using Android.App;
using Android.OS;
using Android.Views;
using Android.Window;
using Android.Content.PM;

[assembly: global::Android.App.UsesPermission(global::Android.Manifest.Permission.Vibrate)]
[assembly: global::Android.App.Application(Icon = "@mipmap/ic_launcher", RoundIcon = "@mipmap/ic_launcher_round")]
namespace GlamourGames.Android;

static class Assets_ { public static void Preload() => GlamourGames.Assets.Preload(); }

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.KeyboardHidden,
    Exported = true)]
public sealed class MainActivity : Activity
{
    IGameSurface gameView;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
        App.TestModal = Intent?.GetIntExtra("modal", 0) ?? 0;
        App.StartGame = Intent?.GetIntExtra("game", -1) ?? -1;
        bool gl = Intent?.GetStringExtra("renderer") != "canvas";
        var v = gl ? (View)new GLGameView(this) : new GameView(this);
        gameView = (IGameSurface)v;
        SetContentView(v);
        Window.SetSoftInputMode(SoftInput.AdjustNothing);
        App.Keyboard = show => RunOnUiThread(() =>
        {
            var imm = (global::Android.Views.InputMethods.InputMethodManager)GetSystemService(InputMethodService);
            if (show) { v.RequestFocus(); imm.ShowSoftInput(v, global::Android.Views.InputMethods.ShowFlags.Implicit); }
            else imm.HideSoftInputFromWindow(v.WindowToken, 0);
        });
        System.Threading.Tasks.Task.Run(Assets_.Preload);
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(0, new BackCb(() => HandleBack()));
    }

    protected override void OnResume()
    {
        base.OnResume();
        gameView?.ResumeGame();
        Sfx.AppResume();
    }

    protected override void OnPause()
    {
        gameView?.PauseGame();
        Sfx.AppPause();
        base.OnPause();
    }

    void HandleBack()
    {
        if (gameView is GLGameView) TouchQueue.Post(App.Back); else App.Back();
    }

    public override void OnBackPressed() => HandleBack();

    sealed class BackCb : Java.Lang.Object, IOnBackInvokedCallback
    {
        readonly Action a;
        public BackCb(Action a) => this.a = a;
        public void OnBackInvoked() => a();
    }
}