using Akka.Actor;
using Akka.Event;
using NewsTopicServer.Messages;
using NewsTopicServer.Services;
 
namespace NewsTopicServer.Actors;
public class KeywordCoordinatorActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly NewsApiClient _newsApiClient;
    private readonly IActorRef _topicModelingActor;
 
    public KeywordCoordinatorActor(NewsApiClient newsApiClient, IActorRef topicModelingActor)
    {
        _newsApiClient = newsApiClient;
        _topicModelingActor = topicModelingActor;
 
        Receive<GetTopicsRequest>(HandleGetTopics);
        Receive<InternalDescriptionsFetched>(HandleDescriptionsFetched);
        Receive<InternalClassificationDone>(HandleClassificationDone);
        Receive<InternalOperationFailed>(HandleOperationFailed);
    }
 
    private void HandleGetTopics(GetTopicsRequest req)
    {
        var keyword = req.Keyword.Trim().ToLowerInvariant();
        var replyTo = Sender;
        var self = Self;
 
        _log.Info("Zahtev za keyword='{0}' - pokrećem Rx fetch sa NewsAPI", keyword);
 
        _newsApiClient.FetchSortedDescriptions(keyword).Subscribe(
            onNext: descriptions => self.Tell(new InternalDescriptionsFetched(keyword, descriptions, replyTo)),
            onError: ex =>
            {
                _log.Error(ex, "Greška pri Rx fetch-u za keyword='{0}'", keyword);
                self.Tell(new InternalOperationFailed(keyword, replyTo, ex.Message));
            }
        );
    }
 
    private void HandleDescriptionsFetched(InternalDescriptionsFetched msg)
    {
        _log.Info("Rx pipeline završen: keyword='{0}', {1} opisa - šaljem TopicModelingActor-u",
            msg.Keyword, msg.Descriptions.Count);
 
        if (msg.Descriptions.Count == 0)
        {
            var emptyResult = new TopicsResult(
                msg.Keyword, new Dictionary<string, int>(), 0, DateTime.UtcNow);
            msg.ReplyTo.Tell(emptyResult);
            return;
        }
 
        var keyword = msg.Keyword;
        var replyTo = msg.ReplyTo;
 
        _topicModelingActor
            .Ask<TopicsResult>(new ClassifyDescriptions(keyword, msg.Descriptions), TimeSpan.FromSeconds(15))
            .PipeTo(
                Self,
                success: result => new InternalClassificationDone(keyword, result, replyTo),
                failure: ex => new InternalOperationFailed(keyword, replyTo, ex.GetBaseException().Message)
            );
    }
 
    private void HandleClassificationDone(InternalClassificationDone msg)
    {
        msg.ReplyTo.Tell(msg.Result);
    }
 
    private void HandleOperationFailed(InternalOperationFailed msg)
    {
        _log.Warning("Operacija neuspesna za keyword='{0}': {1}", msg.Keyword, msg.Reason);
        msg.ReplyTo.Tell(new Status.Failure(new Exception(msg.Reason)));
    }
}