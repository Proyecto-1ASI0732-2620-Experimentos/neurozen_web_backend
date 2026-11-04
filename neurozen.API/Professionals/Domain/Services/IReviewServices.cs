using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Model.ReadModels;

namespace neurozen.API.Professionals.Domain.Services;

public interface IReviewCommandService
{
    Task<ReviewDetails> Handle(CreateReviewCommand command);
}

public interface IReviewQueryService
{
    Task<IReadOnlyList<ReviewDetails>> Handle(GetProfessionalReviewsQuery query);
}
