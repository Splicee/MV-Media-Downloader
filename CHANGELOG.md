# Přehled změn

## 3.5.0 - 2026-07-29

- Spolehlivější zrušení stahování a konverzí včetně ukončení celého stromu procesů.
- Živá změna limitu rychlosti; přímé přenosy se upraví okamžitě a yt-dlp bezpečně naváže rozpracovaný soubor.
- Opravené navazování `.part` souborů, neúplné HTTP odpovědi a dokončení již stažené části.
- Klidnější průběhový log, aktuální název položky a správné zobrazení české diakritiky.
- Odolnější zpracování výstupu procesů, časových limitů a souběžného čtení standardního i chybového výstupu.
- Přesnější konverze H.264, H.265, AV1 a AVI se zachováním metadat, kapitol a kompatibilních parametrů.
- Samostatné logy stahování a konverze, průběh jednotlivých souborů a bezpečné odstranění neúplných výstupů.
- Lepší responzivní WPF rozhraní, tmavý i světlý motiv a přehlednější pokročilé volby.
- Zapamatování poslední otevřené karty, rychlé přepnutí přes `Ctrl+1` a `Ctrl+2`.
- Další vložené nebo přetažené odkazy se přidají k seznamu a po dokončení lze poslední soubor rovnou zobrazit ve složce.
- Rozšířené regresní, UI a živé integrační testy pro stahování, JOJ, Webshare, FFmpeg a aktualizátor.

## 3.3.0 - 2026-07-27

- Přechod na standardní WPF projekt pro .NET se solution, XAML a samostatným distribučním projektem.
- Automatické aktualizace přes GitHub Releases.
- Podpora yt-dlp, přímých odkazů, Webshare a veřejně dostupného obsahu JOJ Play.
