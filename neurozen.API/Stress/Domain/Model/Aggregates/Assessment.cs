using neurozen.API.Stress.Domain.Model.ValueObjects;

namespace neurozen.API.Stress.Domain.Model.Aggregates;

/// <summary>Result of the self-assessment. Score and level are always computed on the server.</summary>
public class Assessment
{
    protected Assessment()
    {
        Answers = [];
    }

    /// <summary>Answers must already be validated with <see cref="PerceivedStressScale.AreValidAnswers" />.</summary>
    public Assessment(long userId, IReadOnlyList<int> answers, DateTime completedAt)
    {
        UserId = userId;
        Answers = answers.ToList();
        Score = PerceivedStressScale.Score(Answers);
        Level = PerceivedStressScale.LevelFor(Score);
        CompletedAt = completedAt;
    }

    public long Id { get; private set; }
    public long UserId { get; private set; }
    public IReadOnlyList<int> Answers { get; private set; }
    public int Score { get; private set; }
    public AssessmentLevel Level { get; private set; }
    public DateTime CompletedAt { get; private set; }
}
