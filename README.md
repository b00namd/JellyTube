# JellyTube & JellyTubbing – Jellyfin Plugins

Zwei Jellyfin-Plugins rund um YouTube:

| Plugin | Zweck |
|---|---|
| **JellyTube** | YouTube-Videos herunterladen und in die Mediathek speichern |
| **JellyTubbing** | YouTube-Videos direkt in Jellyfin streamen (ohne Download) |

---

## JellyTube

Lädt YouTube-Videos und Playlists direkt in die Mediathek. Nutzt [yt-dlp](https://github.com/yt-dlp/yt-dlp) für den Download und erstellt automatisch NFO-Metadaten sowie Vorschaubilder.

### Features

- Videos und Playlists per URL herunterladen
- Metadaten-Vorschau vor dem Download
- NFO-Dateien und Vorschaubilder automatisch generieren
- Nach Kanal in Unterordner sortieren
- Untertitel herunterladen (mehrere Sprachen)
- Download-Warteschlange mit Fortschrittsanzeige, Laufzeitanzeige und sofortigem Start
- Aktive und wartende Jobs einzeln abbrechen
- Abgeschlossene Jobs per Knopfdruck leeren
- Jellyfin-Bibliothek nach Download automatisch aktualisieren
- Geplante Playlist-Downloads per Scheduled Task
- Pro geplantem Download: eigenes Maximalalter und „Gesehen löschen"-Option
- Archiv für geplante Downloads – bereits heruntergeladene Videos werden übersprungen
- Archiv zurücksetzen direkt im Plugin möglich
- Smarter Kanal-Scan: stoppt beim ersten bereits archivierten oder zu alten Video (`--break-on-existing`, `--break-on-reject`)
- Standard-Audiosprache konfigurierbar (ISO 639-2, z. B. `deu`) – wird als Sprachmetadaten in die Audiodatei eingebettet
- Verwaiste yt-dlp-Prozesse werden beim Neustart automatisch beendet
- yt-dlp und ffmpeg Verfügbarkeitscheck in den Einstellungen
- Vollständig auf Deutsch

### Einstellungen

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
| Standard-Audiosprache | ISO 639-2 Sprachcode (z. B. `deu`), der in die Audiometadaten eingebettet wird |
| Geplante Downloads | Playlists automatisch prüfen, inkl. Maximalalter und „Gesehen löschen" pro Eintrag |
| Max. Videoalter (Playlist) | Globales Limit: nur Videos der letzten N Tage herunterladen |
| Gesehene Videos löschen | Nur für geplante Downloads: Datei nach dem Schauen löschen, kein erneuter Download |

---

## JellyTubbing

Streamt YouTube-Videos direkt in Jellyfin – ohne Download. Nutzt [Invidious](https://invidious.io) als primäre Quelle für Stream-URLs, mit [yt-dlp](https://github.com/yt-dlp/yt-dlp) als automatischem Fallback.

Nach der Installation erscheint JellyTubbing als **Kanal** in Jellyfin (Dashboard → Kanäle).

### Features

- Trending-Videos direkt in Jellyfin durchsuchen und abspielen
- Suche über die Jellyfin-Suchfunktion
- Invidious als primäre Stream-Quelle (keine YouTube-API notwendig)
- Automatischer yt-dlp-Fallback wenn Invidious keinen Stream liefert
- Konfigurierbare bevorzugte Qualität (360p–1080p)
- Konfigurierbare Trending-Region (z. B. DE, US)
- Konnektivitätstest für die Invidious-Instanz direkt in den Einstellungen

### Einstellungen

| Einstellung | Beschreibung |
|---|---|
| Invidious-Instanz URL | URL einer Invidious-Instanz (eigene empfohlen) |
| yt-dlp Programmpfad | Optionaler Pfad zur yt-dlp-Binary für den Fallback |
| Bevorzugte Qualität | Maximale Stream-Auflösung (360p, 480p, 720p, 1080p) |
| Trending-Region | ISO 3166-1 alpha-2 Ländercode (z. B. DE, US, GB) |

### Invidious-Instanz

Eine eigene Invidious-Instanz ist am zuverlässigsten. Öffentliche Instanzen: [api.invidious.io](https://api.invidious.io)

---

## Installation

### 1. Repository hinzufügen

In Jellyfin:
**Admin Dashboard → Plugins → Repositories → Hinzufügen**

Repository-URL:
```
https://raw.githubusercontent.com/b00namd/JellyYT/master/dist/manifest.json
```

### 2. Plugins installieren

**Admin Dashboard → Plugins → Katalog → JellyTube / JellyTubbing → Installieren**

### 3. Jellyfin neu starten

Nach der Installation muss Jellyfin neu gestartet werden.

---

## Docker Compose – Schnellstart

Wer Jellyfin und Invidious noch nicht betreibt, kann mit dieser Vorlage beide Dienste starten:

```yaml
services:

  jellyfin:
    image: lscr.io/linuxserver/jellyfin:latest
    container_name: jellyfin
    restart: unless-stopped
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Europe/Berlin
    volumes:
      - ./jellyfin/config:/config
      - /media:/media                              # Mediathek / JellyTube-Downloads
      - /usr/local/bin/yt-dlp:/usr/local/bin/yt-dlp:ro   # yt-dlp auf dem Host
      - /usr/local/bin/ffmpeg:/usr/local/bin/ffmpeg:ro    # ffmpeg auf dem Host
    ports:
      - "8096:8096"
    # GPU-Transcoding (optional, nur wenn eine NVIDIA-Karte vorhanden ist):
    # deploy:
    #   resources:
    #     reservations:
    #       devices:
    #         - driver: nvidia
    #           count: all
    #           capabilities: [gpu, video, compute, utility]

  invidious:
    image: quay.io/invidious/invidious:latest
    container_name: invidious
    restart: unless-stopped
    ports:
      - "3000:3000"
    environment:
      INVIDIOUS_CONFIG: |
        db:
          dbname: invidious
          user: kemal
          password: kemal
          host: invidious-db
          port: 5432
        check_tables: true
        hmac_key: "HIER_EINEN_LANGEN_ZUFAELLIGEN_STRING_EINTRAGEN"
        invidious_companion_key: "COMPANION_SECRET_HIER_EINTRAGEN"
        invidious_companion:
          - private_url: "http://companion:8282/companion"
    depends_on:
      - invidious-db
      - companion

  companion:
    image: quay.io/invidious/invidious-companion:latest
    container_name: companion
    restart: unless-stopped
    environment:
      - SERVER_SECRET_KEY=COMPANION_SECRET_HIER_EINTRAGEN   # muss mit invidious_companion_key übereinstimmen
    volumes:
      - companion-cache:/var/tmp/youtubei.js:rw
    cap_drop:
      - ALL
    read_only: true
    security_opt:
      - no-new-privileges:true

  invidious-db:
    image: docker.io/library/postgres:14
    container_name: invidious-db
    restart: unless-stopped
    volumes:
      - invidious-db-data:/var/lib/postgresql/data
    environment:
      POSTGRES_DB: invidious
      POSTGRES_USER: kemal
      POSTGRES_PASSWORD: kemal

volumes:
  invidious-db-data:
  companion-cache:
```

> **Vor dem Start anpassen:**
> - `hmac_key` und `invidious_companion_key` / `SERVER_SECRET_KEY` durch eigene, zufällige Strings ersetzen (beide Werte müssen übereinstimmen)
> - Pfade für `yt-dlp` und `ffmpeg` an die tatsächlichen Installationsorte auf dem Host anpassen
> - `TZ` auf die eigene Zeitzone setzen

**Starten:**
```bash
docker compose up -d
```

**Dienste:**
| Dienst | URL |
|---|---|
| Jellyfin | `http://<server-ip>:8096` |
| Invidious | `http://<server-ip>:3000` |

**JellyTubbing-Einstellung – Invidious-URL:**
- `http://invidious:3000` – wenn Jellyfin im selben Docker-Netzwerk läuft
- `http://<server-ip>:3000` – wenn von außen erreichbar sein soll

---

## Voraussetzungen

- Jellyfin 10.9.x oder neuer
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) auf dem Server installiert
- [ffmpeg](https://ffmpeg.org/) für Containerkonvertierung (mp4/mkv) – nur für JellyTube

yt-dlp und ffmpeg können entweder im Systempfad (PATH) liegen oder der vollständige Pfad wird in den Plugin-Einstellungen angegeben.

---

## Selbst bauen

```powershell
# Beide Plugins bauen, ZIPs und Manifest erstellen
.\build.ps1
```

---

## Hinweis

Diese Plugins ermöglichen den Zugriff auf YouTube-Inhalte. Das Herunterladen und Streamen von Videos kann gegen die Nutzungsbedingungen von YouTube verstoßen. Die Nutzung erfolgt auf eigene Verantwortung und sollte ausschließlich für den persönlichen Gebrauch erfolgen.
