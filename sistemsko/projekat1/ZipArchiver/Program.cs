using System;
using System.Net;

string folderSaFajlovima = Path.Combine(Directory.GetCurrentDirectory(), "ServerFajlovi");
Directory.CreateDirectory(folderSaFajlovima);
BlockingZahtevRed<HttpListenerContext> redZahteva = new();
ZipCache cache = new(50 * 1024 * 1024); // 50 mb

int brojRadnihNiti = 4;

for (int i = 0; i < brojRadnihNiti; i++)
{
    Thread nit = new Thread(RadnaNit);
    nit.Start();
}
HttpListener listener = new();
listener.Prefixes.Add("http://localhost:5050/");
listener.Start();

Console.WriteLine("Server pokrenut na http://localhost:5050/");
Logger.Log("Server pokrenut.");
while (true){
 
    HttpListenerContext context = listener.GetContext();
    Logger.Log("Primljen zahtev: " + context.Request.RawUrl);
    redZahteva.Dodaj(context);
}
void RadnaNit() {
    while (true)
    {
        {
            HttpListenerContext context = redZahteva.Uzmi();
            ObradiZahtev(context);
        }
    }
}
void ObradiZahtev(HttpListenerContext context)
{
    try
    {
        if (context.Request.HttpMethod != "GET")
        {
            PosaljiTekst(context, "Dozvoljena je samo get metoda.", 405);
            return;
        }
        List<string> trazeniFajlovi = ParsirajFajlove(context.Request.RawUrl ?? string.Empty);

        if (trazeniFajlovi.Count == 0)
        {
            PosaljiTekst(context, "Niste naveli nijedan fajl.");
            return;
        }
        List<FileInfo> postojeciFajlovi = NadjiPostojeceFajlove(trazeniFajlovi);

        if (postojeciFajlovi.Count == 0)
        {
            PosaljiTekst(context, "Nijedan od trazenih fajlova ne postoji na serveru.");
            return;
        }
        string kljuc = ZipCache.KreirajKljuc(postojeciFajlovi);
        byte[] zipPodaci = cache.VratiIliKreiraj(kljuc, () =>
        {
            Logger.Log("Kreiranje ZIP arhive za: " + string.Join(", ", postojeciFajlovi.Select(f => f.Name)));
            return ZipServis.KreirajZip(postojeciFajlovi);
        });
        PosaljiZip(context, zipPodaci, "archive.zip");

        Logger.Log("Zahtev uspesan.");
    }
    catch (Exception ex)
    {
        Logger.Log("error: " + ex);

        try
        {
            PosaljiTekst(context, "Doslo je do greske na serveru.", 500);
        }
        catch
        {
            Logger.Log("Greska pri slanju odgovora o gresci: " + ex.Message);
        }
    }
}
List<string> ParsirajFajlove(string rawUrl)
{
    string putanja = rawUrl.TrimStart('/');
    if (string.IsNullOrWhiteSpace(rawUrl))
    {
        return [];
    }
    return putanja
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(x => WebUtility.UrlDecode(x) ?? string.Empty)
        .Select(x => Path.GetFileName(x) ?? string.Empty)
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}
List<FileInfo> NadjiPostojeceFajlove(List<string> trazeniFajlovi)
{
    List<FileInfo> rezultat = new();

    foreach (string imeFajla in trazeniFajlovi)
    {
        string ime = Path.GetFileName(imeFajla);
        string path = Path.Combine(folderSaFajlovima, ime);

        if (File.Exists(path))
        {
            rezultat.Add(new FileInfo(path));
        }
        else
        {
            Logger.Log("Fajl ne postoji: " + ime);
        }
    }
    return rezultat;
}
void PosaljiZip(HttpListenerContext context, byte[] zipPodaci, string imeFajla)
{
    context.Response.StatusCode = 200;
    context.Response.ContentType = "application/zip";
    context.Response.AddHeader("Content-Disposition", $"attachment; filename=\"{imeFajla}\"");
    context.Response.ContentLength64 = zipPodaci.Length;

    context.Response.OutputStream.Write(zipPodaci, 0, zipPodaci.Length);
    context.Response.OutputStream.Close();
}
void PosaljiTekst(HttpListenerContext context, string tekst, int statusKod = 200)
{
    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(tekst);

    context.Response.StatusCode = statusKod;
    context.Response.ContentType = "text/plain; charset=utf-8";
    context.Response.ContentLength64 = bytes.Length;

    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
    context.Response.OutputStream.Close();
}