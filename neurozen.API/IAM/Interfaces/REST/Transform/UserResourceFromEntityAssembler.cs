using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Interfaces.REST.Resources;

namespace neurozen.API.IAM.Interfaces.REST.Transform;

public static class UserResourceFromEntityAssembler
{
    public static UserResource ToResourceFromEntity(User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Role.ToWire(),
        user.PhoneNumber ?? string.Empty,
        user.Address ?? string.Empty,
        user.AvatarUrl ?? string.Empty,
        user.DateOfBirth,
        user.AgeRange ?? string.Empty,
        user.Occupation ?? string.Empty,
        user.WorkType ?? string.Empty,
        user.CreatedAt);
}
