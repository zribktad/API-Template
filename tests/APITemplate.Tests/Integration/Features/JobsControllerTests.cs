using System.Net;
using System.Text.Json;
using Alba;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Enums;
using APITemplate.Tests.Integration;
using Shouldly;
using TestCommon;
using Xunit;

namespace APITemplate.Tests.Integration.Features;

public class JobsControllerTests : IClassFixture<AlbaApiFixture>
{
    private readonly IAlbaHost _host;

    public JobsControllerTests(AlbaApiFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task Submit_ValidRequest_Returns202WithLocationHeader()
    {
        _ = await _host.Scenario(_ =>
        {
            _.WithBearerToken(IntegrationAuthHelper.CreateTestToken());
            _.Post.Json(new { JobType = "data-export" }).ToUrl("/api/v1/jobs");
            _.StatusCodeShouldBe(HttpStatusCode.Accepted);
            _.Header("Location").ShouldHaveOneNonNullValue();
        });
    }

    [Fact]
    public async Task Submit_ValidRequest_ResponseContainsPendingStatus()
    {
        var result = await _host.Scenario(_ =>
        {
            _.WithBearerToken(IntegrationAuthHelper.CreateTestToken());
            _.Post.Json(new { JobType = "report-generation" }).ToUrl("/api/v1/jobs");
            _.StatusCodeShouldBe(HttpStatusCode.Accepted);
        });

        var body = await result.ReadAsTextAsync();
        var jobResult = JsonSerializer.Deserialize<JobStatusResponse>(
            body,
            TestJsonOptions.CaseInsensitive
        );
        jobResult.ShouldNotBeNull();
        jobResult!.Status.ShouldBe(JobStatus.Pending);
        jobResult.JobType.ShouldBe("report-generation");
    }

    [Fact]
    public async Task GetStatus_AfterSubmit_ReturnsJobWithMatchingId()
    {
        var submitResult = await _host.Scenario(_ =>
        {
            _.WithBearerToken(IntegrationAuthHelper.CreateTestToken());
            _.Post.Json(new { JobType = "async-task" }).ToUrl("/api/v1/jobs");
            _.StatusCodeShouldBe(HttpStatusCode.Accepted);
        });

        var submitBody = await submitResult.ReadAsTextAsync();
        var submitted = JsonSerializer.Deserialize<JobStatusResponse>(
            submitBody,
            TestJsonOptions.CaseInsensitive
        )!;

        var getResult = await _host.Scenario(_ =>
        {
            _.WithBearerToken(IntegrationAuthHelper.CreateTestToken());
            _.Get.Url($"/api/v1/jobs/{submitted.Id}");
            _.StatusCodeShouldBeOk();
        });

        var getBody = await getResult.ReadAsTextAsync();
        var status = JsonSerializer.Deserialize<JobStatusResponse>(
            getBody,
            TestJsonOptions.CaseInsensitive
        );
        status.ShouldNotBeNull();
        status!.Id.ShouldBe(submitted.Id);
        status.JobType.ShouldBe("async-task");
    }

    [Fact]
    public async Task GetStatus_NonExistentId_Returns404()
    {
        _ = await _host.Scenario(_ =>
        {
            _.WithBearerToken(IntegrationAuthHelper.CreateTestToken());
            _.Get.Url($"/api/v1/jobs/{Guid.NewGuid()}");
            _.StatusCodeShouldBe(HttpStatusCode.NotFound);
        });
    }

    [Fact]
    public async Task Submit_EmptyJobType_Returns400()
    {
        _ = await _host.Scenario(_ =>
        {
            _.WithBearerToken(IntegrationAuthHelper.CreateTestToken());
            _.Post.Json(new { JobType = "" }).ToUrl("/api/v1/jobs");
            _.StatusCodeShouldBe(HttpStatusCode.BadRequest);
        });
    }
}
