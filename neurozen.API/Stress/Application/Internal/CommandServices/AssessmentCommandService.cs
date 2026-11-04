using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Commands;
using neurozen.API.Stress.Domain.Model.ValueObjects;
using neurozen.API.Stress.Domain.Repositories;
using neurozen.API.Stress.Domain.Services;

namespace neurozen.API.Stress.Application.Internal.CommandServices;

public class AssessmentCommandService(
    IAssessmentRepository assessmentRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IAssessmentCommandService
{
    public async Task<Assessment> Handle(CreateAssessmentCommand command)
    {
        var userId = StressAccess.ResolveOwnUserId(currentUser, command.UserId);

        if (!PerceivedStressScale.AreValidAnswers(command.Answers))
        {
            var message =
                $"Responde las {PerceivedStressScale.QuestionCount} preguntas con valores entre " +
                $"{PerceivedStressScale.MinAnswer} y {PerceivedStressScale.MaxAnswer}";
            throw new ValidationFailedException(message,
                new Dictionary<string, string[]> { ["answers"] = [message] });
        }

        // Score, level and completion time are computed here; the client values are ignored.
        var assessment = new Assessment(userId, command.Answers!, DateTime.UtcNow);
        await assessmentRepository.AddAsync(assessment);
        await unitOfWork.CompleteAsync();
        return assessment;
    }
}
