using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.Entities;
using neurozen.API.Professionals.Domain.Model.ReadModels;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Professionals.Domain.Services;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Professionals.Application.Internal.CommandServices;

/// <summary>
///     Manual license verification by an ADMIN (the CPsP has no official API; no scraping).
///     Every decision is stored in professional_verification_audits and logged (ids only).
/// </summary>
public class ProfessionalVerificationCommandService(
    IProfessionalRepository professionalRepository,
    IVerificationAuditRepository auditRepository,
    IIamContextFacade iamContextFacade,
    IUnitOfWork unitOfWork,
    ILogger<ProfessionalVerificationCommandService> logger) : IProfessionalVerificationCommandService
{
    public const int ReasonMinLength = 10;

    public async Task<ProfessionalDetails> Handle(VerifyProfessionalCommand command)
    {
        var professional = await FindAsync(command.ProfessionalId);
        var now = DateTime.UtcNow;

        professional.Verify(command.AdminUserId, now);
        await auditRepository.AddAsync(new VerificationAuditEntry(
            professional.Id, command.AdminUserId, VerificationAuditEntry.VerifiedAction, null, now));
        await unitOfWork.CompleteAsync();

        logger.LogInformation("Professional {ProfessionalId} VERIFIED by admin {AdminUserId}",
            professional.Id, command.AdminUserId);
        return await WithOwnerAsync(professional);
    }

    public async Task<ProfessionalDetails> Handle(RejectProfessionalCommand command)
    {
        var reason = (command.Reason ?? string.Empty).Trim();
        if (reason.Length is < ReasonMinLength or > Professional.RejectionReasonMaxLength)
        {
            var message =
                $"El motivo del rechazo debe tener entre {ReasonMinLength} y {Professional.RejectionReasonMaxLength} caracteres";
            throw new ValidationFailedException(message,
                new Dictionary<string, string[]> { ["reason"] = [message] });
        }

        var professional = await FindAsync(command.ProfessionalId);
        var now = DateTime.UtcNow;

        professional.Reject(reason, now);
        await auditRepository.AddAsync(new VerificationAuditEntry(
            professional.Id, command.AdminUserId, VerificationAuditEntry.RejectedAction, reason, now));
        await unitOfWork.CompleteAsync();

        logger.LogInformation("Professional {ProfessionalId} REJECTED by admin {AdminUserId}",
            professional.Id, command.AdminUserId);
        return await WithOwnerAsync(professional);
    }

    private async Task<Professional> FindAsync(long professionalId)
    {
        var professional = await professionalRepository.FindByIdAsync(professionalId);
        if (professional is null || professional.IsDeleted)
            throw new ResourceNotFoundException("El profesional no existe");
        return professional;
    }

    private async Task<ProfessionalDetails> WithOwnerAsync(Professional professional)
    {
        var owner = await iamContextFacade.FetchUserSummaryAsync(professional.UserId);
        return new ProfessionalDetails(professional, owner?.FullName ?? string.Empty, owner?.Email ?? string.Empty);
    }
}
