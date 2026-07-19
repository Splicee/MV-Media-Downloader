# Test JOJ Play

Datum kontroly: 19. 7. 2026

## Výsledek

- vložený očíslovaný seznam se správně rozpozná jako 15 samostatných odkazů;
- oddělený Chrome profil předá pouze přihlášení JOJ Play;
- odkazy 01 až 14 vracejí nešifrovaný HLS zdroj a prošly simulací yt-dlp;
- odkaz 15 vyžaduje zakoupení daného videa a aplikace jej správně odmítne;
- DRM zdroje konektor nepřijímá.

## Reálný vzorek

Z odkazu 01 byl stažen třísekundový úsek:

- kontejner: MP4;
- obraz: H.264 High, 1920 × 1080, 25 fps;
- zvuk: AAC LC, 48 kHz;
- výsledná velikost: přibližně 2,19 MiB.

Ověřené varianty kvality:

- přibližně 1,58 Mb/s: 720 × 404;
- přibližně 3,62 Mb/s: 1280 × 720;
- přibližně 5,66 Mb/s: 1920 × 1080.

Testovací vzorek je uložen pouze v `artifacts/joj-play-test` a není součástí vydávaného ZIP balíčku.
