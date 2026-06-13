using System.Threading.Channels;
public class AsyncZahtevRed<T>
{
    private readonly Channel<T> kanal;

    public AsyncZahtevRed(int maxKapacitet = -1)
    {
        if (maxKapacitet > 0)
        {
            kanal = Channel.CreateBounded<T>(new BoundedChannelOptions(maxKapacitet)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = false
            });
        }
        else
        {
            kanal = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false
            });
        }
    }

    public async Task DodajAsync(T zahtev)
    {
        await kanal.Writer.WriteAsync(zahtev);
    }

    public async Task<T> UzmiAsync(CancellationToken ct = default)
    {
        return await kanal.Reader.ReadAsync(ct);
    }

    public void Zatvori() => kanal.Writer.Complete();
}
