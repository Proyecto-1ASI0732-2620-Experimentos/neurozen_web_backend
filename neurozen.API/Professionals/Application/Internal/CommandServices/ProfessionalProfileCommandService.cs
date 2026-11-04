using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.ReadModels;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Professionals.Domain.Services;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Professionals.Application.Internal.CommandServices;

public class ProfessionalProfileCommandService(
    IProfessionalRepository professionalRepository,
    IIamContextFacade iamContextFacade,
    IUnitOfWork unitOfWork,
    ILogger<ProfessionalProfileCommandService> logger) : IProfessionalProfileCommandService
{
    public async Task<ProfessionalDetails> Handle(UpdateMyProfessionalProfileCommand command)
    {
        var professional = await professionalRepository.FindByUserIdAsync(command.UserId);
        if (professional is null || professional.IsDeleted)
            throw new ResourceNotFoundException("Tu cuenta no tiene un perfil profesional");

        var owner = await iamContextFacade.FetchUserSummaryAsync(command.UserId)
                    ?? throw new ResourceNotFoundException("La cuenta no existe");

        var errors = new ValidationErrors();

        var fullName = (command.FullName ?? string.Empty).Trim();
        iamContextFacade.ValidateFullName(fullName, "fullName", errors);

        errors.AddIf(!SpecialtyExtensions.TryFromWire(command.Specialty, out var specialty), "specialty",
            "Selecciona una especialidad válida");

        var licenseNumber = (command.LicenseNumber ?? string.Empty).Trim();
        errors.AddIf(!Professional.IsValidLicenseNumber(licenseNumber), "licenseNumber",
            "El número de colegiatura debe tener entre 3 y 6 dígitos");

        var bio = string.IsNullOrWhiteSpace(command.Bio) ? null : command.Bio.Trim();
        errors.AddIf(bio?.Length > Professional.BioMaxLength, "bio",
            $"La presentación no puede superar {Professional.BioMaxLength} caracteres");

        errors.AddIf(command.ExperienceYears is < 0 or > 60, "experienceYears",
            "Los años de experiencia deben estar entre 0 y 60");
        errors.AddIf(command.Price is < 0 or > 1000, "price", "La tarifa debe estar entre 0 y 1000");

        AvailabilitySchedule? schedule = null;
        if (!command.HasAvailabilitySchedule)
            errors.Add("availabilitySchedule", "Indica tus días y horario de atención");
        else
            schedule = AvailabilitySchedule.TryCreate(command.AvailabilityDays, command.AvailabilityStart,
                command.AvailabilityEnd, errors);

        var availability = string.IsNullOrWhiteSpace(command.Availability) ? null : command.Availability.Trim();
        errors.AddIf(availability?.Length > Professional.AvailabilityMaxLength, "availability",
            $"El texto de disponibilidad no puede superar {Professional.AvailabilityMaxLength} caracteres");

        errors.ThrowIfAny();

        if (licenseNumber != professional.LicenseNumber &&
            await professionalRepository.ExistsByLicenseNumberForOtherAsync(licenseNumber, professional.Id))
            throw new ConflictException("Ya existe una cuenta con esta colegiatura");

        var fullNameChanged = !string.Equals(fullName, owner.FullName, StringComparison.Ordinal);
        var resubmitted = professional.UpdateProfile(
            specialty,
            licenseNumber,
            bio,
            command.ExperienceYears,
            command.Price is null ? null : Math.Round(command.Price.Value, 2),
            schedule!,
            availability ?? schedule!.Describe(),
            fullNameChanged);

        if (fullNameChanged) await iamContextFacade.ChangeFullNameAsync(command.UserId, fullName);

        await unitOfWork.CompleteAsync();

        if (resubmitted)
            logger.LogInformation("Professional {ProfessionalId} returned to PENDING verification", professional.Id);

        return new ProfessionalDetails(professional, fullName, owner.Email);
    }
}
