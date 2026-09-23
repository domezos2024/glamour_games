# GlamourGames – Android-Portierungsleitfaden

**Projekt:** GlamourGames  
**Ausgangsprojekt:** `D:\source\repos\Small Games\GlamourGames`  
**Android-Zielworkspace:** `D:\AndroidStudioProjects\NiceGames`  
**Status:** Planung / Bestandsaufnahme abgeschlossen – Implementierung noch NICHT gestartet  
**Stand:** 21.09.2026

---

## 1. Ziel

GlamourGames soll als eigenständige Android-App umgesetzt werden und funktional sowie visuell so nah wie technisch sinnvoll an der vorhandenen Windows-Version bleiben.

Das bedeutet ausdrücklich:

- kein vereinfachter „Mobile-Port“
- keine ausgelassenen Spiele
- keine entfernten Spielmechaniken
- keine pauschale Ersetzung der vorhandenen Effekte durch einfache Android-Widgets
- Übernahme der vorhandenen Spielregeln, Animationen, UI-Struktur, Sounds, Speicherstände und Grafiklogik, soweit die Windows-Plattformabhängigkeiten dies erlauben
- Anpassung nur dort, wo Android andere Eingabe-, Lebenszyklus-, Audio-, Speicher- oder Displaymechanismen benötigt
- Touch-Bedienung statt Mausbedienung, mit gleicher visueller Hierarchie
- hochwertige 2D-Grafik, Partikel, Glow/Bloom, Übergänge und die vorhandenen 3D-artigen Darstellungen sollen erhalten oder bei Bedarf technisch verbessert werden
- saubere Android-Lifecycle-Behandlung, Rotation/Resize-Konzept, Pause/Resume und Wiederherstellung
- reproduzierbare Debug- und Release-Builds

**Wichtig:** Die Windows-Version bleibt Referenz und darf während der Portierung nicht unbeabsichtigt verändert werden.

---

## 2. Bestandsaufnahme des Windows-Projekts

Die Quelle wurde direkt auf dem Windows-Rechner untersucht.

## 2.1 Aktuelle Technologie

Das Projekt ist **kein Unity- oder Unreal-Projekt**.

Es handelt sich um eine eigene C#-Game-Anwendung mit:

- .NET 10
- Silk.NET.Windowing 2.22.0
- Silk.NET.Input 2.22.0
- Silk.NET.OpenGL 2.22.0
- SkiaSharp 3.119.0
- NAudio.WinMM 2.2.1
- eigener Scene-/UI-/Animations-/Partikel-/Audio-/Save-Architektur
- 1600×900 virtuellem Designraum
- OpenGL/SkiaSharp-basierter Darstellung
- eigenem prozeduralem Audio-System
- eigener 3D-Würfel-/Dice-Darstellung
- eigenen Glow-/Bloom-/Partikel-/Animationseffekten

Der Windows-Build wurde erfolgreich geprüft:

- .NET SDK: 10.0.401
- Windows 11 Build: 10.0.26200
- Build-Ergebnis: **0 Fehler / 0 Warnungen**

## 2.2 Umfang

Aktuell vorhanden:

- **24 C#-Dateien**
- **3.366 Zeilen C#-Quellcode** nach der aktuellen Bestandsaufnahme
- **11 Spiele bzw. spielbezogene Scenes**
- **33 Grafik-Assets**
  - 21 PNG
  - 2 JPG
  - 10 WEBP
- separate Core-Systeme für Animation, Rendering, UI, Audio, Speicherung, Partikel, Bloom und Würfel-/3D-Darstellung

## 2.3 Vorhandene Spiele

Folgende Spiele müssen vollständig berücksichtigt werden:

1. Battleship
2. Connect Four
3. Memory
4. Nim
5. Snake
6. Tic-Tac-Toe
7. Blackjack
8. Poker / Texas Hold'em
9. Kniffel
10. Slot / „Buch der Pharaonen“
11. Menu als zentrale Auswahl-/Navigations-Scene

Die drei komplexesten Portierungskandidaten sind nach Quellumfang und Rendering-/Mechanikdichte:

- Slot – ca. 590 Zeilen
- Poker – ca. 296 Zeilen
- Battleship – ca. 224 Zeilen
- Kniffel – ca. 182 Zeilen

Diese Reihenfolge ist **keine Qualitätsbewertung**, sondern eine technische Aufwandseinschätzung.

---

## 3. Analyse der vorhandenen Architektur

## 3.1 Core

### Anim.cs

Enthält:

- Easing-Funktionen
- Spring-Animationen
- Tweens
- Timer
- Coroutine-System

Diese Logik ist weitgehend plattformunabhängig und sollte nahezu unverändert übernommen werden.

### App.cs

Enthält:

- Haupt-Loop
- Window-/Framebuffer-Verarbeitung
- virtuelle Koordinaten
- Eingabeverarbeitung
- Scene-Wechsel
- Fade
- Flash
- Screen Shake
- Toasts
- Screenshot-Logik
- Musik-/Mute-HUD
- Rendering-Aufruf

Hier liegt der wichtigste Plattformumbau.

Die Windows-spezifische Windowing-/Input-Schicht wird durch eine Android-Host-/Render-Schicht ersetzt.

### Gfx.cs

Enthält:

- Farben
- Fonts
- Rechtecke
- Gradients
- Strokes
- Glow
- Radialeffekte
- Text
- Bilder
- Herzen
- Karten-Suits
- Sterne

Der Großteil ist ideal für eine SkiaSharp-basierte Android-Portierung geeignet.

### Scene.cs

Enthält:

- gemeinsame Scene-Basis
- UI
- Modal-System
- Celebration-/Result-System
- Highscore-Ausgabe
- Name Entry
- gemeinsames Rendering

Diese Architektur soll erhalten bleiben.

### Ui.cs

Enthält:

- Buttons
- Hover-/Press-Animationen
- Modal-Dialoge
- Texteingabe
- Touch-/Pointer-Logik

Hier ist eine Android-Erweiterung erforderlich:

- Maus-Hover wird optional
- Touch Down/Move/Up
- Tap
- ggf. Long Press
- Keyboard/InputMethod für Namenseingaben
- größere Touch-Hitboxen ohne Änderung der sichtbaren Buttongröße

### Sfx.cs

Das bestehende System basiert auf **NAudio.WinMM** und ist Windows-spezifisch.

Die Soundeffekte selbst sind jedoch prozedural erzeugt.

Für Android wird deshalb nicht das komplette Soundsystem neu erfunden. Stattdessen:

- vorhandene Sounddefinitionen übernehmen
- plattformabhängigen Audio-Ausgabeteil austauschen
- Android AudioTrack bzw. geeignete .NET-/Android-Audio-API verwenden
- Musik-/Effekt-Mixer erhalten
- Mute/Volume/Track-State erhalten
- Audio-Lifecycle bei Pause/Resume korrekt behandeln

### Save.cs

Aktuell wird unter Windows in AppData gespeichert.

Auf Android wird daraus ein App-internes persistentes Verzeichnis bzw. SharedPreferences/Dateispeicher.

Die logische Save-API soll möglichst identisch bleiben:

- Int
- String
- Set
- Scores
- IsHigh
- AddScore

Dadurch müssen die Spiele ihre Save-Logik nicht neu erlernen.

### Particles.cs / Bloom.cs / DiceParade.cs

Diese Systeme sind für die visuelle Identität besonders wichtig.

Sie werden nicht entfernt.

Ziel:

- Partikel erhalten
- Glow erhalten
- Bloom erhalten
- Screen Shake erhalten
- Dice-Parade erhalten
- Animationstiming erhalten
- bei Bedarf GPU-/Skia-Optimierungen für Android ergänzen

---

## 4. Technische Entscheidung für Android

## 4.1 Empfohlene Hauptlösung

### **Native .NET-for-Android + SkiaSharp**

Nicht Unity und nicht Unreal als primäre Portierungsbasis.

### Begründung

Das Ausgangsprojekt besitzt bereits:

- C#-Game-Logik
- eigene Scene-Architektur
- eigene Rendering-Abstraktion
- SkiaSharp-Zeichenfunktionen
- eigene Animationen
- eigene UI
- eigene Partikel
- eigene 3D-artige Würfeldarstellung
- eigenes Audio-System

Ein Wechsel auf Unity/Unreal würde einen großen Teil dieser Architektur vollständig neu schreiben.

Das würde die gewünschte 1:1-Portierung unnötig riskant machen.

Mit .NET for Android kann ein großer Teil des bestehenden C#-Codes weiterverwendet werden.

SkiaSharp bietet auf Android sowohl normale Skia-Views als auch hardwarebeschleunigte Oberflächen. Die Android-Dokumentation beschreibt unter anderem `SKSurfaceView`, `SKGLSurfaceView` und `SKGLTextureView` als hardwarebeschleunigte Optionen.

Quelle: Microsoft Learn – SkiaSharp.Views.Android.

## 4.2 Rendering-Strategie

Primär:

### **SkiaSharp + hardwarebeschleunigte Android-Oberfläche**

Für die vorhandene Anwendung ist das passender als ein kompletter Vulkan-Neubau.

Die aktuelle Anwendung ist überwiegend:

- 2D UI
- 2D Assets
- Text
- Glow/Bloom
- Partikel
- prozedurale Formen
- animierte Karten
- animierte Chips
- 3D-artige Würfel

Es handelt sich nicht um eine klassische vollwertige 3D-Engine-Szene.

### Vulkan

Vulkan wird trotzdem bei der Architekturprüfung berücksichtigt.

Android bezeichnet Vulkan als moderne bzw. bevorzugte Low-Level-Grafik-API und empfiehlt für neue native Game-Engines Vulkan. Für diese konkrete Portierung wäre ein vollständiger Vulkan-Renderer jedoch ein separater Engine-Neubau.

Deshalb:

- **Phase 1:** SkiaSharp-Hardwarepfad
- **Phase 2:** Profiling
- **Phase 3:** Vulkan nur dann, wenn reale Messdaten zeigen, dass SkiaSharp zum Flaschenhals wird

Diese Entscheidung kann später anhand echter Android-Performance geändert werden.

---

## 5. Android-Projektstruktur

Zielstruktur:

`D:\AndroidStudioProjects\Nice\_Games\`

Vorgesehene Struktur:

- `GlamourGames.Android.csproj`
- `MainActivity.cs`
- `GameView.cs`
- `AndroidApp.cs`
- `Core/`
- `Games/`
- `Assets/`
- `Audio/`
- `Platform/Android/`
- `Resources/drawable/`
- `Resources/mipmap-*/`
- `Resources/values/`
- `Properties/`
- `README.md`
- `PORTING_STATUS.md`

Die genaue Ordnerstruktur wird erst nach Erstellung des Grundprojekts festgeschrieben.

---

## 6. Zielplattform

Für die erste stabile Zielversion:

- Android 10+ als bevorzugte Mindestplattform
- 64-bit ARM
- moderne Vulkan-fähige Geräte bevorzugt
- OpenGL ES bzw. Android-GPU-Fallback entsprechend der gewählten SkiaSharp-Oberfläche
- Hochformat/Querformat-Konzept wird vor der Implementierung festgelegt
- Referenzdesign bleibt 1600×900

Android empfiehlt für neue hochwertige Spiele mindestens Android 10/API 29 und Vulkan 1.1 als moderne Basis, wenn Vulkan direkt eingesetzt wird.

Für die konkrete GlamourGames-Portierung wird zunächst keine unnötige harte Vulkan-Pflicht eingeführt, weil die Anwendung über SkiaSharp gerendert werden soll.

---

## 7. Virtuelle Auflösung und Display-Anpassung

Die Windows-Version arbeitet mit:

- VW = 1600
- VH = 900

Diese Koordinaten sollen auf Android **nicht einfach auf Pixel umgerechnet** werden.

Stattdessen:

1. Android-Surface-Größe ermitteln.
2. Seitenverhältnis berechnen.
3. Letterboxing bzw. kontrolliertes Cropping festlegen.
4. Transformationsmatrix für den virtuellen 1600×900-Raum erstellen.
5. Touch-Koordinaten zurück in virtuelle Koordinaten transformieren.
6. UI-Hitboxes auf Touchfreundlichkeit erweitern.
7. keine sichtbaren Proportionen der Originaloberfläche verändern.

Ziel:

**Ein Android-Touch auf derselben visuellen Stelle muss dieselbe Spielaktion auslösen wie ein Mausklick auf Windows.**

---

## 8. Eingabemigration

Windows:

- Mouse Move
- Mouse Down
- Mouse Up
- Wheel
- Keyboard
- Escape

Android:

- Touch Down
- Touch Move
- Touch Up
- ggf. Multi-Touch nur dort, wo sinnvoll
- Back-Taste
- Android-Softkeyboard
- optional Hardwarekeyboard

Mapping:

- Mouse Move → Touch Move
- Mouse Down → Touch Down
- Mouse Up → Touch Up
- Escape → Android Back
- Name Entry → Android InputMethod
- Wheel → Swipe/Scroll, falls erforderlich

Hover-Effekte dürfen nicht die Funktionalität voraussetzen.

Wenn ein Button auf Windows nur bei Hover glüht, muss Android eine gleichwertige visuelle Reaktion über Press/Tap/Focus erzeugen.

---

# 9. Audio-Portierung

Das Audio-System muss vollständig erhalten bleiben.

Zu portieren:

- 45+ SFX-Typen
- Click
- Hover
- Flip
- Match
- NoMatch
- PlaceX
- PlaceO
- Win
- Lose
- Drop
- Hit
- Miss
- Sunk
- Eat
- Die
- Dice
- Deal
- Coin
- Spin
- Stop
- Big
- Chip
- Take
- Turn
- Tick
- Splash
- Blub
- Boom
- Crackle
- Thunder
- Zap
- Launch
- FwPop
- Cheer
- Clap
- Step
- Shake
- Sparkle
- Fanfare
- Creak
- Gurgle
- Whoosh
- Pop
- Boing
- Gong

Zusätzlich:

- fünf vorhandene Musiktracks
- Musik an/aus
- Mute
- Lautstärke
- persistenter Musikstatus

Die prozedurale Soundgenerierung soll nach Möglichkeit weiterverwendet werden.

---

## 10. Save-System

Windows speichert:

`%APPDATA%\GlamourGames\save.json`

Android bekommt ein eigenes internes App-Verzeichnis.

Zu erhalten:

- Spielstände
- Einstellungen
- Musikstatus
- Mute-Status
- Highscores
- Namen
- Spiel-spezifische Keys

Wichtig:

Das Save-System darf nicht von absoluten Windows-Pfaden abhängen.

Empfehlung:

`ISaveStore` bzw. Plattformabstraktion.

---

## 11. Grafik- und Effektziele

Die Android-Version soll mindestens den visuellen Stand der Windows-Version erreichen.

Beizubehalten:

- Neon-Farbschema
- Cyan/Pink/Magenta
- Gold
- dunkler Hintergrund
- Glow
- Bloom
- Gradients
- Text Glow
- Schatten
- Screen Shake
- Flash
- Partikel
- Spring-Animationen
- OutBack/Elastic/Bounce-Easing
- Kartenanimationen
- Slot-Reel-Animationen
- Gewinnlinien
- Würfelanimationen
- Dice-Parade
- Chip-/Card-Effekte

Optimierungen:

- Texture-/Bitmap-Caching
- Paint-Caching
- Blur-Caching
- Vermeidung unnötiger Allokationen pro Frame
- Dirty-Region-Überlegungen
- reduzierter Overdraw
- feste Ziel-FPS
- Frame-Pacing
- Asset-Kompression
- Lebenszyklusabhängiges Pausieren

---

## 12. Asset-Portierung

Alle 33 Assets müssen geprüft und übernommen werden.

Besondere Gruppen:

### Allgemeine Symbole

- butterfly
- car
- clover
- coffee
- crown
- diamond
- dragonfly
- gamepad
- heart
- infinity
- moon
- notes
- palms
- paw
- rose
- star
- sun
- unicorn
- wings
- wolf

### Coin Toss

- coin_H.jpg
- coin_T.jpg

### Slot

- slot_A.webp
- slot_BOOK.webp
- slot_EXPLORER.webp
- slot_GODDESS.webp
- slot_J.webp
- slot_K.webp
- slot_PHARAOH.webp
- slot_Q.webp
- slot_SCARAB.webp
- slot_T.webp

### App-Icons

- icon.png
- app.ico / Icon-Logo.png als Windows-Referenzen

Die Assets dürfen nicht versehentlich durch Platzhalter ersetzt werden.

---

## 13. Portierungsreihenfolge

## Schritt 1 – Baseline sichern

Aufwand: ca. 0,5–1 Stunde  
KI-Aufwand: ca. 2–5k Tokens

- Windows-Projekt nicht verändern.
- Bestandsaufnahme dokumentieren.
- Build reproduzierbar prüfen.
- Assetliste sichern.
- ggf. Ausgangsarchiv erstellen.
- Referenz-Screenshots erzeugen.

Ergebnis:

**Unveränderliche Windows-Referenz.**

---

## Schritt 2 – Android-Projekt anlegen

Aufwand: ca. 1–2 Stunden  
KI-Aufwand: ca. 5–10k Tokens

- .NET Android Projekt erzeugen.
- Namespace festlegen.
- Application ID festlegen.
- Android Manifest.
- Min/Target SDK.
- Debug-Konfiguration.
- Release-Konfiguration.
- Android Icon.
- Landscape-Konfiguration.
- benötigte NuGet-Pakete.

Ergebnis:

**Leere startfähige Android-App.**

---

## Schritt 3 – Rendering-Host

Aufwand: ca. 2–4 Stunden  
KI-Aufwand: ca. 10–20k Tokens

- MainActivity
- GameView
- Surface-Lifecycle
- SkiaSharp initialisieren
- Renderloop
- 1600×900 Virtual Canvas
- Scaling
- Touch-Koordinaten
- Pause/Resume

Ergebnis:

**Leere Android-Szene mit exakt passendem Koordinatensystem.**

---

## Schritt 4 – Core portieren

Aufwand: ca. 3–6 Stunden  
KI-Aufwand: ca. 15–30k Tokens

Portieren:

- Anim
- Ease
- Spring
- Tween
- Timers
- Coroutines
- Scene
- UI
- Gfx
- Art
- Particles
- Bloom
- DiceParade

Windows-spezifische Stellen werden abstrahiert.

Ergebnis:

**Portierbarer Game-Core.**

---

## Schritt 5 – Asset-Pipeline

Aufwand: ca. 1–3 Stunden  
KI-Aufwand: ca. 5–12k Tokens

- Assets kopieren.
- Android Build Actions korrekt setzen.
- WebP/PNG/JPG laden.
- Asset-Cache.
- Dichte-/Scaling-Prüfung.
- fehlende/defekte Dateien erkennen.

Ergebnis:

**Alle Originalgrafiken verfügbar.**

---

## Schritt 6 – Audio

Aufwand: ca. 3–6 Stunden  
KI-Aufwand: ca. 12–25k Tokens

- Android Audio-Ausgabe.
- SFX-Mixer.
- Musik.
- Mute.
- Volume.
- Trackwechsel.
- Pause/Resume.
- AudioFocus.
- Ressourcenfreigabe.

Ergebnis:

**Windows-ähnliches Audioverhalten auf Android.**

---

## Schritt 7 – Menü

Aufwand: ca. 1–2 Stunden  
KI-Aufwand: ca. 5–10k Tokens

- Menü portieren.
- Touch-Hitboxen.
- Navigation.
- Musik/Mute.
- Übergänge.
- Icons.
- Animationsverhalten.

Ergebnis:

**Erste vollständig benutzbare Android-Version.**

---

## Schritt 8 – Einfache Spiele

Aufwand: ca. 5–8 Stunden  
KI-Aufwand: ca. 20–35k Tokens

Reihenfolge:

1. Tic-Tac-Toe
2. Connect Four
3. Nim
4. Snake
5. Memory

Dabei werden gleichzeitig:

- Input
- Scene-Wechsel
- Result-Dialog
- Highscores
- Touch
- Animationen

validiert.

Ergebnis:

**Kompletter Gameplay-Stack mit mehreren Spielen.**

---

## Schritt 9 – Battleship

Aufwand: ca. 3–5 Stunden  
KI-Aufwand: ca. 12–22k Tokens

Prüfen:

- Grid
- Touch-Ziele
- Platzierung
- Treffer
- Versenken
- KI
- Animation
- Result

---

## Schritt 10 – Blackjack

Aufwand: ca. 3–5 Stunden  
KI-Aufwand: ca. 12–22k Tokens

Prüfen:

- Kartenanimation
- Flip
- Bets
- Credits
- Double
- Stand
- Bust
- Dealer
- Blackjack
- Result
- Persistenz

---

## Schritt 11 – Kniffel

Aufwand: ca. 4–7 Stunden  
KI-Aufwand: ca. 15–28k Tokens

Besonderer Schwerpunkt:

- 3D-Würfel
- Würfelanimation
- Würfelrotation
- Held-Würfel
- Score-Tabelle
- Touch-Auswahl
- Dice-Parade
- Result

---

## Schritt 12 – Poker

Aufwand: ca. 5–9 Stunden  
KI-Aufwand: ca. 20–40k Tokens

Prüfen:

- Karten
- Blinds
- Chips
- Pot
- Side-Pots
- Fold
- Call
- Raise
- All-In
- AI
- Hand Evaluation
- Equity
- Showdown
- Animation
- Slider-Touchsteuerung

Poker erhält eine eigene intensive Testphase.

---

## Schritt 13 – Slot

Aufwand: ca. 7–12 Stunden  
KI-Aufwand: ca. 30–55k Tokens

Prüfen:

- fünf Reels
- Symbolstreifen
- Spin
- Fast Stop
- Gewinnlinien
- Paytable
- Scatter
- Free Spins
- Autoplay
- Gamble
- Cards
- Ladder
- Gewinnanimation
- Credits
- Sounds
- Partikel
- Glow
- Touchsteuerung

Dies ist voraussichtlich die umfangreichste Einzelportierung.

---

# 14. Plattformabstraktion

Folgende Schnittstellen sollen eingeführt werden, bevor zu viele Spiele portiert sind:

### IGamePlatform

Mögliche Bereiche:

- File Storage
- Audio
- Input
- Display
- Haptics
- Lifecycle

### IAudioBackend

Windows:

- NAudio

Android:

- Android AudioTrack / geeignete Android-Audio-API

### IStorageBackend

Windows:

- AppData

Android:

- Application Context FilesDir

### IInputBackend

Windows:

- Silk.NET Input

Android:

- Touch/Key/Back/InputMethod

Dadurch bleibt die Game-Logik identisch.

---

# 15. Android-Lifecycle

Muss explizit implementiert werden.

Bei Pause:

- Renderloop pausieren
- Musik kontrolliert pausieren
- SFX-Backend ggf. freigeben
- Zustand erhalten

Bei Resume:

- Surface neu initialisieren
- GPU-Ressourcen prüfen
- Musikzustand wiederherstellen
- Rendering fortsetzen

Bei Surface-Neuerstellung:

- keine Save-Daten verlieren
- keine Scene unkontrolliert neu starten
- keine doppelte Audioausgabe

---

# 16. Performance-Ziele

Primäres Ziel:

- 60 FPS stabil auf modernen Mittelklassegeräten
- 90/120 Hz optional
- keine sichtbaren Frame-Spikes bei normalen UI-Animationen
- keine Speicherlecks
- keine wiederholte Asset-Decodierung
- keine unnötigen großen Objektallokationen im Renderloop

Messungen:

- FPS
- Frame Time
- CPU
- GPU
- RAM
- GC
- Draw Calls bzw. Skia-Aufwand
- Surface-Reinitialisierung
- Audio-Latenz

Android bietet offizielle Performance-/GPU-Analysewerkzeuge; die aktive Grafik-API soll nicht allein anhand von Logcat beurteilt werden.

---

# 17. Qualitätssicherung

Für jedes Spiel wird eine Testmatrix geführt.

## Funktional

- Start
- Neustart
- Zurück
- Spielende
- Result
- Highscore
- Save
- Reload
- Pause
- Resume

## Eingabe

- Tap
- schneller Tap
- langsamer Tap
- Drag
- Swipe
- Android Back
- Softkeyboard

## Grafik

- 16:9
- 18:9
- 19.5:9
- große Displays
- kleine Displays
- unterschiedliche DPI
- Dark/Light-Systemeinstellungen, sofern relevant

## Stabilität

- App minimieren
- App wieder öffnen
- Bildschirm sperren
- Rotation bzw. definierte Orientierung
- Low-Memory-Situation
- längere Spielzeit

---

# 18. Vergleichstest Windows ↔ Android

Für die 1:1-Anforderung wird eine Referenzsammlung aufgebaut.

Für jede Scene:

1. Windows Screenshot.
2. Android Screenshot.
3. gleiche Spielsituation.
4. gleiche Animation möglichst zum gleichen Zeitpunkt.
5. Overlay/Differenzvergleich.
6. Abweichungen dokumentieren.
7. korrigieren.
8. erneut vergleichen.

Besonders wichtig:

- Position
- Größe
- Farbe
- Alpha
- Glow
- Text
- Schriftgröße
- Animation
- Timing
- Button-Hitbox
- Karten-/Würfelrotation
- Slot-Reel-Position

---

# 19. Testgeräte

Mindestens:

### Referenzgerät

- modernes Android-Gerät
- 60+ FPS
- Vulkan-fähig

### Mittelklasse

- durchschnittliche GPU
- durchschnittlicher RAM

### Schwächeres Gerät

- zur Erkennung von Performance-/Kompatibilitätsproblemen

Optional:

- Tablet
- Foldable
- 120-Hz-Gerät

---

# 20. Build- und Release-System

Debug:

- APK
- USB/ADB-Installation
- Logcat

Release:

- Release APK
- AAB
- Signing
- VersionCode
- VersionName
- ProGuard/R8-Konfiguration nur nach Tests
- Release-Smoke-Test

Für Google Play:

- Android App Bundle
- Play App Signing
- Store Icon
- Screenshots
- Beschreibung
- Datenschutz-/Store-Angaben
- Content Rating
- ggf. Data Safety

Diese Store-Schritte werden erst nach technisch stabilem Build durchgeführt.

---

# 21. Manuelle Tätigkeiten des Users

Der Großteil der Entwicklungsarbeit kann auf dem vorhandenen Windows-System durchgeführt werden.

Voraussichtlich manuell erforderlich:

## A. Physisches Android-Gerät

Falls echte Hardware getestet werden soll:

- Android-Gerät per USB anschließen
- Entwickleroptionen aktivieren
- USB-Debugging aktivieren
- ggf. PC-Vertrauensdialog bestätigen

Ein Emulator kann alternativ verwendet werden.

## B. Release-Signierung

Vor einer Veröffentlichung:

- Keystore erzeugen bzw. vorhandenen Keystore bereitstellen
- Passwort sicher verwalten
- Alias festlegen
- Release-Signierung konfigurieren

Das Keystore-Passwort sollte niemals in den Quellcode geschrieben werden.

## C. Google Play

Falls Veröffentlichung gewünscht ist:

- Play-Console-Konto
- App-Eintrag
- Store-Daten
- Datenschutzinformationen
- Screenshots
- Freigabe

Diese Account-/Zahlungs-/Rechtsvorgänge kann der Benutzer selbst bestätigen müssen.

---

# 22. Was automatisiert werden kann

Mit dem vorhandenen Windows-Zugriff können grundsätzlich automatisiert werden:

- Ordner anlegen
- Projektdateien erstellen
- Dateien lesen
- Dateien schreiben
- Dateien ändern
- .NET-Projekte bauen
- NuGet-Abhängigkeiten verwalten
- Android SDK prüfen
- ADB verwenden
- Emulator starten, sofern vorhanden
- APK bauen
- Tests ausführen
- Logs auslesen
- Screenshots erzeugen
- Git verwenden, falls später ein Repository eingerichtet wird
- Web-/Dokumentationsrecherche
- Build-Artefakte prüfen

---

# 23. Was nicht blind automatisiert werden darf

Nicht ohne ausdrückliche Notwendigkeit:

- bestehende Windows-Quelldateien überschreiben
- vorhandene Android-Projekte löschen
- Keystores löschen
- Zugangsdaten speichern
- Produktionssignaturen verändern
- Google-Play-Veröffentlichung durchführen
- Store-Metadaten veröffentlichen
- große externe Assets ungeprüft ersetzen

---

# 24. Risikoanalyse

## Risiko 1 – Silk.NET Android-Kompatibilität

Die Windows-Version verwendet Silk.NET Windowing/Input/OpenGL.

Diese Plattformschicht soll **nicht blind nach Android kopiert** werden.

Strategie:

- Silk.NET aus dem Android Host entfernen, sofern nicht nachweislich sinnvoll.
- Game-Core weiterverwenden.
- Android-spezifischen Host bauen.

## Risiko 2 – NAudio

NAudio.WinMM ist Windows-spezifisch.

Strategie:

- Audio-API abstrahieren.
- Android Audio Backend implementieren.

## Risiko 3 – Font-Unterschiede

Windows verwendet unter anderem Segoe UI.

Android besitzt nicht garantiert dieselben Systemfonts.

Strategie:

- Referenzfont prüfen.
- benötigte Fonts ggf. als lizenzierte Assets bundlen.
- Textmetriken vergleichen.

## Risiko 4 – WebP-Decoding

Die Slot-Assets liegen als WEBP vor.

Strategie:

- Android-Decoding testen.
- bei Problemen Assets während Build oder Runtime kontrolliert umwandeln.
- keine unnötige Qualitätsreduktion.

## Risiko 5 – Touch statt Maus

Mauskoordinaten sind präziser als Finger.

Strategie:

- visuelle Größe beibehalten.
- Hitboxen intern vergrößern.
- keine UI-Elemente verkleinern.

## Risiko 6 – Performance

Blur/Bloom/Partikel können auf schwächeren Geräten teuer werden.

Strategie:

- Caching
- Profiling
- Qualitätsstufen nur wenn notwendig
- kein pauschales Abschalten der Effekte

---

# 25. Warum nicht Unity?

Unity wäre technisch möglich.

Für dieses Projekt wäre es jedoch ein erheblicher Architekturwechsel.

Dafür müssten unter anderem neu gebaut werden:

- Scene-System
- UI
- Rendering
- Animation
- Partikel
- Kartensystem
- Würfel
- Slot-Reels
- Poker
- Audio
- Save-System
- sämtliche Unity-Szenen/Prefabs

Das widerspricht dem Ziel einer möglichst direkten Portierung.

Unity bleibt als Fallback-Option erhalten, falls sich im Verlauf herausstellt, dass ein bestimmter Android-Grafikpfad mit SkiaSharp nicht ausreichend performant ist.

---

# 26. Warum nicht Unreal?

Unreal wäre für GlamourGames technisch überdimensioniert.

Die vorhandene Anwendung benötigt keine große 3D-Welt, keine komplexe Physik und keine Unreal-spezifische 3D-Engine.

Ein Unreal-Neubau würde den bestehenden C#-Code größtenteils unbrauchbar machen.

---

# 27. Warum nicht WebView/WebGL/WebGPU als Hauptlösung?

Web-Technologien wären möglich, würden aber zusätzliche Portierungsschichten erzeugen:

- C# → JavaScript/TypeScript
- eigenes Rendering neu
- Audio neu
- Save-System neu
- Input neu
- Packaging
- WebView-spezifische Unterschiede

WebGPU ist technisch interessant, aber für diese konkrete vorhandene C#-Codebasis nicht der kürzeste Weg zu einer echten 1:1-Portierung.

---

# 28. Empfohlene Zielarchitektur

```
GlamourGames.Android
│
├── Android Host
│   ├── MainActivity
│   ├── Lifecycle
│   ├── Touch Input
│   ├── Back Button
│   └── Android Audio
│
├── Game Core
│   ├── Anim
│   ├── Scene
│   ├── UI
│   ├── Gfx
│   ├── Art
│   ├── Particles
│   ├── Bloom
│   └── DiceParade
│
├── Games
│   ├── Battleship
│   ├── Blackjack
│   ├── ConnectFour
│   ├── Kniffel
│   ├── Memory
│   ├── Nim
│   ├── Poker
│   ├── Slot
│   ├── Snake
│   └── TicTacToe
│
├── Platform
│   ├── AndroidStorage
│   ├── AndroidAudio
│   ├── AndroidInput
│   └── AndroidLifecycle
│
└── Assets
    ├── General
    ├── Coins
    └── Slot
```

---

# 29. Geschätzter Gesamtaufwand

Sehr grobe technische Planung:

| Bereich | Stunden |
|---|---:|
| Baseline | 0,5–1 |
| Android-Projekt | 1–2 |
| Renderhost | 2–4 |
| Core | 3–6 |
| Assets | 1–3 |
| Audio | 3–6 |
| Menü | 1–2 |
| einfache Spiele | 5–8 |
| Battleship | 3–5 |
| Blackjack | 3–5 |
| Kniffel | 4–7 |
| Poker | 5–9 |
| Slot | 7–12 |
| QA/Performance | 8–15 |
| Release-Build | 2–5 |
| **Gesamt** | **49,5–90 Stunden** |

Das ist keine feste Zusage. Der größte Unsicherheitsfaktor ist die tatsächliche Android-Grafik-/Audio-Kompatibilität und die Anzahl notwendiger visueller Korrekturschleifen.

KI-Token-Aufwand grob:

- kleinere Schritte: 5–15k Tokens
- mittlere Portierung: 15–30k Tokens
- große Spiele: 20–55k Tokens
- Gesamtprojekt: grob **180–350k Tokens**, abhängig von Debugging, Iterationen und Anzahl der Testzyklen.

---

# 30. Geplante Abnahmekriterien

Die Android-Version gilt erst als fertig, wenn:

- alle 10 Spiele spielbar sind
- Menü vollständig funktioniert
- keine Spielmechanik aus der Windows-Version fehlt
- Save/Highscores funktionieren
- Audio funktioniert
- Mute funktioniert
- Musik funktioniert
- Android Back korrekt funktioniert
- Touch vollständig funktioniert
- Animationen vorhanden sind
- Partikel vorhanden sind
- Glow/Bloom vorhanden sind
- Kniffel-Würfelanimation funktioniert
- Poker funktioniert
- Slot inklusive Gamble/Autoplay/Free-Spins funktioniert
- keine bekannten reproduzierbaren Abstürze bestehen
- Debug-APK erfolgreich installiert wird
- Release-Build erfolgreich erzeugt wird
- Performance auf Referenzgerät akzeptabel ist
- kritische Szenen mit der Windows-Referenz visuell verglichen wurden

---

# 31. Aktueller Zustand

Zum Zeitpunkt der Erstellung dieses Leitfadens:

### Windows

**Geprüft:**

- Projekt vorhanden
- 24 C#-Dateien
- 11 Spiel-/Menü-Szenen
- 33 Assets
- .NET 10
- Build erfolgreich
- 0 Fehler
- 0 Warnungen

### Android

**Geprüft:**

- .NET Android Workload installiert
- Android SDK vorhanden
- Android Plattformen vorhanden:
  - android-36
  - android-36.1
  - android-37.0
- Build Tools vorhanden:
  - 36.0.0
  - 36.1.0
  - 37.0.0
- Android Studio installiert
- ADB vorhanden

**Noch nicht durchgeführt:**

- Android-Projektcode
- Android GameView
- Android-Portierung
- Asset-Import
- Audio-Portierung
- Spielportierung
- APK-Build
- echtes Geräte-Testing

Das ist Absicht: Dieser Leitfaden ist die Planungs-/Freigabestufe.

---

# 32. Offizielle technische Referenzen

Für die Umsetzung werden bevorzugt offizielle Dokumentationen verwendet:

- Microsoft Learn – .NET for Android
- Microsoft Learn – SkiaSharp.Views.Android
- Android Developers – Vulkan / Game Development
- Android Developers – Performance / Graphics
- Android Developers – Lifecycle / Activity
- Android SDK / ADB Dokumentation

Besonders relevant:

- SkiaSharp Android bietet hardwarebeschleunigte Oberflächen wie SKSurfaceView und SKGLSurfaceView.
- Android empfiehlt Vulkan für neue native Game-Engines, wenn eine eigene Vulkan-Engine implementiert wird.
- Für dieses Projekt bleibt SkiaSharp die primäre Portierungsstrategie, weil damit die vorhandene Rendering-Architektur weitgehend erhalten werden kann.

---

# 33. Freigabepunkt

**Dieser Leitfaden beschreibt die geplante Umsetzung, aber die eigentliche Android-Portierung wurde noch nicht gestartet.**

Nach Freigabe beginnt die Umsetzung exakt in dieser Reihenfolge:

1. Baseline sichern
2. Android-Projekt anlegen
3. Rendering-Host
4. Core-Portierung
5. Assets
6. Audio
7. Menü
8. einfache Spiele
9. Battleship
10. Blackjack
11. Kniffel
12. Poker
13. Slot
14. Performance
15. vollständige QA
16. Release-Build

**Keine vorhandene Windows-Funktion wird absichtlich entfernt.**

