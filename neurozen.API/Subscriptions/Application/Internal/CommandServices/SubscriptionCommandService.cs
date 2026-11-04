using System.Net.Mail;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Subscriptions.Domain.Model.Aggregates;
using neurozen.API.Subscriptions.Domain.Model.Commands;
using neurozen.API.Subscriptions.Domain.Model.ValueObjects;
using neurozen.API.Subscriptions.Domain.Repositories;
using neurozen.API.Subscriptions.Domain.Services;

namespace neurozen.API.Subscriptions.Application.Internal.CommandServices;

/// <summary>
///     Validates the card in memory (Luhn, expiry, CVV), keeps only the last 4 digits and the expiry, and
///     never logs or echoes card data (error messages do not include the submitted values).
/// </summary>
public class SubscriptionCommandService(
    ISubscriptionRepository subscriptionRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ISubscriptionCommandService
{
    public async Task<SubscriptionResult> Handle(CreateSubscriptionCommand command)
    {
        if (command.UserId is not null && command.UserId.Value != currentUser.Id)
            throw new ForbiddenOperationException("Solo puedes suscribir tu propia cuenta");

        var errors = new ValidationErrors();

        errors.AddIf(command.PlanId is null or < Subscription.MinPlanId or > Subscription.MaxPlanId, "planId",
            $"El plan debe estar entre {Subscription.MinPlanId} y {Subscription.MaxPlanId}");

        var firstName = (command.NameUser ?? string.Empty).Trim();
        var lastName = (command.LastNameUser ?? string.Empty).Trim();
        errors.AddIf(firstName.Length is 0 or > Subscription.NameMaxLength, "nameUser",
            $"Ingresa el nombre del titular (máximo {Subscription.NameMaxLength} caracteres)");
        errors.AddIf(lastName.Length is 0 or > Subscription.NameMaxLength, "lastNameUser",
            $"Ingresa el apellido del titular (máximo {Subscription.NameMaxLength} caracteres)");

        var email = (command.EmailUser ?? string.Empty).Trim().ToLowerInvariant();
        errors.AddIf(!IsValidEmail(email), "emailUser", "Ingresa un correo válido");

        var cardNumber = PaymentCard.NormalizeNumber(command.NumberCard);
        errors.AddIf(!PaymentCard.IsValidNumber(cardNumber), "numberCard", "El número de tarjeta no es válido");

        var expiryOk = PaymentCard.TryParseExpiry(command.ExpirationDate, out var month, out var year,
            out var cardExpiry);
        errors.AddIf(!expiryOk, "expirationDate", "La fecha de vencimiento debe tener el formato MM/AA");
        errors.AddIf(expiryOk && PaymentCard.IsExpired(month, year, DateTime.UtcNow), "expirationDate",
            "La tarjeta está vencida");

        errors.AddIf(!PaymentCard.IsValidCvv(command.Cvv?.Trim()), "cvv", "El CVV debe tener 3 o 4 dígitos");

        errors.ThrowIfAny();

        var cardLast4 = PaymentCard.LastFour(cardNumber!);
        var existing = await subscriptionRepository.FindByUserIdAsync(currentUser.Id);
        if (existing is not null)
        {
            existing.Apply(command.PlanId!.Value, firstName, lastName, email, cardLast4, cardExpiry);
            await unitOfWork.CompleteAsync();
            return new SubscriptionResult(existing, false);
        }

        var subscription = new Subscription(currentUser.Id, command.PlanId!.Value, firstName, lastName, email,
            cardLast4, cardExpiry);
        await subscriptionRepository.AddAsync(subscription);
        await unitOfWork.CompleteAsync();
        return new SubscriptionResult(subscription, true);
    }

    private static bool IsValidEmail(string email) =>
        email.Length is > 0 and <= Subscription.EmailMaxLength &&
        MailAddress.TryCreate(email, out var address) && address.Address == email && email.Contains('.');
}
