using System.Globalization;
using neurozen.API.Safety.Domain.Model.Entities;
using neurozen.API.Safety.Interfaces.REST.Resources;

namespace neurozen.API.Safety.Interfaces.REST.Transform;

public static class EmergencyResourceAssembler
{
    public static EmergencyResourceResource ToResource(EmergencyResource resource, DateOnly? verifiedAt) => new(
        resource.Name,
        resource.Phone,
        resource.Description,
        resource.Country,
        verifiedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
