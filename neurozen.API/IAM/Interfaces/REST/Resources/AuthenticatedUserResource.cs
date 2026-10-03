namespace neurozen.API.IAM.Interfaces.REST.Resources;

public record AuthenticatedUserResource(Guid Id, string Username, string Token);