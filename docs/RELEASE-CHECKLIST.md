# Vydání MV Media Downloader

1. Zvyš `AssemblyVersion` a `AssemblyFileVersion` v `src\App.cs`.
2. Spusť `test.cmd`.
3. Spusť `package.cmd` a zkontroluj ZIP i SHA-256.
4. Ověř čisté spuštění, stahování, konverzi a ruční kontrolu aktualizací.
5. Commitni změny do větve `main`.
6. Vytvoř tag ve tvaru `vX.Y.Z` shodný s verzí aplikace a odešli ho na GitHub.
7. Workflow **Vydání** sestaví nový balíček a přiloží oba stabilně pojmenované soubory ke GitHub Release.
8. Po dokončení workflow ověř adresu `/releases/latest` a SHA-256 zveřejněného ZIPu.

Bez veřejně důvěryhodného Authenticode certifikátu může Windows SmartScreen zobrazit varování i u správně vytvořeného vydání.
