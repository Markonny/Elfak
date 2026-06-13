using System.Threading.Channels;
public static class Logger
{
    private static readonly string logFajl = "server.log";
    private static readonly Channel<string> kanal = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = false }
    );

    static Logger()
    {
        Thread loggerThread = new Thread(() =>
        {
            foreach (string linija in kanal.Reader.ReadAllAsync().ToBlockingEnumerable())
            {
                try
                {
                    File.AppendAllText(logFajl, linija + Environment.NewLine);
                }
                catch { /* da ne rusi logeer */ }
            }
        })
        {
            IsBackground = true,
            Name = "NitLogger"
        };

        loggerThread.Start();
    }

    public static void Log(string poruka)
    {
        string linija = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Thread {Environment.CurrentManagedThreadId}] {poruka}";
        kanal.Writer.TryWrite(linija);
    }
    public static void Zatvori()
    {
        kanal.Writer.Complete();
    }
}