namespace NewsTopicServer.Messages;
public record GetTopicsRequest(string Keyword);

public record TopicsResult(
    string Keyword,
    Dictionary<string, int> TopicDistribution,
    int TotalDescriptionsAnalyzed,
    DateTime GeneratedAt
);
public record InternalDescriptionsFetched(string Keyword, List<string> Descriptions, Akka.Actor.IActorRef ReplyTo);
public record ClassifyDescriptions(string Keyword, List<string> NewDescriptions);
public record InternalClassificationDone(string Keyword, TopicsResult Result, Akka.Actor.IActorRef ReplyTo);
public record InternalOperationFailed(string Keyword, Akka.Actor.IActorRef ReplyTo, string Reason);
