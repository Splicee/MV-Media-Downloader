# MV Media Downloader

Windows aplikace od MV pro stahování médií přes yt-dlp, Webshare a přímé HTTP odkazy a pro dávkovou konverzi přes FFmpeg.

## Funkce

- adaptivní WPF rozhraní, které využije větší okno a při menší šířce se samo přeskupí;
- světlý a tmavý režim včetně nabídek, dialogů a posuvníků;
- oddělené obrazovky Stahování a Konverze;
- jednoduchý režim s bezpečnými výchozími hodnotami a volitelný pokročilý režim;
- kompatibilní výchozí profil MP4 / H.264 s omezením nechtěného AV1;
- kvalita videa, titulky, playlisty, zvukové profily a cílová složka;
- automatické rozpoznání běžných zdrojů přímo po vložení odkazů;
- stručné upozornění na přihlášení, DRM nebo známou změnu českého přehrávače;
- automatická browser impersonace pro MůjRozhlas, kde běžný požadavek končí chybou 403;
- načtení existujícího přihlášení z Chrome, Edge, Firefoxu nebo Brave;
- veřejné epizody TV JOJ a přímé odkazy JOJ Play bez obcházení DRM;
- dávková konverze až 20 souborů do MP4, MKV, WebM, MOV nebo AVI;
- H.264, H.265 / HEVC a AV1, CRF i pevný bitrate;
- FFprobe analýza kodeku, rozlišení a bitrate v pokročilém režimu;
- samostatný průběh každého souboru a oddělené technické logy;
- instalace a kontrola yt-dlp, FFmpeg a Deno přímo z aplikace;
- Webshare přes oficiální API, volitelné přihlášení a relace chráněná účtem Windows;
- vlastní HTTP downloader s navázáním `.part`, průběhem a živou změnou limitu rychlosti;
- Webshare a přímé soubory respektují zvolený formát i maximální kvalitu pomocí následného FFmpeg převodu;
- Markdown seznamy odkazů se čistí a stejné adresy se zpracují pouze jednou;
- přímé soubory a oficiální dočasné CDN odkazy bez předávání tokenů do logu;
- uložení očištěného diagnostického `.txt` souboru a následné předání správci e-mailem nebo přes GitHub;
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

Živá kontrola českých zdrojů bez stahování celých médií:

```bat
czech-sites-test.cmd
```

Distribuční balíček a SHA-256:

```bat
package.cmd
```

`package.cmd` při chybějícím nebo nefunkčním yt-dlp stáhne aktuální oficiální verzi a ověří její SHA-256.

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

## Další weby

Běžné webové adresy zpracovává aktuální yt-dlp. Živá kontrola z 26. 7. 2026 s yt-dlp 2026.07.04 ověřila veřejná média TV Nova, Českého rozhlasu, MůjRozhlasu, Rozhlasu Vltava, Stream.cz, Televize Seznam, TV Noe a DVTV / Aktuálně. Kontrolu lze kdykoli zopakovat přes `czech-sites-test.cmd`; načítá pouze metadata.

Česká televize v době kontroly vracela HTTP 410, CNN Prima změnila přehrávač, Prima+ vyžadovala účet podporovaný přímo yt-dlp a Seznam Zprávy mohl skončit na stránce se souhlasem. Extraktor iDNES / Playtvak je v yt-dlp označený jako dočasně nefunkční. Aplikace tyto stavy ukazuje přímo pod vloženými odkazy a v přehledu **Podporované weby**.

Další běžně rozpoznané platformy zahrnují YouTube, Vimeo, Dailymotion, Twitch, Kick, TikTok, Instagram, Facebook, X, Reddit, Rumble, Streamable, SoundCloud, Bandcamp, Mixcloud a Apple Podcasts. U obsahu přístupného po přihlášení může pomoci **Přihlášení z prohlížeče**; Prima+ je výjimka a samotné cookies nemusí stačit. Aplikace neobchází DRM, předplatné ani oprávnění účtu.

## Webshare a přímé odkazy

Odkazy `webshare.cz/#/file/...` zpracovává aplikace přes oficiální Webshare API. Veřejné soubory fungují bez účtu, u souborů vyžadujících přihlášení použij **Přihlásit Webshare**. Heslo se neukládá a zapamatovaná relace je chráněná účtem Windows. Samostatné heslo chránící konkrétní soubor musí jeho vlastník zpřístupnit zvlášť; přihlášení k účtu ho nenahrazuje.

Vlastní downloader umí běžné přímé HTTP/HTTPS odkazy, dočasné CDN odkazy, navázání přerušeného `.part` souboru a okamžitou změnu limitu rychlosti. Běžná stránka Přehraj.to se automaticky nezpracovává, protože služba neposkytuje povolené veřejné API. Použít lze oficiální přímý odkaz ke stažení nebo CDN odkaz získaný uživatelem z vlastního účtu.

U mediálních souborů se po stažení použije volba z pole **Typ souboru**. MP4 se převádí do H.264/AAC, WebM do VP9/Opus, zvukové profily vyjmou první zvukovou stopu a **MKV / nejlepší** zachová původní datové proudy, pokud není potřeba snížit rozlišení. Při neúspěšném nebo zrušeném převodu se stažený originál nemaže.

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
