# Changelog

## [1.8.1] - 2026-09-23

Erste öffentliche Version auf GitHub.

### Neu
- Versions- und Urheberangabe im Optionen-Bildschirm
- Öffentliches Repository mit Dokumentation, Screenshots und Issue-Vorlagen

### Build
- Release-Signierung repariert: getrennte Passwortdatei für Store und Key (apksigner las beide Werte aus demselben Stream und brach mit `end of file reached` ab)
- versionCode 181, versionName 1.8.1

## [1.1] - 2026-09-21

Private Kundenfreigabe.

- Eingebettete Schriften, Mindest-Textgröße 24, Touch-Ziele mindestens 48 dp
- GPU-Rendering über SKGLSurfaceView (Menü 10,9 → 81 FPS, Spiele 120 FPS auf Pixel 10)
- Optionen: Musik, Effekte, Lautstärke, Spielernamen, Vibration
- Münzwurf zu Beginn jedes Zwei-Spieler-Spiels, Sieger-Feier mit Würfel-/Spielstein-Parade
- Snake-Steuerkreuz, Softtastatur für Namensdialoge, Schiffe Versenken mit verdeckter Gegnerflotte
