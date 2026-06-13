using System.Net;
using System.Threading.Channels;

string folderSaFajlovima = Path.Combine(Directory.GetCurrentDirectory(), "ServerFajlovi");
Directory.CreateDirectory(folderSaFajlovima);

ZipCache cache = new(50 * 1024 * 1024);

AsyncZahtevRed<HttpListenerContext> redZahteva = new();

const int maxParalelnih = 4;
CancellationTokenSource cts = new();

List<Task> radniTaskovi = new();
for (int i = 0; i < maxParalelnih; i++)
{
    int id = i + 1;
    radniTaskovi.Add(Task.Run(async () => await RadniTask(id, cts.Token)));
}

HttpListener listener = new();
listener.Prefixes.Add("http://localhost:5050/");
listener.Start();

Console.WriteLine("Server pokrenut na http://localhost:5050/");
Console.WriteLine($"Max paralelnih obrada: {maxParalelnih}");
Console.WriteLine("Pritisnite Ctrl+C za gasenje...");
Logger.Log("Server pokrenut.");

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Logger.Log("Server se gasi...");
    cts.Cancel();
    listener.Stop();
    redZahteva.Zatvori();
    Logger.Zatvori();
};

try
{
    while (!cts.IsCancellationRequested)
    {
        HttpListenerContext context;
        try
        {
            context = await listener.GetContextAsync();
        }
        catch (HttpListenerException) when (cts.IsCancellationRequested)
        {
            break;
        }

        Logger.Log("Primljen zahtev: " + context.Request.RawUrl);

        await redZahteva.DodajAsync(context);
    }
}
finally
{
    await Task.WhenAll(radniTaskovi);
    Logger.Log("Server ugasen.");
    Console.WriteLine("Server ugasen.");
}

async Task RadniTask(int id, CancellationToken ct)
{
    Logger.Log($"Radni task {id} pokrenut.");
    while (!ct.IsCancellationRequested)
    {
        HttpListenerContext context;
        try
        {
            context = await redZahteva.UzmiAsync(ct);
        }
        catch (Exception)
        {
            break;
        }

        
        await ObradiZahtevAsync(context, id).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Logger.Log($"[Task {id}] Neuhvacena greska: {t.Exception?.GetBaseException().Message}");
            }
            else if (t.IsCompletedSuccessfully && t.Result)
            {
                Logger.Log($"[Task {id}] Obrada zavrseno ");
            }
            else
            {
                Logger.Log($"[Task {id}] Obrada otkazano ");
            }
        }, TaskScheduler.Default);
    }
    Logger.Log($"Radni task {id} zavrsen.");
}

async Task<bool> ObradiZahtevAsync(HttpListenerContext context, int radnikId)
{
    try
    {
        if (context.Request.HttpMethod != "GET")
        {
            await PosaljiTekstAsync(context, "Dozvoljena je samo GET metoda.", 405);
            return false;
        }

        List<string> trazeniFajlovi = ParsirajFajlove(context.Request.RawUrl ?? string.Empty);

        if (trazeniFajlovi.Count == 0)
        {
            await PosaljiTekstAsync(context, "Nije naveden nijedan fajl.");
            return false;
        }

        List<FileInfo> postojeciFajlovi = NadjiPostojeceFajlove(trazeniFajlovi);

        if (postojeciFajlovi.Count == 0)
        {
            await PosaljiTekstAsync(context, "Nijedan od trazenih fajlova ne postoji na serveru.");
            return false;
        }

        string kljuc = ZipCache.KreirajKljuc(postojeciFajlovi);

        byte[] zipPodaci = await cache.VratiIliKreirajAsync(kljuc, async () =>
        {
            Logger.Log($"[Task {radnikId}] Kreiranje zipa za: " + string.Join(", ", postojeciFajlovi.Select(f => f.Name)));
            return await ZipServis.KreirajZipAsync(postojeciFajlovi);
        });

        Logger.Log($"[Task {radnikId}] ZIP spreman: {zipPodaci.Length} bajtova");

        await PosaljiZipAsync(context, zipPodaci, "archive.zip");
        return true;
    }
    catch (Exception ex)
    {
        Logger.Log($"[Task {radnikId}] Greska u obradi zahteva: " + ex.Message);
        try
        {
            await PosaljiTekstAsync(context, "Greske na serveru.", 500);
        }
        catch
        {
            Logger.Log($"[Task {radnikId}] Greska.");
        }
        return false;
    }
}

List<string> ParsirajFajlove(string rawUrl)
{
    string putanja = rawUrl.TrimStart('/');
    if (string.IsNullOrWhiteSpace(putanja))
        return [];

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
            rezultat.Add(new FileInfo(path));
        else
            Logger.Log("Fajl ne postoji: " + ime);
    }
    return rezultat;
}

async Task PosaljiZipAsync(HttpListenerContext context, byte[] zipPodaci, string imeFajla)
{
    context.Response.StatusCode = 200;
    context.Response.ContentType = "application/zip";
    context.Response.AddHeader("Content-Disposition", $"attachment; filename=\"{imeFajla}\"");
    context.Response.ContentLength64 = zipPodaci.Length;
    await context.Response.OutputStream.WriteAsync(zipPodaci);
    context.Response.OutputStream.Close();
}

async Task PosaljiTekstAsync(HttpListenerContext context, string tekst, int statusKod = 200)
{
    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(tekst);
    context.Response.StatusCode = statusKod;
    context.Response.ContentType = "text/plain; charset=utf-8";
    context.Response.ContentLength64 = bytes.Length;
    await context.Response.OutputStream.WriteAsync(bytes);
    context.Response.OutputStream.Close();
}