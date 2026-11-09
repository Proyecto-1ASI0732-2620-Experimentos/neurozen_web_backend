using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Wellness.Domain.Entities;
using neurozen.API.Wellness.Domain.Model.Commands;
using neurozen.API.Wellness.Domain.Repositories;
using neurozen.API.Wellness.Domain.Services;

namespace neurozen.API.Wellness.Application.Internal.CommandServices;

public class HealthMetricCommandService(
    IHealthMetricRepository healthMetricRepository,
    IUnitOfWork unitOfWork) : IHealthMetricCommandService
{
    public async Task<HealthMetric> Handle(CreateHealthMetricCommand command)
    {
        var errors = new ValidationErrors();
        errors.AddIf(command.StressLevel is < 1 or > 10, "stressLevel",
            "El nivel de estrés debe estar entre 1 y 10");
        errors.AddIf(command.HeartRate is <= 0 or > 250, "heartRate",
            "La frecuencia cardiaca debe estar entre 1 y 250");
        errors.AddIf(command.SleepHours is < 0 or > 24, "sleepHours",
            "Las horas de sueño deben estar entre 0 y 24");
        errors.AddIf(command.Notes?.Length > HealthMetric.NotesMaxLength, "notes",
            $"Las notas no pueden superar {HealthMetric.NotesMaxLength} caracteres");
        errors.ThrowIfAny();

        var metric = new HealthMetric(command.UserId, command.StressLevel, command.HeartRate,
            command.SleepHours, command.Notes);
        await healthMetricRepository.AddAsync(metric);
        await unitOfWork.CompleteAsync();
        return metric;
    }
}
