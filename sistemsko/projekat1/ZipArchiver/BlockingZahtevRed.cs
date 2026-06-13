public class BlockingZahtevRed<T> //genericka klasa znaci moze da cuva bilo koji tip podataka, u nasem slucaju cuva HttpListenerContext
{ 
    private readonly Queue<T> red = new(); // pravimo red, (on nije sam po sebi thread safe)
    private readonly object locker = new(); //kreiramo pomocni objekat koji sluzi kao kljuc/key ... readonly osigurava da niko ne može slučajno da promeni ovaj objekat
    public void Dodaj(T zahtev)  //Ovu metodu poziva glavna nit kada stigne novi HTTP zahtev
    {
        lock (locker)  //Nit (glavna nit) pokušava da uzme ključ. Ako neka radna nit već drži ključ (jer vadi nešto iz reda), glavna nit ovde staje i čeka.
        {
            red.Enqueue(zahtev); //dodajemo zahtev na kraj reda
            Monitor.Pulse(locker); // ova komanda salje zahtev niti koja spava(wait) da moze da krene da radi
        }
    }//cim nit izadje iz lock bloka oslobadja se kljuc
    public T Uzmi()
    {
        lock (locker)
        {
            while (red.Count == 0)
            {
                Monitor.Wait(locker); //Ako je red prazan, nit ovde staje i zaspi.
            }
            return red.Dequeue(); //Kada izađemo iz while petlje (što znači da u redu ima bar jedan zahtev)
        }
    }
}