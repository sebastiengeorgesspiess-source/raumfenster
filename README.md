# Raumfenster

Experimentelle Offline-Demo für **XREAL One Pro, XREAL Eye und Beam Pro** — von Sebastien Spiess.

**Status: Prototyp, kein stabiles Release.** Nach Darstellungsproblemen mit 0.6.1 wurde 0.6.2 installiert; der Nutzer hat die allgemeine Anzeige danach als funktionierend bestätigt. Version 0.7.0 überarbeitet die Bedienung. Ein umfassender Gerätetest der neuen Funktionen steht noch aus. Ein erfolgreicher Build ist kein Nachweis funktionierender Brillenausgabe. Kamera, Stereo-Wiedergabe und Eye-Bewegung brauchen weitere Gerätetests.

## Was das Projekt erprobt

- Wald als 360°-Panorama und Weltraumszenen mit Bewegung. Die gesamte Demo ist lautlos, einschliesslich Video.
- Lokale Full-SBS-Videos: pro Auge die passende Bildhälfte, auf einer räumlichen Leinwand. Kein 360°-Video und keine frei begehbare Rekonstruktion.
- Anatomisches Herz aus Z-Anatomy: sieben Strukturen, Auswahl aller sieben Teile, Einzelansicht, animiertes Auseinanderziehen, Grössenwechsel und Drehen durch Ziehen. Räumliches Bewegen benötigt tatsächlich verfügbare 6DoF-Positionsdaten.
- Haut-Dokumentation: Eye-Kameravorschau, bewusst ausgelöste lokale Fotos und Vergleich der letzten zwei Aufnahmen. Keine Hautkrebs-Erkennung, Diagnose oder kalibrierte Messung. Eye-Kamerazugriff ist noch nicht auf dem Gerät bestätigt.

Cyberpunk und private Hausfotos sind entfernt, ebenso Strand, Motorrad, Höhle und Fallschacht. Kein Spiel, keine Cloud-KI und kein API-Schlüssel erforderlich.

## Projekt vorbereiten

1. Unity **6000.6.7f1** mit Android Build Support, SDK/NDK und OpenJDK installieren. Dies ist die verwendete Entwicklungsumgebung, keine Zusage offizieller SDK-Kompatibilität.
2. XREAL SDK **3.1.0** separat beziehen und entpacken.
3. In PowerShell im Repository: `./setup.ps1 -XrealSdkPath 'C:/Pfad/com.xreal.xr/package'`. Das entpackt `assets.zip` nach `SpatialWindowUnity/Assets` und trägt den eigenen SDK-Pfad ein. Das Archiv enthält lizenzierte Demo-Assets.
4. `SpatialWindowUnity` in Unity öffnen. `SpatialBuild.Build` ist der Editor-Build-Einstieg; unter Windows mit der genannten Unity-Installation: `./scripts/build-spatial.ps1`.
5. Autorisierten Beam Pro anschliessen: `./scripts/install-spatial.ps1`. Anschliessend Brille anschliessen und über MyGlasses starten, nicht direkt per ADB ohne Brille.

Die enthaltenen Herz-Meshes können ohne die grosse Original-FBX genutzt werden. `MedicalBuild.cs` erzeugt sie neu, wenn die separat bezogene Originaldatei vorhanden ist. Quellcode und Shader sind direkt im Repository lesbar; Texturen, Materialien und die anatomischen Meshes liegen platzsparend in `assets.zip`.

## Eigenes SBS-Video

Das persönliche Bienenvideo ist **nicht** enthalten. Eine eigene kompatible H.264/AAC-Datei in Full-SBS, zum Beispiel 3840x1080 (1920x1080 je Auge), als `spatial/video/bees-h264.mp4` ablegen. `./scripts/install-spatial-videos.ps1` überträgt sie in den App-Ordner und prüft SHA256. In der App BIENEN und FORTSETZEN wählen; die Bezeichnung stammt vom privaten Testclip. Das Video startet ohne Ton und bietet einen Tausch der Augenbilder.

## Foto-Dokumentation und Datenschutz

HAUT → FOTO fordert die Kamera-Berechtigung an. Erneut FOTO startet die Kamera; ein weiterer Tastendruck bei aktuellem Livebild speichert. LIVE / FOTO-VERGLEICH zeigt die letzten zwei Bilder. Aufnahmen nur mit Einverständnis der Person erstellen. Fotos bleiben im App-Ordner `skin-documentation`; sie sind **keine verschlüsselten Patientenakten**. Keine Namen, Diagnosen oder Risikowerte werden erfasst. Keine automatische Übertragung. Deinstallation kann Aufnahmen löschen.

## Prüfstand

- Android-APK-Build erfolgreich; ARM64, Version 0.6.1 / Code 10.
- Kamera-Manifestberechtigung und Installation geprüft.
- Privater Videotest: vollständige H.264/AAC-Decodierung ohne Fehler und übereinstimmende Geräte-Prüfsumme.
- Allgemeine Brillenausgabe in 0.6.2 vom Nutzer bestätigt; funktionale Abnahme aller neuen Szenen bleibt offen.
- Die öffentliche Verpackung enthält keine privaten Fotos und nutzt vorab extrahierte Herz-Meshes. Sie wurde nicht separat als APK gebaut.

## Rechte und Quellen

Siehe [THIRD-PARTY.md](THIRD-PARTY.md). Fremdmodelle und Texturen behalten ihre jeweiligen Lizenzen. Es wird keine allgemeine Open-Source-Lizenz für sämtliche Projektbestandteile behauptet. Private Fotos, Hautaufnahmen, Videos, Zugangsdaten, SDK-Binaries und APKs sind nicht Bestandteil dieser Veröffentlichung.

[Homepage und Tools](https://sebastienspiess.ch/tools/)

## Diagnoseänderungen 0.6.2

Die Sichtweite wurde von 20 auf 500 Meter erweitert, damit Weltraumobjekte nicht abgeschnitten werden. Dynamisch angelegte Collider werden gegen Engine-Stripping geschützt. Der Kamerastart prüft RGB-Unterstützung und Auflösung; bei App-Pause wird die Aufnahme gestoppt. Begrenzte Render-Protokolle helfen bei der weiteren Diagnose. Diese Änderungen sind noch keine bestätigte Behebung aller Brillenprobleme.

## Interaktives Herz (0.7.0)

HERZ öffnet eigene, situationsabhängige Tasten: im Drehfeld ziehen, nächsten Herzteil auswählen, Teile auseinander-/zusammenziehen, Grösse wechseln, Auswahl einzeln anzeigen, automatische Drehung starten/stoppen und zurücksetzen. Zur Übersicht versetzte Teile sind ausdrücklich keine anatomische Lage. Die Raumposition bleibt erhalten, während das Modell gedreht wird.

### Eye-Gesten als Zeigereingabe (experimentell)

XREAL dokumentiert für die One-Serie mit Eye eine firmwareabhängige Gesten-Maus. In der Brille X zweimal drücken → Other → Lab → Gesture Controls einschalten, sofern diese Option vorhanden ist. Zeiger auf das Drehfeld bewegen, Daumen und Zeigefinger zusammenhalten und die Hand bewegen. Die App verarbeitet dafür Maus- und Touch-Eingaben. Das ist keine Implementierung eines frei im Raum greifbaren XR-Handmodells. Die Weiterleitung der Gesten durch MyGlasses an diese App muss auf dem Gerät getestet werden. Touch-Bedienung bleibt verfügbar.

Quellen: https://tutorials.xreal.com/docs/glasses/one-series/gesture/enable-gesture-control/ und https://tutorials.xreal.com/docs/glasses/one-series/gesture/cursor-control/
