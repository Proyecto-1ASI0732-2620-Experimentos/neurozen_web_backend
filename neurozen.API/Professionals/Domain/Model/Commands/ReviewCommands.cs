namespace neurozen.API.Professionals.Domain.Model.Commands;

public record CreateReviewCommand(long ProfessionalId, long? AppointmentId, int? Rating, string? Comment);
