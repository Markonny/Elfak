using Akka.Actor;
using Akka.Event;
using NewsTopicServer.Messages;
using NewsTopicServer.TopicModeling;

namespace NewsTopicServer.Actors;

public class TopicModelingActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly TopicClassifier _classifier = new();

    private readonly Dictionary<string, List<string>> _descriptionsByKeyword = new();

    public TopicModelingActor()
    {
        Receive<ClassifyDescriptions>(HandleClassify);
    }

    private void HandleClassify(ClassifyDescriptions msg)
    {
        var keyword = msg.Keyword.ToLowerInvariant();

        if (!_descriptionsByKeyword.TryGetValue(keyword, out var existing))
        {
            existing = new List<string>();
            _descriptionsByKeyword[keyword] = existing;
        }
        var newOnes = msg.NewDescriptions.Except(existing).ToList();
        existing.AddRange(newOnes);

        _log.Info(
            "TopicModelingActor: keyword='{0}' -> {1} novih opisa, ukupno {2} u internom stanju",
            keyword, newOnes.Count, existing.Count);

        var distribution = _classifier.ClassifyBatch(existing);

        var result = new TopicsResult(
            Keyword: keyword,
            TopicDistribution: distribution,
            TotalDescriptionsAnalyzed: existing.Count,
            GeneratedAt: DateTime.UtcNow
        );

        Sender.Tell(result);
    }

    protected override void PreStart()
    {
        _log.Info("TopicModelingActor pokrenut na dispatcher-u: {0}", Context.Props.Dispatcher);
        base.PreStart();
    }
}
