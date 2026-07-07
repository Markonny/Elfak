namespace NewsTopicServer.TopicModeling;

public class InMemoryEventReader : SharpEntropy.ITrainingEventReader
{
    private readonly List<SharpEntropy.TrainingEvent> _events;
    private int _index;

    public InMemoryEventReader(List<SharpEntropy.TrainingEvent> events)
    {
        _events = events;
        _index = 0;
    }

    public bool HasNext() => _index < _events.Count;

    public SharpEntropy.TrainingEvent ReadNextEvent()
    {
        return _events[_index++];
    }
}
