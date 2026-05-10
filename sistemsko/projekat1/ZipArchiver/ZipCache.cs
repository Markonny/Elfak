public class ZipCache
{
    private class CacheEntry
    {
        public byte[] Podaci { get; set; } = Array.Empty<byte>();
        public long Velicina => Podaci.Length;
        public DateTime PoslednjiPristup { get; set; } = DateTime.UtcNow;
    }

    private class ObradaUToku
    {
        public bool Zavrseno { get; set; }
        public byte[]? Podaci { get; set; }
        public Exception? Greska { get; set; }
    }

    private readonly object locker = new();

    private readonly Dictionary<string, CacheEntry> cache = new();
    private readonly Dictionary<string, ObradaUToku> obradeUToku = new();

    private readonly long maksimalnaVelicina;
    private long trenutnaVelicina;

    public ZipCache(long maksimalnaVelicina)
    {
        this.maksimalnaVelicina = maksimalnaVelicina;
    }

    public byte[] VratiIliKreiraj(string kljuc, Func<byte[]> kreiraj)
    {
        ObradaUToku? obrada;
        lock (locker)
        {
            if (cache.TryGetValue(kljuc, out CacheEntry? entry))
            {
                entry.PoslednjiPristup = DateTime.UtcNow;
                Logger.Log("CACHE HIT: " + kljuc);
                return entry.Podaci;
            }

            if (obradeUToku.TryGetValue(kljuc, out obrada))
            {
                Logger.Log("CACHE WAIT: " + kljuc);

                while (!obrada.Zavrseno)
                {
                    Monitor.Wait(locker);
                }

                if (obrada.Greska != null)
                {
                    throw obrada.Greska;
                }

                return obrada.Podaci!;
            }

            obrada = new ObradaUToku();
            obradeUToku[kljuc] = obrada;
        }

        byte[] rezultat;
        Exception? greska = null;
        try
        {
            Logger.Log("CACHE MISS: " + kljuc);
            rezultat = kreiraj();
        }
        catch (Exception ex)
        {
            rezultat = Array.Empty<byte>();
            greska = ex;
        }

        lock (locker) {
            obrada.Podaci = rezultat;
            obrada.Greska = greska;
            obrada.Zavrseno = true;

            obradeUToku.Remove(kljuc);

            if (greska == null && rezultat.Length <= maksimalnaVelicina)
            {
                DodajUCache(kljuc, rezultat);
            }

            Monitor.PulseAll(locker);
        }
        if (greska != null)
        {
            throw greska;
        }
        return rezultat;
    }
    private void DodajUCache(string kljuc, byte[] podaci)
    {
        while (trenutnaVelicina + podaci.Length > maksimalnaVelicina && cache.Count > 0)
        {
            string najstarijiKljuc = cache
                .OrderBy(x => x.Value.PoslednjiPristup)
                .First()
                .Key;

            trenutnaVelicina -= cache[najstarijiKljuc].Velicina;
            cache.Remove(najstarijiKljuc);

            Logger.Log("CACHE EVICT: " + najstarijiKljuc);
        }

        cache[kljuc] = new CacheEntry
        {
            Podaci = podaci,
            PoslednjiPristup = DateTime.UtcNow
        };

        trenutnaVelicina += podaci.Length;

        Logger.Log($"CACHE ADD: {kljuc}, trenutna velicina cache-a: {trenutnaVelicina} bajtova");
    }

    public static string KreirajKljuc(List<FileInfo> fajlovi)
    {
        return string.Join("|",
            fajlovi
                .OrderBy(f => f.Name)
                .Select(f => $"{f.Name}:{f.Length}:{f.LastWriteTimeUtc.Ticks}")
        );
    }
}