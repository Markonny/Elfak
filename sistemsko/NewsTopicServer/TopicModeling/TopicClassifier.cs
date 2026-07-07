using System.Text.RegularExpressions;

namespace NewsTopicServer.TopicModeling;

public class TopicClassifier
{
    private static readonly Regex WordRegex = new(@"[a-zA-Z]{3,}", RegexOptions.Compiled);

    private readonly SharpEntropy.GisModel _model;

    public TopicClassifier()
    {
        var trainingEvents = TrainingCorpus.BuildTrainingEvents();
        var eventReader = new InMemoryEventReader(trainingEvents);

        var trainer = new SharpEntropy.GisTrainer();
        trainer.TrainModel(100, new SharpEntropy.TwoPassDataIndexer(eventReader, 1));

        _model = new SharpEntropy.GisModel(trainer);
    }

    public string ClassifyOne(string description)
    {
        var context = Tokenize(description);
        if (context.Length == 0)
        {
            return "Nepoznato";
        }

        var probabilities = _model.Evaluate(context);
        return _model.GetBestOutcome(probabilities);
    }
    public Dictionary<string, int> ClassifyBatch(IEnumerable<string> descriptions)
    {
        var distribution = new Dictionary<string, int>();

        foreach (var description in descriptions)
        {
            var topic = ClassifyOne(description);
            distribution[topic] = distribution.GetValueOrDefault(topic) + 1;
        }

        return distribution
            .OrderByDescending(kv => kv.Value)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static string[] Tokenize(string text)
    {
        return WordRegex
            .Matches(text.ToLowerInvariant())
            .Select(m => m.Value)
            .Distinct()
            .ToArray();
    }
}
