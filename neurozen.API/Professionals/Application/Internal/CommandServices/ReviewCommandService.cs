using neurozen.API.Appointments.Interfaces.ACL;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.Entities;
using neurozen.API.Professionals.Domain.Model.ReadModels;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Professionals.Domain.Services;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Professionals.Application.Internal.CommandServices;

public class ReviewCommandService(
    IReviewRepository reviewRepository,
    IProfessionalRepository professionalRepository,
    IAppointmentsContextFacade appointmentsContextFacade,
    IIamContextFacade iamContextFacade,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IReviewCommandService
{
    public const string DuplicateMessage = "Ya calificaste esta sesión";
    private const string NotCompletedMessage = "Solo se puede calificar una sesión atendida";

    public async Task<ReviewDetails> Handle(CreateReviewCommand command)
    {
        var errors = new ValidationErrors();
        errors.AddIf(command.AppointmentId is null, "appointmentId", "appointmentId es obligatorio");
        errors.AddIf(command.Rating is null or < Review.MinRating or > Review.MaxRating, "rating",
            $"La calificación debe ser un entero entre {Review.MinRating} y {Review.MaxRating}");
        var comment = string.IsNullOrWhiteSpace(command.Comment) ? null : command.Comment.Trim();
        errors.AddIf(comment?.Length > Review.CommentMaxLength, "comment",
            $"El comentario no puede superar {Review.CommentMaxLength} caracteres");
        errors.ThrowIfAny();

        var professional = await professionalRepository.FindByIdAsync(command.ProfessionalId);
        if (professional is null || professional.IsDeleted)
            throw new ResourceNotFoundException("El profesional no existe");

        var appointment = await appointmentsContextFacade.FindAppointmentAsync(command.AppointmentId!.Value);
        if (appointment is null || appointment.PatientId != currentUser.Id ||
            appointment.ProfessionalId != professional.Id || !appointment.IsCompleted)
            throw new ValidationFailedException(NotCompletedMessage);

        if (appointment.Reviewed || await reviewRepository.ExistsByAppointmentIdAsync(appointment.Id))
            throw new ConflictException(DuplicateMessage);

        var review = new Review(professional.Id, appointment.Id, currentUser.Id, command.Rating!.Value, comment);

        // Review, appointment.reviewed and the professional's rating/reviews change together.
        // The unique index on reviews.appointment_id still protects concurrent submissions (409).
        ReviewDetails? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var trackedProfessional = await professionalRepository.FindByIdAsync(professional.Id)
                ?? throw new ResourceNotFoundException("El profesional no existe");
            var trackedAppointment = await appointmentsContextFacade.FindAppointmentAsync(appointment.Id)
                ?? throw new ResourceNotFoundException("La cita no existe");

            if (trackedAppointment.Reviewed || await reviewRepository.ExistsByAppointmentIdAsync(trackedAppointment.Id))
                throw new ConflictException(DuplicateMessage);

            await reviewRepository.AddAsync(review);
            await unitOfWork.CompleteAsync();

            var statistics = await reviewRepository.GetStatisticsAsync(trackedProfessional.Id);
            trackedProfessional.ApplyReviewStatistics(statistics.AverageRating, statistics.Count);
            await appointmentsContextFacade.MarkReviewedAsync(trackedAppointment.Id);
            await unitOfWork.CompleteAsync();

            var patient = await iamContextFacade.FetchUserSummaryAsync(currentUser.Id);
            result = new ReviewDetails(review, patient?.AbbreviatedName ?? string.Empty);
        });

        return result ?? throw new InvalidOperationException("Failed to create review");
    }
}
