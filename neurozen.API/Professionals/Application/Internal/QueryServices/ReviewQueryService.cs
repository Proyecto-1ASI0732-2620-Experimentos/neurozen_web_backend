using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Model.ReadModels;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Professionals.Domain.Services;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.Professionals.Application.Internal.QueryServices;

public class ReviewQueryService(
    IReviewRepository reviewRepository,
    IProfessionalRepository professionalRepository,
    IIamContextFacade iamContextFacade) : IReviewQueryService
{
    private const string DeletedUserName = "Usuario eliminado";

    public async Task<IReadOnlyList<ReviewDetails>> Handle(GetProfessionalReviewsQuery query)
    {
        var professional = await professionalRepository.FindByIdAsync(query.ProfessionalId);
        if (professional is not { IsVisibleToPatients: true })
            throw new ResourceNotFoundException("El profesional no existe");

        var reviews = await reviewRepository.ListByProfessionalIdAsync(professional.Id);
        var patients = await iamContextFacade.FetchUserSummariesAsync(reviews.Select(r => r.PatientId));

        return reviews
            .Select(r => new ReviewDetails(r,
                patients.TryGetValue(r.PatientId, out var patient) && !patient.IsDeleted
                    ? patient.AbbreviatedName
                    : DeletedUserName))
            .ToList();
    }
}
