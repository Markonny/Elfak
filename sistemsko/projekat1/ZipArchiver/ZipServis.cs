using ICSharpCode.SharpZipLib.Zip;
public static class ZipServis
{
    public static byte[] KreirajZip(List<FileInfo> fajlovi)
    {
        using MemoryStream memoryStream = new();

        using (ZipOutputStream zipStream = new(memoryStream))
        {
            zipStream.IsStreamOwner = false;
            byte[] buffer = new byte[8192];

            foreach (FileInfo fajl in fajlovi)
            {
                ZipEntry entry = new(ZipEntry.CleanName(fajl.Name))
                {
                    DateTime = fajl.LastWriteTime,
                    Size = fajl.Length
                };

                zipStream.PutNextEntry(entry);

                using FileStream fileStream = File.OpenRead(fajl.FullName);

                int procitanoBajtova;
                while ((procitanoBajtova = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    zipStream.Write(buffer, 0, procitanoBajtova);
                }
                zipStream.CloseEntry();
            }
            zipStream.Finish();
        }
        return memoryStream.ToArray();
    }
}