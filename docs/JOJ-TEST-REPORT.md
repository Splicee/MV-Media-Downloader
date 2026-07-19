# Test archivu TV JOJ

Datum testu: 17. července 2026

## Výsledek

Veřejná epizoda TV JOJ byla úspěšně stažena z běžné adresy epizody:

`https://www.joj.sk/relacia/7-krimi/epizoda/165668-krimi`

Aplikace automaticky našla veřejný přehrávač `media.joj.sk`, předala jej do yt-dlp a stáhla osmivteřinový kontrolní úsek.

- kontejner: MP4;
- video: H.264;
- zvuk: AAC;
- rozlišení: 1280 × 720;
- délka testu: 8,0 s;
- velikost: 1 562 605 bajtů;
- výsledek FFprobe: platné video se zvukem.

Podrobný technický log a testovací soubor jsou v `artifacts\joj-test\run-20260717-124944`.

## Podporované odkazy

- konkrétní veřejná epizoda `www.joj.sk/relacia/.../epizoda/...`;
- přímý veřejný přehrávač `media.joj.sk/embed/...`;
- starší stránky s vloženým `media.joj.sk` iframe.

Pouhý seznam epizod se nestahuje. Nejdříve je potřeba otevřít konkrétní epizodu. Aplikace neobchází přihlášení, JOJ Play Premium ani DRM.
