namespace neurozen.API.Wellness.Domain.Model.Queries;

public record GetDashboardQuery(long UserId);

public record GetHealthMetricsQuery(long UserId, int? Page = null, int? PageSize = null);

public record GetMeditationsQuery;
