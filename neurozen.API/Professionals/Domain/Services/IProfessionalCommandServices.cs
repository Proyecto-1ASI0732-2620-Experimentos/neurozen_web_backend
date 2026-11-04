using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.ReadModels;

namespace neurozen.API.Professionals.Domain.Services;

public interface IProfessionalProfileCommandService
{
    Task<ProfessionalDetails> Handle(UpdateMyProfessionalProfileCommand command);
}

public interface IProfessionalVerificationCommandService
{
    Task<ProfessionalDetails> Handle(VerifyProfessionalCommand command);

    Task<ProfessionalDetails> Handle(RejectProfessionalCommand command);
}
