using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.ReadModels;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Interfaces.REST.Resources;

namespace neurozen.API.Professionals.Interfaces.REST.Transform;

public static class ProfessionalResourceFromEntityAssembler
{
    public static ProfessionalResource ToResourceFromEntity(ProfessionalDetails details)
    {
        var p = details.Professional;
        return new ProfessionalResource(
            p.Id,
            p.UserId,
            details.Name,
            p.Specialty.ToWire(),
            p.Bio ?? string.Empty,
            p.ExperienceYears,
            p.Price,
            p.Rating,
            p.ReviewsCount,
            p.Availability ?? string.Empty,
            p.Image ?? string.Empty,
            p.VerificationStatus.ToWire());
    }
}

public static class MyProfessionalProfileResourceFromEntityAssembler
{
    public static MyProfessionalProfileResource ToResourceFromEntity(ProfessionalDetails details)
    {
        var p = details.Professional;
        return new MyProfessionalProfileResource(
            p.Id,
            p.UserId,
            details.Name,
            details.Email,
            p.Specialty.ToWire(),
            p.LicenseNumber ?? string.Empty,
            p.VerificationStatus.ToWire(),
            p.RejectionReason ?? string.Empty,
            p.Bio ?? string.Empty,
            p.ExperienceYears,
            p.Price,
            p.Rating,
            p.ReviewsCount,
            p.AvailabilitySchedule is null
                ? null
                : new AvailabilityScheduleResource(p.AvailabilitySchedule.Days, p.AvailabilitySchedule.Start,
                    p.AvailabilitySchedule.End),
            p.Availability ?? string.Empty,
            p.CreatedAt);
    }
}

public static class AdminProfessionalResourceFromEntityAssembler
{
    public static AdminProfessionalResource ToResourceFromEntity(ProfessionalDetails details)
    {
        var p = details.Professional;
        return new AdminProfessionalResource(
            p.Id,
            p.UserId,
            details.Name,
            details.Email,
            p.Specialty.ToWire(),
            p.LicenseNumber ?? string.Empty,
            p.VerificationStatus.ToWire(),
            p.RejectionReason ?? string.Empty,
            p.CreatedAt);
    }
}

public static class UpdateMyProfessionalProfileCommandFromResourceAssembler
{
    public static UpdateMyProfessionalProfileCommand ToCommandFromResource(long userId,
        UpdateMyProfessionalProfileResource resource) =>
        new(userId,
            resource.FullName,
            resource.Specialty,
            resource.LicenseNumber,
            resource.Bio,
            resource.ExperienceYears,
            resource.Price,
            resource.AvailabilitySchedule is not null,
            resource.AvailabilitySchedule?.Days,
            resource.AvailabilitySchedule?.Start,
            resource.AvailabilitySchedule?.End,
            resource.Availability);
}

public static class ReviewResourceAssembler
{
    public static ReviewResource ToResource(ReviewDetails details) => new(
        details.Review.Id,
        details.UserName,
        details.Review.Rating,
        details.Review.Comment ?? string.Empty,
        details.Review.CreatedAt);
}
