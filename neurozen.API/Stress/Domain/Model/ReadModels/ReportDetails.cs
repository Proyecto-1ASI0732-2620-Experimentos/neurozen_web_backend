using neurozen.API.Stress.Domain.Model.Aggregates;

namespace neurozen.API.Stress.Domain.Model.ReadModels;

public record ReportDetails(Report Report, string PatientName);
