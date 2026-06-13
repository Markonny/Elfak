public static class Logger //deklarisana kao static da bi mogli da je pozovemo iz bilo kog mesta u kodu sa logger.log("poruka")
{
    private static readonly object locker = new(); //objekat kao kljuc
    private static readonly string logFajl = "server.log"; //Definiše ime fajla u koji se upisuju logovi

    public static void Log(string poruka)
    {
        lock (locker)
        {
            string linija = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Nit {Thread.CurrentThread.ManagedThreadId}] {poruka}";
            File.AppendAllText(logFajl, linija + Environment.NewLine);
        }
    }
}