namespace NewsTopicServer.TopicModeling;
public static class TrainingCorpus
{
    public static readonly Dictionary<string, string[]> CategoryKeywords = new()
    {
        ["Sport"] = new[]
        {
            "team", "player", "match", "game", "season", "league", "coach", "score",
            "championship", "tournament", "goal", "win", "victory", "cup", "athlete",
            "football", "basketball", "tennis", "olympic", "stadium"
        },
        ["Tehnologija"] = new[]
        {
            "technology", "software", "app", "device", "startup", "artificial", "intelligence",
            "computer", "data", "internet", "digital", "chip", "smartphone", "google", "apple",
            "microsoft", "code", "developer", "robot", "algorithm"
        },
        ["Politika"] = new[]
        {
            "government", "president", "election", "policy", "senate", "congress", "minister",
            "law", "vote", "political", "parliament", "campaign", "administration", "diplomat",
            "sanctions", "president", "war", "conflict", "military", "treaty"
        },
        ["Biznis"] = new[]
        {
            "company", "market", "stock", "investment", "economy", "business", "profit",
            "revenue", "trade", "bank", "financial", "shares", "ceo", "startup", "merger",
            "inflation", "economic", "industry", "billion", "million"
        },
        ["Zdravlje"] = new[]
        {
            "health", "hospital", "doctor", "patient", "disease", "virus", "medicine",
            "treatment", "vaccine", "medical", "drug", "study", "researchers", "cancer",
            "mental", "covid", "surgery", "clinical", "healthcare", "diagnosis"
        },
        ["Zabava"] = new[]
        {
            "movie", "film", "music", "concert", "actor", "actress", "singer", "album",
            "celebrity", "show", "television", "series", "award", "festival", "star",
            "hollywood", "director", "song", "entertainment", "streaming"
        },
        ["Nauka"] = new[]
        {
            "scientists", "research", "study", "space", "nasa", "climate", "discovery",
            "experiment", "university", "physics", "biology", "environment", "energy",
            "planet", "species", "ocean", "scientific", "laboratory", "satellite", "telescope"
        }
    };
    public static List<SharpEntropy.TrainingEvent> BuildTrainingEvents()
    {
        var events = new List<SharpEntropy.TrainingEvent>();
        var random = new Random(42);

        foreach (var (category, words) in CategoryKeywords)
        {
 
            for (int i = 0; i < 30; i++)
            {
                var sampleSize = Math.Min(4, words.Length);
                var context = words
                    .OrderBy(_ => random.Next())
                    .Take(sampleSize)
                    .ToArray();

                events.Add(new SharpEntropy.TrainingEvent(category, context));
            }
        }

        return events;
    }
}
