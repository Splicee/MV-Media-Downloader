# Architektura MV Media Downloader

## Rozdělení odpovědností

`MainWindow` vytváří WPF rozhraní a řídí pouze uživatelské pracovní postupy. Stahovací, konverzní a opravné procesy nejsou součástí šablon ovládacích prvků.

`Core` obsahuje datové modely, nastavení a čisté skládání argumentů. Tato část je testovatelná bez spuštění GUI.

`Services` řeší spouštění externích procesů, čtení stdout/stderr, kontrolu nástrojů, bezpečné stažení, SHA-256 ověření a FFprobe analýzu.

`UI` obsahuje barevné zdroje a vlastní šablony tlačítek, vstupů, rozbalovacích nabídek a progress barů. Téma se přepíná bez restartu aplikace.

## Stahování

1. GUI ověří URL a uloží nastavení.
2. `DownloadArgumentBuilder` zvolí formát. MP4 nejprve hledá H.264/AVC, potom ne-AV1 MP4 a až nakonec obecný fallback.
3. `ProcessService` spustí yt-dlp bez shellu a předá argumenty s bezpečným uvozováním.
4. Výstup se parsuje na procenta, rychlost a ETA.
5. Technický výstup zůstává v samostatném download logu.

## Konverze

1. Uživatel vloží 1 až 20 souborů.
2. Pokud je dostupný FFprobe, aplikace zjistí zdrojový kodek, rozlišení, bitrate a délku.
3. `ConversionArgumentBuilder` vytvoří FFmpeg argumenty pro formát, kodek a zvolený režim kvality.
4. Fronta se zpracovává postupně, aby více enkodérů současně nepřetížilo počítač.
5. `-progress pipe:1` poskytuje průběh pro každý soubor i celou frontu.

## Aktualizace nástrojů

yt-dlp, FFmpeg a Deno se stahují pouze přes HTTPS. Před instalací se ověřují proti SHA-256 součtům publikovaným stejnými oficiálními distribucemi. Do cílového umístění se kopírují až po úspěšném ověření.

## Uživatelská data

Nastavení a logy nejsou ukládány vedle EXE, takže aplikace funguje i z chráněné složky. Výchozí cesta je `%LocalAppData%\MV\MediaDownloader`; při nemožnosti zápisu se použije lokální nebo dočasná cesta.
