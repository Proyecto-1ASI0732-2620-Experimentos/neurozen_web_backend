using System.Globalization;
using Microsoft.Extensions.Options;
using neurozen.API.Safety.Application.Internal.Configuration;
using neurozen.API.Safety.Domain.Model.Entities;
using neurozen.API.Safety.Domain.Model.Queries;
using neurozen.API.Safety.Domain.Services;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.Safety.Application.Internal.QueryServices;

public class EmergencyResourceQueryService(IOptions<EmergencyResourcesSettings> settings)
    : IEmergencyResourceQueryService
{
    private const string InvalidCountryMessage = "country debe ser un código de país de 2 letras (por ejemplo, PE)";

    public DateOnly? VerifiedAt =>
        DateOnly.TryParseExact(settings.Value.VerifiedAt?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date
            : null;

    public IReadOnlyList<EmergencyResource> Handle(GetEmergencyResourcesQuery query)
    {
        var country = string.IsNullOrWhiteSpace(query.Country)
            ? EmergencyResourceCatalog.Peru
            : query.Country.Trim().ToUpperInvariant();
        if (country.Length != 2 || !country.All(c => c is >= 'A' and <= 'Z'))
            throw new ValidationFailedException(InvalidCountryMessage,
                new Dictionary<string, string[]> { ["country"] = [InvalidCountryMessage] });

        return EmergencyResourceCatalog.All.Where(resource => resource.Country == country).ToList();
    }
}
