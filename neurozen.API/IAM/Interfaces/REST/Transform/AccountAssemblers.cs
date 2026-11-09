using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Interfaces.REST.Resources;

namespace neurozen.API.IAM.Interfaces.REST.Transform;

public static class UpdateUserProfileCommandFromResourceAssembler
{
    public static UpdateUserProfileCommand ToCommandFromResource(long userId, UpdateUserProfileResource resource) =>
        new(userId,
            string.IsNullOrWhiteSpace(resource.Email) ? resource.Username : resource.Email,
            resource.FullName,
            resource.PhoneNumber,
            resource.Address,
            resource.AvatarUrl,
            resource.DateOfBirth,
            resource.AgeRange,
            resource.Occupation,
            resource.WorkType,
            resource.CurrentPassword);
}

public static class ChangePasswordCommandFromResourceAssembler
{
    public static ChangePasswordCommand ToCommandFromResource(long userId, ChangePasswordResource resource) =>
        new(userId, resource.CurrentPassword, resource.NewPassword);
}
