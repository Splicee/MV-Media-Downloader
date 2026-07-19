# Reálné testy 2026-07-19

Test proběhl přes stejné `DownloadArgumentBuilder`, `ConversionArgumentBuilder`, `ProcessService`, `ToolService` a `MediaProbeService`, které používá GUI.

Použité nástroje:

- yt-dlp 2026.07.04;
- FFmpeg 8.1.2 essentials build;
- Deno 2.9.3.

## Stažení

| Test | Výsledek | Ověřený soubor |
|---|---|---|
| W3C MP4/H.264 s filtrem kvality | OK | MP4, H.264, 1920 × 1080, 12 s |
| W3C Sintel, pouze zvuk MP3 | OK | MP3 |
| W3C přímé MP4 do zvoleného MKV | OK | MKV, H.264, 1920 × 1080, 12 s |
| W3C WebM | OK | WebM, VP9, 320 × 240, 12 s |

## Konverze

| Test | Výsledek | Kontrola |
|---|---|---|
| MP4 → MP4 / H.264 / CRF 23 | OK | FFprobe H.264 + úplné dekódování |
| MKV → MKV / H.265 / CRF 28 | OK | FFprobe HEVC + úplné dekódování |
| MP4 → WebM / AV1 / CRF 28 | OK | FFprobe AV1 + úplné dekódování |
| MKV → AVI / H.264 / 2500k | OK | FFprobe H.264 + úplné dekódování |

Celkový výsledek: **8 z 8 reálných testů prošlo**.

Test odhalil a ověřil dvě opravy: fallback pro přímé zdroje bez předem známého rozlišení a povinný bezztrátový remux přímého souboru do MKV.
