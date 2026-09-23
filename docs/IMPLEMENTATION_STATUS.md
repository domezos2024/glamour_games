# GlamourGames Android – Implementierungsstatus

Stand: 21.09.2026 (Version 1.1, Kundenfreigabe-Roadmap abgearbeitet)

## Erledigt

- Windows-Referenzprojekt unverändert gelassen.
- Android-Workspace aufgebaut:
  - `D:\AndroidStudioProjects\Nice\_Games\GlamourGames_Android\`
- .NET 10 Android-Projekt erstellt.
- Android SDK/API 36 als Target konfiguriert.
- Mindestversion Android 10 / API 29.
- Landscape-Only für das Game.
- Vollbild-Activity.
- SkiaSharp 4.152.1 integriert.
- Rendering: SKGLSurfaceView (GPU, kontinuierlicher Loop, Standard); SKCanvasView bleibt per Intent-Extra `renderer=canvas` als Fallback.
- Pixel 10 (USB) Messung Hauptmenue: Canvas 10,9 FPS -> GL 81,5 FPS (Worst-Frame 18 ms), im Spiel (Snake) 120 FPS. Logcat-Tag `GGFps`.
- Touch wird per Queue auf den GL-Thread uebergeben; Android-Back per OnBackInvokedCallback (API 36).
- 1600×900 Virtual Canvas übernommen.
- Aspect-Ratio-/Letterbox-Scaling implementiert.
- Touch Down/Move/Up auf die vorhandene Game-Input-Architektur abgebildet.
- Android Back auf Scene-Navigation abgebildet.
- Windows-spezifisches Silk.NET Windowing/OpenGL entfernt.
- Die für die Game-Logik verwendete Key-Abstraktion als kleine Android-kompatible Key-Enum erhalten.
- Game-Core übernommen.
- Alle vorhandenen Game-Dateien übernommen.
- Alle 33 Assets in das APK eingebettet.
- Android-spezifischer Asset-Loader implementiert.
- Android-spezifisches Save-Verzeichnis implementiert.
- Android AudioTrack-basierter SFX-Backend implementiert.
- Prozedurale Hintergrundmusik als Loop implementiert.
- Mute-/Track-Zustand persistent.
- Debug APK erfolgreich gebaut.
- APK auf Pixel_10 Android Emulator installiert.
- Debug-App erfolgreich gestartet.
- Release-APK erfolgreich installiert und gestartet.
- 10 automatisierte Menü-Smoke-Starts durchgeführt; Prozess blieb bei allen 10 aktiv.
- Activity blieb nach Start aktiv; kein eigener FATAL-EXCEPTION-Absturz im geprüften Logcat.
- APK enthält die erwarteten Asset-Dateien.

## Build

Aktueller Build:

- Target: `net10.0-android`
- Debug Build: **0 Fehler**
- Release Build: **0 Fehler** (nach Clean-Build und Deaktivierung der .NET-10-Marshal-Methods für diesen Android-Build)
- Warnungen: überwiegend bestehende SkiaSharp API-Deprecation-Hinweise sowie kleinere Codeanalyse-Warnungen.

APK:

`AndroidApp\bin\Debug\net10.0-android\com.glamourgames.android-Signed.apk`

## Emulator

Vorhandener Emulator:

- `Pixel_10`
- `emulator-5554`

Die App wurde dort installiert und gestartet.

Test-Screenshot:

`Tests\menu_emulator.png`

## Abarbeitung der Offen-Liste (21.09.2026, Pixel 10 per USB)

1. Visuelle Prüfung Menü + alle 10 Scenes: erledigt (Screenshots Tests\smoke).
2. Touch-Smoke aller 10 Spiele (40 Zufallstaps/Wischer je Spiel): erledigt, kein Crash.
3. Windows/Android-Screenshotvergleich: nur GL vs. Canvas verglichen (identisch); Windows-Referenz nicht auf diesem Rechner verglichen.
4. Asset-Darstellung: erledigt (Karten, Slot-Symbole, Würfel, Münze sichtbar korrekt).
5. Audio: AudioTracks (Musik + SFX) laufen laut dumpsys; Musik pausiert nun im Hintergrund. Hörprobe durch Menschen steht aus.
6. Pause/Resume: Home/Zurück 3x ok. GLSurfaceView.OnPause entfernt (EGL-Kontextverlust = schwarzer Bildschirm). Bildschirm-aus/an nicht verifiziert (Sperrbildschirm).
7. Save/Highscore: Snake-Bestwert überlebt Neustart.
8. Alle 10 Spiele per Touch spielbar; Snake bekam Wisch-Steuerung, Tastatur-Hinweistexte durch Touch-Texte ersetzt.
9. Slot: Spin, Autoplay, Freispiele, Gamble-Leiter geprüft.
10. Poker (Hand mit Fold/Call/Raise-UI), Kniffel (Halten, Nachwürfeln, Eintragen, Spielerwechsel): geprüft.
11. Performance: Menü 81 FPS, Spiele 120 FPS (vorher 10,9).
12. Release Build: ok.
13. Signing/AAB: Upload-Key signing\glamourgames-upload.keystore (Passwort signing\store.pass, NICHT weitergeben, Backup anlegen!), AAB in publish\.
14. Warnungen: SkiaSharp-Deprecations (CS0618) u.a. per NoWarn unterdrückt.

Bildschirm-aus-Test und Hörprobe: durch Besitzer bestanden. Langzeittest 4 Min: ok (RAM ~598 MB flach, 118 FPS).
Windows-Vergleich: Quelltext 9 von 11 Spiel-/Core-Dateien identisch, Assets bytegleich (33/33); Bewertung siehe Chat.
Hinweis: Das Spiel wird NICHT im Google Play Store veröffentlicht (Verteilung privat/direkt per APK). Play-Console-Upload entfällt.


## Kundenfreigabe-Roadmap (21.09.2026) - umgesetzt

Details und Belege je Schritt: `ROADMAP_KUNDENFREIGABE.md`, Vergleich Windows/Android: `Tests\compare\BEFUND.md`, Uebergabe: `UEBERGABE_HINWEIS.md`.

- Schriften eingebettet (Selawik, PT Serif, Noto Sans Symbols 2), Mindest-Textgroesse 24, Trefferflaeche mind. 48 dp (Button.HitPad), Snake-Steuerkreuz.
- Speicher/GC: Shader- und Pfad-Caches (Bloom, Backdrop, Gfx.Radial/Ball/Star, Banner-Bitmap).
- Schiffe: fremde Flotte nie sichtbar. Blackjack: "Neues Spiel" mit Bestaetigung.
- Softtastatur fuer Namensdialoge (GLGameView.OnCreateInputConnection, kompakter Dialog).
- Muenzwurf (Core/CoinToss.cs) nur bei neuem Spiel, Folgerunden wechseln Startspieler.
- Party-Engine (Core/DiceParade.cs): Wuerfel, flache Spielsteine, Marine-Matrosen; Sieger-Banner (Scene.Celebrate).
- Optionen (Games/Options.cs): Musik, Effekte, Lautstaerke, Spielernamen (Core/Players.cs), Vibration (Core/Haptics.cs).
- Test-Direktstart: Intent-Extra `game` (0..9).

## Wichtiger technischer Punkt

Der aktuelle Port verwendet bewusst den vorhandenen C#-Game-Core statt Unity/Unreal. Das reduziert die Abweichung von der Windows-Referenz und ermöglicht die Wiederverwendung der vorhandenen Spielmechaniken und Renderlogik.

Die Android-Version ist damit bereits ein echter portierter Game-Core mit Android-Host, nicht lediglich ein Mockup oder eine UI-Demo.
