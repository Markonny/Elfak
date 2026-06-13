public class ZipCache
{
    private class CacheEntry
    {
        public byte[] Podaci { get; set; } = Array.Empty<byte>();
        public long Velicina => Podaci.Length;
        public DateTime PoslednjiPristup { get; set; } = DateTime.UtcNow;
    }

    private readonly object locker = new();
    private readonly Dictionary<string, CacheEntry> cache = new();
    private readonly Dictionary<string, TaskCompletionSource<byte[]>> obradeUToku = new();

    private readonly long maksimalnaVelicina;
    private long trenutnaVelicina;

    public ZipCache(long maksimalnaVelicina)
    {
        this.maksimalnaVelicina = maksimalnaVelicina;
    }
    public async Task<byte[]> VratiIliKreirajAsync(string kljuc, Func<Task<byte[]>> kreiraj)
    {
        TaskCompletionSource<byte[]>? tcs = null;
        bool miKreiramo = false;

        lock (locker)
        {
            if (cache.TryGetValue(kljuc, out CacheEntry? entry))
            {
                entry.PoslednjiPristup = DateTime.UtcNow;
                Logger.Log("CACHE HIT: " + kljuc);
                return entry.Podaci;
            }

            if (obradeUToku.TryGetValue(kljuc, out TaskCompletionSource<byte[]>? postojeciTcs))
            {
                Logger.Log("CACHE WAIT: " + kljuc);
                tcs = postojeciTcs;
            }
            else
            {
                tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                obradeUToku[kljuc] = tcs;
                miKreiramo = true;
            }
        }

        if (!miKreiramo)
        {
            return await tcs!.Task;
        }

        byte[] rezultat;
        try
        {
            Logger.Log("CACHE MISS: " + kljuc);
            rezultat = await kreiraj();

            lock (locker)
            {
                obradeUToku.Remove(kljuc);
                if (rezultat.Length <= maksimalnaVelicina)
                {
                    DodajUCache(kljuc, rezultat);
                }
            }

            tcs.SetResult(rezultat);
        }
        catch (Exception ex)
        {
            lock (locker)
            {
                obradeUToku.Remove(kljuc);
            }
            tcs.SetException(ex);
            throw;
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
            Logger.Log("CACHE EVICT (LRU): " + najstarijiKljuc);
        }

        cache[kljuc] = new CacheEntry
        {
            Podaci = podaci,
            PoslednjiPristup = DateTime.UtcNow
        };
        trenutnaVelicina += podaci.Length;
        Logger.Log($"CACHE ADD: {kljuc} | Velicina kesa: {trenutnaVelicina} / {maksimalnaVelicina} bajtova");
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
