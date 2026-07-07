namespace NewsTopicServer.Services;
public static class Logger
{
    private static readonly object LockObject = new();
    private static readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "server.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    public static void Request(string method, string path, string query) =>
        Write("REQUEST", $"{method} {path}{(string.IsNullOrEmpty(query) ? "" : "?" + query)}");

    public static void RequestResult(string keyword, bool success, string details) =>
        Write("RESULT", $"keyword='{keyword}' success={success} - {details}");

    private static void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

        lock (LockObject)
        {
            Console.WriteLine(line);
            try
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
            catch
            {
                // loger
            }
        }
    }
}
