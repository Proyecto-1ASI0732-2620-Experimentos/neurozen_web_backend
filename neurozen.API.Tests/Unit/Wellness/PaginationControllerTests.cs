using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using neurozen.API.Appointments.Domain.Model.Queries;
using neurozen.API.Appointments.Domain.Model.ReadModels;
using neurozen.API.Appointments.Domain.Services;
using neurozen.API.Appointments.Interfaces.REST;
using neurozen.API.Shared.Domain.Model.Pagination;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Queries;
using neurozen.API.Stress.Domain.Model.ReadModels;
using neurozen.API.Stress.Domain.Services;
using neurozen.API.Stress.Interfaces.REST;
using neurozen.API.Wellness.Domain.Entities;
using neurozen.API.Wellness.Domain.Model.Queries;
using neurozen.API.Wellness.Domain.Services;
using neurozen.API.Wellness.Interfaces.REST;

namespace neurozen.API.Tests.Unit.Wellness;

public class PaginationControllerTests
{
    [Fact]
    public async Task AppointmentsList_ShouldExposeTotalCountHeader()
    {
        var service = new Mock<IAppointmentQueryService>();
        service.Setup(s => s.Handle(It.IsAny<GetAppointmentsQuery>()))
            .ReturnsAsync(new PageResult<AppointmentDetails>([], 10));
        var controller = new AppointmentsController(Mock.Of<IAppointmentCommandService>(), service.Object);
        SetHttpContext(controller);

        var response = await controller.GetAppointments(7, null, 2, 5);

        Assert.Equal("10", controller.Response.Headers["X-Total-Count"]);
        Assert.IsType<OkObjectResult>(response.Result);
        service.Verify(s => s.Handle(It.Is<GetAppointmentsQuery>(q =>
            q.PatientId == 7 && q.Page == 2 && q.PageSize == 5)), Times.Once);
    }

    [Fact]
    public async Task TriggersList_ShouldExposeTotalCountHeader()
    {
        var service = new Mock<IStressTriggerQueryService>();
        service.Setup(s => s.Handle(It.IsAny<GetTriggersQuery>()))
            .ReturnsAsync(new PageResult<StressTrigger>([], 11));
        var controller = new TriggersController(Mock.Of<IStressTriggerCommandService>(), service.Object);
        SetHttpContext(controller);

        var response = await controller.GetTriggers(7, 2, 5);

        Assert.Equal("11", controller.Response.Headers["X-Total-Count"]);
        Assert.IsType<OkObjectResult>(response.Result);
        service.Verify(s => s.Handle(It.Is<GetTriggersQuery>(q => q.Page == 2 && q.PageSize == 5)), Times.Once);
    }

    [Fact]
    public async Task AssessmentsList_ShouldExposeTotalCountHeader()
    {
        var service = new Mock<IAssessmentQueryService>();
        service.Setup(s => s.Handle(It.IsAny<GetAssessmentsQuery>()))
            .ReturnsAsync(new PageResult<Assessment>([], 12));
        var controller = new AssessmentsController(Mock.Of<IAssessmentCommandService>(), service.Object);
        SetHttpContext(controller);

        var response = await controller.GetAssessments(7, 2, 5);

        Assert.Equal("12", controller.Response.Headers["X-Total-Count"]);
        Assert.IsType<OkObjectResult>(response.Result);
        service.Verify(s => s.Handle(It.Is<GetAssessmentsQuery>(q => q.Page == 2 && q.PageSize == 5)), Times.Once);
    }

    [Fact]
    public async Task ReportsList_ShouldExposeTotalCountHeader()
    {
        var service = new Mock<IReportQueryService>();
        service.Setup(s => s.Handle(It.IsAny<GetReportsQuery>()))
            .ReturnsAsync(new PageResult<ReportDetails>([], 13));
        var controller = new ReportsController(Mock.Of<IReportCommandService>(), service.Object);
        SetHttpContext(controller);

        var response = await controller.GetReports(7, null, 2, 5);

        Assert.Equal("13", controller.Response.Headers["X-Total-Count"]);
        Assert.IsType<OkObjectResult>(response.Result);
        service.Verify(s => s.Handle(It.Is<GetReportsQuery>(q =>
            q.PatientId == 7 && q.Page == 2 && q.PageSize == 5)), Times.Once);
    }

    [Fact]
    public async Task HealthMetricsList_ShouldExposeTotalCountHeader()
    {
        var service = new Mock<IHealthMetricQueryService>();
        service.Setup(s => s.Handle(It.IsAny<GetHealthMetricsQuery>()))
            .ReturnsAsync(new PageResult<HealthMetric>([], 14));
        var controller = new HealthMetricsController(Mock.Of<IHealthMetricCommandService>(), service.Object);
        SetHttpContext(controller);

        var response = await controller.GetHealthMetrics(7, 2, 5);

        Assert.Equal("14", controller.Response.Headers["X-Total-Count"]);
        Assert.IsType<OkObjectResult>(response.Result);
        service.Verify(s => s.Handle(It.Is<GetHealthMetricsQuery>(q =>
            q.UserId == 7 && q.Page == 2 && q.PageSize == 5)), Times.Once);
    }

    private static void SetHttpContext(ControllerBase controller) =>
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
}
