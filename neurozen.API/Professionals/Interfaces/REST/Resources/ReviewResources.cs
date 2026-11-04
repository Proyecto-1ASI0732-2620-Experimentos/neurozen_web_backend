namespace neurozen.API.Professionals.Interfaces.REST.Resources;

/// <summary>Public review: abbreviated patient name only ("Ana T."), never the email.</summary>
public record ReviewResource(long Id, string UserName, int Rating, string Comment, DateTime CreatedAt);

public record CreateReviewResource(long? AppointmentId, int? Rating, string? Comment);
