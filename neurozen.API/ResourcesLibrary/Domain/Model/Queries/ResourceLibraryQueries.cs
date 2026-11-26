namespace neurozen.API.ResourcesLibrary.Domain.Model.Queries;

public record GetAllResourceLibrariesQuery;

/// <summary>
///     Relevance search (ZenBot). Q: free text; Tags: comma-separated; Type: Video | Audio | Article | Exercise;
///     Limit: 1-10 (default 5). At least Q or Tags is required.
/// </summary>
public record SearchResourceLibrariesQuery(string? Q, string? Tags, string? Type, int? Limit)
{
    public const int DefaultLimit = 5;
    public const int MaxLimit = 10;
}

public record GetResourceLibraryByIdQuery(long Id);

/// <summary>ADMIN: every resource, whatever its plan.</summary>
public record GetAllResourceLibrariesForAdminQuery;

/// <summary>ADMIN: one resource, whatever its plan (404 if it does not exist).</summary>
public record GetResourceLibraryForAdminQuery(long Id);
