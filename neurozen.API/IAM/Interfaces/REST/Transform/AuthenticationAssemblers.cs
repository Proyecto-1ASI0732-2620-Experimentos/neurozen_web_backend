using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Services;
using neurozen.API.IAM.Interfaces.REST.Resources;

namespace neurozen.API.IAM.Interfaces.REST.Transform;

public static class SignInCommandFromResourceAssembler
{
    public static SignInCommand ToCommandFromResource(SignInResource resource) =>
        new(resource.Username, resource.Password);
}

public static class SignUpCommandFromResourceAssembler
{
    public static SignUpCommand ToCommandFromResource(SignUpResource resource) =>
        new(resource.Email,
            resource.Password,
            resource.FullName,
            resource.Role,
            resource.AcceptedTerms == true,
            resource.Specialty,
            resource.LicenseNumber,
            resource.Dni,
            resource.ConfirmPassword,
            resource.AcceptedPrivacyPolicy);
}

public static class AuthenticatedUserResourceFromEntityAssembler
{
    public static AuthenticatedUserResource ToResourceFromEntity(AuthenticatedUser authenticated) =>
        new(authenticated.User.Id,
            authenticated.User.FullName,
            authenticated.User.Email,
            authenticated.User.Role.ToWire(),
            authenticated.Token);
}
