public static class Logger
{
    private static readonly object locker = new();
    private static readonly string logFajl = "server.log";

    public static void Log(string poruka)
    {
        lock (locker)
        {
            string linija = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Nit {Thread.CurrentThread.ManagedThreadId}] {poruka}";
            File.AppendAllText(logFajl, linija + Environment.NewLine);
        }
    }
}