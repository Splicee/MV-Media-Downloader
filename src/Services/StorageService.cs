using System;
using System.IO;

namespace MVMediaStudio.Services
{
    internal static class StorageService
    {
        public static void EnsureWritableDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Nebyla vybrána cílová složka.", "path");

            Directory.CreateDirectory(path);
            string probe = Path.Combine(path, ".mv-write-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (FileStream stream = new FileStream(
                    probe,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    1,
                    FileOptions.WriteThrough))
                {
                    stream.WriteByte(1);
                    stream.Flush(true);
                }
            }
            catch (Exception error)
            {
                throw new IOException("Do cílové složky nelze zapisovat. Vyber jinou složku nebo zkontroluj oprávnění.", error);
            }
            finally
            {
                try { if (File.Exists(probe)) File.Delete(probe); } catch { }
            }
        }

        public static void DeleteIncompleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
