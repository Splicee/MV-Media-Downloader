# MV Media Downloader

Windows aplikace od MV pro stahování médií přes yt-dlp a dávkovou konverzi přes FFmpeg.

## Funkce

- přehledné WPF rozhraní se světlým a tmavým režimem;
- oddělené obrazovky Stahování a Konverze;
- kompatibilní výchozí profil MP4 / H.264 s omezením nechtěného AV1;
- kvalita videa, titulky, playlisty, zvukové profily a cílová složka;
- veřejné epizody TV JOJ a přímé odkazy JOJ Play bez obcházení DRM;
- dávková konverze až 20 souborů do MP4, MKV, WebM, MOV nebo AVI;
- H.264, H.265 / HEVC a AV1, CRF i pevný bitrate;
- FFprobe analýza kodeku, rozlišení a bitrate v pokročilém režimu;
- samostatný průběh každého souboru a oddělené technické logy;
- instalace a kontrola yt-dlp, FFmpeg a Deno přímo z aplikace;
- automatické aktualizace přes ověřené GitHub Releases.

## Sestavení

Projekt používá systémový kompilátor .NET Framework a nepotřebuje NuGet ani samostatné .NET SDK.

```bat
build.cmd
```

Výstup:

- `dist\MV Media Downloader.exe`
- `dist\MV Media Downloader Updater.exe`

Testy:

```bat
test.cmd
```

Distribuční balíček a SHA-256:

```bat
package.cmd
```

Výstup:

- `release\MV-Media-Downloader-win-x64.zip`
- `release\MV-Media-Downloader-win-x64.zip.sha256`

## Aktualizace

Aplikace kontroluje pouze poslední stabilní GitHub Release. Novou verzi nabídne uživateli, stáhne ZIP přes HTTPS a před použitím ověří SHA-256. Samostatný updater počká na ukončení aplikace, zazálohuje nahrazované soubory, provede aktualizaci a aplikaci znovu spustí. Když nová verze nepotvrdí úspěšné spuštění, updater obnoví předchozí soubory.

Aktualizace neprobíhá během stahování nebo konverze a před stažením i restartem vyžaduje potvrzení. Nezapisuje do registru, nemění `PATH`, neinstaluje službu a pracuje pouze ve složce aplikace a ve své dočasné složce.

Automatická kontrola se dá vypnout v menu **Nástroje**. Ruční kontrola je ve stejném menu.

## TV JOJ

Do pole lze vložit adresu epizody `https://www.joj.sk/...` i přímý odkaz `https://play.joj.sk/player/...`. Číslované seznamy typu `01 https://play.joj.sk/player/...` se načtou po jednotlivých řádcích.

U přihlášeného obsahu použij tlačítko **Přihlásit JOJ Play**. Aplikace otevře oddělený profil Chrome pouze pro JOJ. Premium obsah bez oprávnění a DRM se nestahuje.

Reálné integrační testy jsou dostupné přes `integration-test.cmd` a `joj-test.cmd`. Tyto testy používají internet a ukládají výsledky do `artifacts`.

## Podpis EXE

Build podporuje Authenticode certifikát přes proměnné prostředí:

```bat
set SIGN_CERT_PATH=C:\certifikaty\mv-code-signing.pfx
set SIGN_CERT_PASSWORD=heslo
build.cmd
```

Nebo certifikát z Windows úložiště:

```bat
set SIGN_CERT_SHA1=THUMBPRINT_CERTIFIKATU
build.cmd
```

Self-signed certifikát SmartScreen nevyřeší. Pro důvěryhodný první start je potřeba veřejně důvěryhodný OV/EV code-signing certifikát a reputace vydavatele.

## Data

Nastavení, nástroje a logy jsou oddělené od předchozí aplikace v `%LocalAppData%\MV\MediaDownloader`. Výchozí výstup je `%UserProfile%\Downloads\MV Media Downloader`.

Používej aplikaci pouze pro obsah, ke kterému máš oprávnění.
