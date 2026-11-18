namespace neurozen.API.ResourcesLibrary.Domain.Model.Queries;

public record GetAllResourceLibrariesQuery;

public record GetResourceLibraryByIdQuery(long Id);

/// <summary>ADMIN: every resource, whatever its plan.</summary>
public record GetAllResourceLibrariesForAdminQuery;

/// <summary>ADMIN: one resource, whatever its plan (404 if it does not exist).</summary>
public record GetResourceLibraryForAdminQuery(long Id);
