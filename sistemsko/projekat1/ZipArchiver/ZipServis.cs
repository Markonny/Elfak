using ICSharpCode.SharpZipLib.Zip;
public static class ZipServis
{
    public static byte[] KreirajZip(List<FileInfo> fajlovi) //metoda prima listu fileinfo objekata i cuvamo niz bajtova jer onda mozemo lako da sacuvamo u kes te bajtove ili kroz http
    {
        using MemoryStream memoryStream = new(); //kreiramo tok podataka u ram umesto na hdd

        using (ZipOutputStream zipStream = new(memoryStream)) //zipoutputstream pretvara fajlove u zip fajl
        {
            zipStream.IsStreamOwner = false; //ovo je da nam ne zatvori automatski memorystream kada se zipstream zatvori
            byte[] buffer = new byte[8192]; //pravimo bafer da ne ocita npr 1 giga odjednom vec 8kb po 8kb deo po deo...

            foreach (FileInfo fajl in fajlovi)
            {
                ZipEntry entry = new(ZipEntry.CleanName(fajl.Name))
                {
                    DateTime = fajl.LastWriteTime,
                    Size = fajl.Length
                };

                zipStream.PutNextEntry(entry); //pocinjemo da saljemo podatke za konkretan fajl

                using FileStream fileStream = File.OpenRead(fajl.FullName); 

                int procitanoBajtova;
                while ((procitanoBajtova = fileStream.Read(buffer, 0, buffer.Length)) > 0) //nula znaci da pocinjemo od 0 elementa u baferu
                {
                    zipStream.Write(buffer, 0, procitanoBajtova);
                }
                zipStream.CloseEntry();
            }
            zipStream.Finish();
        }
        return memoryStream.ToArray(); //ToArray Pretvara sve te bajtove koji su se nakupili u memoriji u jedan čist niz bajtova (byte[]) koji vraćamo kao rezultat
    }
}