public class BlockingZahtevRed<T>
{
    private readonly Queue<T> red = new();
    private readonly object locker = new();
    public void Dodaj(T zahtev)
    {
        lock (locker)
        {
            red.Enqueue(zahtev);
            Monitor.Pulse(locker);
        }
    }
    public T Uzmi()
    {
        lock (locker)
        {
            while (red.Count == 0)
            {
                Monitor.Wait(locker);
            }
            return red.Dequeue();
        }
    }
}