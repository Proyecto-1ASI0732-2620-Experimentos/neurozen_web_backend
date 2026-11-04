namespace neurozen.API.Stress.Domain.Model.ValueObjects;

/// <summary>
///     Scoring of the self-assessment, based on the Perceived Stress Scale (PSS-10).
///     Questions 4, 5, 7 and 8 (indexes 3, 4, 6, 7) are reverse-scored (4 - value); the rest are direct.
///     Total 0-40 → 0-13 low · 14-26 moderate · 27-40 high. Guidance only, not a clinical diagnosis.
/// </summary>
public static class PerceivedStressScale
{
    public const int QuestionCount = 10;
    public const int MinAnswer = 0;
    public const int MaxAnswer = 4;
    public const int MaxScore = QuestionCount * MaxAnswer;

    private static readonly HashSet<int> ReversedIndexes = [3, 4, 6, 7];

    public static bool AreValidAnswers(IReadOnlyList<int>? answers) =>
        answers is { Count: QuestionCount } && answers.All(answer => answer is >= MinAnswer and <= MaxAnswer);

    public static int Score(IReadOnlyList<int> answers) =>
        answers.Select((answer, index) => ReversedIndexes.Contains(index) ? MaxAnswer - answer : answer).Sum();

    public static AssessmentLevel LevelFor(int score) => score switch
    {
        <= 13 => AssessmentLevel.Low,
        <= 26 => AssessmentLevel.Moderate,
        _ => AssessmentLevel.High
    };
}
