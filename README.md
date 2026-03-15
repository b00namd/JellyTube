# JellyTube – Jellyfin Plugin

Ein Jellyfin-Plugin zum Herunterladen von YouTube-Videos und Playlists direkt in die Mediathek. Nutzt [yt-dlp](https://github.com/yt-dlp/yt-dlp) für den Download und erstellt automatisch NFO-Metadaten sowie Vorschaubilder.

---

## Features

- Videos und Playlists per URL herunterladen
- Metadaten-Vorschau vor dem Download
- NFO-Dateien und Vorschaubilder automatisch generieren
- Nach Kanal in Unterordner sortieren
- Untertitel herunterladen (mehrere Sprachen)
- Download-Warteschlange mit Fortschrittsanzeige und sofortigem Start
- Jellyfin-Bibliothek nach Download automatisch aktualisieren
- Geplante Playlist-Downloads per Scheduled Task
- yt-dlp und ffmpeg Verfügbarkeitscheck in den Einstellungen
- Gesehene Videos aus geplanten Downloads automatisch löschen (kein erneuter Download)
- Vollständig auf Deutsch

---

## Installation

### 1. Repository hinzufügen

In Jellyfin:
**Admin Dashboard → Plugins → Repositories → Hinzufügen**

Repository-URL:
```
https://raw.githubusercontent.com/b00namd/JellyTube/master/dist/manifest.json
```

### 2. Plugin installieren

**Admin Dashboard → Plugins → Katalog → JellyTube → Installieren**

### 3. Jellyfin neu starten

Nach der Installation muss Jellyfin neu gestartet werden.

---

## Voraussetzungen

- Jellyfin 10.9.x oder neuer
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) auf dem Server installiert
- [ffmpeg](https://ffmpeg.org/) für Containerkonvertierung (mp4/mkv)

yt-dlp und ffmpeg können entweder im Systempfad (PATH) liegen oder der vollständige Pfad wird in den Plugin-Einstellungen angegeben.

---

## Einstellungen

| Einstellung | Beschreibung |
|---|---|
| Download-Pfad | Basisverzeichnis für heruntergeladene Videos |
| yt-dlp Programmpfad | Optionaler vollständiger Pfad zur yt-dlp-Binary |
| ffmpeg Programmpfad | Optionaler vollständiger Pfad zur ffmpeg-Binary |
| Videoformat | Voreinstellung oder benutzerdefinierter yt-dlp Format-String |
| Bevorzugter Container | MP4, MKV oder WebM |
| Max. gleichzeitige Downloads | 1–10 |
| Nach Kanal sortieren | Erstellt Unterordner pro Kanal |
| Untertitel herunterladen | Inkl. Sprachauswahl |
| NFO-Dateien schreiben | Metadaten für Jellyfin |
| Vorschaubilder herunterladen | Thumbnails speichern |
| Bibliothek aktualisieren | Scan nach Download |
| Geplante Downloads | Playlists automatisch prüfen |
| Gesehene Videos löschen | Nur für geplante Downloads: Datei nach dem Schauen löschen, kein erneuter Download |

---

## Selbst bauen

```powershell
# Plugin bauen und ZIP + Manifest erstellen
.\build.ps1
```

---

## Hinweis

Dieses Plugin ermöglicht das Herunterladen von YouTube-Inhalten. Das Herunterladen von Videos kann gegen die Nutzungsbedingungen von YouTube verstoßen. Die Nutzung erfolgt auf eigene Verantwortung und sollte ausschließlich für den persönlichen Gebrauch erfolgen.
